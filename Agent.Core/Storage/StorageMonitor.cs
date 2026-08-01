using Agent.Core.Models;

namespace Agent.Core.Storage;

public class StorageMonitor : IStorageMonitor
{
    public Task<StorageMetrics> GetStorageMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new StorageMetrics();

        try
        {
            var driveInfos = DriveInfo.GetDrives();

            foreach (var drive in driveInfos)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var driveMetric = new DriveMetrics
                {
                    Name = drive.Name,
                    DriveType = drive.DriveType.ToString(),
                    IsReady = drive.IsReady
                };

                if (drive.IsReady)
                {
                    try
                    {
                        driveMetric.Label = drive.VolumeLabel;
                        driveMetric.DriveFormat = drive.DriveFormat;
                        driveMetric.TotalSizeBytes = drive.TotalSize;
                        driveMetric.FreeSizeBytes = drive.AvailableFreeSpace;

                        metrics.TotalStorageBytes += drive.TotalSize;
                        metrics.TotalFreeStorageBytes += drive.AvailableFreeSpace;
                    }
                    catch
                    {
                        // Drive ready state changed or access restriction
                    }
                }

                metrics.Drives.Add(driveMetric);
            }
        }
        catch
        {
            // Fallback for restricted security sandbox
        }

        return Task.FromResult(metrics);
    }
}
