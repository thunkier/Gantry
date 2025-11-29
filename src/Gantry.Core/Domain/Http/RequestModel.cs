using System.Collections.Generic;

namespace Gantry.Core.Domain.Http;

public class RequestModel
{
    // Basic request properties
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 30000;

    // Auth Settings
    public AuthConfig? Auth { get; set; }

    // HTTP Settings
    public string HttpVersion { get; set; } = "HTTP/1.1";
    public bool EnableSslCertificateVerification { get; set; } = true;
    public bool AutomaticallyFollowRedirects { get; set; } = true;
    public bool FollowOriginalHttpMethod { get; set; } = false;
    public bool FollowAuthorizationHeader { get; set; } = false;
    public bool RemoveRefererHeaderOnRedirect { get; set; } = false;
    public bool EnableStrictHttpParser { get; set; } = false;
    public bool EncodeUrlAutomatically { get; set; } = true;
    public bool DisableCookieJar { get; set; } = false;
    public bool UseServerCipherSuiteDuringHandshake { get; set; } = false;
    public int MaximumNumberOfRedirects { get; set; } = 10;
    public List<string> DisabledTlsProtocols { get; set; } = new();
    public string CipherSuiteSelection { get; set; } = "Default";
}

public class AuthConfig
{
    public Gantry.Core.Domain.Settings.AuthType Type { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
}