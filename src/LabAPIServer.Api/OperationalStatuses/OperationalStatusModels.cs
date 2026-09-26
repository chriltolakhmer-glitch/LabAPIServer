using System.ComponentModel.DataAnnotations;

namespace LabAPIServer.Api.OperationalStatuses;

public static class OperationalStatusSeverities
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Critical = "Critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Info,
        Warning,
        Critical
    };
}

public static class OperationalStatusLifecycle
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Complete = "Complete";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Open,
        InProgress,
        Complete
    };

    public static bool IsValidTransition(string previousStatus, string nextStatus)
        => (previousStatus, nextStatus) switch
        {
            (Open, InProgress) => true,
            (InProgress, Complete) => true,
            _ => false
        };
}

public sealed record OperationalStatusDto(
    Guid Id,
    string Key,
    string Status,
    string Value,
    string Severity,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string UpdatedBy,
    string RowVersion);

public sealed record UpdateOperationalStatusRequest
{
    [Required, StringLength(32)]
    public required string Status { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Value { get; init; }

    [Required, StringLength(16)]
    public required string Severity { get; init; }

    [Required]
    public required string RowVersion { get; init; }
}

public sealed record OperationalStatusRow(
    Guid Id,
    string Key,
    string Status,
    string Value,
    string Severity,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string UpdatedBy,
    byte[] RowVersion);

public sealed record OperationalStatusHistoryDto(
    Guid Id,
    Guid StatusId,
    string PreviousStatus,
    string NewStatus,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);

public sealed record OperationalStatusHistoryRow(
    Guid Id,
    Guid StatusId,
    string PreviousStatus,
    string NewStatus,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);

public enum OperationalStatusUpdateOutcome
{
    Updated,
    NotFound,
    Conflict,
    InvalidTransition
}

public sealed record OperationalStatusUpdateResult(
    OperationalStatusUpdateOutcome Outcome,
    OperationalStatusRow? Status);