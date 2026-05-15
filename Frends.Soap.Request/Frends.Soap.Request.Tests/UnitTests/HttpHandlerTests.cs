namespace Frends.Soap.Request.Tests.UnitTests;

using System.Linq;
using System.Net.Http;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

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
        Assert.That(actionParam?.Value, Is.EqualTo("\"https://example.com/GetWeather\""));
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
}
