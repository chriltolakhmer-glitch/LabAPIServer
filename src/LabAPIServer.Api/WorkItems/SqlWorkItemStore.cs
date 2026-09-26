using System.Data;
using LabAPIServer.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace LabAPIServer.Api.WorkItems;

public sealed class SqlWorkItemStore(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> databaseOptions) : IWorkItemStore
{
    private const string ListProcedure = "app.WorkItems_List";
    private const string GetProcedure = "app.WorkItems_Get";
    private const string CreateProcedure = "app.WorkItems_Create";
    private const string UpdateProcedure = "app.WorkItems_Update";
    private const string DeleteProcedure = "app.WorkItems_Delete";

    public async Task<IReadOnlyList<WorkItemRow>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, ListProcedure);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<WorkItemRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadItem(reader));
        }

        return items;
    }

    public async Task<WorkItemRow?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, GetProcedure);
        command.Parameters.Add("@WorkItemId", System.Data.SqlDbType.UniqueIdentifier).Value = id;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadItem(reader) : null;
    }

    public async Task<WorkItemRow> CreateAsync(WorkItemRow item, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, CreateProcedure);
        AddParameters(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return item;
    }

    public async Task<bool> UpdateAsync(WorkItemRow item, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, UpdateProcedure);
        AddUpdateParameters(command, item);
        return await ExecuteAffectedRowsAsync(command, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, DeleteProcedure);
        command.Parameters.Add("@WorkItemId", System.Data.SqlDbType.UniqueIdentifier).Value = id;
        return await ExecuteAffectedRowsAsync(command, cancellationToken);
    }

    private SqlCommand CreateCommand(SqlConnection connection, string commandText)
        => new(commandText, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = databaseOptions.Value.CommandTimeoutSeconds
        };

    private static void AddParameters(SqlCommand command, WorkItemRow item)
    {
        command.Parameters.Add("@WorkItemId", System.Data.SqlDbType.UniqueIdentifier).Value = item.Id;
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 200).Value = item.Name;
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 2000).Value = (object?)item.Description ?? DBNull.Value;
        command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 32).Value = item.Status;
        command.Parameters.Add("@CreatedAtUtc", System.Data.SqlDbType.DateTime2).Value = item.CreatedAtUtc.UtcDateTime;
        command.Parameters.Add("@UpdatedAtUtc", System.Data.SqlDbType.DateTime2).Value = item.UpdatedAtUtc.UtcDateTime;
        command.Parameters.Add("@CreatedBy", System.Data.SqlDbType.NVarChar, 1024).Value = item.CreatedBy;
        command.Parameters.Add("@UpdatedBy", System.Data.SqlDbType.NVarChar, 1024).Value = item.UpdatedBy;
    }

    private static void AddUpdateParameters(SqlCommand command, WorkItemRow item)
    {
        command.Parameters.Add("@WorkItemId", System.Data.SqlDbType.UniqueIdentifier).Value = item.Id;
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 200).Value = item.Name;
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 2000).Value = (object?)item.Description ?? DBNull.Value;
        command.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar, 32).Value = item.Status;
        command.Parameters.Add("@UpdatedAtUtc", System.Data.SqlDbType.DateTime2).Value = item.UpdatedAtUtc.UtcDateTime;
        command.Parameters.Add("@UpdatedBy", System.Data.SqlDbType.NVarChar, 1024).Value = item.UpdatedBy;
    }

    private static async Task<bool> ExecuteAffectedRowsAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private static WorkItemRow ReadItem(SqlDataReader reader)
        => new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)),
            reader.GetString(6),
            reader.GetString(7));
}
