namespace Frends.Soap.Request.Definitions.Enums;

public enum WsdlSource
{
    /// <summary>
    /// Do not use WSDL.
    /// </summary>
    None = 0,

    /// <summary>
    /// WSDL is provided as a URL.
    /// </summary>
    Url = 1,

    /// <summary>
    /// WSDL is provided as a file path.
    /// </summary>
    File = 2,

    /// <summary>
    /// WSDL is provided as a string.
    /// </summary>
    String = 3,
}
