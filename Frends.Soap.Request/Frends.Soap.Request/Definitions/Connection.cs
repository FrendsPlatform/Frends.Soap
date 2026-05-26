using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.Soap.Request.Attributes;
using Frends.Soap.Request.Definitions.Enums;

namespace Frends.Soap.Request.Definitions;

/// <summary>
/// Connection parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// The URL with protocol and path. You can include query parameters directly in the url.
    /// </summary>
    /// <example>https://example.org/path/to</example>
    [Required]
    [DisplayFormat(DataFormatString = "Text")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Method of authenticating request.
    /// </summary>
    /// <example>OAuth</example>
    [DefaultValue(Authentication.None)]
    public Authentication Authentication { get; set; } = Authentication.None;

    /// <summary>
    /// OAuth2 Bearer token used when Authentication is set to OAuth.
    /// </summary>
    /// <example>eyJhbGciOiJSUzI1NiJ9...</example>
    [UIHint(nameof(Authentication), "", Authentication.OAuth)]
    [RequiredIf(nameof(Authentication), Authentication.OAuth)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string OAuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Username used when HTTP Basic authentication is selected.
    /// </summary>
    /// <example>basic-user</example>
    [UIHint(nameof(Authentication), "", Authentication.Basic)]
    [RequiredIf(nameof(Authentication), Authentication.Basic)]
    [DisplayFormat(DataFormatString = "Text")]
    public string BasicUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password used when HTTP Basic authentication is selected.
    /// </summary>
    /// <example>basic-password</example>
    [UIHint(nameof(Authentication), "", Authentication.Basic)]
    [RequiredIf(nameof(Authentication), Authentication.Basic)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string BasicPassword { get; set; } = string.Empty;

    /// <summary>
    /// Username used when Windows authentication is selected.
    /// </summary>
    /// <example>DOMAIN\windows-user</example>
    [UIHint(nameof(Authentication), "", Authentication.WindowsAuthentication)]
    [RequiredIf(nameof(Authentication), Authentication.WindowsAuthentication)]
    [DisplayFormat(DataFormatString = "Text")]
    public string WindowsAuthenticationUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password used when Windows authentication is selected.
    /// </summary>
    /// <example>windows-password</example>
    [UIHint(nameof(Authentication), "", Authentication.WindowsAuthentication)]
    [RequiredIf(nameof(Authentication), Authentication.WindowsAuthentication)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string WindowsAuthenticationPassword { get; set; } = string.Empty;

    /// <summary>
    /// Do not throw an exception on certificate error.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool AllowInvalidCertificate { get; set; }

    /// <summary>
    /// Path to the client certificate file (PFX or P12 format).
    /// </summary>
    /// <example>C:\certs\client.pfx</example>
    [UIHint(nameof(Authentication), "", Authentication.ClientCertificate)]
    [RequiredIf(nameof(Authentication), Authentication.ClientCertificate)]
    [DisplayFormat(DataFormatString = "Text")]
    public string ClientCertPath { get; set; } = string.Empty;

    /// <summary>
    /// Password for the client certificate.
    /// </summary>
    /// <example>MyStrongPassword123</example>
    [UIHint(nameof(Authentication), "", Authentication.ClientCertificate)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ClientCertPassword { get; set; } = string.Empty;

    /// <summary>
    /// Optional HTTP proxy address.
    /// </summary>
    /// <example>http://localhost:8888</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ProxyAddress { get; set; } = string.Empty;

    /// <summary>
    /// Username used for the HTTP proxy.
    /// </summary>
    /// <example>proxy-user</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ProxyUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password used for the HTTP proxy.
    /// </summary>
    /// <example>proxy-password</example>
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ProxyPassword { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether HTTP redirects are automatically followed.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool FollowRedirects { get; set; } = true;

    /// <summary>
    /// HTTP request timeout in seconds. Set to 0 to disable the timeout.
    /// </summary>
    /// <example>100</example>
    [DefaultValue(100)]
    [Range(0, int.MaxValue)]
    public int ConnectionTimeoutSeconds { get; set; } = 100;

    /// <summary>
    /// Cache and reuse HTTP clients for identical connection settings.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool CacheHttpClients { get; set; }

    /// <summary>
    /// HTTP protocol version used for the outgoing request.
    /// </summary>
    /// <example>HttpProtocolVersion.Http11</example>
    [DefaultValue(HttpProtocolVersion.Http11)]
    public HttpProtocolVersion HttpProtocolVersion { get; set; } = HttpProtocolVersion.Http11;

    /// <summary>
    /// SSL/TLS protocol version used when negotiating HTTPS.
    /// </summary>
    /// <example>SslProtocolVersion.SystemDefault</example>
    [DefaultValue(SslProtocolVersion.SystemDefault)]
    public SslProtocolVersion SslProtocolVersion { get; set; } = SslProtocolVersion.SystemDefault;

    /// <summary>
    /// Custom HTTP headers added to the outgoing request.
    /// </summary>
    /// <example>Header[] { new Header { Name = "X-My-Header", Value = "value" } }</example>
    public Header[] CustomHeaders { get; set; } = [];

    /// <summary>
    /// Expected server certificate thumbprint(s) for validation.
    /// Only used when AllowInvalidCertificate is false.
    /// Used in mTLS mode for certificate pinning.
    /// </summary>
    /// <example>E5FA62B8B5F3B0B2B3B4B5B6B7B8B9B0B1B2B3B4</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string[] ServerCertificateThumbprints { get; set; } = [];

    /// <summary>
    /// Enables or disables certificate revocation checking.
    /// </summary>
    /// <example>false</example>
    public bool CertificationRevocationCheck { get; set; }
}
