using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Soap.Read.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// The SOAP payload to read. This is the raw message as it would arrive at a SOAP
    /// endpoint, including the SOAP Envelope, optional Header and the Body (or Fault).
    /// When Options.PayloadEncoding is set to Base64 the value is expected
    /// to be a Base64 encoded string of the SOAP XML.
    /// </summary>
    /// <example>&lt;soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"&gt;&lt;soap:Body&gt;&lt;GetPrice xmlns="http://tempuri.org/"/&gt;&lt;/soap:Body&gt;&lt;/soap:Envelope&gt;</example>
    [DisplayFormat(DataFormatString = "Text")]
    [Required]
    public string Payload { get; set; } = string.Empty;
}
