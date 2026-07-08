namespace Frends.Soap.Read.Definitions;

/// <summary>
/// Represents a SOAP Fault extracted from the payload body (SOAP 1.1 or 1.2).
/// </summary>
public class SoapFault
{
    /// <summary>
    /// Fault code. For SOAP 1.1 this is the faultcode element, for SOAP 1.2 the Code/Value element.
    /// </summary>
    /// <example>soap:Server</example>
    public string Code { get; set; }

    /// <summary>
    /// Human-readable fault reason. For SOAP 1.1 this is the faultstring element,
    /// for SOAP 1.2 the Reason/Text element.
    /// </summary>
    /// <example>Something went wrong.</example>
    public string Reason { get; set; }

    /// <summary>
    /// Fault actor/node. For SOAP 1.1 this is the faultactor element, for SOAP 1.2 the Node element.
    /// </summary>
    /// <example>http://example.com/service</example>
    public string Actor { get; set; }

    /// <summary>
    /// Application-specific detail information contained in the fault, as XML.
    /// </summary>
    /// <example>&lt;detail&gt;...&lt;/detail&gt;</example>
    public string Detail { get; set; }

    /// <summary>
    /// The full outer XML of the fault element.
    /// </summary>
    /// <example>&lt;soap:Fault&gt;...&lt;/soap:Fault&gt;</example>
    public string Xml { get; set; }
}
