using System.Security.Cryptography;
using System.Text;

namespace LabAPIServer.Api.Data;

public sealed record MigrationScript(int VersionNumber, string Name, string Path, string Sha256);

public static class MigrationScriptCatalog
{
    public static IReadOnlyList<MigrationScript> Load(string migrationsDirectory)
    {
        if (!Directory.Exists(migrationsDirectory))
        {
            return [];
        }

        var scripts = Directory.EnumerateFiles(migrationsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(path => CreateScript(path))
            .OrderBy(script => script.VersionNumber)
            .ToArray();

        var duplicateVersions = scripts
            .GroupBy(script => script.VersionNumber)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateVersions is not null)
        {
            throw new InvalidOperationException($"Migration version {duplicateVersions.Key} is declared more than once.");
        }

        return scripts;
    }

    private static MigrationScript CreateScript(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var separatorIndex = fileName.IndexOf('_');
        if (separatorIndex <= 0 || !int.TryParse(fileName[..separatorIndex], out var versionNumber))
        {
            throw new InvalidOperationException($"Migration file '{Path.GetFileName(path)}' must use the format 0001_name.sql.");
        }

        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        return new MigrationScript(versionNumber, fileName, path, hash);
    }
}
