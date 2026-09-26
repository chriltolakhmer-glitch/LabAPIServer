# Phase 2 — Database and API Foundation

Date: 2026-09-21. Status: complete for infrastructure foundation scope; business and Auth work remain deferred.

## Architecture decisions

- Keep one ASP.NET Core API project and one test project.
- Use SQL Server as external application storage owned by LabAPIServer.
- Keep Web free of SQL dependencies and leave LabAuthServer unchanged.
- Keep public liveness independent of SQL: `GET /api/v1/health` returns `200 {"status":"Healthy"}`.
- Use direct, feature-focused SQL access rather than an ORM, generic repository, CQRS, MediatR, or a separate infrastructure application.

## Database and configuration

- SQL access uses `Microsoft.Data.SqlClient` through a small `ISqlConnectionFactory`.
- `Database:ConnectionString` is external and empty in checked-in configuration.
- Configured connection strings must specify an intended database and use `Encrypt=True;TrustServerCertificate=False`.
- `Database:CommandTimeoutSeconds` defaults to 30 and is bounded to 1-60 seconds.
- `LABAPI_CONFIG_PATH` may identify an external JSON configuration file; secrets are not stored in source.
- A missing database target does not prevent the liveness endpoint from starting.

`BUSINESS SCHEMA DEFERRED — OWNER DECISION REQUIRED`

No business entities, tables, seed data, or feature migrations were created.

## Migration/versioning strategy

Migration source belongs in `database/migrations/` and uses immutable ordered names such as `0001_<purpose>.sql`. The Phase 2 infrastructure ledger script is `0000_MigrationLedger.sql`; it contains only migration metadata, not business data. `MigrationScriptCatalog` validates naming, ordering, duplicate versions, and SHA-256 checksums. A future migration runner must use an explicit non-production target, migration lock, checksum rejection, transactional application where supported, and a separate migration identity. Runtime API DDL execution is not implemented.

## Tests performed

PASS:

- `dotnet restore LabAPIServer.slnx`
- `dotnet build LabAPIServer.slnx -c Release --no-restore`
- `dotnet test LabAPIServer.slnx -c Release --no-build`
- Four routine tests passed, including in-process health HTTP verification, invalid SQL transport configuration rejection, explicit-target enforcement, and migration ordering/checksum validation.
- Live `GET https://localhost:7168/api/v1/health` returned `HTTP/1.1 200 OK` with `{"status":"Healthy"}`.
- API started without a configured database target.

NOT RUN:

- SQL connection and `SELECT 1` integration test: a local SQL Server service was detected, but the available PowerShell host could not load the net9 SQL client assembly for a safe read-only check. No connection attempt or database change was made.
- Migration execution against any database.
- Production database inspection or provisioning.
- Business schema, CRUD, Auth/JWT, AD, Web integration, deployment, and release work.

## Security and boundaries

No password, SQL credential, production connection string, certificate, DPAPI material, JWT validation, AD logic, or Auth integration was added. The API receives no Web SQL dependency, and LabAuthServer was used only as a reference. No production service or database object was modified.

## Assumptions and deferred decisions

- The first business feature, entities, and permissions remain OWNER DECISION REQUIRED.
- The Auth audience remains OWNER DECISION REQUIRED and blocks Phase 3 JWT integration.
- The deployment target remains TBD / OWNER DECISION REQUIRED.
- A separately authorized isolated SQL target and migration identity are required before infrastructure integration is accepted.

## Acceptance criteria

- API health remains SQL-independent and returns the documented response.
- SQL configuration is externalizable and rejects unsafe transport settings.
- Data access is minimal and does not introduce unnecessary architecture.
- Migration versioning is deterministic and source-controlled without inventing business schema.
- Routine tests are independent of SQL and pass.
- No Web or LabAuthServer boundary changed.

## Definition of Done

Phase 2 is complete for the available foundation scope when the API configuration, safe SQL access boundary, migration ledger convention, tests, and live health verification are in place. SQL integration remains explicitly NOT RUN because no safe executable check was available in the current shell, and business schema remains deferred to the owner-approved feature phase.
