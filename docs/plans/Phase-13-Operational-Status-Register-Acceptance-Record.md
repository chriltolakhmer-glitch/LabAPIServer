# Phase 13 - Operational Status Register Acceptance Record

Date: 2026-09-22.

**PHASE 13 COMPLETE**

## Feature selected

Owner-selected Option A: Operational Status Register. The slice maintains one operational status entity with `StatusId`, `Key`, `Value`, `Severity`, `ObservedAtUtc`, `UpdatedAtUtc`, `UpdatedBy`, and SQL `rowversion` concurrency. Supported severities are `Info`, `Warning`, and `Critical`.

## Files changed

API:

- `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusModels.cs`
- `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusRequestValidator.cs`
- `src/LabAPIServer.Api/OperationalStatuses/IOperationalStatusStore.cs`
- `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusService.cs`
- `src/LabAPIServer.Api/OperationalStatuses/SqlOperationalStatusStore.cs`
- `src/LabAPIServer.Api/Program.cs`
- `database/migrations/0002_OperationalStatuses.sql`
- `database/test-data/Seed-OperationalStatuses-Phase13.sql`
- `database/test-data/Reset-OperationalStatuses-Phase13.sql`
- `database/README.md`
- `database/test-data/README.md`
- `tests/LabAPIServer.Api.Tests/OperationalStatusTests.cs`
- `tests/LabAPIServer.Api.Tests/LocalDbIntegrationTests.cs`

Web:

- `src/LabWebAppServer.Web/Clients/ApiContracts.cs`
- `src/LabWebAppServer.Web/Clients/ILabApiClient.cs`
- `src/LabWebAppServer.Web/Clients/LabApiClient.cs`
- `src/LabWebAppServer.Web/Pages/OperationalStatuses/Index.cshtml`
- `src/LabWebAppServer.Web/Pages/OperationalStatuses/Index.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/OperationalStatuses/Details.cshtml`
- `src/LabWebAppServer.Web/Pages/OperationalStatuses/Details.cshtml.cs`
- `src/LabWebAppServer.Web/Pages/Shared/_Layout.cshtml`
- `tests/LabWebAppServer.Web.Tests/UnitTest1.cs`

Planning:

- `docs/plans/Phase-13-Owner-Feature-Choice-Proposal.md`
- This acceptance record.

## Database objects

Migration `0002_OperationalStatuses.sql` added `app.OperationalStatuses`, a unique key constraint, severity check constraint, and rowversion column. It added exactly these stored procedures:

- `app.OperationalStatuses_List`
- `app.OperationalStatuses_Get`
- `app.OperationalStatuses_Update`

The API SQL store uses `CommandType.StoredProcedure` for every database operation. No API C# CRUD SQL was added. Migration version 2 was applied to the approved isolated `DC01/LabAPIServer_Test` database and recorded with SHA-256 `88af5d835244808e048c778417c64c4cb8a42dcc411b2e483deb1e7edb310c85`.

## API endpoints

- `GET /api/v1/operational-statuses` — Reader, Operator, Administrator.
- `GET /api/v1/operational-statuses/{key}` — Reader, Operator, Administrator.
- `PUT /api/v1/operational-statuses/{key}` — Operator, Administrator; validates value, severity, and base64 rowversion; returns `409 Conflict` for stale updates.

## Web pages

- `/OperationalStatuses` lists statuses and links to detail.
- `/OperationalStatuses/{key}` shows detail.
- Operator and Administrator users receive the update form; Reader users receive read-only detail and list views.
- API validation, not-found, unauthorized, forbidden, conflict, and temporary-unavailability outcomes receive friendly server-rendered messages.

## Validation results

| Check | Result |
| --- | --- |
| API Release build | PASS |
| API focused status tests | PASS - 6 passed, 0 failed |
| API complete suite with SQL enabled | PASS - 30 passed, 0 failed, 0 skipped |
| SQL migration and ledger version 2 | PASS |
| Direct stored-procedure update/restore check | PASS |
| SQL procedure presence | PASS - 3 procedures |
| SQL API integration against `DC01/LabAPIServer_Test` | PASS - 1 passed, 0 failed |
| Web Release build | PASS |
| Web tests | PASS - 6 passed, 0 failed, 0 skipped |
| Fixture cleanup | PASS - final `phase13-fixture` count 0 |
| `git diff --check` | PASS |

The real SQL acceptance used encrypted SQL Server connectivity with Windows integrated authentication and the existing approved isolated target. The synthetic fixture was seeded only for acceptance, updated/restored through the stored procedure, and removed afterward. No Auth source, JWT architecture, deployment infrastructure, or Phase 14 work was changed.

## Security checks

- API SQL boundary scan: status persistence is procedure-only; no inline `SELECT`, `INSERT`, `UPDATE`, or `DELETE` was added to C#.
- Web boundary scan: no SQL client, connection string, JWT validation, token persistence, or password handling was added.
- No credentials, tokens, private keys, or production data were added to source or fixtures.
- Role policies are explicit; Administrator access is declared rather than inherited implicitly.
- Rowversion comparison occurs inside the stored procedure and stale updates return `409`.

## Known limitations

- Operational status rows are not created or deleted by the approved contract; fixture creation/removal is test setup/cleanup only.
- No durable business audit table was required by the selected contract.
- Web tests cover the typed client and existing foundation; a deployed browser acceptance with a dedicated Reader account was not added to this source slice.
- API and Web source changes were not deployed to IIS, so the Phase 11 deployed baseline remains the accepted deployed artifact.

## NOT RUN

- API/Web deployment, packaging, and post-deployment browser acceptance.
- Administrator credentialed deployed acceptance.
- Reader credentialed deployed acceptance.
- Phase 14.

**PHASE 13 COMPLETE**