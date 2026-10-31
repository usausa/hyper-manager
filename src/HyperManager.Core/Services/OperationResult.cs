namespace HyperManager.Services;

// Error is set when the operation failed
public sealed record OperationResult(bool Success, string? Error);
