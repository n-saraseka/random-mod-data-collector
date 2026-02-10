using System.Text;
using System.Text.Json;
using osu.Game.Beatmaps;
using osu.Game.IO;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using random_mod_data_collector.Entities;

namespace random_mod_data_collector;

public class OsuApiService(
    IConfiguration config,
    RateLimiter limiter)
{
    private static TokenInfo _token;
    private static readonly SemaphoreSlim TokenSemaphore = new(1, 1);
    
    /// <summary>
    /// Sends a request to the API within the osu! API rate limit
    /// </summary>
    /// <param name="method">HTTP method (either HttpMethod.Get or HttpMethod.Post)</param>
    /// <param name="requestString">Request URL</param>
    /// <param name="content">Request content (for Post requests)</param>
    /// <param name="isTokenRequest">Whether the request is a token request or not</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Request response text</returns>
    private async Task<string> SendRequestAsync(HttpMethod method, 
        string requestString, 
        HttpContent? content, 
        bool isTokenRequest = false, 
        CancellationToken ct = default)
    {
        using (var client = new HttpClient())
        {
            var requestMessage = new HttpRequestMessage(method, requestString);
            requestMessage.Content = content;
            if (!isTokenRequest)
            {
                var tokenData = await GetValidTokenAsync(ct);
                requestMessage.Headers.Add("Authorization", "Bearer " + tokenData.AccessToken);
                requestMessage.Headers.Add("x-api-version", config["ApiVersion"]);
            }
            var responseText = "";
        
            while (!ct.IsCancellationRequested)
            {
                using var lease = await limiter.AcquireAsync(cancellationToken: ct);
                if (lease.IsAcquired)
                {
                    try
                    {
                        var response = await client.SendAsync(requestMessage, ct);
                        response.EnsureSuccessStatusCode();
                        responseText = await response.Content.ReadAsStringAsync(ct);
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine(ex.StatusCode);
                        throw;
                    }

                    break;
                }
                else
                {
                    if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        await Task.Delay(retryAfter, ct);
                    else
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
            }
            return responseText;
        }
    }
    
    /// <summary>
    /// Set fresh token data for API access
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    private async Task SetTokenAsync(CancellationToken ct = default)
    {
        var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var data = new Dictionary<string, string>();
        data.Add("client_id", config["ApiId"]);
        data.Add("client_secret", config["ApiSecret"]);
        data.Add("grant_type", "client_credentials");
        data.Add("scope", "public");
        var dataJson = JsonSerializer.Serialize<Dictionary<string, string>>(data);
        
        // getting the token
        var tokenResponse = await SendRequestAsync(HttpMethod.Post, 
            config["ApiTokenUrl"], 
            new StringContent(dataJson, Encoding.UTF8, "application/json"),
            true,
            ct);

        // writing new token data
        _token = JsonSerializer.Deserialize<TokenInfo>(tokenResponse);
        _token.ExpiresIn += seconds;
    }
    
    /// <summary>
    /// Download a map from the API and decode it into a Beatmap object
    /// </summary>
    /// <param name="beatmapId">Beatmap ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Parsed Beatmap object</returns>
    public async Task<Beatmap?> GetScoreBeatmapAsync(int beatmapId, CancellationToken ct = default)
    {
        Beatmap beatmap = null;
        using (var client = new HttpClient())
        {
            while (!ct.IsCancellationRequested)
            {
                using var lease = await limiter.AcquireAsync(cancellationToken: ct);
                if (lease.IsAcquired)
                {
                    try
                    {
                        using var stream = await client.GetStreamAsync($"https://osu.ppy.sh/osu/{beatmapId}", ct);
                        using var reader = new LineBufferedReader(stream);
                        beatmap = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
                        return beatmap;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine(ex.StatusCode);
                        throw;
                    }
                }
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    await Task.Delay(retryAfter, ct);
                else
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }

        return beatmap;
    }

    /// <summary>
    /// Check if token has expired
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns></returns>
    private async Task<TokenInfo> GetValidTokenAsync(CancellationToken ct)
    {
        await TokenSemaphore.WaitAsync(ct);
        try
        {
            var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (_token == null || seconds > _token.ExpiresIn - 60)
                await SetTokenAsync(ct);
            return _token;
        }
        finally
        {
            TokenSemaphore.Release();
        }
    }
}