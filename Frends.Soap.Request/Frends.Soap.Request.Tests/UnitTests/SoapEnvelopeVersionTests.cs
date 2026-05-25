using System.Xml;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

namespace Frends.Soap.Request.Tests.UnitTests;

[TestFixture]
public class SoapEnvelopeVersionTests
{
    private const string SimpleBody =
        @"<GetWeather xmlns=""https://example.com/service""><City>London</City></GetWeather>";

    [Test]
    public void Soap11Envelope_HasCorrectNamespace()
    {
        // Arrange
        var options = CreateMinimalOptions(SoapVersion.Soap11);

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap11, options);

        // Assert
        Assert.That(envelope, Does.Contain("http://schemas.xmlsoap.org/soap/envelope/"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");

        var envelopeNode = doc.SelectSingleNode("//soap:Envelope", nsManager);
        Assert.That(envelopeNode, Is.Not.Null);
    }

    [Test]
    public void Soap12Envelope_HasCorrectNamespace()
    {
        // Arrange
        var options = CreateMinimalOptions(SoapVersion.Soap12);

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options);

        // Assert
        Assert.That(envelope, Does.Contain("http://www.w3.org/2003/05/soap-envelope"));

        var doc = new XmlDocument();
        doc.LoadXml(envelope);
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");

        var envelopeNode = doc.SelectSingleNode("//soap:Envelope", nsManager);
        Assert.That(envelopeNode, Is.Not.Null);
    }

    [Test]
    public void BothVersions_ContainBodyElement()
    {
        // Test SOAP 1.1
        var envelope11 =
            SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap11, CreateMinimalOptions(SoapVersion.Soap11));
        var doc11 = new XmlDocument();
        doc11.LoadXml(envelope11);
        var nsManager11 = new XmlNamespaceManager(doc11.NameTable);
        nsManager11.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        var bodyNode11 = doc11.SelectSingleNode("//soap:Body", nsManager11);
        Assert.That(bodyNode11, Is.Not.Null);

        // Test SOAP 1.2
        var envelope12 =
            SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, CreateMinimalOptions(SoapVersion.Soap12));
        var doc12 = new XmlDocument();
        doc12.LoadXml(envelope12);
        var nsManager12 = new XmlNamespaceManager(doc12.NameTable);
        nsManager12.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");
        var bodyNode12 = doc12.SelectSingleNode("//soap:Body", nsManager12);
        Assert.That(bodyNode12, Is.Not.Null);
    }

    [Test]
    public void Soap11Fault_HasCorrectStructure()
    {
        // Act
        var fault = SoapMessageBuilder.BuildFaultEnvelope("soap:Client", "Test fault", SoapVersion.Soap11);

        // Assert
        var doc = new XmlDocument();
        doc.LoadXml(fault);
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");

        var faultCode = doc.SelectSingleNode("//soap:Fault/faultcode", nsManager);
        var faultString = doc.SelectSingleNode("//soap:Fault/faultstring", nsManager);

        Assert.That(faultCode, Is.Not.Null);
        Assert.That(faultString, Is.Not.Null);
        Assert.That(faultCode?.InnerText, Is.EqualTo("soap:Client"));
        Assert.That(faultString?.InnerText, Is.EqualTo("Test fault"));
    }

    [Test]
    public void Soap12Fault_HasCorrectStructure()
    {
        // Act
        var fault = SoapMessageBuilder.BuildFaultEnvelope("soap:Sender", "Test fault", SoapVersion.Soap12);

        // Assert
        var doc = new XmlDocument();
        doc.LoadXml(fault);
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "http://www.w3.org/2003/05/soap-envelope");

        var code = doc.SelectSingleNode("//soap:Fault/soap:Code/soap:Value", nsManager);
        var reason = doc.SelectSingleNode("//soap:Fault/soap:Reason/soap:Text", nsManager);

        Assert.That(code, Is.Not.Null);
        Assert.That(reason, Is.Not.Null);
        Assert.That(code?.InnerText, Is.EqualTo("soap:Sender"));
        Assert.That(reason?.InnerText, Is.EqualTo("Test fault"));
    }

    private static Options CreateMinimalOptions(SoapVersion version)
    {
        return new Options
        {
            SoapVersion = version,
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
