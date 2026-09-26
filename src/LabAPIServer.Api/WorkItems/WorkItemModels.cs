using System.ComponentModel.DataAnnotations;

namespace LabAPIServer.Api.WorkItems;

public static class WorkItemStatuses
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
}

public sealed record WorkItemDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string CreatedBy,
    string UpdatedBy);

public sealed record CreateWorkItemRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required, StringLength(32)]
    public required string Status { get; init; }
}

public sealed record UpdateWorkItemRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required, StringLength(32)]
    public required string Status { get; init; }
}

public sealed record WorkItemRow(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string CreatedBy,
    string UpdatedBy);
