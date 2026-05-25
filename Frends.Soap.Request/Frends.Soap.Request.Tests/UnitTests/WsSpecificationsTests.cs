using System.Xml;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.UnitTests;

[TestFixture]
public class WsSpecificationsTests
{
    private const string SimpleBody = @"<GetWeather xmlns=""https://example.com/service""><City>London</City></GetWeather>";
    private const string TestUrl = "https://example.com/service";

    [Test]
    public void BuildEnvelope_WithWsAddressing_IncludesAddressingHeader()
    {
        // Arrange
        var options = CreateOptionsWithWsAddressing();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options, string.Empty, TestUrl, "GetWeather");

        // Assert
        Assert.That(envelope, Does.Contain("https://www.w3.org/2005/08/addressing"));
        Assert.That(envelope, Does.Contain("Action"));
        Assert.That(envelope, Does.Contain("ReplyTo"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        Assert.That(doc.DocumentElement?.HasChildNodes, Is.True);
    }

    [Test]
    public void BuildEnvelope_WithWsSecurity_IncludesSecurityHeader()
    {
        // Arrange
        var options = CreateOptionsWithWsSecurity();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options);

        // Assert
        Assert.That(envelope, Does.Contain("https://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"));
        Assert.That(envelope, Does.Contain("UsernameToken"));
        Assert.That(envelope, Does.Contain("test-user"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        Assert.That(doc.DocumentElement?.HasChildNodes, Is.True);
    }

    [Test]
    public void BuildEnvelope_WithWsReliableMessaging_IncludesSequenceHeader()
    {
        // Arrange
        var options = CreateOptionsWithWsReliableMessaging();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options);

        // Assert
        Assert.That(envelope, Does.Contain("https://docs.oasis-open.org/ws-rx/wsrm/200702"));
        Assert.That(envelope, Does.Contain("Sequence"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        Assert.That(doc.DocumentElement?.HasChildNodes, Is.True);
    }

    [Test]
    public void BuildEnvelope_WithWsPolicy_IncludesPolicyHeader()
    {
        // Arrange
        var options = CreateOptionsWithWsPolicy();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options, string.Empty, TestUrl);

        // Assert
        Assert.That(envelope, Does.Contain("https://schemas.xmlsoap.org/ws/2004/09/policy"));
        Assert.That(envelope, Does.Contain("PolicyReference"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        Assert.That(doc.DocumentElement?.HasChildNodes, Is.True);
    }

    [Test]
    public void BuildEnvelope_WithWsTrust_IncludesTrustHeader()
    {
        // Arrange
        var options = CreateOptionsWithWsTrust();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options, string.Empty, TestUrl);

        // Assert
        Assert.That(envelope, Does.Contain("https://docs.oasis-open.org/ws-sx/ws-trust/200512"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        Assert.That(doc.DocumentElement?.HasChildNodes, Is.True);
    }

    [Test]
    public void BuildEnvelope_WithWsFederation_IncludesFederationHeader()
    {
        // Arrange
        var options = CreateOptionsWithWsFederation();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options, string.Empty, TestUrl);

        // Assert
        Assert.That(envelope, Does.Contain("https://docs.oasis-open.org/wsfed/federation/200706"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        Assert.That(doc.DocumentElement?.HasChildNodes, Is.True);
    }

    [Test]
    public void BuildEnvelope_WithNoHeaders_HasOnlyBodyElement()
    {
        // Arrange
        var options = CreateMinimalOptions();

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options);

        // Assert
        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");

        var headerNodes = doc.SelectNodes("//soap:Header", nsManager);
        Assert.That(headerNodes.Count, Is.EqualTo(0));

        var bodyNodes = doc.SelectNodes("//soap:Body", nsManager);
        Assert.That(bodyNodes.Count, Is.EqualTo(1));
    }

    private static Options CreateMinimalOptions()
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
            WsSecurityUsername = "test-user",
            WsSecurityPassword = "test-password",
            WsSecurityPasswordType = "PasswordText",
        };
    }

    private static Options CreateOptionsWithWsAddressing()
    {
        var options = CreateMinimalOptions();
        options.IncludeWsAddressing = true;
        options.WsAddressingReplyTo = "https://www.w3.org/2005/08/addressing/anonymous";
        return options;
    }

    private static Options CreateOptionsWithWsSecurity()
    {
        var options = CreateMinimalOptions();
        options.IncludeWsSecurity = true;
        options.WsSecurityUsername = "test-user";
        options.WsSecurityPassword = "test-password";
        options.WsSecurityPasswordType = "PasswordText";
        options.WsSecurityTimestampMinutes = 5;
        return options;
    }

    private static Options CreateOptionsWithWsReliableMessaging()
    {
        var options = CreateMinimalOptions();
        options.IncludeWsReliableMessaging = true;
        options.WsReliableMessagingMessageNumber = 1;
        return options;
    }

    private static Options CreateOptionsWithWsPolicy()
    {
        var options = CreateMinimalOptions();
        options.IncludeWsPolicy = true;
        return options;
    }

    private static Options CreateOptionsWithWsTrust()
    {
        var options = CreateMinimalOptions();
        options.IncludeWsTrust = true;
        options.WsTrustRequestType = "https://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue";
        options.WsTrustTokenType = "urn:oasis:names:tc:SAML:2.0:assertion";
        return options;
    }

    private static Options CreateOptionsWithWsFederation()
    {
        var options = CreateMinimalOptions();
        options.IncludeWsFederation = true;
        return options;
    }
}
