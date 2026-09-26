# Phase 14 - Operational Status Register Enhancement

Date: 2026-09-22.

**OWNER-APPROVED SCOPE RECORDED — IMPLEMENTATION AND ACCEPTANCE COMPLETE**

Phase 13 is complete and remains closed. Phase 14 adds a controlled lifecycle and append-only transition history to the existing Operational Status Register. This plan authorizes planning only; it does not authorize source changes, SQL execution, deployment, or Phase 15.

## Business purpose

Make each operational status progression explicit and auditable while retaining the existing status record, value, severity, optimistic concurrency, and role boundaries.

## Users and roles

- Reader: may list, view current status, and view chronological history; cannot change status.
- Operator: may change status through the existing write boundary when the lifecycle transition is valid.
- Administrator: same status-change permission as Operator.
- Auth/JWT roles, token issuance, validation, and claims remain unchanged.

## Lifecycle contract

The new current `Status` field uses the existing Work-item status vocabulary:

- Allowed values: `Open`, `InProgress`, `Complete`.
- Allowed transitions: `Open -> InProgress`; `InProgress -> Complete`.
- Rejected transitions: `Open -> Complete`, any transition from `Complete`, reopening, unknown values, and same-value updates.
- Existing `Severity` values (`Info`, `Warning`, `Critical`) remain independent and unchanged.

## Required database changes

Planned migration: `database/migrations/0003_OperationalStatusLifecycle.sql`.

Planned API repository files: `database/migrations/0003_OperationalStatusLifecycle.sql`, the Phase 14 seed/reset SQL and database README files, `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusModels.cs`, `OperationalStatusRequestValidator.cs`, `IOperationalStatusStore.cs`, `OperationalStatusService.cs`, `SqlOperationalStatusStore.cs`, `src/LabAPIServer.Api/Program.cs`, and the existing Operational Status and LocalDB integration test files.

- Add non-null `Status nvarchar(32)` to `app.OperationalStatuses`, initialized to `Open` for existing rows and constrained to the three lifecycle values.
- Add `app.OperationalStatusHistory` with `HistoryId`, `StatusId`, `PreviousStatus`, `NewStatus`, `ChangedBy`, and `ChangedAtUtc`, plus foreign-key, lifecycle-value, and UTC/ordering constraints and an index by `StatusId, ChangedAtUtc, HistoryId`.
- Preserve existing operational status rows, keys, values, severities, timestamps, and rowversion behavior.
- Extend `app.OperationalStatuses_Update` so rowversion validation, lifecycle validation, current-row update, and history insert occur atomically. Invalid transitions must make no current-row or history change.
- Add `app.OperationalStatuses_History` to return one status record's history in chronological order.
- Keep all API persistence through stored procedures; no inline SQL in C#.
- Add/update isolated Phase 14 seed/reset scripts and database documentation. Cleanup must remove history before its fixture status row.

No Auth database, Auth schema, production database, IIS configuration, certificate, deployment configuration, or infrastructure change is in scope. Migration execution is not authorized until implementation acceptance.

## Required API changes

Planned files/surfaces:

- `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusModels.cs`
- `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusRequestValidator.cs`
- `src/LabAPIServer.Api/OperationalStatuses/IOperationalStatusStore.cs`
- `src/LabAPIServer.Api/OperationalStatuses/OperationalStatusService.cs`
- `src/LabAPIServer.Api/OperationalStatuses/SqlOperationalStatusStore.cs`
- `src/LabAPIServer.Api/Program.cs`
- `tests/LabAPIServer.Api.Tests/OperationalStatusTests.cs`
- `tests/LabAPIServer.Api.Tests/LocalDbIntegrationTests.cs`

- Extend `OperationalStatusRow`, `OperationalStatusDto`, and Web-facing contracts with `Status`.
- Add `OperationalStatusHistoryRow`/DTO containing record ID, previous status, new status, changed-by identity, and changed-at UTC.
- Add lifecycle constants and transition validation alongside the existing request validator.
- Extend the update request with the target `Status`; preserve `Value`, `Severity`, and `RowVersion` updates.
- Extend `IOperationalStatusStore` and `SqlOperationalStatusStore` with atomic lifecycle update and history retrieval methods, all invoking stored procedures.
- Preserve `GET /api/v1/operational-statuses`, `GET /api/v1/operational-statuses/{key}`, and `PUT /api/v1/operational-statuses/{key}`. The PUT returns the updated current record on success, `403` for unauthorized role access, `404` for a missing key, `409` for stale rowversion, `422` for a validly shaped but disallowed transition, and `400` for malformed values/rowversion.
- Add `GET /api/v1/operational-statuses/{key}/history`, authorized by the existing read policy, returning chronological history or `404` for a missing key.
- Preserve existing authorization policies and the Work-item endpoints unchanged.

## Required WebApp changes

Planned files/surfaces:

- `src/LabWebAppServer.Web/Clients/ApiContracts.cs`
- `src/LabWebAppServer.Web/Clients/ILabApiClient.cs`
- `src/LabWebAppServer.Web/Clients/LabApiClient.cs`
- `src/LabWebAppServer.Web/Pages/OperationalStatuses/Details.cshtml`
- `src/LabWebAppServer.Web/Pages/OperationalStatuses/Details.cshtml.cs`
- the existing Web client/page test file(s) covering the Operational Status page and API client

- Extend API contracts and `ILabApiClient`/`LabApiClient` for `Status` and history retrieval.
- Keep the existing list/detail navigation and current-value/severity display.
- Add the current lifecycle status to the detail view.
- Add a simple chronological history table showing record ID, previous status, new status, changed-by, and changed-at UTC.
- Add a lifecycle status selector to the existing Operator/Administrator edit form; keep Reader detail and history read-only with no update controls.
- Render a clear validation message for `422` invalid transitions and preserve existing unauthorized, forbidden, not-found, conflict, and temporary-unavailability handling.
- Do not add Web SQL access, Auth changes, token persistence, or a new business domain.

## Required workflows

1. Reader opens a status detail page and sees the current status plus chronological history, without an edit action.
2. Operator or Administrator selects the next valid lifecycle status and submits the existing edit form with the current rowversion.
3. API and SQL atomically update the current status and append one history record.
4. An invalid transition returns `422`, leaves the current row and history unchanged, and displays the Web error.
5. A stale rowversion returns `409` as today; no history row is appended.

## Acceptance checklist

- [x] Valid `Open -> InProgress` and `InProgress -> Complete` transitions succeed through API and WebApp.
- [x] Direct completion, reopening, same-value, and unknown transitions are rejected with `422` and no data change.
- [x] Each successful transition persists exactly one history record through stored procedures with all five required audit values.
- [x] History is returned in chronological order and displays correctly in the WebApp.
- [x] Reader cannot modify status; Operator and Administrator can modify status within the lifecycle.
- [x] Existing value, severity, rowversion conflict, list, detail, and Work-item behavior remain passing.
- [x] Auth/JWT behavior and LabAuthServer remain unchanged.
- [x] API automated tests pass, including lifecycle matrix, role authorization, invalid-transition response, concurrency, and history mapping.
- [x] Web automated tests pass, including status selection, history rendering, Reader read-only behavior, and `422` handling.
- [x] SQL integration verifies transition plus history persistence and verifies rejected transitions do not write history.
- [x] Phase 14 fixture rows and history rows are removed after acceptance; final fixture count is zero.
- [x] `git diff --check` passes where applicable; repository roots are predominantly untracked.
- [x] No secrets, credentials, bearer JWTs, or private keys are introduced into source or artifacts.

## Implementation gate

**IMPLEMENTATION GATE PASSED.** Evidence is recorded in [Phase-14-Operational-Status-Register-Acceptance-Record.md](Phase-14-Operational-Status-Register-Acceptance-Record.md). No deployment was performed and Phase 15 remains outside this work.
