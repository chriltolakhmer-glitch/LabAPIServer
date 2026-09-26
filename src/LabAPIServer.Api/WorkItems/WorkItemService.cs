namespace LabAPIServer.Api.WorkItems;

public sealed class WorkItemService(
    IWorkItemStore store,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<WorkItemDto>> ListAsync(CancellationToken cancellationToken)
        => (await store.ListAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<WorkItemDto?> GetAsync(Guid id, CancellationToken cancellationToken)
        => (await store.GetAsync(id, cancellationToken)) is { } item ? ToDto(item) : null;

    public async Task<WorkItemDto> CreateAsync(CreateWorkItemRequest request, string subject, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var item = new WorkItemRow(Guid.NewGuid(), request.Name.Trim(), request.Description?.Trim(), request.Status, now, now, subject, subject);
        return ToDto(await store.CreateAsync(item, cancellationToken));
    }

    public async Task<WorkItemDto?> UpdateAsync(Guid id, UpdateWorkItemRequest request, string subject, CancellationToken cancellationToken)
    {
        var existing = await store.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = existing with
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            UpdatedAtUtc = timeProvider.GetUtcNow(),
            UpdatedBy = subject
        };

        return await store.UpdateAsync(updated, cancellationToken) ? ToDto(updated) : null;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        => store.DeleteAsync(id, cancellationToken);

    private static WorkItemDto ToDto(WorkItemRow item)
        => new(item.Id, item.Name, item.Description, item.Status, item.CreatedAtUtc, item.UpdatedAtUtc, item.CreatedBy, item.UpdatedBy);
}
