using Microsoft.Data.SqlClient;

namespace LabAPIServer.Api.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
