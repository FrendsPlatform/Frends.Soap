using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;

namespace Frends.Soap.Request.Helpers;

/// <summary>
/// Creates HTTP client handlers and SOAP HTTP request messages.
/// </summary>
internal static class HttpHandler
{
    internal static HttpClientHandler BuildHttpClientHandler(Connection connection)
    {
        var handler = new HttpClientHandler
        {
            CheckCertificateRevocationList = connection.CertificationRevocationCheck,
        };

        var cert = string.IsNullOrWhiteSpace(connection.ClientCertPassword)
            ? new X509Certificate2(connection.ClientCertPath)
            : new X509Certificate2(connection.ClientCertPath, connection.ClientCertPassword);
        handler.ClientCertificates.Add(cert);

        if (connection.AllowInvalidCertificate)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        else if (connection.ServerCertificateThumbprints?.Length > 0)
        {
            var pinned = connection.ServerCertificateThumbprints;
            handler.ServerCertificateCustomValidationCallback =
                (_, serverCert, _, errors) =>
                {
                    if (errors == SslPolicyErrors.None) return true;

                    return serverCert != null && Array.Exists(
                        pinned,
                        t => string.Equals(t, serverCert.Thumbprint, StringComparison.OrdinalIgnoreCase));
                };
        }

        return handler;
    }

    internal static HttpRequestMessage BuildHttpRequest(
        Connection connection,
        Input input,
        Options options,
        string soapEnvelope)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, connection.Url);

        if (options.SoapVersion == SoapVersion.Soap11)
        {
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            var soapAction = input.SoapAction ?? string.Empty;
            request.Headers.Add("SOAPAction", $"\"{soapAction}\"");
        }
        else
        {
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "application/soap+xml");

            if (!string.IsNullOrWhiteSpace(input.SoapAction))
            {
                request.Content.Headers.ContentType!.Parameters.Add(
                    new NameValueHeaderValue("action", $"\"{input.SoapAction}\""));
            }
        }

        if (connection.Authentication == Authentication.OAuth &&
            !string.IsNullOrWhiteSpace(connection.OAuthToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", connection.OAuthToken);
        }

        return request;
    }
}
