using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace LabAPIServer.Api.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string? ConnectionString { get; init; }

    [Range(1, 60)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}

public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Success;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(options.ConnectionString);
            if (!builder.Encrypt)
            {
                return ValidateOptionsResult.Fail("Database:ConnectionString must set Encrypt=True.");
            }

            if (builder.TrustServerCertificate)
            {
                return ValidateOptionsResult.Fail("Database:ConnectionString must set TrustServerCertificate=False.");
            }

            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                return ValidateOptionsResult.Fail("Database:ConnectionString must specify an Initial Catalog.");
            }
        }
        catch (ArgumentException exception)
        {
            return ValidateOptionsResult.Fail($"Database:ConnectionString is invalid: {exception.Message}");
        }

        return ValidateOptionsResult.Success;
    }
}
