namespace Agent.Core.Models;

public class DriveMetrics
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DriveFormat { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public long FreeSizeBytes { get; set; }
    public long UsedSizeBytes => TotalSizeBytes - FreeSizeBytes;
    public double UsagePercentage => TotalSizeBytes > 0 ? ((double)UsedSizeBytes / TotalSizeBytes) * 100.0 : 0.0;
    public bool IsReady { get; set; }
}

public class StorageMetrics
{
    public List<DriveMetrics> Drives { get; set; } = new();
    public long TotalStorageBytes { get; set; }
    public long TotalFreeStorageBytes { get; set; }
    public double OverallStorageUsagePercentage => TotalStorageBytes > 0 ? ((double)(TotalStorageBytes - TotalFreeStorageBytes) / TotalStorageBytes) * 100.0 : 0.0;
}
