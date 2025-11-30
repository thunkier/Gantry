namespace Gantry.Core.Domain.Settings;

public enum AuthType
{
    Inherit,
    None,
    BearerToken,
    Basic,
    ApiKey,
    AwsV4,
    Digest,
    EdgeGrid,
    Hawk,
    Ntlm,
    OAuth1,
    OAuth2
}

public class AuthSettings
{
    public AuthType Type { get; set; } = AuthType.Inherit;
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Additional Auth Properties (Stored as Dictionary for flexibility or specific properties)
    public Dictionary<string, string> Attributes { get; set; } = new();
}
