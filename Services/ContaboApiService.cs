using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContaboSnapshotService.Configuration;
using ContaboSnapshotService.Models;
using Microsoft.Extensions.Options;

namespace ContaboSnapshotService.Services;

/// <summary>
/// Handles API calls to Contabo for managing instances and snapshots.
/// All requests include the required x-request-id header (UUID4) and Bearer token.
/// </summary>
public class ContaboApiService
{
    private readonly HttpClient _httpClient;
    private readonly ContaboAuthService _authService;
    private readonly ContaboSettings _settings;
    private readonly ILogger<ContaboApiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ContaboApiService(
        HttpClient httpClient,
        ContaboAuthService authService,
        IOptions<ContaboSettings> settings,
        ILogger<ContaboApiService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _settings = settings.Value;
        _logger = logger;
    }

    // ── Instances ────────────────────────────────────────────────

    /// <summary>
    /// Retrieves all compute instances across all pages.
    /// GET /v1/compute/instances
    /// </summary>
    public async Task<List<InstanceData>> GetAllInstancesAsync(CancellationToken cancellationToken = default)
    {
        var allInstances = new List<InstanceData>();
        int page = 1;
        int totalPages;

        do
        {
            var url = $"{_settings.ApiBaseUrl}/v1/compute/instances?page={page}&size=100";
            var request = await CreateAuthorizedRequest(HttpMethod.Get, url, cancellationToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ListInstancesResponse>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize instances response.");

            allInstances.AddRange(result.Data);
            totalPages = result.Pagination?.TotalPages ?? 1;
            page++;

        } while (page <= totalPages);

        _logger.LogInformation("Retrieved {Count} instances total", allInstances.Count);
        return allInstances;
    }

    // ── Snapshots ────────────────────────────────────────────────

    /// <summary>
    /// Lists all snapshots for a given instance across all pages.
    /// GET /v1/compute/instances/{instanceId}/snapshots
    /// </summary>
    public async Task<List<SnapshotData>> GetSnapshotsAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        var allSnapshots = new List<SnapshotData>();
        int page = 1;
        int totalPages;

        do
        {
            var url = $"{_settings.ApiBaseUrl}/v1/compute/instances/{instanceId}/snapshots?page={page}&size=100";
            var request = await CreateAuthorizedRequest(HttpMethod.Get, url, cancellationToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ListSnapshotResponse>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize snapshots response.");

            allSnapshots.AddRange(result.Data);
            totalPages = result.Pagination?.TotalPages ?? 1;
            page++;

        } while (page <= totalPages);

        return allSnapshots;
    }

    /// <summary>
    /// Deletes a specific snapshot for an instance.
    /// DELETE /v1/compute/instances/{instanceId}/snapshots/{snapshotId}
    /// </summary>
    public async Task DeleteSnapshotAsync(long instanceId, string snapshotId, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.ApiBaseUrl}/v1/compute/instances/{instanceId}/snapshots/{snapshotId}";
        var request = await CreateAuthorizedRequest(HttpMethod.Delete, url, cancellationToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Deleted snapshot {SnapshotId} for instance {InstanceId}", snapshotId, instanceId);
    }

    /// <summary>
    /// Creates a new snapshot for an instance.
    /// POST /v1/compute/instances/{instanceId}/snapshots
    /// </summary>
    public async Task<SnapshotData?> CreateSnapshotAsync(long instanceId, string name, string? description = null, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.ApiBaseUrl}/v1/compute/instances/{instanceId}/snapshots";
        var request = await CreateAuthorizedRequest(HttpMethod.Post, url, cancellationToken);

        var body = new CreateSnapshotRequest
        {
            Name = name,
            Description = description
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<CreateSnapshotResponse>(json, JsonOptions);

        var snapshot = result?.Data.FirstOrDefault();
        _logger.LogInformation("Created snapshot '{Name}' (ID: {SnapshotId}) for instance {InstanceId}",
            name, snapshot?.SnapshotId ?? "unknown", instanceId);

        return snapshot;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private async Task<HttpRequestMessage> CreateAuthorizedRequest(
        HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("x-request-id", Guid.NewGuid().ToString());
        return request;
    }
}
