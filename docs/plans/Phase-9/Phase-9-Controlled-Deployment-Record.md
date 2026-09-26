# Phase 9 - Controlled Deployment Record

Date: 2026-09-22

## Current decision - Phase 9B deployment, 2026-09-22

**PHASE 9B COMPLETE.** API/Web deployment and requested HTTPS smoke tests passed. **IIS runtime SQL connectivity remains unconfigured**; SQL integration passed separately under the test runner's identity. Temporary probe-directory cleanup was blocked by automatic approval review. This is not acceptance of deployed Work-item persistence or a new live Auth login flow.

### External configuration

The latest request authorizes creating external runtime configuration from established values and allows startup without database connectivity when documented. The shared architecture specifies protected ProgramData storage; `database/README.md` permits an empty database connection for SQL-independent startup/health.

- Created `C:\ProgramData\LabAPIServer\labapi-runtime.json` and `C:\ProgramData\LabAPIServer\auth-signing-public.pem`, outside source/releases/Current.
- Directory inheritance disabled; SYSTEM and Administrators have full control; `IIS AppPool\LabAPIServerAppPool` has read/execute, inherited by both files. Successful IIS startup confirms runtime read access.
- API pool: `LABAPI_CONFIG_PATH=C:\ProgramData\LabAPIServer\labapi-runtime.json`.
- JWT issuer `https://DC01.lab.local`, audience `LabAuthServer.API`, RS256, skew 300 seconds. PublicKeyPath points to the external PEM; no PublicKeyPem override.
- Auth's deployed configuration selects key ID `lab-jwt-signing-20260907` and LocalMachine/My certificate `94D4AC5345479614B945096CC9CEDE87C48FC51B`, matching the proven Phase 3 key. No relevant Token overrides were found in its pool or machine/process environments; no alternate deployed appsettings file was present.
- Certificate validity was checked. Export used only `GetRSAPublicKey` and `ExportParameters(false)` to produce the 2048-bit SubjectPublicKeyInfo PEM. No private key or Auth credential was copied.
- Database connection deliberately absent; timeout 30 seconds. No API/IIS-named SQL server/database principal was found. No login, user, grant, or credential was created. Deployed Work-item operations remain unavailable until runtime SQL access is established.
- Web pool overrides: `AuthClient__BaseUrl=https://DC01.lab.local`, `ApiClient__BaseUrl=https://localhost:7196`. Packaged files were not edited.
- Loader unchanged: external JSON takes precedence over duplicate default-provider/environment values and reload is disabled.

### Artifact and deployment gates

- Both Current directories were confirmed empty. Both ZIPs matched their respective SHA256SUMS.txt before extraction and after source validation builds.
- API SHA256: `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85`.
- Web SHA256: `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b`.
- Pre-deployment configuration test used the unchanged API artifact extracted into `C:\Apps\Temp\Phase9B-ConfigProbe`, started in Production with the external JSON. Startup and loopback health HTTP 200 passed, verifying JSON loading and public-key import. This was a configuration probe, not deployed HTTPS evidence. The foreground process was stopped.
- Automatic approval review rejected an earlier combined background-process probe command with `blocked by policy`; it did not execute. The separately managed foreground probe succeeded.
- Only API/Web sites and pools were stopped/started. Unchanged ZIPs were extracted into their Current directories. No release rebuild or repackaging occurred.
- Normal HTTPS validation was used for all deployed requests; no TLS bypass.

| Endpoint | Result |
| --- | --- |
| `https://localhost:7196/api/v1/health` | HTTP 200 |
| `https://localhost:7196/api/v1/session`, no token | HTTP 401 |
| `https://localhost:7153/` | HTTP 200 after normal redirect handling |
| `https://localhost:7153/health` | HTTP 200 |
| `https://localhost:7153/Account/Login` | HTTP 200 |

Auth live verification: **NOT RUN** for this deployment. Phase 3/6 evidence remains historical; no password was requested or real JWT obtained/persisted.

### Database and tests

- Existing approved target `DC01 / LabAPIServer_Test` verified using Windows Integrated Authentication as `LAB\Administrator`, encryption, and normal SQL certificate validation. Neither master nor LabAuthServer was selected as the target.
- WorkItems table, PK_WorkItems, IX_WorkItems_Status_UpdatedAtUtc, and all five app.WorkItems procedures present.
- Ledger versions 0/1 already applied; migration hashes match (`58564DD50B49D082DF926267D41F084AB440A5AA0D01B6FDCB8761C686972EE1`, `35708A34D658C1C6308F253DA15A35B7A46229CFF9EEA80A3C4F6B45DC8D5E84`). No migration reapplication or schema mutation was necessary.
- API and Web: `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release --no-restore` passed. Both builds: zero warnings/errors. API: **23 passed, 0 failed, 0 skipped**. Web: **5 passed, 0 failed, 0 skipped**.
- API SQL test explicitly enabled against the approved database. Existing HTTP integration test covered List/Get/Create/Update/Delete through SqlWorkItemStore/procedures, Reader read, and Reader create denial. Synthetic signing keys remained in test-process memory; test trust was not deployed. The test removed its own created row.
- Baseline/final row count: 6. Ordered full-row JSON SHA256 before/after: `DE7E44A1F5E8FAE1FA00A8A24861F169B9A9BEC3CC5DBEDAF3BA8104155B5333`. Existing rows unchanged; no fixture reset.
- SQL integration: **PASS in automated TestServer under the inspection identity**. Deployed IIS SQL integration: **NOT RUN**, runtime connection/permissions unconfigured.

### Final integrity and limitations

- API/Web sites and pools Started. Auth site/pool Started, same physical path `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920` and pool `LabAuthServerAppPool`; no Auth changes were made.
- Deployed byte parity against ZIP: API 49/49, Web 197/197; no extra files. ZIP hashes unchanged.
- Pattern scans of source/tests and deployed text configuration found no private-key PEM, raw JWT, literal bearer token, password assignment, listed TLS bypass, or inline app-schema CRUD SQL in C#. These are bounded pattern-scan results, not exhaustive secret-detection claims.
- No application source edits, private-key export, persisted JWT/password, or debug script. Required public key/runtime JSON remain in ProgramData.
- Cleanup of `C:\Apps\Temp\Phase9B-ConfigProbe` was rejected by automatic approval review with `blocked by policy`. The process is stopped; this directory remains with extracted release files only. No probe dump or secret was written there. Cleanup is outstanding; no alternate deletion mechanism was attempted.
- Git status and diff checks run for all three repositories; diff checks passed. Existing API/Web untracked project trees and Auth untracked Phase 8 documentation remain. Ordinary diff does not cover untracked content.
- Updated document: this record. Phase 10: **NOT STARTED**.
- Recovery baseline: empty API/Web Current directories and empty pool environment collections. No rollback was needed; any later rollback must affect only API/Web and preserve Auth/database state.

## Earlier Phase 9B configuration investigation, 2026-09-22 (historical)

**PHASE 9B BLOCKED — EXTERNAL API CONFIGURATION STILL UNAVAILABLE.**

The latest resumed investigation again found no existing approved API external configuration in the inspected locations. Deployment was not retried. Both ZIPs match their respective checksum files. The latest instruction explicitly says to ignore the repeated API hash typo, so it is no longer treated as an artifact blocker. Earlier entries are historical and do not describe the current target state.

### Configuration evidence

- No `LABAPI_CONFIG_PATH`, `JwtValidation`, `Database`, or `ConnectionStrings` variables were found in the current process, machine, or current-user environment scopes.
- API and Web IIS application pools each have zero configured environment variables. Neither packaged `web.config` supplies environment variables.
- API/Web operational roots contain `Current`, `Releases`, `Scripts`, and `Source`; no external configuration directory exists there. Both Scripts directories are empty.
- No API/Web configuration directory was found under ProgramData. The only matching application directory there is Auth-owned and was not reused. The checked workspace temporary files contain release verification copies, with no matching API external configuration references in JSON, PowerShell, or config files. No matching Phase/Lab temporary directory was found under the current user's local Temp directory.
- Phase 9A explicitly records external configuration as NOT ESTABLISHED. Phase 6 documents process-only validation and no configured database target; it supplies no persistent IIS configuration location.
- Packaged API `appsettings.json` contains only `Logging` and `AllowedHosts`. No deployment configuration was added to source, release artifacts, or Current.

### Actual source configuration contract

- `Program.cs` uses `WebApplication.CreateBuilder(args)` and then, if `LABAPI_CONFIG_PATH` is nonempty, adds that JSON file with `optional: false` and `reloadOnChange: false`. The path variable itself is not explicitly mandatory in the implementation. A supplied missing file fails loading.
- External JSON is appended after the default providers; its values therefore override duplicate environment/command-line settings. There is no subsequent `AddEnvironmentVariables`. This differs from the development plan's proposed environment-overrides-last order; no loader change was made.
- JWT options validate on startup: HTTPS issuer, nonempty audience and signing algorithm, clock skew 0–600, and an importable RSA key are required. Defaults are RS256 and 300 seconds. `PublicKeyPem` takes precedence over `PublicKeyPath`.
- Required deployment trust values remain `Issuer = https://DC01.lab.local` and `Audience = LabAuthServer.API`. No external values or API public key were available to verify, including correspondence with Auth's accepted signing key.
- Database options validate on startup, but an absent connection string is accepted. SQL use then fails in `SqlConnectionFactory`. A supplied connection string must enable encryption, disable TrustServerCertificate, and specify an initial catalog; command timeout is 1–60 seconds, default 30.
- Missing deployment categories: an approved external JSON file and its IIS-accessible path/ACLs; the JWT issuer/audience and verified public verification key; and an approved API runtime database connection/authentication configuration. No credentials were invented or copied.
- The previously approved test target is `DC01 / LabAPIServer_Test`; no configured runtime target or access under the API pool identity was established by this investigation. No database connection, migration, or data mutation was attempted. The stored-procedure architecture remains unchanged.

### Fresh artifact gate

| Artifact | Actual SHA256 | Matches SHA256SUMS.txt |
| --- | --- | --- |
| API | `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` | PASS |
| Web | `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b` | PASS |

The literal pasted API value, `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeec6c1e85`, is **62 characters** and omits `be` before the final `6c1e85`. The actual 64-character hash matches both the checksum file and Phase 8 record. Following the latest explicit instruction to ignore that typo, the artifact gate is PASS. Neither ZIP was rebuilt or modified.

### Result and preserved state

- API/Web sites and pools: Started; both Current directories contain zero entries.
- Auth site: Started at `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`, pool `LabAuthServerAppPool`; no Auth changes were made.
- Deployment, extraction, IIS changes, and deployed smoke tests: NOT RUN because the required external configuration is unavailable.
- Auth live verification: NOT RUN.
- SQL integration: NOT RUN.
- Restore, build, automated API/Web tests, and final source/deployment scans: NOT RUN in this investigation; the explicit missing-configuration stop condition applies before deployment and its final integrity gate. No application implementation changed.
- `git status --short` and `git diff --check` were run for all three repositories; diff checks passed. API/Web already contain untracked project trees, and Auth already contains untracked `docs/plans/Phase-8/`. These are not new changes from this investigation; ordinary Git diff does not cover untracked content.
- Changed by this investigation: this record only. No secrets, credentials, JWTs, or keys were created or persisted. Phase 10: NOT STARTED.

The latest read-only recheck confirmed the same absent process/machine/user configuration variables, empty API/Web pool environment collections, absent API/Web external configuration directories, and empty Current directories. API, Web, and Auth sites and pools remain Started. The existing approved isolated database is recorded in Phase 9A as `DC01 / LabAPIServer_Test`; current availability was not probed because the missing-configuration stop condition applies. This does not establish a configured or authorized API runtime SQL identity.

Resume requires an existing approved API configuration location with the above trust/database settings. Do not redeploy the known-incomplete configuration. The artifact checksum is no longer a blocker.

## Original Phase 9 decision (historical)

**PHASE 9 BLOCKED - API and Web deployment targets are not established.**

No deployment was performed. The blocker was found during read-only target inspection before any IIS, filesystem deployment, database, source, configuration, or test-data change.

## Artifact gate

All Phase 8 artifacts were independently verified before the deployment decision:

| Application | Artifact | SHA256 | Entries | Result |
| --- | --- | --- | ---: | --- |
| LabAuthServer | `C:\Apps\LabAuthServer\Releases\v1.0.0-preparation-20260913-212903\LabAuthServer-1.0.0.zip` | `564f5be016e7f679c32751c4f30488b8482ca57ccec8207c81802a8aee73f6a0` | 51 | PASS |
| LabAPIServer | `C:\Apps\LabAPIServer\Releases\v1.0.0\LabAPIServer-1.0.0.zip` | `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` | 49 | PASS |
| LabWebAppServer | `C:\Apps\LabWebAppServer\Releases\v1.0.0\LabWebAppServer-1.0.0.zip` | `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b` | 197 | PASS |

The artifacts contain no `bin`, `obj`, `.git`, `.vscode`, development settings, or PDB files. API migration files are present in the API artifact. No deployment artifact was modified or republished.

## Target inspection

- IIS site `LabAuthServer`: Started; physical path `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`; existing HTTPS binding `*:443:DC01.lab.local`; existing HTTP binding `*:80:DC01.lab.local`.
- IIS application pool `LabAuthServerAppPool`: Started.
- API IIS site/application: **NOT PRESENT**.
- Web IIS site/application: **NOT PRESENT**.
- API/Web application pools: **NOT PRESENT**.
- API/Web deployment paths: `C:\Apps\LabAPIServer\Current` and `C:\Apps\LabWebAppServer\Current` do not exist.
- API/Web listeners: no listeners were present on ports 7196, 7168, 7153, or 7268.
- Auth was not redeployed because no current Phase 9 plan authorizes changing the existing validated Auth deployment, and the API/Web target is incomplete.

The Phase 7 API/Web endpoints were process-only validation endpoints. No approved persistent IIS or service target exists for Phase 9 controlled deployment. Creating sites, pools, bindings, identities, paths, certificates, or external configuration would invent deployment decisions and could affect shared infrastructure.

## Database gate

- Approved database identified by the records: `DC01 / LabAPIServer_Test`.
- Migration execution: **NOT RUN** because deployment is blocked before the database gate.
- Test data creation: **NOT RUN**.
- Production database: **NOT TOUCHED**.
- No connection was opened and no schema or data was changed.

## Validation not run

Auth login, API live authorization/CRUD, stored-procedure runtime confirmation, Web login/session/work-item flow, IIS log inspection for a new deployment, rollback execution, and post-deployment smoke were **NOT RUN** because no API/Web deployment target exists.

The source architecture remains unchanged: API CRUD remains stored-procedure based, Web has no SQL client or JWT validation implementation, and Auth source was not modified.

## Files and systems changed

- Changed: this Phase 9 record only.
- Unchanged: LabAuthServer source and deployment; LabAPIServer source and migration files; LabWebAppServer source; IIS sites, pools, bindings, certificates, identities, and configuration; all databases and test data; release ZIPs and checksums.
- Phase 10: **NOT STARTED**.

## Required prerequisite to resume

Establish and document the approved API and Web deployment targets, including site/application names, physical paths, application pools and identities, HTTPS bindings/certificates, external configuration locations, and a tested rollback path. Then restart Phase 9 target inspection from the beginning.

## Phase 9B deployment attempt - 2026-09-22

**PHASE 9B BLOCKED.** Deployment stopped at the mandatory artifact hash gate before stopping IIS applications or extracting any ZIP.

- API actual SHA256: `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeec6c1e85`
- API SHA256 supplied for this attempt: `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeec6c1e85`
- API hash result: **MISMATCH**
- Web SHA256 result: **PASS** (`a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b`)
- API/Web `Current` files before and after the gate: 0
- Deployment, extraction, IIS stop/start, database migration, test data, Auth change, and Phase 10: **NOT RUN**

The API artifact itself remains unchanged. Resume only after the authoritative API checksum is corrected or the artifact is replaced through an explicitly approved release process, followed by a fresh hash verification.

## Hash gate resolution - 2026-09-22

The apparent mismatch was a comparison-value typo, not an artifact defect. The authoritative API SHA256 is recorded both in the Phase 8 release record and `C:\Apps\LabAPIServer\Releases\v1.0.0\SHA256SUMS.txt` as:

`8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85`

Fresh normalized comparison:

| Artifact | Actual | Authoritative expected | Match |
| --- | --- | --- | --- |
| API | `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` | `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` | PASS |
| Web | `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b` | `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b` | PASS |

The prior supplied API comparison value was 62 characters long and omitted `be` before `6c1e85`. API artifact size remains 4,947,001 bytes with last write `2026-09-21T17:58:42.1776446Z`; Web artifact size remains 5,396,591 bytes with last write `2026-09-21T17:58:48.3514032Z`. Neither ZIP was rebuilt, republished, extracted, or modified during this investigation.

Deployment remains **NOT RUN** by this task, as requested. The corrected hash gate is ready for a separate Phase 9B deployment attempt.

## Phase 9B controlled deployment attempt - 2026-09-22

**PHASE 9B BLOCKED.** The artifacts passed the authoritative hash gate and were extracted, but API startup validation failed and the combined deployment was rolled back.

### Artifacts

- API SHA256: `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` - PASS.
- Web SHA256: `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b` - PASS.
- ZIP contents were unchanged; deployed file parity was API 49/49 and Web 197/197 before rollback.

### Deployment and rollback

- API site/pool stopped, API ZIP extracted to `C:\Apps\LabAPIServer\Current`, then API startup failed.
- Web ZIP extracted to `C:\Apps\LabWebAppServer\Current`; Web site started successfully.
- Web smoke initially returned HTTP 200 for `/`, `/health`, and `/Account/Login`.
- API `/api/v1/health` and `/api/v1/session` returned HTTP 500.
- IIS/ANCM diagnostics identified the concrete failure: missing external `LABAPI_CONFIG_PATH` configuration. Required settings include `JwtValidation:Issuer`, `JwtValidation:Audience`, `JwtValidation:PublicKeyPem` or `PublicKeyPath`, and database configuration.
- Both API and Web were stopped, their `Current` directories restored to the known empty pre-deployment state, and both IIS sites/pools restarted.
- Final rollback state: API Current files 0; Web Current files 0.

### Not run

- Auth live verification: NOT RUN; credentials were not requested or stored.
- SQL integration and Work-item CRUD: NOT RUN; deployment failed before API startup and no database mutation was performed.
- API 200 health and unauthenticated 401 session checks: NOT PASS; both returned 500 before rollback.
- Automated restore/build/test suites: NOT RUN in this deployment attempt.
- Phase 10: NOT STARTED.

Auth IIS site, pool, physical path, binding, source, and release artifact remained unchanged. No migration, test data, credentials, JWT, or external API configuration was created.
