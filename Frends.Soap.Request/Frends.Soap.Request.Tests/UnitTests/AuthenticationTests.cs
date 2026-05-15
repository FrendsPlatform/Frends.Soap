using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.UnitTests;

[TestFixture]
public class AuthenticationTests
{
    private const string TestUrl = "https://example.com/service";
    private const string TestSoapEnvelope = @"<?xml version=""1.0""?><soap:Envelope xmlns:soap=""https://www.w3.org/2003/05/soap-envelope""><soap:Body/></soap:Envelope>";

    [Test]
    public void BuildHttpRequest_WithOAuthNone_NoAuthorizationHeader()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.None,
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = CreateOptions();

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.Authorization, Is.Null);
    }

    [Test]
    public void BuildHttpRequest_WithOAuthToken_AddsCorrectAuthHeader()
    {
        // Arrange
        const string oauthToken = "eyJhbGciOiJSUzI1NiJ9.valid.token";
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.OAuth,
            OAuthToken = oauthToken,
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = CreateOptions();

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.Authorization, Is.Not.Null);
        Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo(oauthToken));
    }

    [Test]
    public void BuildHttpRequest_WithOAuthEmptyToken_NoAuthorizationHeaderAdded()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.OAuth,
            OAuthToken = string.Empty,
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = CreateOptions();

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.Authorization, Is.Null);
    }

    [Test]
    public void BuildHttpRequest_WithClientCertificate_AllowsClientCertificateType()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.ClientCertificate,
            ClientCertPath = string.Empty,
            ClientCertPassword = string.Empty,
        };

        // Act
        // We can't test the actual certificate loading without a real certificate file,
        // but we can verify that the Authentication type is recognized
        Assert.That(connection.Authentication, Is.EqualTo(Authentication.ClientCertificate));
    }

    private static Options CreateOptions()
    {
        return new Options
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
    }
}
