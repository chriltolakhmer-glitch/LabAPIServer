# API Database Foundation

The API owns its future application database schema. SQL Server is the external database technology; Web has no SQL dependency and Auth's audit database remains separate.

## Current status

`WORK-ITEM REGISTER SCHEMA AND STORED PROCEDURES VERIFIED — LOCAL TEST DATABASE`

Phase 4 defines the single `app.WorkItems` table and its stored-procedure data-access boundary in `migrations/0001_WorkItems.sql`. The migration and API data path were verified against isolated local `MSSQLLocalDB` database `LabAPIServer_Test`. The documented local baseline is the six-row synthetic fixture in `test-data/`.

## Configuration

Set `Database:ConnectionString` through an external development or deployment configuration source. The checked-in value is empty by design. A configured connection string must specify an intended database and use:

```text
Encrypt=True;TrustServerCertificate=False
```

`Database:CommandTimeoutSeconds` is bounded to 1-60 seconds and defaults to 30. No runtime connection is attempted when the setting is empty, allowing the application health endpoint to remain independent of SQL.

## Migrations

Migration scripts belong in `database/migrations/` and use immutable, ordered names such as `0001_<purpose>.sql`. The migration runner must apply scripts in order, record version/name/checksum/applied UTC in its ledger, reject changed checksums, acquire an exclusive migration lock, and stop safely on failure.

The runtime application will not apply DDL and will not receive migration privileges. Migration execution used the local development/test target `LabAPIServer_Test` with Windows integrated authentication. Production and the separate local `LabAuthServer` database were not accessed.

## Work-item stored procedures

The API calls these procedures with `CommandType.StoredProcedure`; it does not embed application CRUD SQL:

- `app.WorkItems_List`
- `app.WorkItems_Get`
- `app.WorkItems_Create`
- `app.WorkItems_Update`
- `app.WorkItems_Delete`

The procedures own the `SELECT`, `INSERT`, `UPDATE`, and `DELETE` statements against `app.WorkItems`. The migration uses `CREATE OR ALTER PROCEDURE` and guarded schema/table/index creation so the script is rerunnable under the repository's ordered migration process.

## Local test data

`test-data/Reset-LabAPIServer-Test.sql` deletes only six fixed fixture UUIDs. `test-data/Seed-LabAPIServer-Test.sql` inserts those six synthetic rows after reset. The scripts must target only `(localdb)\MSSQLLocalDB`, database `LabAPIServer_Test`; they do not create or alter schema and cannot remove unknown Work-items. See `test-data/README.md` for the exact dataset and commands.
