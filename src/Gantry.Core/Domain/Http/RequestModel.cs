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
    public HttpVersionPolicy VersionPolicy { get; set; } = HttpVersionPolicy.RequestVersionOrLower;
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

    // Postman Specific
    public ProxyConfig? Proxy { get; set; }
    public CertificateConfig? Certificate { get; set; }
}

public class AuthConfig
{
    public Gantry.Core.Domain.Settings.AuthType Type { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}

public class ProxyConfig
{
    public string Match { get; set; } = "http+https://*/*";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public bool Tunnel { get; set; }
    public bool Disabled { get; set; }
}

public class CertificateConfig
{
    public string Name { get; set; } = string.Empty;
    public List<string> Matches { get; set; } = new();
    public string KeySrc { get; set; } = string.Empty;
    public string CertSrc { get; set; } = string.Empty;
    public string Passphrase { get; set; } = string.Empty;
}

public enum HttpVersionPolicy
{
    RequestVersionOrLower,
    RequestVersionOrHigher,
    RequestVersionExact
}