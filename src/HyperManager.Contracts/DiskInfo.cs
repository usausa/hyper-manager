namespace HyperManager.Contracts;

public sealed record DiskInfo(
    string Name,
    long TotalBytes,
    long FreeBytes);
