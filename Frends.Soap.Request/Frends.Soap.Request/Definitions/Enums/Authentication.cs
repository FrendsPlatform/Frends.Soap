namespace Frends.Soap.Request.Definitions.Enums;

/// <summary>
/// Request authentication.
/// </summary>
public enum Authentication
{
    /// <summary>
    /// No authentication.
    /// </summary>
    None = 0,

    /// <summary>
    /// OAuth authentication.
    /// </summary>
    OAuth = 1,

    /// <summary>
    /// Client Certificate authentication.
    /// </summary>
    ClientCertificate = 2,
}
