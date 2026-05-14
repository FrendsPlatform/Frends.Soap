using System;
using System.ComponentModel;
using System.Net.Http;
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

            using var handler = HttpHandler.BuildHttpClientHandler(connection);
            using var httpClient = new HttpClient(handler);

            string targetNamespace = null;

            if (options.WsdlSource != WsdlSource.None)
            {
                var wsdlContent = await WsdlHandler.LoadWsdlContentAsync(options, httpClient, cancellationToken);
                targetNamespace = WsdlHandler.GetTargetNamespace(wsdlContent);

                var validationResult = WsdlHandler.ValidateBodyAgainstWsdl(input.MessageBody, wsdlContent);
                if (!validationResult.IsValid)
                    throw new InvalidOperationException($"SOAP body validation against WSDL failed: {validationResult.Error}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var soapEnvelope = SoapMessageBuilder.BuildEnvelope(
                input.MessageBody,
                options.SoapVersion,
                options,
                targetNamespace,
                connection.Url,
                input.SoapAction);
            using var httpRequest = HttpHandler.BuildHttpRequest(connection, input, options, soapEnvelope);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

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
        catch (Exception ex)
        {
            if (options.ThrowErrorOnFailure)
            {
                ex.Handle(options);
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
}
