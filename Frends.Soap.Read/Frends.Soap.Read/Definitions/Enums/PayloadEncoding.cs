namespace Frends.Soap.Read.Definitions.Enums;

/// <summary>
/// How the incoming payload string is encoded before it can be parsed as XML.
/// </summary>
public enum PayloadEncoding
{
    /// <summary>
    /// The payload is a plain SOAP XML string and is parsed as-is.
    /// </summary>
    None = 0,

    /// <summary>
    /// The payload is a Base64 encoded SOAP XML string and is decoded before parsing.
    /// </summary>
    Base64 = 1,
}
