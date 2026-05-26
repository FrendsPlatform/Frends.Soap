using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;

namespace Frends.Soap.Request.Helpers;

/// <summary>
/// Creates HTTP client handlers and SOAP HTTP request messages.
/// </summary>
internal static class HttpHandler
{
    private static readonly ConcurrentDictionary<string, HttpClient> HttpClientCache = new();

    internal static HttpClientHandler BuildHttpClientHandler(Connection connection)
    {
        var handler = new HttpClientHandler
        {
            CheckCertificateRevocationList = connection.CertificationRevocationCheck,
            AllowAutoRedirect = connection.FollowRedirects,
            SslProtocols = ToSslProtocols(connection.SslProtocolVersion),
        };

        if (!string.IsNullOrWhiteSpace(connection.ProxyAddress))
        {
            handler.UseProxy = true;
            var proxy = new WebProxy(connection.ProxyAddress);

            if (!string.IsNullOrWhiteSpace(connection.ProxyUsername) || !string.IsNullOrWhiteSpace(connection.ProxyPassword))
            {
                proxy.Credentials = new NetworkCredential(connection.ProxyUsername, connection.ProxyPassword);
            }

            handler.Proxy = proxy;
        }

        if (connection.Authentication == Authentication.ClientCertificate)
        {
            var cert = string.IsNullOrWhiteSpace(connection.ClientCertPassword)
                ? new X509Certificate2(connection.ClientCertPath)
                : new X509Certificate2(connection.ClientCertPath, connection.ClientCertPassword);
            handler.ClientCertificates.Add(cert);
        }
        else if (connection.Authentication == Authentication.WindowsAuthentication)
        {
            handler.PreAuthenticate = true;
            handler.UseDefaultCredentials = false;
            handler.Credentials = new NetworkCredential(
                connection.WindowsAuthenticationUsername,
                connection.WindowsAuthenticationPassword);
        }
        else if (connection.Authentication == Authentication.WindowsIntegratedSecurity)
        {
            handler.PreAuthenticate = true;
            handler.UseDefaultCredentials = true;
            handler.Credentials = CredentialCache.DefaultNetworkCredentials;
        }

        if (connection.AllowInvalidCertificate)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        else if (connection.ServerCertificateThumbprints?.Length > 0)
        {
            var pinned = connection.ServerCertificateThumbprints;
            handler.ServerCertificateCustomValidationCallback =
                (_, serverCert, _, _) =>
                {
                    return serverCert != null && Array.Exists(
                        pinned,
                        t => string.Equals(t, serverCert.Thumbprint, StringComparison.OrdinalIgnoreCase));
                };
        }

        return handler;
    }

    internal static HttpClient BuildHttpClient(Connection connection)
    {
        var handler = BuildHttpClientHandler(connection);
        var httpClient = new HttpClient(handler)
        {
            Timeout = connection.ConnectionTimeoutSeconds <= 0
                ? Timeout.InfiniteTimeSpan
                : TimeSpan.FromSeconds(connection.ConnectionTimeoutSeconds),
        };

        return httpClient;
    }

    internal static (HttpClient Client, bool ShouldDispose) GetHttpClient(Connection connection)
    {
        if (!connection.CacheHttpClients)
        {
            return (BuildHttpClient(connection), true);
        }

        var cacheKey = CreateClientCacheKey(connection);
        var cachedClient = HttpClientCache.GetOrAdd(cacheKey, _ => BuildHttpClient(connection));
        return (cachedClient, false);
    }

    internal static HttpRequestMessage BuildHttpRequest(
        Connection connection,
        Input input,
        Options options,
        string soapEnvelope)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, connection.Url);
        request.Version = ToVersion(connection.HttpProtocolVersion);
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

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

        foreach (var customHeader in connection.CustomHeaders ?? [])
        {
            if (!request.Headers.TryAddWithoutValidation(customHeader.Name, customHeader.Value))
            {
                request.Content?.Headers.TryAddWithoutValidation(customHeader.Name, customHeader.Value);
            }
        }

        if (connection.Authentication == Authentication.Basic)
        {
            var rawCredentials = $"{connection.BasicUsername}:{connection.BasicPassword}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }
        else if (connection.Authentication == Authentication.OAuth &&
                 !string.IsNullOrWhiteSpace(connection.OAuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.OAuthToken);
        }

        return request;
    }

    private static string CreateClientCacheKey(Connection connection)
    {
        var thumbprints = connection.ServerCertificateThumbprints == null
            ? string.Empty
            : string.Join(",", connection.ServerCertificateThumbprints);

        return string.Join(
            "|",
            connection.Authentication,
            connection.ClientCertPath ?? string.Empty,
            connection.ClientCertPassword ?? string.Empty,
            connection.WindowsAuthenticationUsername ?? string.Empty,
            connection.WindowsAuthenticationPassword ?? string.Empty,
            connection.ProxyAddress ?? string.Empty,
            connection.ProxyUsername ?? string.Empty,
            connection.ProxyPassword ?? string.Empty,
            connection.AllowInvalidCertificate,
            connection.CertificationRevocationCheck,
            connection.FollowRedirects,
            connection.ConnectionTimeoutSeconds,
            connection.HttpProtocolVersion,
            connection.SslProtocolVersion,
            thumbprints);
    }

    private static Version ToVersion(HttpProtocolVersion version)
    {
        return version switch
        {
            HttpProtocolVersion.Http10 => new Version(1, 0),
            HttpProtocolVersion.Http11 => new Version(1, 1),
            HttpProtocolVersion.Http20 => new Version(2, 0),
            HttpProtocolVersion.Http30 => new Version(3, 0),
            _ => new Version(1, 1),
        };
    }

    private static SslProtocols ToSslProtocols(SslProtocolVersion version)
    {
        return version switch
        {
            SslProtocolVersion.SystemDefault => SslProtocols.None,
            SslProtocolVersion.Tls12 => SslProtocols.Tls12,
            SslProtocolVersion.Tls13 => SslProtocols.Tls13,
            _ => SslProtocols.None,
        };
    }
}
