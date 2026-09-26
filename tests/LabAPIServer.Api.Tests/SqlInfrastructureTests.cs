using System.Text;
using LabAPIServer.Api.Data;

namespace LabAPIServer.Api.Tests;

public sealed class SqlInfrastructureTests
{
    [Fact]
    public void Migration_catalog_returns_ordered_scripts_with_sha256()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"labapi-migrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "0002_second.sql"), "SELECT 2;", Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "0001_first.sql"), "SELECT 1;", Encoding.UTF8);

            var scripts = MigrationScriptCatalog.Load(directory);

            Assert.Equal([1, 2], scripts.Select(script => script.VersionNumber));
            Assert.All(scripts, script => Assert.Matches("^[0-9a-f]{64}$", script.Sha256));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

}
