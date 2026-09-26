namespace LabAPIServer.Api.WorkItems;

public interface IWorkItemStore
{
    Task<IReadOnlyList<WorkItemRow>> ListAsync(CancellationToken cancellationToken);

    Task<WorkItemRow?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<WorkItemRow> CreateAsync(WorkItemRow item, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(WorkItemRow item, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
