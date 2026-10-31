namespace HyperManager.Models.Entity;

public sealed class OperationLogEntity
{
    public long Id { get; set; }

    public DateTime ExecutedAt { get; set; }

    public string UserId { get; set; } = default!;

    public string HostName { get; set; } = default!;

    public string VmId { get; set; } = default!;

    public string VmName { get; set; } = default!;

    // Name of VmOperation
    public string Operation { get; set; } = default!;

    public bool Success { get; set; }

    public string? Message { get; set; }
}
