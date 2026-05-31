namespace AH.TestU4IDS.NET10.ParentAPI;

/// <summary>
/// Settings used to exchange a user access token for a new access token
/// via the custom "agent_delegation" extension grant at the authority's token endpoint.
/// </summary>
public sealed class AgentDelegationOptions
{
    public const string SectionName = "AgentDelegation";

    /// <summary>The agent's client id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The agent's client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The scope requested for the new access token.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The resource (audience) the new access token is requested for.</summary>
    public string Resource { get; set; } = string.Empty;
}
