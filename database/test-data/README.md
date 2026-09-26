# LabAPIServer local test data

This fixture set is for the approved isolated SQL target only:

- Server: `DC01`
- Database: `LabAPIServer_Test`
- Authentication: Windows integrated authentication

The documented baseline is the six-row deterministic dataset. Run `Reset-LabAPIServer-Test.sql` followed by `Seed-LabAPIServer-Test.sql` to restore it.

## Safe workflow

From the repository root, using `sqlcmd.exe` and the explicitly approved target:

```powershell
sqlcmd -S "DC01" -d LabAPIServer_Test -E -b -i database/test-data/Reset-LabAPIServer-Test.sql
sqlcmd -S "DC01" -d LabAPIServer_Test -E -b -i database/test-data/Seed-LabAPIServer-Test.sql
sqlcmd -S "DC01" -d LabAPIServer_Test -E -b -i database/test-data/Reset-OperationalStatuses-Phase14.sql
sqlcmd -S "DC01" -d LabAPIServer_Test -E -b -Q "SELECT COUNT(*) AS SyntheticWorkItemCount FROM app.WorkItems WHERE WorkItemId IN ('11111111-1111-1111-1111-111111111101','11111111-1111-1111-1111-111111111102','11111111-1111-1111-1111-111111111103','11111111-1111-1111-1111-111111111104','11111111-1111-1111-1111-111111111105','11111111-1111-1111-1111-111111111106')"
```

The scripts must be run against exactly `DC01/LabAPIServer_Test`. They do not create or alter databases, schemas, tables, indexes, procedures, or migration metadata. Reset deletes only the six listed UUIDs and the exact Phase 14 status fixture plus its history. It does not delete by status, date, or a broad test-data query, and it cannot remove unknown rows.

These are synthetic records. Names begin with `[TEST DATA]`; descriptions contain no personal information. No password, token, private key, or production value is used.

## Dataset

| UUID suffix | Status | Description | Updated UTC |
| --- | --- | --- | --- |
| `101` | Open | Present | `2026-01-10T08:00:00Z` |
| `102` | InProgress | Absent | `2026-01-11T09:00:00Z` |
| `103` | Complete | Present | `2026-01-12T10:00:00Z` |
| `104` | Open | Absent | `2026-01-13T11:00:00Z` |
| `105` | InProgress | Present | `2026-01-14T12:00:00Z` |
| `106` | Complete | Absent | `2026-01-15T13:00:00Z` |

`CreatedAtUtc` is one hour before `UpdatedAtUtc` for each row. `CreatedBy` and `UpdatedBy` are the synthetic subject `test-data-fixture`.

## API integration

Use an external configuration file or environment-specific configuration with this connection string shape; do not commit it to source or fall back to LocalDB:

```text
Server=tcp:DC01,1433;Database=LabAPIServer_Test;Integrated Security=True;Encrypt=True;TrustServerCertificate=False
```

Run migrations first, then reset and seed. Start the API with the external configuration, use the existing in-memory RSA JWT mechanism for automated tests, and verify the HTTP CRUD and role matrix. After an integration run, restore the documented six-row baseline and run the Phase 14 reset when the cleanup convention for the run requires zero synthetic rows. Never point these scripts at `LabAuthServer`, a deployment database, or production.

The opt-in SQL HTTP test uses the normal stored-procedure stores and can be run from the repository root after migration, reset, and seed:

```powershell
$env:LABAPI_RUN_SQL_TESTS = '1'
dotnet test LabAPIServer.slnx -c Release --no-restore --filter 'FullyQualifiedName~LocalDbIntegrationTests'
Remove-Item Env:LABAPI_RUN_SQL_TESTS
```
