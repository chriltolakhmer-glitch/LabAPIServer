# LabAPIServer development plan

Date: 2026-09-21. Status: approved planning architecture; Phase 0 decisions pending; implementation NOT RUN. Operational root: `C:\Apps\LabAPIServer`, sibling of LabAuthServer and LabWebAppServer. Repository location: `C:\Apps\LabAPIServer\Source\LabAPIServer`. No Git initialization, application or database implementation exists yet.

Read [shared architecture, contracts, decisions and delivery](Shared-System-Architecture-and-Delivery-Plan.md) first, then the [Web App plan](../../../../../LabWebAppServer/Source/LabWebAppServer/docs/plans/LabWebAppServer-Development-Plan.md). The shared document is authoritative for JWT trust, decisions D1-D8, the 11 phases, release compatibility and end-to-end smoke. Do not duplicate those contracts in a second implementation.

## 1. Purpose and smallest architecture

Current decisions and Phase 1 blockers: [Phase 0 record](Phase-0/Phase-0-Architecture-and-Decisions.md). Foundation routes are confirmed as planned; business scope, permissions, audience acceptance and deployment target remain owner-required.

Own business endpoints, rules, authorization, application SQL and required business audit. Accept only Auth-issued access tokens; no password endpoint, directory lookup, token issuer, user password database, or refresh-token service. Scope of actual business features remains D1. Do not invent tables or endpoints merely to demonstrate connectivity.

Recommend ASP.NET Core net10.0 controller API, matching the reference's familiar HTTP/validation tooling. Use one executable project with folders; one test project can contain unit and integration categories. Thin controllers call feature services; services use narrow SQL adapters where substitution is needed. Add a separate Application/Infrastructure assembly only if a concrete independent reuse or dependency constraint appears. No Domain project, generic repository, CQRS/MediatR, automatic mapper, event bus, licensing subsystem, or copied LDAP machinery.

Outer folders now are only `Source`, `Releases`, `Scripts`. Documentation has moved under `Source\LabAPIServer\docs`; Releases and Scripts remain empty. Initialize Git only at the repository location in Phase 1. Future repository contents below are not application files created by this review:

```text
LabAPIServer/Source/LabAPIServer/
  AGENTS.md, README.md, global.json, .gitignore
  LabAPIServer.slnx
  src/LabAPIServer.Api/
    Program.cs
    Controllers/
    Features/<agreed-feature>/       # request/response DTOs, validator, service
    Data/                           # feature SQL adapters and transaction boundary
    Security/                       # public trust options and action policies
    Configuration/
    Auditing/
  tests/LabAPIServer.Tests/          # Unit, Http, SqlInfrastructure
  database/
    schema/README.md                 # authoritative schema map, not duplicate DDL
    migrations/                     # ordered immutable SQL scripts
    seed/                           # only required nonsensitive reference data
  docs/plans/                       # these plans
```

Database migrations are the schema source of truth. Avoid a second executable schema snapshot that can diverge. Add API contract documentation/OpenAPI output, status, runbook and recovery checklist only as their implementation exists. Initialize Git and ignore local settings, certificates/private keys, database files, logs, build/test outputs during foundation, not now.

## 2. HTTP and feature contracts

Phase 2 adds anonymous `GET /api/v1/health`, returning 200 `{"status":"Healthy"}` with no environment/dependency details. Phase 3 adds protected `GET /api/v1/session`, returning `{subject, role, expiresAt}` after full JWT validation, with no SQL query. `subject` and `role` come only from the validated principal; `expiresAt` is the signed `exp` converted to a UTC ISO-8601 timestamp. Require expiry still in the future for this session endpoint even when the bearer clock-skew allowance would otherwise accept the token. An elapsed expiry returns 401 with Bearer challenge; never establish a fresh Web session in the skew window.

The session endpoint accepts any of the three valid Auth roles and supplies verified identity, not a business permission grant. It does not return the JWT or accept identity in a request body. Web calls it over its fixed trusted HTTPS client immediately after Auth login, and uses its response as the sole local session identity/expiry source. Invalid token -> 401; valid but denied business action -> 403. Return no-store. This is a required integration contract, not a placeholder business feature or an assumed Auth user-info route. Business routes and tables start in Phase 4 only for the Phase 0 agreed feature.

Use lower-case plural resource routes below `/api/v1`, JSON camelCase DTOs, UTC ISO-8601 timestamps, bounded pagination when a real list exists. Publish only fields the UI needs. Input DTOs exclude server-owned owner/role/audit fields; reject or ignore unbound fields according to documented validation rules, never bind SQL entities directly. Define request, success DTO, error statuses, maximum lengths/ranges, nullable fields, sort allowlist and permission before each slice. Do not invent a universal response wrapper.

Use DataAnnotations for field requirements/length/ranges and a small feature validator for cross-field/business constraints. Controller/HTTP validation is not sufficient: service checks business rules and data-dependent authorization before persistence. Bound request bodies and collection counts; proposed default JSON business body limit 64 KiB, narrowed or revised for the actual DTO. No upload feature is assumed. Map malformed input to 400 ValidationProblemDetails, absent resource to 404, real concurrency/state conflict to 409, unexpected failure to safe 500, known transient SQL unavailability to 503. Writes, if scoped, use 201 with Location for creation and documented 200/204 for updates/deletion.

Use centralized ProblemDetails with stable status/title and safe `correlationId`; validation adds field errors. Do not return stack traces, SQL details, internal paths or connection strings. Bearer challenge remains 401 with `WWW-Authenticate`; denied authenticated policy is 403. Unknown routes return 404 consistently. Native host rejection may lack ProblemDetails/correlation; Web handles that safely.

Accept a syntactically valid nonempty `X-Correlation-ID` GUID or generate one; issue a separate per-request identifier. Propagate the safe correlation to diagnostics/business audit. Do not trust arbitrary header strings, or treat correlation as identity. Exclude bodies, Authorization, cookies and SQL parameter contents from logs.

## 3. Security and trust implementation

Implement the exact shared JWT profile using the framework bearer handler, explicit TokenValidationParameters and public-only external key mapping. `MapInboundClaims=false`, `NameClaimType=sub`, `RoleClaimType=role`, `SaveToken=false`; enforce configured algorithm and known `kid`, signature, exact issuer/audience, lifetime/skew and required claim shape. Additional semantic checks after cryptographic validation cover nonempty identity, numeric dates and duplicate claims; do not trust an unvalidated decode.

No `Authority` discovery call, AD validation, or login fallback. The API trusts only the configured Auth assertion. Public-key availability does not imply audience authorization: D2 must be settled first. Baseline JWT limits: 12288 encoded bytes and 12352 Authorization-value bytes, including Bearer prefix. Reject duplicate/ambiguous bearer inputs; no token in query parameters. Size guards execute before expensive token parsing/key work; native ingress needs separate route testing.

Recommended pipeline: trusted host/proxy handling; safe exception/correlation/response headers; HTTPS behavior/routing and size limits; authentication; authorization; endpoints. Configure a bearer-authenticated fallback policy. Explicit anonymous exception is health only. The session route requires a valid token; business endpoints additionally name an action policy. Do not add anonymous documentation or administrative endpoints in production by accident.

Each action policy uses the D3 role allowlist. Unknown/multiple role claims fail authentication; a known but disallowed role produces 403. There is no implied Administrator bypass. Resource-level authorization checks database ownership/relationships inside the service, including list filtering; a guessed ID must not disclose another user's record. Resolve stable identity and rename behavior before creating owner foreign keys. Client-supplied role/owner fields never override the principal.

Require HTTPS for all clients, including Web-to-API. For sensitive API operations reject insecure requests at ingress rather than relying on a redirect after a body has already been sent. Set `Cache-Control: no-store`, `X-Content-Type-Options: nosniff` and deny framing for API responses. Enable production HSTS only with confirmed HTTPS topology; no preload/includeSubDomains by assumption. Set explicit AllowedHosts; trust forwarded headers only from the known hosting/proxy path. No CORS needed with server-side Web calls. Do not add cookie auth to the API.

## 4. SQL ownership, schema and migration strategy

Proposed database name: `LabApplication`; development `LabApplication_Dev`; tests `LabApplication_Test_<unique-run>`. These are recommendations pending D5, not existing targets. Use an existing approved SQL Server instance, logically separate from the Auth database. API exclusively owns application schema evolution. No cross-database foreign keys or reads from Auth audit/AD tables; SQL is external infrastructure, not a fourth application project.

Default schema `app`; add `audit` only for agreed persisted business audit. Schemas are owned by a controlled database owner/principal, never the runtime identity. Use primary/foreign keys, unique/check constraints, explicit lengths/nullability and UTC datetime2 values appropriate to D1. Add indexes for actual queries. For scoped writes, choose transactions and optimistic concurrency (for example rowversion with a DTO concurrency value) where lost updates are possible; do not add it to an exclusively read-only slice.

Recommend direct `Microsoft.Data.SqlClient` with parameterized commands and feature-specific adapters: minimal dependency and familiar tooling. Explicit SQL parameter types/sizes, cancellation, bounded command/connect timeout, deterministic ordering and bounded result sets. Whitelist dynamic sort identifiers; parameters cannot sanitize SQL identifiers. No SQL concatenation of user values or `AddWithValue` inference. Use stored procedures only for an existing SQL contract or a concrete execute-only boundary; do not replicate Auth's procedure architecture by default. Revisit EF Core only if the agreed domain would materially benefit from its mapping/migrations.

Use ordered scripts such as `0001_<purpose>.sql` and a migration ledger containing version, name, checksum and applied UTC. The deployment-side runner obtains a migration lock, rejects an altered checksum, applies each transactional script with its ledger entry atomically, and stops on failure. Handle nontransactional SQL explicitly per script with a recovery note. No runtime automatic migration or runtime DDL privileges. A small PowerShell/sqlcmd-based runner is sufficient; implement once with tested ordering/locking, not a new database service. Test fresh setup and upgrade from last release. Applied scripts are immutable; corrections add a migration.

All application database tooling belongs to `C:\Apps\LabAPIServer\Source\LabAPIServer\database`. Phase 2 establishes connectivity, schema/migration bookkeeping and the explicit test target, not a sample business schema. Phase 4 introduces only the agreed feature's tables/queries. No separate root Database project or fourth application is created.

Local SQL: per-developer LocalDB or an existing isolated SQL Server database, created only through explicit setup. Disposable test databases require an enable flag plus explicit connection/target, recognizable test name, and guarded teardown confined to the exact created DB; never infer a production/localhost fallback. Routine HTTP tests replace SQL entirely. Seed only required reference rows; synthetic business fixtures live in tests, not production seed scripts. Never copy operational data.

Production connection: `Encrypt=True;TrustServerCertificate=False`, trusted server hostname/chain, explicit intended database. Prefer Windows integrated authentication using a dedicated API service identity where the host/domain supports it; if SQL authentication is necessary, protect the password in external secret storage. Web receives neither credential nor SQL access. API identity gets only required SELECT/INSERT/UPDATE/DELETE on named objects (or EXECUTE on agreed procedures), no db_owner, DDL, broad schema control or migration rights. Separate authorized migration/backup/inspection identities. Deny runtime UPDATE/DELETE on audit rows unless a separate retention role needs them.

Backup/restore expectations follow D5: proposed daily full backups for low-value lab data, verified encrypted restricted/off-host copy, retention and RPO/RTO confirmed before deployment. More valuable data may require FULL recovery and log backups; do not select that without recovery requirements. Before migration, identify a usable recovery point; verify backup and restore into isolation with integrity checks and actual queries. Binary rollback keeps compatible expanded schema. Destructive change requires its own forward-fix/restore plan and explicit data-loss accounting.

## 5. Auditing and diagnostics

Do not duplicate Auth login events in application SQL. Define business events with each actual slice: action, UTC time, validated subject/role, resource identifier where safe, outcome, correlation/request ID and application version. Avoid full before/after data, passwords, tokens, request bodies and raw exceptions. Schema field capacity must support the chosen identity contract; never silently truncate identities into another apparent user.

For required audit of a business mutation, persist change and audit in the same SQL transaction; failure rolls back both and returns a safe error. For reads/denials/security diagnostics, propose structured best-effort logs unless D5 requires durable read auditing. If durable read audit is required, define its failure response and prove persistence before success. No queue/outbox or replay system absent a need. Auth's existing audit remains best effort and cannot be represented as guaranteed by these apps.

Use ILogger structured events for request outcomes, dependency failures and correlation; never log connection strings or parameter values. Local Console is sufficient for development. For IIS, use the shared proposed bounded Windows Event Log plus IIS request logs, verify capture, and document gaps. No logging platform is needed. Audit retention is a D5 requirement, implemented only with an agreed policy and separate maintenance identity.

## 6. Configuration contract

Implement typed validated options; fail startup clearly for missing/unsafe required values without printing secrets. Proposed configuration names:

| Section/key | Type/default and rule |
| --- | --- |
| `JwtTrust:Issuer`, `Audience` | Required strings from D2; exact issuer HTTPS identifier and one agreed audience |
| `JwtTrust:Algorithm`, `ClockSkewSeconds`, `MaximumLifetimeSeconds` | Baseline RS256, 300, 3600; bounded and tested, coordinated with Auth |
| `JwtTrust:Keys` | Required public certificate paths/fingerprints + unique kid + active/previous acceptance deadlines; no private material |
| `Database:ConnectionString` | Required external value; intended database and encryption/trust policy validated |
| `Database:CommandTimeoutSeconds` | Proposed 30, allowed 1-60; revise against actual workload |
| `AllowedHosts` | Required environment-specific list, no wildcard production default |
| `Logging` | Safe level/sink/retention settings; no sensitive HTTP body logging |

Set a documented explicit configuration order: safe checked-in appsettings; optional development file/user secrets only in Development; required external production JSON specified by `LABAPI_CONFIG_PATH`; process environment overrides last. Disable arbitrary production command-line configuration overrides or document/restrict them explicitly. Implement the loader: an arbitrary ProgramData file is not automatically loaded. Missing production file or invalid settings prevents startup. Restart to apply security/database settings; no unnoticed live trust reload. Test actual precedence with benign sentinel values. Production files live outside artifact/source with deployment-write and runtime-read ACLs.

## 7. Tests and implementation acceptance

Use xUnit and a real ASP.NET Core test host with fake data adapters and synthetic ephemeral RSA keys. No test may implicitly access certificate stores, operational SQL, AD or host secrets. Reuse behavior patterns, not the reference's thousand-test inventory or concurrency machinery.

| Area | Required behavior tests when implemented |
| --- | --- |
| Foundation | Host starts with safe config; unsafe/missing config fails; health 200/no dependency call; unknown route 404 |
| Authentication | Anonymous protected 401; valid JWT 200 at session route with exact verified subject/role/expiry and no SQL; session rejects elapsed expiry even within skew; wrong key/signature/issuer/audience/algorithm/kid, missing/duplicate claims, unsupported role, expired/not-yet-valid token rejected |
| Time and rotation | Expiry/skew boundaries, active and valid previous keys, retired previous denied without breaking active |
| Authorization | Exact allowed/denied role on each actual action; no Administrator hierarchy assumption; unauthorized resource/list access blocked before data exposure |
| HTTP safety | Validation/ProblemDetails, safe correlation, body/header/token size bounds, no credentials in logs/errors |
| Business | Real D1 happy/failure rules, invalid state/concurrency only if scoped, user input cannot set server-owned fields |
| SQL opt-in | Fresh/upgrade/checksum migration; constraints; parameter safety; least privilege; actual queries; transaction/audit atomicity and unavailable DB |
| System | Real Auth token + API + application SQL; then Web's actual data journey and audit evidence |

Future commands from repository root `C:\Apps\LabAPIServer\Source\LabAPIServer`:

```powershell
dotnet restore LabAPIServer.slnx
dotnet build LabAPIServer.slnx -c Release --no-restore
dotnet test LabAPIServer.slnx -c Release --no-build --filter "Category!=SqlInfrastructure&Category!=SystemAcceptance"
# Separate shell, only after deliberate disposable target selection:
# LABAPI_RUN_SQL_TESTS=1 and LABAPI_SQL_TEST_CONNECTION supplied securely
dotnet test LabAPIServer.slnx -c Release --no-build --filter "Category=SqlInfrastructure"
```

The flags are a proposed test contract to implement, not currently available scripts. A target alone does not enable tests. Enabled tests without a valid target fail before connecting; mandatory skipped tests do not pass. Full package/target smoke is defined once in the shared plan. No business-route smoke runs before that route exists.

## 8. Phase ownership, deployment and Definition of Done

Use the shared 11-phase roadmap as the execution checklist. Phase 0 resolves the listed pre-Phase-1 decisions; Phase 1 scaffolds both solutions; Phase 2 adds database/API foundation without business code; Phase 3 adds JWT/session validation; Phase 4 delivers exactly one real business slice. API then supports Web foundation in Phase 5, browser integration in Phase 6 and full validation in Phase 7. Phase 8 packages, Phase 9 deploys and Phase 10 establishes operations. Every shared phase specifies objective, prerequisites, work, tests, deliverables, acceptance, DoD and explicit NOT RUN items. None is complete merely because its files exist.

Release one immutable API ZIP from the tested commit including checksummed database migrations; retain ZIP, SHA256 and release record under outer `Releases\<version>`. An extracted `app` subdirectory and preserved prior versions stay under Releases; operational script copies belong in outer Scripts with versioned originals inside the repository. No outer Backups/Deployments/Database folder. Restricted configuration and recovery sets remain external as defined in the shared plan. Use the compatibility row and source/tag verification. Database deployment precedes compatible Auth confirmation and API activation; Web follows API. No runtime migration or build on IIS. Separate runtime and migration identities; verify encrypted SQL and real JWT acceptance through actual IIS before accepting deployment.

Before valuable data goes live, complete backup/restore expectations and schema rollback compatibility. Runtime rollback switches the previous app/configuration, keeps compatible schema, and verifies data/audit through the user flow. First-install recovery disables only the new API and preserves data. Operations must identify actual liveness endpoint, logs, SQL audit if present, config location, scoped pool restart and backup/recovery results; do not equate a 200 health with working SQL.

API implementation is done when the agreed business slice and its denials work through Web, JWT public trust is verified, SQL privileges/encryption and required audit are proven, meaningful tests pass, a reproducible compatible artifact exists, and applicable deployment/recovery/operations evidence is recorded. If deployment is outside the later implementation request, report its NOT RUN status explicitly instead of claiming system completion.

Planning validation: documents relocated/revised and boundaries reviewed; no application code/schema/configuration created. All implementation/runtime checks are NOT RUN. Remaining decisions are shared D1-D8; section 5 distinguishes the seven specific pre-Phase-1 answers from later prerequisites. No SQL, certificate, IIS or Auth changes accompany this review.
