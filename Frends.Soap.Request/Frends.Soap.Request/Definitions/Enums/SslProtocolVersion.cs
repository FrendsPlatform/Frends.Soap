namespace Frends.Soap.Request.Definitions.Enums;

/// <summary>
/// SSL/TLS protocol version used when establishing the HTTPS connection.
/// </summary>
public enum SslProtocolVersion
{
    /// <summary>
    /// Use the operating system default SSL/TLS protocols.
    /// </summary>
    SystemDefault = 0,

    /// <summary>
    /// TLS 1.0.
    /// </summary>
    Tls = 1,

    /// <summary>
    /// TLS 1.1.
    /// </summary>
    Tls11 = 2,

    /// <summary>
    /// TLS 1.2.
    /// </summary>
    Tls12 = 3,

    /// <summary>
    /// TLS 1.3.
    /// </summary>
    Tls13 = 4,
}


