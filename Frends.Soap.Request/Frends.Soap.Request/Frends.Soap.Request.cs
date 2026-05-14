using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;

namespace Frends.Soap.Request;

/// <summary>
/// Task Class for Soap operations.
/// </summary>
public static class Soap
{
    /// <summary>
    /// Sends a SOAP 1.1 or 1.2 request with the given message body.
    /// Supports OAuth2 Bearer token, mTLS, certificate pinning, certificate revocation
    /// checking, and optional WSDL-based body validation.
    /// W3C Trace Context headers (traceparent / tracestate) are propagated automatically
    /// by the .NET HttpClient when a distributed tracing Activity is active.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Soap-Request)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string XmlResponse, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> Request(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection, options);
            cancellationToken.ThrowIfCancellationRequested();

            using var handler = BuildHttpClientHandler(connection);

            // HttpClient in .NET 8 propagates W3C trace context headers (traceparent /
            // tracestate) automatically via DiagnosticsHandler when Activity.Current != null.
            using var httpClient = new HttpClient(handler);

            // ── WSDL loading & validation ──────────────────────────────────────────────
            string targetNamespace = null;

            if (options.WsdlSource != WsdlSource.None)
            {
                var wsdlContent = await WsdlHandler.LoadWsdlContentAsync(options, httpClient, cancellationToken);
                targetNamespace = WsdlHandler.GetTargetNamespace(wsdlContent);

                var (isValid, validationError) = WsdlHandler.ValidateBodyAgainstWsdl(input.MessageBody, wsdlContent);
                if (!isValid)
                    throw new InvalidOperationException($"SOAP body validation against WSDL failed: {validationError}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // ── Build & send the SOAP message ──────────────────────────────────────────
            var soapEnvelope = SoapMessageBuilder.BuildEnvelope(input.MessageBody, options.SoapVersion, targetNamespace);
            using var httpRequest = BuildHttpRequest(connection, input, options, soapEnvelope);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // ── Response handling ──────────────────────────────────────────────────────
            var isSoapFault = SoapMessageBuilder.IsSoapFault(responseBody);

            if (response.IsSuccessStatusCode && !isSoapFault)
                return new Result { Success = true, XmlResponse = responseBody };

            // Either an HTTP error or a SOAP Fault was returned
            var errorMessage = isSoapFault
                ? "SOAP Fault received from the endpoint"
                : $"HTTP error {(int)response.StatusCode}: {response.ReasonPhrase}";

            if (options.ThrowErrorOnFailure)
            {
                var messageToThrow = string.IsNullOrEmpty(options.ErrorMessageOnFailure)
                    ? $"{errorMessage}\n{responseBody}"
                    : options.ErrorMessageOnFailure;
                throw new HttpRequestException(messageToThrow);
            }

            // Return the SOAP Fault XML as-is when already a fault, otherwise wrap in one
            var faultXml = isSoapFault
                ? responseBody
                : SoapMessageBuilder.BuildFaultEnvelope(
                    options.SoapVersion == SoapVersion.Soap11 ? "soap:Server" : "soap:Receiver",
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    options.SoapVersion);

            return new Result
            {
                Success = false,
                XmlResponse = faultXml,
                Error = new Error { Message = errorMessage },
            };
        }
        catch (OperationCanceledException)
        {
            throw; // Never suppress cancellation
        }
        catch (Exception ex)
        {
            if (options.ThrowErrorOnFailure)
            {
                var msg = string.IsNullOrEmpty(options.ErrorMessageOnFailure)
                    ? ex.Message
                    : options.ErrorMessageOnFailure;
                throw new Exception(msg, ex);
            }

            var faultXml = SoapMessageBuilder.BuildFaultEnvelope(
                options.SoapVersion == SoapVersion.Soap11 ? "soap:Client" : "soap:Sender",
                ex.Message,
                options.SoapVersion);

            return new Result
            {
                Success = false,
                XmlResponse = faultXml,
                Error = new Error
                {
                    Message = string.IsNullOrEmpty(options.ErrorMessageOnFailure)
                        ? ex.Message
                        : $"{options.ErrorMessageOnFailure}: {ex.Message}",
                    AdditionalInfo = ex,
                },
            };
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────
    private static HttpClientHandler BuildHttpClientHandler(Connection connection)
    {
        var handler = new HttpClientHandler
        {
            CheckCertificateRevocationList = connection.CertificationRevocationCheck,
        };

        // ── Client certificate (mTLS only) ──────────────────────────────────────────
        var cert = string.IsNullOrWhiteSpace(connection.ClientCertPassword)
            ? new X509Certificate2(connection.ClientCertPath)
            : new X509Certificate2(connection.ClientCertPath, connection.ClientCertPassword);
        handler.ClientCertificates.Add(cert);

        // ── Server certificate validation ─────────────────────────────────────────
        if (connection.AllowInvalidCertificate)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        else if (connection.ServerCertificateThumbprints?.Length > 0)
        {
            var pinned = connection.ServerCertificateThumbprints;
            handler.ServerCertificateCustomValidationCallback =
                (_, cert, _, errors) =>
                {
                    if (errors == SslPolicyErrors.None) return true;
                    return cert != null && Array.Exists(
                        pinned,
                        t => string.Equals(t, cert.Thumbprint, StringComparison.OrdinalIgnoreCase));
                };
        }

        return handler;
    }

    private static HttpRequestMessage BuildHttpRequest(
        Connection connection,
        Input input,
        Options options,
        string soapEnvelope)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, connection.Url);

        if (options.SoapVersion == SoapVersion.Soap11)
        {
            // SOAP 1.1: Content-Type text/xml + SOAPAction header (WS-Specs)
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            var soapAction = input.SoapAction ?? string.Empty;
            request.Headers.Add("SOAPAction", $"\"{soapAction}\"");
        }
        else
        {
            // SOAP 1.2: Content-Type application/soap+xml with optional action parameter (WS-Specs)
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "application/soap+xml");
            if (!string.IsNullOrWhiteSpace(input.SoapAction))
            {
                request.Content.Headers.ContentType!.Parameters.Add(
                    new NameValueHeaderValue("action", $"\"{input.SoapAction}\""));
            }
        }

        // ── OAuth2 Bearer token ────────────────────────────────────────────────────
        if (connection.Authentication == Authentication.OAuth &&
            !string.IsNullOrWhiteSpace(connection.OAuthToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", connection.OAuthToken);
        }

        return request;
    }
}
