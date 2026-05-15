using System;
using System.Xml;
using Frends.Soap.Request.Definitions;

namespace Frends.Soap.Request.Helpers;

/// <summary>
/// Builds and validates WS-* specification headers for SOAP envelopes.
/// </summary>
internal static class WsSpecificationsHandler
{
    private const string WsseNamespace =
        "https://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    private const string WsuNamespace =
        "https://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    private const string WsaNamespace = "https://www.w3.org/2005/08/addressing";
    private const string WsrmNamespace = "https://docs.oasis-open.org/ws-rx/wsrm/200702";
    private const string WspNamespace = "https://schemas.xmlsoap.org/ws/2004/09/policy";
    private const string WstNamespace = "https://docs.oasis-open.org/ws-sx/ws-trust/200512";
    private const string WfedNamespace = "https://docs.oasis-open.org/wsfed/federation/200706";

    internal static bool HasHeaders(Options options) =>
        options.IncludeWsSecurity ||
        options.IncludeWsAddressing ||
        options.IncludeWsReliableMessaging ||
        options.IncludeWsPolicy ||
        options.IncludeWsTrust ||
        options.IncludeWsFederation;

    internal static void AppendHeaders(
        XmlDocument doc,
        XmlElement header,
        string soapNs,
        Options options,
        string endpointUrl,
        string soapAction)
    {
        if (options.IncludeWsAddressing) AppendWsAddressing(doc, header, endpointUrl, soapAction, options);
        if (options.IncludeWsSecurity) AppendWsSecurity(doc, header, soapNs, options);
        if (options.IncludeWsReliableMessaging) AppendWsReliableMessaging(doc, header, options);
        if (options.IncludeWsPolicy) AppendWsPolicy(doc, header, endpointUrl, options);
        if (options.IncludeWsTrust) AppendWsTrust(doc, header, endpointUrl, options);
        if (options.IncludeWsFederation) AppendWsFederation(doc, header, endpointUrl, options);
    }

    private static void AppendWsAddressing(
        XmlDocument doc,
        XmlElement header,
        string endpointUrl,
        string soapAction,
        Options options)
    {
        var action = doc.CreateElement("wsa", "Action", WsaNamespace);
        action.InnerText = !string.IsNullOrWhiteSpace(soapAction) ? soapAction : endpointUrl;
        header.AppendChild(action);

        var messageId = doc.CreateElement("wsa", "MessageID", WsaNamespace);
        messageId.InnerText = BuildUuidUri(options.WsAddressingMessageId);
        header.AppendChild(messageId);

        var to = doc.CreateElement("wsa", "To", WsaNamespace);
        to.InnerText = endpointUrl;
        header.AppendChild(to);

        var replyTo = doc.CreateElement("wsa", "ReplyTo", WsaNamespace);
        AppendTextElement(
            doc,
            replyTo,
            "wsa",
            "Address",
            WsaNamespace,
            options.WsAddressingReplyTo);
        header.AppendChild(replyTo);
    }

    private static void AppendWsSecurity(XmlDocument doc, XmlElement header, string soapNs, Options options)
    {
        var security = doc.CreateElement(
            "wsse",
            "Security",
            WsseNamespace);
        AppendAttribute(
            doc,
            security,
            "soap",
            "mustUnderstand",
            soapNs,
            "1");

        if (options.WsSecurityTimestampMinutes > 0)
        {
            var timestamp = doc.CreateElement(
                "wsu",
                "Timestamp",
                WsuNamespace);
            AppendAttribute(
                doc,
                timestamp,
                "wsu",
                "Id",
                WsuNamespace,
                $"TS-{Guid.NewGuid():N}");

            var created = DateTime.UtcNow;
            var expires = created.AddMinutes(options.WsSecurityTimestampMinutes);
            AppendTextElement(
                doc,
                timestamp,
                "wsu",
                "Created",
                WsuNamespace,
                XmlConvert.ToString(
                    created,
                    XmlDateTimeSerializationMode.Utc));
            AppendTextElement(
                doc,
                timestamp,
                "wsu",
                "Expires",
                WsuNamespace,
                XmlConvert.ToString(
                    expires,
                    XmlDateTimeSerializationMode.Utc));
            security.AppendChild(timestamp);
        }

        if (!string.IsNullOrWhiteSpace(options.WsSecurityUsername) ||
            !string.IsNullOrWhiteSpace(options.WsSecurityPassword))
        {
            var usernameToken = doc.CreateElement(
                "wsse",
                "UsernameToken",
                WsseNamespace);
            AppendAttribute(
                doc,
                usernameToken,
                "wsu",
                "Id",
                WsuNamespace,
                $"UT-{Guid.NewGuid():N}");

            AppendTextElement(
                doc,
                usernameToken,
                "wsse",
                "Username",
                WsseNamespace,
                options.WsSecurityUsername);

            var password = doc.CreateElement(
                "wsse",
                "Password",
                WsseNamespace);
            password.InnerText = options.WsSecurityPassword;
            password.SetAttribute("Type", options.WsSecurityPasswordType);
            usernameToken.AppendChild(password);
            security.AppendChild(usernameToken);
        }

        header.AppendChild(security);
    }

    private static void AppendWsReliableMessaging(XmlDocument doc, XmlElement header, Options options)
    {
        var sequence = doc.CreateElement("wsrm", "Sequence", WsrmNamespace);
        AppendTextElement(
            doc,
            sequence,
            "wsrm",
            "Identifier",
            WsrmNamespace,
            BuildUuidUri(options.WsReliableMessagingSequenceId));
        AppendTextElement(
            doc,
            sequence,
            "wsrm",
            "MessageNumber",
            WsrmNamespace,
            options.WsReliableMessagingMessageNumber.ToString());
        header.AppendChild(sequence);
    }

    private static void AppendWsPolicy(XmlDocument doc, XmlElement header, string endpointUrl, Options options)
    {
        var policyReference = doc.CreateElement("wsp", "PolicyReference", WspNamespace);
        policyReference.SetAttribute("URI", NormalizeOptionalValue(options.WsPolicyReferenceUri, endpointUrl));
        header.AppendChild(policyReference);
    }

    private static void AppendWsTrust(XmlDocument doc, XmlElement header, string endpointUrl, Options options)
    {
        var rst = doc.CreateElement("wst", "RequestSecurityToken", WstNamespace);
        AppendTextElement(
            doc,
            rst,
            "wst",
            "RequestType",
            WstNamespace,
            options.WsTrustRequestType);
        AppendTextElement(
            doc,
            rst,
            "wst",
            "TokenType",
            WstNamespace,
            options.WsTrustTokenType);

        var appliesTo = doc.CreateElement("wsp", "AppliesTo", WspNamespace);
        var endpointReference = doc.CreateElement("wsa", "EndpointReference", WsaNamespace);
        AppendTextElement(
            doc,
            endpointReference,
            "wsa",
            "Address",
            WsaNamespace,
            NormalizeOptionalValue(
                options.WsTrustAppliesTo,
                endpointUrl));
        appliesTo.AppendChild(endpointReference);
        rst.AppendChild(appliesTo);

        header.AppendChild(rst);
    }

    private static void AppendWsFederation(XmlDocument doc, XmlElement header, string endpointUrl, Options options)
    {
        var federation = doc.CreateElement("wfed", "Federation", WfedNamespace);
        AppendTextElement(
            doc,
            federation,
            "wfed",
            "Realm",
            WfedNamespace,
            NormalizeOptionalValue(
                options.WsFederationRealm,
                endpointUrl));
        AppendTextElement(
            doc,
            federation,
            "wfed",
            "PassiveRequestorEndpoint",
            WfedNamespace,
            NormalizeOptionalValue(options.WsFederationPassiveRequestorEndpoint, endpointUrl));
        header.AppendChild(federation);
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

    private static void AppendAttribute(
        XmlDocument doc,
        XmlElement element,
        string prefix,
        string localName,
        string ns,
        string value)
    {
        var attr = doc.CreateAttribute(prefix, localName, ns);
        attr.Value = value;
        element.Attributes.Append(attr);
    }

    private static string BuildUuidUri(string value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? Guid.NewGuid().ToString()
            : value;

        return normalizedValue.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase)
            ? normalizedValue
            : $"urn:uuid:{normalizedValue}";
    }

    private static string NormalizeOptionalValue(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? (fallback ?? string.Empty)
            : value;
    }
}
