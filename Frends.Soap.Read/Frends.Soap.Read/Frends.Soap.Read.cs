using System;
using System.ComponentModel;
using System.Threading;
using System.Xml;
using Frends.Soap.Read.Definitions;
using Frends.Soap.Read.Definitions.Enums;
using Frends.Soap.Read.Helpers;

namespace Frends.Soap.Read;

/// <summary>
/// Task Class for Soap operations.
/// </summary>
public static class Soap
{
    /// <summary>
    /// Task to read Soap payload
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Soap-Read)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, enum SoapVersion, string Body, List&lt;object&gt; Headers {string Name, string Namespace, string Value, string Xml }, object SoapFault { string Code, string Reason, string Actor, string Detail, string Xml } , object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result Read(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, options);
            var xml = SoapHandler.DecodePayload(input.Payload, options);
            cancellationToken.ThrowIfCancellationRequested();

            var doc = new XmlDocument
            {
                PreserveWhitespace = false,
            };

            try
            {
                doc.LoadXml(xml);
            }
            catch (XmlException ex)
            {
                throw new XmlException($"Payload is not valid XML: {ex.Message}", ex);
            }

            var envelope = doc.DocumentElement;

            if (envelope == null || !string.Equals(envelope.LocalName, "Envelope", StringComparison.Ordinal))
            {
                throw new FormatException("Payload is not a valid SOAP message: missing Envelope element.");
            }

            var version = SoapHandler.ResolveVersion(envelope.NamespaceURI);

            if (version == SoapVersion.Unknown)
            {
                throw new FormatException(
                    $"Payload is not a valid SOAP message: unrecognized envelope namespace '{envelope.NamespaceURI}'.");
            }

            var soapNs = envelope.NamespaceURI;
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("soap", soapNs);

            var headers = SoapHandler.ReadHeaders(
                envelope.SelectSingleNode("soap:Header", nsManager) as XmlElement);

            var bodyElement = envelope.SelectSingleNode("soap:Body", nsManager) as XmlElement
                              ?? throw new FormatException(
                                  "Payload is not a valid SOAP message: missing Body element.");

            var fault = SoapHandler.ReadFault(
                bodyElement.SelectSingleNode("soap:Fault", nsManager) as XmlElement,
                version);

            return new Result
            {
                Success = true,
                SoapVersion = version,
                Headers = headers,
                Body = bodyElement.InnerXml.Trim(),
                Fault = fault,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}
