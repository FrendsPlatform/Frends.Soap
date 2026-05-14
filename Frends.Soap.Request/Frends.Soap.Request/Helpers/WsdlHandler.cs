using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Frends.Soap.Request.Definitions;
using Frends.Soap.Request.Definitions.Enums;

namespace Frends.Soap.Request.Helpers;

internal static class WsdlHandler
{
    internal static async Task<string> LoadWsdlContentAsync(
        Options options,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        return options.WsdlSource switch
        {
            WsdlSource.String => options.WsdlString,
            WsdlSource.File => await File.ReadAllTextAsync(options.WsdlPath, cancellationToken),
            WsdlSource.Url => await httpClient.GetStringAsync(options.WsdlUrl, cancellationToken),
            _ => null,
        };
    }

    internal static string GetTargetNamespace(string wsdlContent)
    {
        if (string.IsNullOrWhiteSpace(wsdlContent))
            return null;

        var doc = new XmlDocument();
        doc.LoadXml(wsdlContent);
        return doc.DocumentElement?.GetAttribute("targetNamespace");
    }

    internal static (bool IsValid, string Error) ValidateBodyAgainstWsdl(string body, string wsdlContent)
    {
        if (string.IsNullOrWhiteSpace(wsdlContent))
            return (true, null);

        var wsdlDoc = new XmlDocument();
        wsdlDoc.LoadXml(wsdlContent);

        var nsMgr = new XmlNamespaceManager(wsdlDoc.NameTable);
        nsMgr.AddNamespace("wsdl", "https://schemas.xmlsoap.org/wsdl/");
        nsMgr.AddNamespace("xsd", "https://www.w3.org/2001/XMLSchema");

        var schemaNodes = wsdlDoc.SelectNodes("//wsdl:types/xsd:schema | //types/schema", nsMgr);
        var schemaSet = new XmlSchemaSet();

        if (schemaNodes != null)
        {
            foreach (XmlNode schemaNode in schemaNodes)
            {
                using var r = new StringReader(schemaNode.OuterXml);
                var schema = XmlSchema.Read(r, null);
                if (schema != null)
                    schemaSet.Add(schema);
            }
        }

        if (schemaSet.Count == 0)
            return (true, null);

        schemaSet.Compile();

        var errors = new List<string>();
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = schemaSet };
        settings.ValidationEventHandler += (_, e) => errors.Add(e.Message);

        using var xmlReader = XmlReader.Create(new StringReader(body), settings);
        try
        {
            while (xmlReader.Read())
            {
            }
        }
        catch (XmlException ex)
        {
            errors.Add(ex.Message);
        }

        return errors.Count == 0 ? (true, null) : (false, string.Join("; ", errors));
    }
}
