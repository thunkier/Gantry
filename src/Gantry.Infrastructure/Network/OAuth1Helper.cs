using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Gantry.Infrastructure.Network;

public static class OAuth1Helper
{
    public static string GetAuthorizationHeader(string url, string method, Dictionary<string, string> authAttributes)
    {
        var consumerKey = authAttributes.GetValueOrDefault("consumerKey", "");
        var consumerSecret = authAttributes.GetValueOrDefault("consumerSecret", "");
        var token = authAttributes.GetValueOrDefault("token", "");
        var tokenSecret = authAttributes.GetValueOrDefault("tokenSecret", "");
        var signatureMethod = authAttributes.GetValueOrDefault("signatureMethod", "HMAC-SHA1");
        var timestamp = authAttributes.GetValueOrDefault("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        var nonce = authAttributes.GetValueOrDefault("nonce", Guid.NewGuid().ToString("N"));
        var version = authAttributes.GetValueOrDefault("version", "1.0");
        var realm = authAttributes.GetValueOrDefault("realm", "");

        var oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", consumerKey },
            { "oauth_nonce", nonce },
            { "oauth_signature_method", signatureMethod },
            { "oauth_timestamp", timestamp },
            { "oauth_token", token },
            { "oauth_version", version }
        };

        // 1. Collect parameters (Query params + OAuth params)
        // For simplicity, we are assuming no query params in URL for now or they are handled separately. 
        // Ideally, we should parse URL query params and merge them.
        // TODO: Parse URL query params and add to signature base string if needed.

        // 2. Normalize parameters
        var sortedParams = oauthParams.OrderBy(k => k.Key).ThenBy(k => k.Value);
        var parameterString = string.Join("&", sortedParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        // 3. Create Signature Base String
        var signatureBaseString = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(parameterString)}";

        // 4. Calculate Signature
        var signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";
        string signature;

        using (var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
        {
            var hashBytes = hasher.ComputeHash(Encoding.ASCII.GetBytes(signatureBaseString));
            signature = Convert.ToBase64String(hashBytes);
        }

        oauthParams.Add("oauth_signature", signature);

        // 5. Construct Header
        var headerBuilder = new StringBuilder();
        headerBuilder.Append("OAuth ");
        if (!string.IsNullOrEmpty(realm))
        {
            headerBuilder.Append($"realm=\"{Uri.EscapeDataString(realm)}\",");
        }

        foreach (var param in oauthParams)
        {
            headerBuilder.Append($"{Uri.EscapeDataString(param.Key)}=\"{Uri.EscapeDataString(param.Value)}\",");
        }

        return headerBuilder.ToString().TrimEnd(',');
    }

    private static string GetValueOrDefault(this Dictionary<string, string> dict, string key, string defaultValue)
    {
        return dict.TryGetValue(key, out var val) ? val : defaultValue;
    }
}
