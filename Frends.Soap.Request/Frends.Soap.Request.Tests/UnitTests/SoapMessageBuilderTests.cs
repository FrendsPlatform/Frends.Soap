namespace Frends.Soap.Request.Tests.UnitTests;

using System;
using System.IO;
using System.Xml;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;
using Frends.Soap.Request.Helpers;
using NUnit.Framework;

[TestFixture]
public class SoapMessageBuilderTests
{
    private const string SimpleBody = @"<GetWeather xmlns=""https://example.com/service""><City>London</City></GetWeather>";
    private const string TargetNamespace = "https://example.com/weatherservice";

    [Test]
    public void BuildEnvelope_WithSoap11_CreatesValidEnvelope()
    {
        // Arrange
        var options = DefaultOptions();
        options.SoapVersion = SoapVersion.Soap11;
        options.IncludeWsSecurity = false;
        options.IncludeWsAddressing = false;
        options.IncludeWsReliableMessaging = false;
        options.IncludeWsPolicy = false;
        options.IncludeWsTrust = false;
        options.IncludeWsFederation = false;

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap11, options);

        // Assert
        Assert.That(envelope, Does.Contain("https://schemas.xmlsoap.org/soap/envelope/"));
        Assert.That(envelope, Does.Contain("<soap:Envelope"));
        Assert.That(envelope, Does.Contain("<soap:Body"));
        Assert.That(envelope, Does.Contain("GetWeather"));

        // Verify it's valid XML
        var doc = new XmlDocument();
        Assert.DoesNotThrow((Action)(() => doc.LoadXml(envelope)));
    }

    [Test]
    public void BuildEnvelope_WithSoap12_CreatesValidEnvelope()
    {
        // Arrange
        var options = DefaultOptions();
        options.SoapVersion = SoapVersion.Soap12;
        options.IncludeWsSecurity = false;
        options.IncludeWsAddressing = false;
        options.IncludeWsReliableMessaging = false;
        options.IncludeWsPolicy = false;
        options.IncludeWsTrust = false;
        options.IncludeWsFederation = false;

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options);

        // Assert
        Assert.That(envelope, Does.Contain("https://www.w3.org/2003/05/soap-envelope"));
        Assert.That(envelope, Does.Contain("<soap:Envelope"));
        Assert.That(envelope, Does.Contain("<soap:Body"));
        Assert.That(envelope, Does.Contain("GetWeather"));

        // Verify it's valid XML
        var doc = new XmlDocument();
        Assert.DoesNotThrow((Action)(() => doc.LoadXml(envelope)));
    }

    [Test]
    public void BuildEnvelope_WithTargetNamespace_IncludesNamespaceDeclaration()
    {
        // Arrange
        var options = DefaultOptions();
        options.IncludeWsSecurity = false;
        options.IncludeWsAddressing = false;
        options.IncludeWsReliableMessaging = false;
        options.IncludeWsPolicy = false;
        options.IncludeWsTrust = false;
        options.IncludeWsFederation = false;

        // Act
        var envelope = SoapMessageBuilder.BuildEnvelope(SimpleBody, SoapVersion.Soap12, options, TargetNamespace);

        // Assert
        Assert.That(envelope, Does.Contain($"xmlns:tns=\"{TargetNamespace}\""));

        // Verify it's valid XML
        var doc = new XmlDocument();
        Assert.DoesNotThrow((Action)(() => doc.LoadXml(envelope)));
    }

    [Test]
    public void BuildFaultEnvelope_WithSoap11_CreatesFaultWithCorrectStructure()
    {
        // Act
        var fault = SoapMessageBuilder.BuildFaultEnvelope("soap:Server", "Test error message", SoapVersion.Soap11);

        // Assert
        var doc = new XmlDocument();
        doc.LoadXml(fault);

        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "https://schemas.xmlsoap.org/soap/envelope/");

        var faultNode = doc.SelectSingleNode("//soap:Fault", nsManager);
        Assert.That(faultNode, Is.Not.Null);

        var faultCodeNode = doc.SelectSingleNode("//soap:Fault/faultcode", nsManager);
        Assert.That(faultCodeNode?.InnerText, Is.EqualTo("soap:Server"));

        var faultStringNode = doc.SelectSingleNode("//soap:Fault/faultstring", nsManager);
        Assert.That(faultStringNode?.InnerText, Is.EqualTo("Test error message"));
    }

    [Test]
    public void BuildFaultEnvelope_WithSoap12_CreatesFaultWithCorrectStructure()
    {
        // Act
        var fault = SoapMessageBuilder.BuildFaultEnvelope("soap:Receiver", "Test error message", SoapVersion.Soap12);

        // Assert
        var doc = new XmlDocument();
        doc.LoadXml(fault);

        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("soap", "https://www.w3.org/2003/05/soap-envelope");

        var faultNode = doc.SelectSingleNode("//soap:Fault", nsManager);
        Assert.That(faultNode, Is.Not.Null);

        var codeNode = doc.SelectSingleNode("//soap:Fault/soap:Code/soap:Value", nsManager);
        Assert.That(codeNode?.InnerText, Is.EqualTo("soap:Receiver"));

        var reasonNode = doc.SelectSingleNode("//soap:Fault/soap:Reason/soap:Text", nsManager);
        Assert.That(reasonNode?.InnerText, Is.EqualTo("Test error message"));
    }

    [Test]
    public void IsSoapFault_WithValidSoap11Fault_ReturnsTrue()
    {
        // Act
        var result = SoapMessageBuilder.IsSoapFault(GetTestFile("soap_fault11.xml"));

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsSoapFault_WithValidSoap12Fault_ReturnsTrue()
    {
        // Act
        var result = SoapMessageBuilder.IsSoapFault(GetTestFile("soap_fault12.xml"));

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsSoapFault_WithValidResponse_ReturnsFalse()
    {
        // Act
        var result = SoapMessageBuilder.IsSoapFault(GetTestFile("soap_response.xml"));

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSoapFault_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = SoapMessageBuilder.IsSoapFault(string.Empty);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSoapFault_WithInvalidXml_ReturnsFalse()
    {
        // Act
        var result = SoapMessageBuilder.IsSoapFault("Not valid XML");

        // Assert
        Assert.That(result, Is.False);
    }

    private static string GetTestFile(string filename)
    {
        var testFilesPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles");
        var filePath = Path.Combine(testFilesPath, filename);
        return File.ReadAllText(filePath);
    }

    private static Options DefaultOptions() => new Options
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
