namespace AH.TestU4IDS.NET10.Web;

/// <summary>
/// Strongly-typed OpenID Connect settings bound from the "OpenIdConnect" configuration section.
/// </summary>
public sealed class OpenIdConnectSettings
{
    public const string SectionName = "OpenIdConnect";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = ["openid", "profile"];

    public string Tenant { get; set; } = string.Empty;

    public string IdpName { get; set; } = string.Empty;
}
