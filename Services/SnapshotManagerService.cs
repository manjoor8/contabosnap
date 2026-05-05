namespace ContaboSnapshotService.Services;

/// <summary>
/// Orchestrates the snapshot rotation workflow:
/// 1. Get all instances
/// 2. For each instance, delete all existing snapshots
/// 3. Create a fresh snapshot for each instance
/// </summary>
public class SnapshotManagerService
{
    private readonly ContaboApiService _apiService;
    private readonly ILogger<SnapshotManagerService> _logger;

    public SnapshotManagerService(
        ContaboApiService apiService,
        ILogger<SnapshotManagerService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// Runs the full snapshot rotation for all instances.
    /// </summary>
    public async Task RunSnapshotRotationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        _logger.LogInformation("  Starting Contabo Snapshot Rotation - {Time}", DateTime.Now);
        _logger.LogInformation("═══════════════════════════════════════════════════════════");

        try
        {
            // Step 1: Get all instances
            var instances = await _apiService.GetAllInstancesAsync(cancellationToken);

            if (instances.Count == 0)
            {
                _logger.LogWarning("No instances found. Nothing to do.");
                return;
            }

            _logger.LogInformation("Found {Count} instance(s). Processing snapshots...", instances.Count);

            int successCount = 0;
            int failCount = 0;

            foreach (var instance in instances)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogInformation("───────────────────────────────────────────────────");
                    _logger.LogInformation("Processing instance: {Name} (ID: {InstanceId}, Status: {Status})",
                        instance.DisplayName, instance.InstanceId, instance.Status);

                    // Step 2: Delete existing snapshots
                    await DeleteExistingSnapshotsAsync(instance.InstanceId, cancellationToken);

                    // Step 3: Create new snapshot
                    var snapshotName = $"Auto-{DateTime.UtcNow:yyyyMMdd}";
                    var description = $"Automated weekly snapshot created on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

                    await _apiService.CreateSnapshotAsync(
                        instance.InstanceId,
                        snapshotName,
                        description,
                        cancellationToken);

                    successCount++;
                    _logger.LogInformation("✓ Successfully rotated snapshot for instance {InstanceId}", instance.InstanceId);
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "✗ Failed to process instance {InstanceId} ({Name})",
                        instance.InstanceId, instance.DisplayName);
                }
            }

            _logger.LogInformation("═══════════════════════════════════════════════════════════");
            _logger.LogInformation("  Snapshot Rotation Complete: {Success} succeeded, {Failed} failed",
                successCount, failCount);
            _logger.LogInformation("═══════════════════════════════════════════════════════════");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Snapshot rotation was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during snapshot rotation");
            throw;
        }
    }

    /// <summary>
    /// Deletes all existing snapshots for a given instance.
    /// </summary>
    private async Task DeleteExistingSnapshotsAsync(long instanceId, CancellationToken cancellationToken)
    {
        var snapshots = await _apiService.GetSnapshotsAsync(instanceId, cancellationToken);

        if (snapshots.Count == 0)
        {
            _logger.LogInformation("  No existing snapshots found for instance {InstanceId}", instanceId);
            return;
        }

        _logger.LogInformation("  Found {Count} existing snapshot(s) to delete for instance {InstanceId}",
            snapshots.Count, instanceId);

        foreach (var snapshot in snapshots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("  Deleting snapshot: {Name} (ID: {SnapshotId}, Created: {Created})",
                snapshot.Name, snapshot.SnapshotId, snapshot.CreatedDate);

            await _apiService.DeleteSnapshotAsync(instanceId, snapshot.SnapshotId, cancellationToken);

            // Small delay between delete operations to avoid rate-limiting
            await Task.Delay(1000, cancellationToken);
        }

        _logger.LogInformation("  All existing snapshots deleted for instance {InstanceId}", instanceId);
    }
}
