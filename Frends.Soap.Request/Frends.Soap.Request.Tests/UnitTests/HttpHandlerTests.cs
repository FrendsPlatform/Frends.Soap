using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.UnitTests;

[TestFixture]
public class HttpHandlerTests
{
    private const string TestUrl = "https://example.com/service";
    private const string TestSoapEnvelope = @"<?xml version=""1.0""?><soap:Envelope xmlns:soap=""https://www.w3.org/2003/05/soap-envelope""><soap:Body/></soap:Envelope>";

    [Test]
    public void BuildHttpRequest_WithSoap11_SetsSoapActionHeader()
    {
        // Arrange
        var connection = new Connection { Url = TestUrl, Authentication = Authentication.None };
        var input = new Input { MessageBody = "<test/>", SoapAction = "https://example.com/GetWeather" };
        var options = new Options
        {
            SoapVersion = SoapVersion.Soap11,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(request.RequestUri?.ToString(), Is.EqualTo(TestUrl));
        Assert.That(request.Headers.Contains("SOAPAction"), Is.True);
        Assert.That(request.Headers.GetValues("SOAPAction").First(), Is.EqualTo("\"https://example.com/GetWeather\""));
        Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo("text/xml"));
    }

    [Test]
    public void BuildHttpRequest_WithSoap12_SetsActionInContentType()
    {
        // Arrange
        var connection = new Connection { Url = TestUrl, Authentication = Authentication.None };
        var input = new Input { MessageBody = "<test/>", SoapAction = "https://example.com/GetWeather" };
        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Content?.Headers.ContentType?.MediaType, Is.EqualTo("application/soap+xml"));
        var actionParam = request.Content?.Headers.ContentType?.Parameters
            .FirstOrDefault(p => p.Name == "action");
        Assert.That(actionParam, Is.Not.Null);
        Assert.That(actionParam.Value, Is.EqualTo("\"https://example.com/GetWeather\""));
    }

    [Test]
    public void BuildHttpRequest_WithOAuth_AddsAuthorizationHeader()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.OAuth,
            OAuthToken = "test-token-12345",
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.Authorization, Is.Not.Null);
        Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("test-token-12345"));
    }

    [Test]
    public void BuildHttpRequest_WithoutSoapAction_OmitsSoapActionForSoap11()
    {
        // Arrange
        var connection = new Connection { Url = TestUrl, Authentication = Authentication.None };
        var input = new Input { MessageBody = "<test/>", SoapAction = string.Empty };
        var options = new Options
        {
            SoapVersion = SoapVersion.Soap11,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.Contains("SOAPAction"), Is.True);
        Assert.That(request.Headers.GetValues("SOAPAction").First(), Is.EqualTo("\"\""));
    }

    [Test]
    public void BuildHttpRequest_WithCustomHeaders_AddsHeaders()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.None,
            CustomHeaders =
            [
                new Header { Name = "X-Custom-Header-One", Value = "ValueOne" },
                new Header { Name = "X-Custom-Header-Two", Value = "ValueTwo" },
            ],
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.TryGetValues("X-Custom-Header-One", out var firstValues), Is.True);
        Assert.That(firstValues.First(), Is.EqualTo("ValueOne"));
        Assert.That(request.Headers.TryGetValues("X-Custom-Header-Two", out var secondValues), Is.True);
        Assert.That(secondValues.First(), Is.EqualTo("ValueTwo"));
    }

    [Test]
    public void BuildHttpRequest_WithHttpProtocolVersion_SetsRequestVersion()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.None,
            HttpProtocolVersion = HttpProtocolVersion.Http20,
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = new Options
        {
            SoapVersion = SoapVersion.Soap12,
            IncludeWsSecurity = false,
            IncludeWsAddressing = false,
            IncludeWsReliableMessaging = false,
            IncludeWsPolicy = false,
            IncludeWsTrust = false,
            IncludeWsFederation = false,
            WsSecurityUsername = "user",
            WsSecurityPassword = "pass",
            WsSecurityPasswordType = "PasswordText",
        };

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Version, Is.EqualTo(new Version(2, 0)));
    }

    [Test]
    public void BuildHttpClientHandler_WithPinnedThumbprint_RejectsDifferentCertificateEvenWithoutSslErrors()
    {
        // Arrange
        var pinnedCert = CreateSelfSignedCertificate();
        var otherCert = CreateSelfSignedCertificate();
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.None,
            ServerCertificateThumbprints = [pinnedCert.Thumbprint],
        };

        // Act
        using var handler = HttpHandler.BuildHttpClientHandler(connection);
        var isAllowed = handler.ServerCertificateCustomValidationCallback?.Invoke(
            null!,
            otherCert,
            null,
            SslPolicyErrors.None);

        // Assert
        Assert.That(isAllowed, Is.False);
    }

    [Test]
    public void BuildHttpClientHandler_WithPinnedThumbprint_AllowsMatchingCertificate()
    {
        // Arrange
        var pinnedCert = CreateSelfSignedCertificate();
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.None,
            ServerCertificateThumbprints = [pinnedCert.Thumbprint],
        };

        // Act
        using var handler = HttpHandler.BuildHttpClientHandler(connection);
        var isAllowed = handler.ServerCertificateCustomValidationCallback?.Invoke(
            null!,
            pinnedCert,
            null,
            SslPolicyErrors.None);

        // Assert
        Assert.That(isAllowed, Is.True);
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
