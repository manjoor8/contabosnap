using System.Text.Json;
using ContaboSnapshotService.Configuration;
using ContaboSnapshotService.Models;
using Microsoft.Extensions.Options;

namespace ContaboSnapshotService.Services;

/// <summary>
/// Handles OAuth2 token retrieval and caching for the Contabo API.
/// Tokens are obtained via Resource Owner Password Credentials grant.
/// </summary>
public class ContaboAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ContaboSettings _settings;
    private readonly ILogger<ContaboAuthService> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ContaboAuthService(
        HttpClient httpClient,
        IOptions<ContaboSettings> settings,
        ILogger<ContaboAuthService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns a valid access token, refreshing it if expired or about to expire.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Return cached token if still valid (with 60s buffer)
        if (_cachedToken != null && DateTime.UtcNow.AddSeconds(60) < _tokenExpiry)
        {
            return _cachedToken;
        }

        _logger.LogInformation("Requesting new access token from Contabo OAuth2 provider...");

        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["username"] = _settings.ApiUser,
            ["password"] = _settings.ApiPassword,
            ["grant_type"] = "password"
        });

        var response = await _httpClient.PostAsync(_settings.AuthUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize token response.");

        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        _logger.LogInformation("Access token obtained successfully. Expires in {ExpiresIn}s", tokenResponse.ExpiresIn);

        return _cachedToken;
    }
}
