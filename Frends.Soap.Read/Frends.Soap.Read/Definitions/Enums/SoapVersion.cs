namespace Frends.Soap.Read.Definitions.Enums;

/// <summary>
/// SOAP protocol version detected from the payload envelope namespace.
/// </summary>
public enum SoapVersion
{
    /// <summary>
    /// Unknown or non-SOAP envelope.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// SOAP 1.1 (http://schemas.xmlsoap.org/soap/envelope/).
    /// </summary>
    Soap11 = 1,

    /// <summary>
    /// SOAP 1.2 (http://www.w3.org/2003/05/soap-envelope).
    /// </summary>
    Soap12 = 2,
}
