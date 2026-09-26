using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace LabAPIServer.Api.Data;

public sealed class SqlConnectionFactory(IOptions<DatabaseOptions> options) : ISqlConnectionFactory
{
    public SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        {
            throw new InvalidOperationException("Database connectivity is not configured for this environment.");
        }

        return new SqlConnection(options.Value.ConnectionString);
    }
}
