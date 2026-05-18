namespace Frends.Soap.Request.Definitions.Enums;

/// <summary>
/// HTTP protocol version used when sending requests.
/// </summary>
public enum HttpProtocolVersion
{
    /// <summary>
    /// HTTP/1.0.
    /// </summary>
    Http10 = 0,

    /// <summary>
    /// HTTP/1.1.
    /// </summary>
    Http11 = 1,

    /// <summary>
    /// HTTP/2.0.
    /// </summary>
    Http20 = 2,

    /// <summary>
    /// HTTP/3.0.
    /// </summary>
    Http30 = 3,
}
