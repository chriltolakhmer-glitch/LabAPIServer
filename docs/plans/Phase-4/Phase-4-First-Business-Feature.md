# Phase 4 - First Business Feature

## 1. Objective

Select one small, real business capability for LabAPIServer before implementing business code. This plan is a decision proposal only. Phase 4 implementation must remain inside LabAPIServer and use the existing JWT, authorization, API, and SQL foundations.

## 2. Selected feature or owner decision required

**SELECTED: OPTION A - WORK-ITEM REGISTER.** The owner selected this option for Phase 4. The health and session endpoints remain unchanged foundation capabilities.

## 3. Scope options

### Selected feature - Work-item register

- **Purpose:** Create and maintain a small list of named work items with a status and optional note.
- **Minimum data:** `WorkItemId`, `Name`, `Description`, `Status`, `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy`.
- **Minimum tables:** One `app.WorkItems` table.
- **Minimum API endpoints:**
  - `GET /api/v1/work-items`
  - `GET /api/v1/work-items/{id}`
  - `POST /api/v1/work-items`
  - `PUT /api/v1/work-items/{id}`
  - `DELETE /api/v1/work-items/{id}`
- **Reader:** Read list and detail.
- **Operator:** Read, create, update, and delete.
- **Administrator:** Read, create, update, and delete for this feature by explicit policy; this is not inherited implicitly from the role name.
- **Test complexity:** Standard authenticated HTTP tests, role allow/deny tests, validation, not-found, and CRUD persistence tests.
- **Why small:** One table, one resource, ordinary CRUD, and no external dependency beyond the existing SQL boundary.

### Deferred alternative - Operational status record

- **Purpose:** Record a small set of operational status entries that can be read and updated by authorized operators.
- **Minimum data:** `StatusId`, `Key`, `Value`, `Severity`, `ObservedAtUtc`, `UpdatedBy`, `RowVersion`.
- **Minimum tables:** One `app.OperationalStatuses` table.
- **Minimum API endpoints:**
  - `GET /api/v1/operational-statuses`
  - `GET /api/v1/operational-statuses/{key}`
  - `PUT /api/v1/operational-statuses/{key}`
- **Reader:** Read status entries.
- **Operator:** Read and update status entries.
- **Administrator:** Explicitly selected by the owner; no automatic inheritance is assumed.
- **Test complexity:** Authenticated reads, role-based update denial/success, key/value validation, not-found, and optimistic-concurrency tests.
- **Why small:** One table, two read shapes and one write path, with a bounded operational record and no workflow engine.

### Deferred alternative - Simple request register

- **Purpose:** Submit and track a small request from creation through a controlled status change.
- **Minimum data:** `RequestId`, `Title`, `Details`, `Status`, `CreatedAtUtc`, `CreatedBy`, `ResolvedAtUtc`, `ResolvedBy`.
- **Minimum tables:** One `app.Requests` table.
- **Minimum API endpoints:**
  - `GET /api/v1/requests`
  - `GET /api/v1/requests/{id}`
  - `POST /api/v1/requests`
  - `PUT /api/v1/requests/{id}/status`
- **Reader:** Read requests.
- **Operator:** Read, create, and change request status.
- **Administrator:** Explicitly selected by the owner; no automatic inheritance is assumed.
- **Test complexity:** Authenticated reads, create/status permissions, input and state validation, not-found, and transition tests.
- **Why small:** One table, one controlled status transition endpoint, and no assignment, notification, attachment, or escalation subsystem.

## 4. Out of scope

This implementation does not include Web integration, Auth changes, deployment, packaging, background workers, queues, CQRS/MediatR, generic repositories, or speculative authorization hierarchy. It also does not add required durable business audit because the selected work-item contract has no audit requirement beyond created/updated identity and timestamps.

## 5. Data model

Create only `app.WorkItems` through migration `0001_WorkItems.sql`:

- `WorkItemId uniqueidentifier` primary key.
- `Name nvarchar(200)` required.
- `Description nvarchar(2000)` nullable.
- `Status nvarchar(32)` required with `Open`, `InProgress`, or `Complete` check constraint.
- `CreatedAtUtc`, `UpdatedAtUtc datetime2(7)` required UTC timestamps.
- `CreatedBy`, `UpdatedBy nvarchar(1024)` required validated-subject values.
- Index on `(Status, UpdatedAtUtc DESC)` for filtered operational lists.

No relationship, seed data, concurrency column, or audit table is required for this first slice.

## 6. API contract

All routes use camelCase JSON and UTC ISO-8601 timestamps. `/api/v1/health` and `/api/v1/session` remain unchanged. Every work-item route requires an explicit named role policy.

| Method and route | Authorization | Request | Response | Errors |
| --- | --- | --- | --- | --- |
| `GET /api/v1/work-items` | Reader, Operator, Administrator | None | 200 with `WorkItemDto[]` | 401 unauthenticated; 503 database unavailable |
| `GET /api/v1/work-items/{id}` | Reader, Operator, Administrator | Route GUID | 200 with `WorkItemDto` | 401, 404, 503 |
| `POST /api/v1/work-items` | Operator, Administrator | `{name, description, status}`; name 1-200, description <=2000, status from fixed set | 201 with `WorkItemDto` | 400 validation, 401, 403, 503 |
| `PUT /api/v1/work-items/{id}` | Operator, Administrator | Same request shape | 200 with `WorkItemDto` | 400 validation, 401, 403, 404, 503 |
| `DELETE /api/v1/work-items/{id}` | Operator, Administrator | Route GUID | 204 empty | 401, 403, 404, 503 |

Unexpected failures use the existing safe problem-details handler. No client-supplied identity, role, owner, or timestamps are accepted.

## 7. Authorization matrix

| Role | Action | Decision |
| --- | --- | --- |
| Reader | Read list and detail | Allowed |
| Reader | Create, update, delete | Denied |
| Operator | Read, create, update, delete | Allowed |
| Administrator | Read, create, update, delete | Allowed by explicit feature policy |

The API must enforce the selected matrix with named policies. A valid JWT proves identity but does not grant business permission.

## 8. Implementation sequence

1. Record the selected work-item contract and explicit role matrix.
2. Add the single ordered `app.WorkItems` migration; do not execute it against production.
3. Implement request/response DTOs, validation, service, and stored-procedure-backed SQL store.
4. Add named read/write policies and safe database error handling.
5. Add focused service and API tests using an in-memory fake store.
6. Run the Release build and complete the selected feature acceptance checks.

## 9. Tests

The selected feature must include:

- unauthenticated request -> HTTP 401;
- allowed role -> documented success;
- disallowed role -> HTTP 403;
- invalid input -> documented 4xx;
- successful applicable create/read/update/delete operations;
- not-found -> HTTP 404;
- state/concurrency validation where applicable;
- safe SQL integration against an explicitly selected disposable or development test database;
- no production data, credentials, or implicit SQL target.

## 10. Acceptance criteria

Phase 4 acceptance requires one owner-approved feature with:

- the work-item purpose, minimum data model, and exact API contract above;
- the explicit Reader/Operator/Administrator action matrix above;
- the single migration is catalogued and has not been executed against production;
- passing unit and API tests;
- passing opt-in database tests against an explicit safe target if persistence is required;
- no changes to LabAuthServer or LabWebAppServer;
- no Web integration, deployment, or release activity.

## 11. Rollback considerations

Planning only: no migration or application rollback is currently needed. After selection, use an additive migration and preserve the prior API artifact. Define a forward-fix or compatible rollback before applying any schema change. Never run a migration against an unknown or production target during development.

## 12. Definition of Done

Phase 4 is complete when the work-item endpoints, explicit role policies, validation, stored-procedure SQL boundary, migration, and focused tests pass against a safe selected development/test target. The API store calls only the named procedures defined by `0001_WorkItems.sql`; table CRUD statements remain in SQL Server procedure bodies. The current implementation uses a fake store for routine API tests; real SQL execution remains opt-in.

## 13. Explicit NOT RUN items

- Real SQL migration execution: PASS against isolated local `MSSQLLocalDB` database `LabAPIServer_Test`.
- Real SQL persistence acceptance: PASS through direct stored-procedure tests and the API data path against `LabAPIServer_Test`.
- Web integration: NOT RUN.
- LabAuthServer changes: NOT RUN and prohibited.
- Deployment, packaging, release, and GitHub operations: NOT RUN.

**PHASE 4 IMPLEMENTATION COMPLETE - WORK-ITEM REGISTER**

Release build and the complete automated suite pass: API 16 tests succeeded, 0 failed, 0 skipped; Web 4 tests succeeded, 0 failed, 0 skipped. Migration and stored-procedure integration were verified against isolated local `MSSQLLocalDB` database `LabAPIServer_Test`. Web integration, deployment, and release packaging remain out of scope.

### Data-access architecture correction

The initial store implementation embedded direct `SELECT`, `INSERT`, `UPDATE`, and `DELETE` statements in C#. This was corrected before Phase 7. `SqlWorkItemStore` now uses `CommandType.StoredProcedure` and strongly typed parameters for:

- `app.WorkItems_List`
- `app.WorkItems_Get`
- `app.WorkItems_Create`
- `app.WorkItems_Update`
- `app.WorkItems_Delete`

Migration `0001_WorkItems.sql` now defines the Work-item schema, index, and idempotently replaceable procedure bodies. API and Web automated tests pass after the correction. Real stored-procedure SQL integration was verified against isolated local `MSSQLLocalDB` database `LabAPIServer_Test`; all synthetic test rows were removed afterward.

### Isolated SQL integration evidence

- Safe SQL test target: YES — local `MSSQLLocalDB`, database `LabAPIServer_Test`, Windows integrated authentication. The separate local `LabAuthServer` database was not accessed.
- Migration execution: PASS — `0000_MigrationLedger.sql` and `0001_WorkItems.sql` executed successfully.
- Migration ledger: PASS — versions 0 and 1 recorded with 64-character SHA-256 hashes.
- Schema verification: PASS — `app.WorkItems`, primary key, and `IX_WorkItems_Status_UpdatedAtUtc` present.
- Stored procedures verified: PASS — all five `app.WorkItems_*` procedures present and directly exercised.
- Direct procedure tests: PASS — create, list, get, missing-get, update, delete, unrelated-row preservation, and cleanup.
- API -> stored procedure -> SQL integration: PASS — API list 200, create 201, get 200, update 200, delete 204, deleted get 404.
- Authorization: PASS — Reader GET 200 and writes 403; Administrator create 201, update 200, delete 204.
- Test data baseline: six deterministic synthetic rows are documented under `database/test-data/`, with fixed UUIDs covering Open, InProgress, Complete, present/absent descriptions, different timestamps, and list ordering.
- Reset safety: the reset script deletes only those six fixed UUIDs; it does not drop the database, alter schema, or delete unknown Work-items.
- Cleanup convention: restore the documented six-row baseline after integration validation, or run the reset script when a specific test requires zero synthetic rows. No production data is used.
