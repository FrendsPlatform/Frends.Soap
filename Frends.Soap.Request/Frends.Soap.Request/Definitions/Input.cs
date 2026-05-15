namespace Frends.Soap.Request.Definitions;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Xml Body of a message that will be wrapped into Soap envelope.
    /// </summary>
    /// <example>&lt;GetWeather xmlns="https://example.com/"&gt;&lt;City&gt;London&lt;/City&gt;&lt;/GetWeather&gt;</example>
    [Required]
    [DisplayFormat(DataFormatString = "Xml")]
    public string MessageBody { get; set; } = string.Empty;

    /// <summary>
    /// The SOAPAction HTTP header value (SOAP 1.1) or the action parameter in Content-Type (SOAP 1.2).
    /// Known as the WS-Specs / WS-Addressing action field.
    /// </summary>
    /// <example>https://example.com/service/GetWeather</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string SoapAction { get; set; } = string.Empty;
}
