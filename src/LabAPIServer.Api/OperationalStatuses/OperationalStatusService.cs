namespace LabAPIServer.Api.OperationalStatuses;

public sealed class OperationalStatusService(IOperationalStatusStore store, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<OperationalStatusDto>> ListAsync(CancellationToken cancellationToken)
        => (await store.ListAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<OperationalStatusDto?> GetAsync(string key, CancellationToken cancellationToken)
        => (await store.GetAsync(key, cancellationToken)) is { } status ? ToDto(status) : null;

    public async Task<(IReadOnlyList<OperationalStatusHistoryDto> History, bool NotFound)> GetHistoryAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (await store.GetAsync(key, cancellationToken) is null)
        {
            return ([], true);
        }

        var history = await store.GetHistoryAsync(key, cancellationToken);
        return (history.Select(ToDto).ToArray(), false);
    }

    public async Task<(OperationalStatusDto? Status, bool NotFound, bool Conflict, bool InvalidTransition)> UpdateAsync(
        string key,
        UpdateOperationalStatusRequest request,
        string subject,
        CancellationToken cancellationToken)
    {
        var existing = await store.GetAsync(key, cancellationToken);
        if (existing is null)
        {
            return (null, true, false, false);
        }

        OperationalStatusRequestValidator.TryDecodeRowVersion(request.RowVersion, out var rowVersion);
        if (!OperationalStatusLifecycle.IsValidTransition(existing.Status, request.Status))
        {
            return (null, false, false, true);
        }

        var now = timeProvider.GetUtcNow();
        var updated = existing with
        {
            Status = request.Status,
            Value = request.Value.Trim(),
            Severity = request.Severity,
            ObservedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedBy = subject
        };
        var result = await store.UpdateAsync(updated, rowVersion, cancellationToken);
        return result.Outcome switch
        {
            OperationalStatusUpdateOutcome.Updated => (ToDto(result.Status!), false, false, false),
            OperationalStatusUpdateOutcome.NotFound => (null, true, false, false),
            OperationalStatusUpdateOutcome.InvalidTransition => (null, false, false, true),
            _ => (null, false, true, false)
        };
    }

    private static OperationalStatusDto ToDto(OperationalStatusRow status)
        => new(status.Id, status.Key, status.Status, status.Value, status.Severity, status.ObservedAtUtc, status.UpdatedAtUtc, status.UpdatedBy, Convert.ToBase64String(status.RowVersion));

    private static OperationalStatusHistoryDto ToDto(OperationalStatusHistoryRow history)
        => new(history.Id, history.StatusId, history.PreviousStatus, history.NewStatus, history.ChangedBy, history.ChangedAtUtc);
}
