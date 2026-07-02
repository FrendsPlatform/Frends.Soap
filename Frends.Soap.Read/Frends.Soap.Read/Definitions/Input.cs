using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Soap.Read.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// The input string to be repeated and output.
    /// </summary>
    /// <example>foobar</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("Lorem ipsum dolor sit amet.")]
    [Required]
    public string Payload { get; set; } = string.Empty;
}
