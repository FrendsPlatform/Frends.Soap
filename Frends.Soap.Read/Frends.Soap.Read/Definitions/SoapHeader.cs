namespace Frends.Soap.Read.Definitions;

/// <summary>
/// A single header element found inside the SOAP Header element of the payload.
/// </summary>
public class SoapHeader
{
    /// <summary>
    /// Local name of the header element.
    /// </summary>
    /// <example>Action</example>
    public string Name { get; set; }

    /// <summary>
    /// Namespace URI of the header element, if any.
    /// </summary>
    /// <example>http://www.w3.org/2005/08/addressing</example>
    public string Namespace { get; set; }

    /// <summary>
    /// Text content of the header element.
    /// </summary>
    /// <example>http://tempuri.org/IService/DoWork</example>
    public string Value { get; set; }

    /// <summary>
    /// The full outer XML of the header element.
    /// </summary>
    /// <example>&lt;wsa:Action&gt;http://tempuri.org/IService/DoWork&lt;/wsa:Action&gt;</example>
    public string Xml { get; set; }
}
