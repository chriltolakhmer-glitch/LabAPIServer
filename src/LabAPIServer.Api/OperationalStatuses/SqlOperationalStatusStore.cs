using System.Data;
using LabAPIServer.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace LabAPIServer.Api.OperationalStatuses;

public sealed class SqlOperationalStatusStore(ISqlConnectionFactory connectionFactory, IOptions<DatabaseOptions> databaseOptions) : IOperationalStatusStore
{
    public async Task<IReadOnlyList<OperationalStatusRow>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "app.OperationalStatuses_List");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var statuses = new List<OperationalStatusRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            statuses.Add(ReadStatus(reader));
        }

        return statuses;
    }

    public async Task<OperationalStatusRow?> GetAsync(string key, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "app.OperationalStatuses_Get");
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 100).Value = key;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadStatus(reader) : null;
    }

    public async Task<OperationalStatusUpdateResult> UpdateAsync(OperationalStatusRow status, byte[] expectedRowVersion, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "app.OperationalStatuses_Update");
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 100).Value = status.Key;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 32).Value = status.Status;
        command.Parameters.Add("@Value", SqlDbType.NVarChar, 200).Value = status.Value;
        command.Parameters.Add("@Severity", SqlDbType.NVarChar, 16).Value = status.Severity;
        command.Parameters.Add("@ObservedAtUtc", SqlDbType.DateTime2).Value = status.ObservedAtUtc.UtcDateTime;
        command.Parameters.Add("@UpdatedAtUtc", SqlDbType.DateTime2).Value = status.UpdatedAtUtc.UtcDateTime;
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 1024).Value = status.UpdatedBy;
        command.Parameters.Add("@ExpectedRowVersion", SqlDbType.Binary, 8).Value = expectedRowVersion;
        var resultParameter = command.Parameters.Add("@Result", SqlDbType.Int);
        resultParameter.Direction = ParameterDirection.Output;

        OperationalStatusRow? saved = null;
        await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                saved = ReadStatus(reader);
            }
        }

        var outcome = resultParameter.Value is int result
            ? (OperationalStatusUpdateOutcome)result
            : throw new InvalidOperationException("The operational status update procedure did not return an outcome.");
        return new(outcome, saved);
    }

    public async Task<IReadOnlyList<OperationalStatusHistoryRow>> GetHistoryAsync(string key, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "app.OperationalStatuses_History");
        command.Parameters.Add("@Key", SqlDbType.NVarChar, 100).Value = key;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var history = new List<OperationalStatusHistoryRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc))));
        }

        return history;
    }

    private SqlCommand CreateCommand(SqlConnection connection, string procedure)
        => new(procedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = databaseOptions.Value.CommandTimeoutSeconds
        };

    private static OperationalStatusRow ReadStatus(SqlDataReader reader)
    {
        var rowVersion = new byte[8];
        reader.GetBytes(8, 0, rowVersion, 0, 8);
        return new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
            reader.GetString(7),
            rowVersion);
    }
}
