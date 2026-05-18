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
    /// HTTP Basic authentication.
    /// </summary>
    Basic = 3,

    /// <summary>
    /// Client Certificate authentication.
    /// </summary>
    ClientCertificate = 2,

    /// <summary>
    /// Windows authentication using explicit credentials.
    /// </summary>
    WindowsAuthentication = 4,

    /// <summary>
    /// Windows integrated security using the current process identity.
    /// </summary>
    WindowsIntegratedSecurity = 5,
}
