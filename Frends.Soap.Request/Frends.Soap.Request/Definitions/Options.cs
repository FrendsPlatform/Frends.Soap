using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.Soap.Request.Attributes;
using Frends.Soap.Request.Definitions.Enums;

namespace Frends.Soap.Request.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// SOAP version to use for wrapping the message.
    /// </summary>
    /// <example>SoapVersion.Soap12</example>
    [DefaultValue(SoapVersion.Soap12)]
    public SoapVersion SoapVersion { get; set; } = SoapVersion.Soap12;

    /// <summary>
    /// Include the WS-Security header block in the SOAP envelope.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeWsSecurity { get; set; } = true;

    /// <summary>
    /// Username used when WS-Security UsernameToken is emitted.
    /// </summary>
    /// <example>service-account</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsSecurity), "", true)]
    [RequiredIf(nameof(IncludeWsSecurity), true)]
    public string WsSecurityUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password used when WS-Security UsernameToken is emitted.
    /// </summary>
    /// <example>MyStrongPassword123</example>
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsSecurity), "", true)]
    [RequiredIf(nameof(IncludeWsSecurity), true)]
    public string WsSecurityPassword { get; set; } = string.Empty;

    /// <summary>
    /// Password token type used in the WS-Security UsernameToken header.
    /// Use a full token type URI.
    /// </summary>
    /// <example>PasswordText</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsSecurity), "", true)]
    [RequiredIf(nameof(IncludeWsSecurity), true)]
    public string WsSecurityPasswordType { get; set; } = string.Empty;

    /// <summary>
    /// Validity window for the WS-Security timestamp, in minutes.
    /// Set to 0 to disable the WS-Security timestamp.
    /// </summary>
    /// <example>5</example>
    [DefaultValue(5)]
    [UIHint(nameof(IncludeWsSecurity), "", true)]
    [RequiredIf(nameof(IncludeWsSecurity), true)]
    [Range(0, int.MaxValue)]
    public int WsSecurityTimestampMinutes { get; set; } = 5;

    /// <summary>
    /// Include the WS-Addressing header block in the SOAP envelope.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeWsAddressing { get; set; } = true;

    /// <summary>
    /// WS-Addressing message identifier.
    /// If empty, a new UUID based identifier is generated automatically.
    /// </summary>
    /// <example>urn:uuid:8f4f3f77-9a0a-4c1c-8fb5-3a2f8ecb6a13</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsAddressing), "", true)]
    public string WsAddressingMessageId { get; set; } = string.Empty;

    /// <summary>
    /// WS-Addressing ReplyTo address.
    /// </summary>
    /// <example>https://www.w3.org/2005/08/addressing/anonymous</example>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsAddressing), "", true)]
    [RequiredIf(nameof(IncludeWsAddressing), true)]
    public string WsAddressingReplyTo { get; set; } = string.Empty;

    /// <summary>
    /// Include the WS-ReliableMessaging header block in the SOAP envelope.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeWsReliableMessaging { get; set; } = true;

    /// <summary>
    /// WS-ReliableMessaging sequence identifier.
    /// If empty, a new UUID based identifier is generated automatically.
    /// </summary>
    /// <example>urn:uuid:1c0a8ef0-8e2f-4ae8-a5f6-9d0e2a8710d4</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsReliableMessaging), "", true)]
    public string WsReliableMessagingSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// WS-ReliableMessaging message number.
    /// </summary>
    /// <example>1</example>
    [DefaultValue(1)]
    [UIHint(nameof(IncludeWsReliableMessaging), "", true)]
    [RequiredIf(nameof(IncludeWsReliableMessaging), true)]
    [Range(1, int.MaxValue)]
    public int WsReliableMessagingMessageNumber { get; set; } = 1;

    /// <summary>
    /// Include the WS-Policy header block in the SOAP envelope.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeWsPolicy { get; set; } = true;

    /// <summary>
    /// URI referenced by WS-Policy PolicyReference.
    /// If empty, the request endpoint URL is used.
    /// </summary>
    /// <example>https://example.com/policy/service-policy</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsPolicy), "", true)]
    public string WsPolicyReferenceUri { get; set; } = string.Empty;

    /// <summary>
    /// Include the WS-Trust header block in the SOAP envelope.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeWsTrust { get; set; } = true;

    /// <summary>
    /// WS-Trust request type URI.
    /// </summary>
    /// <example>https://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</example>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsTrust), "", true)]
    [RequiredIf(nameof(IncludeWsTrust), true)]
    public string WsTrustRequestType { get; set; } = string.Empty;

    /// <summary>
    /// WS-Trust token type URI.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:assertion</example>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsTrust), "", true)]
    [RequiredIf(nameof(IncludeWsTrust), true)]
    public string WsTrustTokenType { get; set; }

    /// <summary>
    /// WS-Trust AppliesTo address.
    /// If empty, the request endpoint URL is used.
    /// </summary>
    /// <example>https://example.com/secured-service</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsTrust), "", true)]
    public string WsTrustAppliesTo { get; set; } = string.Empty;

    /// <summary>
    /// Include the WS-Federation header block in the SOAP envelope.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeWsFederation { get; set; } = true;

    /// <summary>
    /// WS-Federation realm value.
    /// If empty, the request endpoint URL is used.
    /// </summary>
    /// <example>https://example.com/realm</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsFederation), "", true)]
    public string WsFederationRealm { get; set; } = string.Empty;

    /// <summary>
    /// WS-Federation passive requestor endpoint.
    /// If empty, the request endpoint URL is used.
    /// </summary>
    /// <example>https://login.example.com/passive</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(IncludeWsFederation), "", true)]
    public string WsFederationPassiveRequestorEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Source from which the WSDL is obtained for body validation.
    /// </summary>
    /// <example>WsdlSource.None</example>
    [DefaultValue(WsdlSource.None)]
    public WsdlSource WsdlSource { get; set; } = WsdlSource.None;

    /// <summary>
    /// WSDL provided as an XML string. Used when WsdlSource is String.
    /// </summary>
    /// <example>&lt;definitions ...&gt;...&lt;/definitions&gt;</example>
    [UIHint(nameof(WsdlSource), "", WsdlSource.String)]
    [RequiredIf(nameof(WsdlSource), WsdlSource.String)]
    [DisplayFormat(DataFormatString = "Xml")]
    public string WsdlString { get; set; } = string.Empty;

    /// <summary>
    /// Path to the WSDL file on disk. Used when WsdlSource is File.
    /// </summary>
    /// <example>C:\wsdl\service.wsdl</example>
    [UIHint(nameof(WsdlSource), "", WsdlSource.File)]
    [RequiredIf(nameof(WsdlSource), WsdlSource.File)]
    [DisplayFormat(DataFormatString = "Text")]
    public string WsdlPath { get; set; } = string.Empty;

    /// <summary>
    /// URL pointing to the WSDL. Used when WsdlSource is Url.
    /// </summary>
    /// <example>https://example.com/service?wsdl</example>
    [UIHint(nameof(WsdlSource), "", WsdlSource.Url)]
    [RequiredIf(nameof(WsdlSource), WsdlSource.Url)]
    [DisplayFormat(DataFormatString = "Text")]
    public string WsdlUrl { get; set; } = string.Empty;

    /// <summary>
    /// When true, exceptions are thrown on failure instead of returning a failed Result.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
