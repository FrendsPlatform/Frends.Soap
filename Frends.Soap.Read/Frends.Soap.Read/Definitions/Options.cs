using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.Soap.Read.Attributes;
using Frends.Soap.Read.Definitions.Enums;

namespace Frends.Soap.Read.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// How the incoming payload is encoded. Use Base64 when the payload has been
    /// Base64 encoded (for example when transported as a binary blob); it will be
    /// decoded before being parsed as SOAP XML.
    /// </summary>
    /// <example>None</example>
    [DefaultValue(PayloadEncoding.None)]
    public PayloadEncoding PayloadEncoding { get; set; } = PayloadEncoding.None;

    /// <summary>
    /// Character encoding used to turn the decoded bytes into a string when
    /// PayloadEncoding is Base64. Any name accepted by
    /// System.Text.Encoding is valid (e.g. utf-8, utf-16, iso-8859-1).
    /// </summary>
    /// <example>utf-8</example>
    [RequiredIf(nameof(PayloadEncoding), PayloadEncoding.Base64)]
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("utf-8")]
    public string CharacterEncoding { get; set; } = "utf-8";

    /// <summary>
    /// Whether to throw an error on failure.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
