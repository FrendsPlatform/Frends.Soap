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
