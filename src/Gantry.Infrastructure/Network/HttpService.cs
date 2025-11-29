using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gantry.Core.Domain.Http;
using Gantry.Core.Interfaces;

namespace Gantry.Infrastructure.Network;

public class HttpService : IHttpService
{
    // 5MB limit for text preview to prevent UI crashing
    private const long MaxBodyTextSize = 5 * 1024 * 1024;

    public async Task<ResponseModel> SendRequestAsync(RequestModel request, CancellationToken cancellationToken = default)
    {
        var handler = CreateHandler(request);
        using var client = new HttpClient(handler);

        // Prevent timeout from being handled by HttpClient so we can report it accurately
        client.Timeout = TimeSpan.FromMilliseconds(request.TimeoutMs > 0 ? request.TimeoutMs : 100000);

        using var httpRequest = CreateHttpRequestMessage(request);
        ApplyAuthentication(httpRequest, request.Auth);

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;

        try
        {
            // This prevents waiting for the whole body to download before we get status code
            response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Capture TTFB
            var ttfb = stopwatch.Elapsed;

            // Read Body
            var (bodyString, size) = await ReadBodySafeAsync(response.Content, cancellationToken);

            stopwatch.Stop();

            return new ResponseModel
            {
                StatusCode = (int)response.StatusCode,
                StatusText = response.ReasonPhrase ?? response.StatusCode.ToString(),
                Headers = response.Headers.Concat(response.Content.Headers)
                                          .ToDictionary(k => k.Key, v => string.Join(", ", v.Value)),
                Body = bodyString,
                Duration = stopwatch.Elapsed, // Total duration
                TimeToFirstByte = ttfb,
                Size = size
            };
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return new ResponseModel { StatusCode = 408, Body = "Error: Request Timed Out", Duration = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ResponseModel { StatusCode = 0, Body = $"Error: {ex.Message}", Duration = stopwatch.Elapsed };
        }
        finally
        {
            response?.Dispose();
        }
    }

    private SocketsHttpHandler CreateHandler(RequestModel request)
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = request.AutomaticallyFollowRedirects,
            MaxAutomaticRedirections = request.MaximumNumberOfRedirects,
            UseCookies = !request.DisableCookieJar,
            AutomaticDecompression = DecompressionMethods.All,
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = GetEnabledSslProtocols(request.DisabledTlsProtocols),
                RemoteCertificateValidationCallback = request.EnableSslCertificateVerification
                    ? null // Use default validation
                    : (sender, cert, chain, errors) => true // Dangerous: Accept all
            }
        };
    }

    private HttpRequestMessage CreateHttpRequestMessage(RequestModel request)
    {
        var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(request.Url),
            Method = new HttpMethod(request.Method),
            Version = Version.TryParse(request.HttpVersion.Replace("HTTP/", ""), out var v) ? v : HttpVersion.Version11
        };

        string? contentType = null;

        foreach (var header in request.Headers)
        {
            // Capture Content-Type to use in StringContent, but don't add to Headers collection yet
            // HttpClient validation throws if you add Content-Type to request headers directly
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentType = header.Value;
            }
            else
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrEmpty(request.Body))
        {
            // Default to application/json if not specified
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType ?? "application/json");
        }

        return httpRequest;
    }

    private async Task<(string Content, long Size)> ReadBodySafeAsync(HttpContent content, CancellationToken token)
    {
        // Check content length header first
        if (content.Headers.ContentLength > MaxBodyTextSize)
        {
            return ($"Preview unavailable: Response size ({content.Headers.ContentLength} bytes) exceeds limit.", content.Headers.ContentLength ?? 0);
        }

        // Read as byte array first to get true size
        var bytes = await content.ReadAsByteArrayAsync(token);
        var size = bytes.LongLength;

        if (size > MaxBodyTextSize)
        {
            return ($"Preview unavailable: Response size ({size} bytes) exceeds limit.", size);
        }

        return (Encoding.UTF8.GetString(bytes), size);
    }

    private void ApplyAuthentication(HttpRequestMessage request, AuthConfig? auth)
    {
        if (auth == null || auth.Type == Gantry.Core.Domain.Settings.AuthType.None) return;

        // Uses AuthenticationHeaderValue for stricter RFC compliance
        switch (auth.Type)
        {
            case Gantry.Core.Domain.Settings.AuthType.Basic:
                if (!string.IsNullOrEmpty(auth.Username))
                {
                    var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
                }
                break;
            case Gantry.Core.Domain.Settings.AuthType.BearerToken:
                if (!string.IsNullOrEmpty(auth.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                }
                break;
        }
    }

#pragma warning disable SYSLIB0039 
    private SslProtocols GetEnabledSslProtocols(System.Collections.Generic.List<string> disabledProtocols)
    {
        var protocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        if (!disabledProtocols.Contains("TLS 1.1")) protocols |= SslProtocols.Tls11;
        if (!disabledProtocols.Contains("TLS 1.0")) protocols |= SslProtocols.Tls;
        if (disabledProtocols.Contains("TLS 1.3")) protocols &= ~SslProtocols.Tls13;
        if (disabledProtocols.Contains("TLS 1.2")) protocols &= ~SslProtocols.Tls12;
        return protocols == SslProtocols.None ? SslProtocols.Tls12 : protocols;
    }
#pragma warning restore SYSLIB0039
}