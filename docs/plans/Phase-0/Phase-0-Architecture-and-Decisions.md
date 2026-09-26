# Phase 0 — Architecture & Decisions

Date: 2026-09-21. Status: planning only; implementation not started.

Authority: [Shared System Architecture and Delivery Plan](../Shared-System-Architecture-and-Delivery-Plan.md), [LabAPIServer Development Plan](../LabAPIServer-Development-Plan.md), [LabWebAppServer Development Plan](../../../../../../LabWebAppServer/Source/LabWebAppServer/docs/plans/LabWebAppServer-Development-Plan.md), [Solo Developer Project Roadmap](../../../../../../Solo-Developer-Project-Roadmap.md), and the current LabAuthServer repo guidance. The root-level `C:\Apps\AGENTS.md` file does not exist, so no replacement instruction file was created.

## Decisions

1. First business feature: OWNER DECISION REQUIRED. No real business feature was supplied. Health and session validation are foundation capabilities, not a business feature.
2. Entities/data: OWNER DECISION REQUIRED. No entities, fields, tables, or database work were created or invented.
3. API endpoints: confirm the planned anonymous `GET /api/v1/health` and protected `GET /api/v1/session`. Business endpoints remain pending the first real feature.
4. Role/action matrix: preserve Reader, Operator, and Administrator. Do not assume Administrator inherits every permission. Business permissions remain pending the actual feature.
5. Auth audience: OWNER DECISION REQUIRED. If the existing LabAuthServer audience is used, it must be explicitly accepted for LabAPIServer. Otherwise, a dedicated audience requires a future separately authorized Auth change. Do not silently modify LabAuthServer or bypass audience validation.
6. Development URLs: use the planned defaults unless a collision is detected: Auth `https://localhost:7068`, API `https://localhost:7168`, Web `https://localhost:7268`. Obvious port conflict check only; no services started.
7. Deployment target: TBD / OWNER DECISION REQUIRED. Do not assume the LabAuthServer domain controller is the deployment host.

## Assumptions

- The project remains in architecture/planning only; no application code is created in Phase 0.
- LabAuthServer remains the authentication authority. LabAPIServer validates bearer tokens and enforces API policies.
- LabWebAppServer remains a separate web app that uses server-side sessions and verified API session responses; it does not validate JWTs or own app data.
- No database schema is designed or created yet.
- No product features are invented in the absence of a real owner-approved business feature.

## Unresolved items

- D1: first real business feature, its data model, and required outcomes.
- D1: concrete API business routes and DTO contract once the feature is known.
- D3: allowed/denied action matrix for Reader, Operator, and Administrator.
- D2: explicit audience choice and Auth trust contract.
- D6: deployment host/topology and runtime identity model.
- D8: actual nonproduction Auth/API/Web provisioning and TLS trust later in Phase 3+.

## Dependencies

- Owner decision on the first business feature and resulting permissions.
- Explicit decision on shared versus dedicated Auth audience.
- Agreement on deployment target and hosting model.
- Later real Auth and database integration are out of scope for Phase 0.

## Explicit items blocking Phase 1

- No first business feature selected.
- No entities/data model approved.
- No business endpoint contract approved.
- No role/action matrix approved for the actual feature.
- No audience decision approved.
- No deployment target selected.
- No real Auth or database integration target exists yet.

## Items deliberately deferred

- Application projects, Git initialization, solution scaffolding, code implementation.
- Database tables, migrations, and schema design.
- Controllers, Razor Pages, and service logic.
- Build, deploy, release, or runtime configuration changes.
- LabAuthServer modifications, credentials, certificates, and production infrastructure changes.
- Operations, monitoring, and recovery design beyond the decision record.

## Phase 0 acceptance criteria

- The project has a concise, internally consistent Phase 0 decision record.
- Shared architecture, API, Web, and roadmap boundaries agree.
- No product feature is invented.
- No LabAuthServer source is modified.
- No application implementation or database work occurs in Phase 0.
- Remaining owner decisions are clearly listed and block Phase 1.

## Phase 0 Definition of Done

Phase 0 is complete when the decision record is in place, the unresolved items are explicit, the shared boundaries remain unchanged, and the project is ready to proceed only after the owner resolves the required feature, audience, permissions, and deployment decisions. This document does not approve implementation.

PHASE 0 READY FOR PHASE 1
