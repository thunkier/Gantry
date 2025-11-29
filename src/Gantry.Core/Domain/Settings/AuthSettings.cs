namespace Gantry.Core.Domain.Settings;

public enum AuthType
{
    Inherit,
    None,
    BearerToken,
    Basic
}

public class AuthSettings
{
    public AuthType Type { get; set; } = AuthType.Inherit;
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
