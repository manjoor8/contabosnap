using System.Text.Json.Serialization;

namespace ContaboSnapshotService.Models;

// ── Auth ──────────────────────────────────────────────────────────

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

// ── Pagination ───────────────────────────────────────────────────

public class PaginationMeta
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("totalElements")]
    public int TotalElements { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

// ── Instances ────────────────────────────────────────────────────

public class ListInstancesResponse
{
    [JsonPropertyName("_pagination")]
    public PaginationMeta? Pagination { get; set; }

    [JsonPropertyName("data")]
    public List<InstanceData> Data { get; set; } = [];
}

public class InstanceData
{
    [JsonPropertyName("instanceId")]
    public long InstanceId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
}

// ── Snapshots ────────────────────────────────────────────────────

public class ListSnapshotResponse
{
    [JsonPropertyName("_pagination")]
    public PaginationMeta? Pagination { get; set; }

    [JsonPropertyName("data")]
    public List<SnapshotData> Data { get; set; } = [];
}

public class SnapshotData
{
    [JsonPropertyName("snapshotId")]
    public string SnapshotId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("instanceId")]
    public long InstanceId { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("autoDeleteDate")]
    public DateTime AutoDeleteDate { get; set; }

    [JsonPropertyName("imageId")]
    public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("imageName")]
    public string ImageName { get; set; } = string.Empty;
}

public class CreateSnapshotRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class CreateSnapshotResponse
{
    [JsonPropertyName("data")]
    public List<SnapshotData> Data { get; set; } = [];
}
