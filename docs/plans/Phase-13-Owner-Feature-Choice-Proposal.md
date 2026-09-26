# Phase 13 - Owner Feature Choice Proposal

Date: 2026-09-22.

**OWNER SELECTION RECORDED — OPTION A: OPERATIONAL STATUS REGISTER**

The owner selected Candidate A on 2026-09-22. The Work-item register remains unchanged. The selected contract below was implemented as the Phase 13 vertical slice; the completion evidence is recorded in `Phase-13-Operational-Status-Register-Acceptance-Record.md`.

## Candidate A - Operational status register

- **Purpose:** Record and maintain a small set of operational status entries.
- **Minimum entity/data:** `StatusId`, `Key`, `Value`, `Severity`, `ObservedAtUtc`, `UpdatedBy`, and `RowVersion`.
- **API operations:** `GET /api/v1/operational-statuses`, `GET /api/v1/operational-statuses/{key}`, `PUT /api/v1/operational-statuses/{key}`.
- **Stored procedures required:** `app.OperationalStatuses_List`, `app.OperationalStatuses_Get`, `app.OperationalStatuses_Update`.
- **Web pages/actions:** status list, status detail, and an edit action where permitted.
- **Reader permissions:** list and detail only.
- **Operator permissions:** list, detail, and update.
- **Estimated complexity:** Small to medium; includes optimistic-concurrency handling.
- **Test scope:** validation, role allow/deny, not-found, concurrency conflict, stored-procedure persistence, Web rendering/edit errors, and end-to-end update/cleanup.

## Candidate B - Simple request register

- **Purpose:** Submit and track a request through a controlled status change.
- **Minimum entity/data:** `RequestId`, `Title`, `Details`, `Status`, `CreatedAtUtc`, `CreatedBy`, `ResolvedAtUtc`, and `ResolvedBy`.
- **API operations:** `GET /api/v1/requests`, `GET /api/v1/requests/{id}`, `POST /api/v1/requests`, `PUT /api/v1/requests/{id}/status`.
- **Stored procedures required:** `app.Requests_List`, `app.Requests_Get`, `app.Requests_Create`, `app.Requests_UpdateStatus`.
- **Web pages/actions:** request list, request detail, create form, and status-change action.
- **Reader permissions:** list and detail only.
- **Operator permissions:** list, detail, create, and change status.
- **Estimated complexity:** Small to medium; requires explicit state-transition validation.
- **Test scope:** validation, role allow/deny, not-found, allowed/invalid transitions, stored-procedure persistence, Web form/error behavior, and end-to-end create/status-change/cleanup.

## Candidate C - Lightweight note register

- **Purpose:** Capture short internal notes for shared operational follow-up.
- **Minimum entity/data:** `NoteId`, `Title`, `Body`, `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, and `UpdatedBy`.
- **API operations:** `GET /api/v1/notes`, `GET /api/v1/notes/{id}`, `POST /api/v1/notes`, `PUT /api/v1/notes/{id}`, `DELETE /api/v1/notes/{id}`.
- **Stored procedures required:** `app.Notes_List`, `app.Notes_Get`, `app.Notes_Create`, `app.Notes_Update`, `app.Notes_Delete`.
- **Web pages/actions:** note list, note detail, create, edit, and delete actions.
- **Reader permissions:** list and detail only.
- **Operator permissions:** list, detail, create, update, and delete.
- **Estimated complexity:** Small; ordinary CRUD with no relationships or workflow.
- **Test scope:** length/content validation, role allow/deny, not-found, CRUD persistence, Web validation/error behavior, and end-to-end fixture cleanup.

## Decision

Candidate A is selected. Final fields, API operations, role matrix, concurrency behavior, and no-additional-audit scope follow the candidate definition above.

Candidate B and Candidate C remain unselected and were not implemented.