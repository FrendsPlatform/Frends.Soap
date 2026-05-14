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
    public string Url { get; set; }

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
    [RequiredIf(nameof(Authentication), Authentication.ClientCertificate)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ClientCertPassword { get; set; } = string.Empty;

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
    public bool CertificationRevocationCheck { get; set; } = false;
}
