using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Frends.Soap.Read.Definitions;
using Frends.Soap.Read.Definitions.Enums;

namespace Frends.Soap.Read.Helpers;

/// <summary>
/// Parses a raw SOAP payload (SOAP 1.1 or 1.2) into a <see cref="Result"/>.
/// </summary>
internal static class SoapHandler
{
    private const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string Soap12Namespace = "http://www.w3.org/2003/05/soap-envelope";

    internal static string DecodePayload(string payload, Options options)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new FormatException("Payload is empty.");

        if (options.PayloadEncoding != PayloadEncoding.Base64)
            return payload;

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(payload.Trim());
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Payload could not be Base64 decoded: {ex.Message}", ex);
        }

        var encodingName = string.IsNullOrWhiteSpace(options.CharacterEncoding) ? "utf-8" : options.CharacterEncoding;
        Encoding encoding;

        try
        {
            encoding = Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Unknown character encoding '{encodingName}'.", ex);
        }

        return encoding.GetString(bytes);
    }

    internal static SoapVersion ResolveVersion(string namespaceUri) => namespaceUri switch
    {
        Soap11Namespace => SoapVersion.Soap11,
        Soap12Namespace => SoapVersion.Soap12,
        _ => SoapVersion.Unknown,
    };

    internal static List<SoapHeader> ReadHeaders(XmlElement headerElement)
    {
        var headers = new List<SoapHeader>();

        if (headerElement == null)
            return headers;

        foreach (var node in headerElement.ChildNodes)
        {
            if (node is not XmlElement element)
                continue;

            headers.Add(new SoapHeader
            {
                Name = element.LocalName,
                Namespace = element.NamespaceURI,
                Value = element.InnerText,
                Xml = element.OuterXml,
            });
        }

        return headers;
    }

    internal static SoapFault ReadFault(XmlElement faultElement, SoapVersion version)
    {
        if (faultElement == null)
            return null;

        return version == SoapVersion.Soap11
            ? ReadSoap11Fault(faultElement)
            : ReadSoap12Fault(faultElement);
    }

    private static SoapFault ReadSoap11Fault(XmlElement faultElement) => new()
    {
        Code = GetChildText(faultElement, "faultcode"),
        Reason = GetChildText(faultElement, "faultstring"),
        Actor = GetChildText(faultElement, "faultactor"),
        Detail = GetChildInnerXml(faultElement, "detail"),
        Xml = faultElement.OuterXml,
    };

    private static SoapFault ReadSoap12Fault(XmlElement faultElement)
    {
        var code = FindDescendant(faultElement, "Code");
        var reason = FindDescendant(faultElement, "Reason");

        return new SoapFault
        {
            Code = code == null ? null : GetChildText(code, "Value"),
            Reason = reason == null ? null : GetChildText(reason, "Text"),
            Actor = GetChildText(faultElement, "Node"),
            Detail = GetChildInnerXml(faultElement, "Detail"),
            Xml = faultElement.OuterXml,
        };
    }

    private static string GetChildText(XmlElement parent, string localName)
    {
        var child = FindDescendant(parent, localName);

        return child?.InnerText;
    }

    private static string GetChildInnerXml(XmlElement parent, string localName)
    {
        var child = FindDescendant(parent, localName);

        return child?.InnerXml.Trim();
    }

    private static XmlElement FindDescendant(XmlElement parent, string localName)
    {
        foreach (var node in parent.ChildNodes)
        {
            if (node is XmlElement element && string.Equals(element.LocalName, localName, StringComparison.Ordinal))
                return element;
        }

        return null;
    }
}
