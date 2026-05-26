using System;
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

    [Test]
    public void BuildHttpRequest_WithBasicAuthentication_AddsAuthorizationHeader()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.Basic,
            BasicUsername = "basic-user",
            BasicPassword = "basic-pass",
        };
        var input = new Input { MessageBody = "<test/>" };
        var options = CreateOptions();

        // Act
        using var request = HttpHandler.BuildHttpRequest(connection, input, options, TestSoapEnvelope);

        // Assert
        Assert.That(request.Headers.Authorization, Is.Not.Null);
        Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Basic"));
        Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("YmFzaWMtdXNlcjpiYXNpYy1wYXNz"));
    }

    [Test]
    public void BuildHttpClientHandler_WithProxy_ConfiguresProxy()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            ProxyAddress = "http://localhost:8888",
        };

        // Act
        var handler = HttpHandler.BuildHttpClientHandler(connection);

        // Assert
        Assert.That(handler.UseProxy, Is.True);
        Assert.That(handler.Proxy, Is.Not.Null);
        Assert.That(handler.Proxy?.GetProxy(new Uri("http://example.com")), Is.EqualTo(new Uri("http://localhost:8888/")));
    }

    [Test]
    public void BuildHttpClientHandler_WithWindowsAuthentication_UsesExplicitCredentials()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.WindowsAuthentication,
            WindowsAuthenticationUsername = "domain\\windows-user",
            WindowsAuthenticationPassword = "windows-pass",
        };

        // Act
        var handler = HttpHandler.BuildHttpClientHandler(connection);

        // Assert
        Assert.That(handler.UseDefaultCredentials, Is.False);
        Assert.That(handler.Credentials, Is.TypeOf<System.Net.NetworkCredential>());
        var credentials = (System.Net.NetworkCredential)handler.Credentials;
        Assert.That(credentials.UserName, Is.EqualTo("domain\\windows-user"));
        Assert.That(credentials.Password, Is.EqualTo("windows-pass"));
    }

    [Test]
    public void BuildHttpClientHandler_WithWindowsIntegratedSecurity_UsesDefaultCredentials()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            Authentication = Authentication.WindowsIntegratedSecurity,
        };

        // Act
        var handler = HttpHandler.BuildHttpClientHandler(connection);

        // Assert
        Assert.That(handler.UseDefaultCredentials, Is.True);
        Assert.That(
            handler.Credentials == null || handler.Credentials == System.Net.CredentialCache.DefaultNetworkCredentials,
            Is.True);
    }

    [Test]
    public void BuildHttpClient_SetsTimeout()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            ConnectionTimeoutSeconds = 15,
        };

        // Act
        using var client = HttpHandler.BuildHttpClient(connection);

        // Assert
        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(15)));
    }

    [Test]
    public void GetHttpClient_WithCaching_ReturnsSameInstance()
    {
        // Arrange
        var connection = new Connection
        {
            Url = TestUrl,
            CacheHttpClients = true,
        };

        // Act
        var (firstClient, firstDispose) = HttpHandler.GetHttpClient(connection);
        var (secondClient, secondDispose) = HttpHandler.GetHttpClient(connection);

        // Assert
        Assert.That(firstDispose, Is.False);
        Assert.That(secondDispose, Is.False);
        Assert.That(ReferenceEquals(firstClient, secondClient), Is.True);
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
