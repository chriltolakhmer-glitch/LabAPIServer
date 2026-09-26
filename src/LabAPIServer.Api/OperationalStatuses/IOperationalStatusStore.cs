namespace LabAPIServer.Api.OperationalStatuses;

public interface IOperationalStatusStore
{
    Task<IReadOnlyList<OperationalStatusRow>> ListAsync(CancellationToken cancellationToken);

    Task<OperationalStatusRow?> GetAsync(string key, CancellationToken cancellationToken);

    Task<OperationalStatusUpdateResult> UpdateAsync(OperationalStatusRow status, byte[] expectedRowVersion, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalStatusHistoryRow>> GetHistoryAsync(string key, CancellationToken cancellationToken);
}