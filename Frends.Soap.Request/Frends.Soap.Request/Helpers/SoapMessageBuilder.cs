namespace Frends.Soap.Request.Helpers;

using System.IO;
using System.Text;
using System.Xml;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;

/// <summary>
/// Builds SOAP 1.1 and 1.2 envelopes and fault messages.
/// </summary>
internal static class SoapMessageBuilder
{
    private const string Soap11Namespace = "https://schemas.xmlsoap.org/soap/envelope/";
    private const string Soap12Namespace = "https://www.w3.org/2003/05/soap-envelope";

    /// <summary>
    /// Wraps the given body XML inside a SOAP envelope according to the specified version.
    /// If a targetNamespace is provided (typically extracted from the WSDL), it is declared
    /// as xmlns:tns on the Envelope element.
    /// </summary>
    /// <param name="body">The raw XML body to embed inside the SOAP Body element.</param>
    /// <param name="version">SOAP version that determines the envelope namespace.</param>
    /// <param name="options">Additional SOAP request options, including the WS-* header toggles.</param>
    /// <param name="targetNamespace">Optional target namespace extracted from the WSDL.</param>
    /// <param name="endpointUrl">The SOAP endpoint URL used as a sensible default for address-based headers.</param>
    /// <param name="soapAction">The logical SOAP action used for the WS-Addressing Action header.</param>
    /// <returns>A UTF-8 encoded SOAP envelope XML string.</returns>
    internal static string BuildEnvelope(
        string body,
        SoapVersion version,
        Options options,
        string targetNamespace = "",
        string endpointUrl = "",
        string soapAction = "")
    {
        var soapNs = version == SoapVersion.Soap11 ? Soap11Namespace : Soap12Namespace;

        var doc = new XmlDocument();

        var envelope = doc.CreateElement("soap", "Envelope", soapNs);
        if (!string.IsNullOrWhiteSpace(targetNamespace))
            envelope.SetAttribute("xmlns:tns", targetNamespace);
        doc.AppendChild(envelope);

        if (WsSpecificationsHandler.HasHeaders(options))
        {
            var headerElem = doc.CreateElement("soap", "Header", soapNs);
            envelope.AppendChild(headerElem);

            WsSpecificationsHandler.AppendHeaders(doc, headerElem, soapNs, options, endpointUrl, soapAction);
        }

        var bodyElem = doc.CreateElement("soap", "Body", soapNs);
        envelope.AppendChild(bodyElem);

        var bodyDoc = new XmlDocument();
        bodyDoc.LoadXml(body);
        bodyElem.AppendChild(doc.ImportNode(bodyDoc.DocumentElement, true));

        return SerializeDocument(doc);
    }

    /// <summary>
    /// Builds a SOAP Fault envelope for the specified version.
    /// </summary>
    /// <param name="faultCode">The fault code string (e.g., soap:Server).</param>
    /// <param name="faultMessage">Human-readable description of the fault.</param>
    /// <param name="version">SOAP version that determines the envelope namespace and fault structure.</param>
    /// <returns>A UTF-8 encoded SOAP Fault envelope XML string.</returns>
    internal static string BuildFaultEnvelope(string faultCode, string faultMessage, SoapVersion version)
    {
        var soapNs = version == SoapVersion.Soap11 ? Soap11Namespace : Soap12Namespace;

        var doc = new XmlDocument();

        var envelope = doc.CreateElement("soap", "Envelope", soapNs);
        doc.AppendChild(envelope);

        var body = doc.CreateElement("soap", "Body", soapNs);
        envelope.AppendChild(body);

        var fault = doc.CreateElement("soap", "Fault", soapNs);
        body.AppendChild(fault);

        if (version == SoapVersion.Soap11)
        {
            // SOAP 1.1 Fault structure
            AppendTextElement(doc, fault, string.Empty, "faultcode", string.Empty, faultCode);
            AppendTextElement(doc, fault, string.Empty, "faultstring", string.Empty, faultMessage);
        }
        else
        {
            // SOAP 1.2 Fault structure
            var code = doc.CreateElement("soap", "Code", soapNs);
            AppendTextElement(doc, code, "soap", "Value", soapNs, faultCode);
            fault.AppendChild(code);

            var reason = doc.CreateElement("soap", "Reason", soapNs);
            var text = doc.CreateElement("soap", "Text", soapNs);
            text.InnerText = faultMessage;
            text.SetAttribute("xml:lang", "en");
            reason.AppendChild(text);
            fault.AppendChild(reason);
        }

        return SerializeDocument(doc);
    }

    /// <summary>
    /// Returns true when the XML string contains a SOAP Fault element (either version).
    /// </summary>
    /// <param name="xmlResponse">The raw XML response string to inspect.</param>
    /// <returns>True when a SOAP Fault element is detected; otherwise false.</returns>
    internal static bool IsSoapFault(string xmlResponse)
    {
        if (string.IsNullOrWhiteSpace(xmlResponse))
            return false;

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("s11", Soap11Namespace);
            nsManager.AddNamespace("s12", Soap12Namespace);

            return doc.SelectSingleNode("//s11:Fault | //s12:Fault", nsManager) != null;
        }
        catch
        {
            return false;
        }
    }

    private static string SerializeDocument(XmlDocument doc)
    {
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
        };

        using (var writer = XmlWriter.Create(ms, settings))
            doc.WriteTo(writer);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void AppendTextElement(
        XmlDocument doc,
        XmlElement parent,
        string prefix,
        string localName,
        string ns,
        string value)
    {
        var elem = string.IsNullOrEmpty(prefix)
            ? doc.CreateElement(localName)
            : doc.CreateElement(prefix, localName, ns);
        elem.InnerText = value;
        parent.AppendChild(elem);
    }
}
