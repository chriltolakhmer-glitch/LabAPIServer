# Phase 11 - Deployed SQL runtime and Work-item acceptance

Date: 2026-09-22 (Asia/Bangkok).

**PHASE 11 COMPLETE — FINAL CREDENTIALED END-TO-END ACCEPTANCE VERIFIED.** The deployed Web/Auth/API/SQL path passed with the authorized Operator account. The API uses the approved `DC01/LabAPIServer_Test` target through integrated security and stored procedures; no schema, procedure, Auth or application-source change was made.

## Scope and records inspected

Authority: the owner's Phase 11 request, with the existing stored-procedure architecture preserved. Read project AGENTS.md files, API/Web/Auth READMEs, the database README/migration, Phase 4 contract, Phase 6 integration, Phase 9 deployment and Phase 10 recovery records. Reviewed current API store/service/endpoints/JWT middleware and Web login/session/client/page implementations. Earlier README statements that deployment/login do not exist are stale; current source, dated records and IIS inspection establish the actual state below.

This record does not downgrade Phase 10's completed isolated recovery exercise or claim that its inspection-identity SQL tests prove IIS runtime access. No Phase 12 or new feature was started.

## Deployment

| Service | IIS site / pool | Active path | HTTPS |
| --- | --- | --- | --- |
| API | LabAPIServer / LabAPIServerAppPool | `C:\Apps\LabAPIServer\Current` | `https://localhost:7196` |
| Web | LabWebAppServer / LabWebAppServerAppPool | `C:\Apps\LabWebAppServer\Current` | `https://localhost:7153` |
| Auth | LabAuthServer / LabAuthServerAppPool | `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920` | `https://DC01.lab.local` |

All three sites/pools were Started. API/Web use ApplicationPoolIdentity; Auth retains its existing `LAB\svc_labauth` identity. Auth's HTTP/HTTPS host bindings remain unchanged. The API HTTPS binding was updated from the untrusted self-signed localhost certificate to CA-issued certificate `75B3DD9DA10C679511C62DD6149A8C671A70989F` from `LAB-ROOT-CA`; Web binding and Auth were unchanged.

API pool variable `LABAPI_CONFIG_PATH` selects `C:\ProgramData\LabAPIServer\labapi-runtime.json`. Its database connection targets `tcp:DC01,1433/LabAPIServer_Test` with Windows Integrated Security, `Encrypt=True`, `TrustServerCertificate=False`, and a 30-second timeout. External JSON is added after default configuration providers and overrides duplicate default-provider/environment values; reload is disabled. API public verification key exists, issuer is `https://DC01.lab.local`, audience `LabAuthServer.API`, algorithm RS256.

Web pool retains its Auth/API URL overrides. No runtime database setting was invented or copied into source, ZIPs or Current.

## SQL gate and exact blocker

- Known documented isolated API test target: server `DC01`, database `LabAPIServer_Test`.
- Read-only inspection identity: Windows integrated authentication as the inspection user, with SQL encryption and normal certificate validation (`Encrypt=True;TrustServerCertificate=False`, or `sqlcmd -E -N`). This is **not** the IIS worker identity.
- IIS site `LabAPIServer` is Started at `C:\Apps\LabAPIServer\Current`, using Started pool `LabAPIServerAppPool` with `ApplicationPoolIdentity`.
- The live `w3wp.exe` for that pool runs as `IIS APPPOOL\LabAPIServerAppPool`. `LAB\DC01$` is a separate existing Windows SQL login and is not the API worker identity.
- `IIS APPPOOL\LabAPIServerAppPool` has a mapped database user in `LabAPIServer_Test`, database `CONNECT`, and explicit `EXECUTE` on exactly the five `app.WorkItems_*` procedures. Under `EXECUTE AS LOGIN`, all five procedure checks returned allowed, while direct table `SELECT`, `INSERT`, `UPDATE`, and `DELETE` returned denied.
- `LAB\DC01$` remains unmapped in `LabAPIServer_Test`; using it for diagnosis would produce:

```text
Msg 916, Level 14, State 4, Server DC01
The server principal "LAB\DC01$" is not able to access the database "LabAPIServer_Test" under the current security context.
```

This proves that the inspected machine-account security context cannot access the test database; it is not a claim that an actual IIS SQL handshake was attempted or that this is necessarily the final chosen runtime principal. The runtime identity/transport and its approved database permissions still need to be established.

The target is the approved isolated API test database, online and owned by `LAB\Administrator`; it is not `master`, `LabAuthServer`, or a shared production database. No SQL password was used or exposed. No additional login, role membership, pool identity change, schema change, or grant was required during this verification because the existing mapped user already has the required procedure-only permissions.

Resume requires an explicitly approved deployed database/runtime principal and sufficient existing access, or explicit authorization to provision narrowly scoped runtime permissions despite the current stop condition. The API needs only the established procedure operations, not db_owner or application-inline CRUD SQL. Do not substitute master, LabAuthServer, or the removed Phase 10 disposable database.

## Read-only schema and stored-procedure verification

The known isolated target contains six existing rows. Inspected schema matches the Work-item contract:

- `WorkItemId uniqueidentifier`, required.
- `Name nvarchar(200)`, required; `Description nvarchar(2000)`, nullable.
- `Status nvarchar(32)`, required.
- `CreatedAtUtc`/`UpdatedAtUtc datetime2`, required.
- `CreatedBy`/`UpdatedBy nvarchar(1024)`, required.
- `PK_WorkItems` and `IX_WorkItems_Status_UpdatedAtUtc` are present.

Each current SQL module definition matches its source migration 0001 procedure after whitespace and CREATE/CREATE OR ALTER normalization:

| Procedure | Definition matches source |
| --- | --- |
| `app.WorkItems_List` | PASS |
| `app.WorkItems_Get` | PASS |
| `app.WorkItems_Create` | PASS |
| `app.WorkItems_Update` | PASS |
| `app.WorkItems_Delete` | PASS |

No migration or procedure replacement was needed or executed. This read-only result is not a procedure execution/persistence test under IIS.

`SqlWorkItemStore` still calls exactly these procedures with typed parameters and `CommandType.StoredProcedure`. C# contains no inline Work-item CRUD SQL. The role policies permit Reader/Operator/Administrator reads and Operator/Administrator writes. Web uses server-held tickets/opaque JWTs and API-verified session identity; no new abstraction or UI behavior was added.

## Authentication, CRUD, persistence and Web acceptance

| Required acceptance | Result |
| --- | --- |
| Real Auth login / API authenticated session | PASS; Auth `200`, API session `200`, subject `test.itd@lab.local`, role `Operator`. |
| Reader list/get and create/update/delete 403 | NOT RUN on deployed services. No authorized Reader account/credential was established in this run; Phase 6 also records Reader as untested. |
| Operator CRUD | PASS; deployed API Create `201`, List `200`, Get `200`, Update `200`, Delete `204`. |
| Direct stored-procedure create/list/get/update/delete | PASS through the existing API SQL acceptance; additional read-only IIS-identity execution of `app.WorkItems_List` succeeded. |
| SQL row existence after create, changed values after update, absence after delete | PASS; independent stored-procedure reads verified create/update/delete, and final query found zero Phase 11 fixture rows. |
| Web login/list/create/update/detail/delete/logout and post-logout denial | PASS; login `302`, authenticated list `200`, create/update/delete `302`, detail `200`, logout `302`, post-logout redirect `302` to `/Account/Login?ReturnUrl=/`. |
| Administrator | NOT RUN; not required by this Phase 11 acceptance. |

Fixture purpose was disposable Phase 11 persistence verification using the actual `{name, description, status}` contract. Two generated fixture IDs were cleaned up through the existing stored procedure path; final SQL query found zero matching Phase 11 fixture rows and total Work-item count remained six. No unrelated row, account, credential or AD membership was changed.

## Validation

Normal HTTPS certificate validation, no bypass:

| URL | Result |
| --- | --- |
| `https://localhost:7196/api/v1/health` | 200 |
| `https://localhost:7196/api/v1/session` without token | 401 |
| `https://localhost:7153/` | 200 after login redirect |
| `https://localhost:7153/health` | 200 |
| `https://localhost:7153/Account/Login` | 200 |

Health proves liveness only.

API commands, from its repository:

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-restore --filter 'FullyQualifiedName!~LocalDbIntegrationTests'
```

Phase 11 rerun used the approved target with SQL opt-in:

```powershell
$env:LABAPI_RUN_SQL_TESTS='1'
$env:LABAPI_TEST_CONNECTION_STRING='Server=tcp:DC01,1433;Database=LabAPIServer_Test;Integrated Security=True;Encrypt=True;TrustServerCertificate=False'
dotnet test -c Release --no-restore
```

Result: API `23 passed, 0 failed, 0 skipped`; Web `5 passed, 0 failed, 0 skipped`. After recycling only `LabAPIServerAppPool`, deployed API smoke checks returned health `200` and anonymous session `401`.

Final credentialed acceptance rerun: Auth login `200`; API session `200`; API CRUD and SQL persistence PASS; Web login `302`; authenticated list `200`; Web create/update/delete `302`; detail `200`; logout `302`; post-logout protected access `302` to `/Account/Login?ReturnUrl=/`; cleanup PASS. The harness initially compared the absolute redirect as a relative string, so the recorded boolean was false despite the correct `302` target; the temporary harness assertion was corrected to validate the URI path. No application behavior was changed.

Restore PASS; Release build PASS, zero warnings/errors; **22 passed, 0 failed, 0 skipped** in the selected non-SQL suite. The one opt-in SQL integration test was explicitly excluded under the unresolved deployed SQL gate, not counted as passed. Full 23-test SQL acceptance was NOT RUN in this phase.

Web commands, from its repository:

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-restore
```

Restore PASS; Release build PASS, zero warnings/errors; **5 passed, 0 failed, 0 skipped**. No test was modified. These source build/test outputs were not published or copied to Current or release ZIPs.

## Artifact/security/integrity evidence

- API ZIP SHA256 `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85`, authoritative checksum match; all 49 Current files match ZIP entries, no extra files.
- Web ZIP SHA256 `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b`, authoritative checksum match; all 197 Current files match ZIP entries, no extra files.
- Bounded scans of 101 source/test/deployed text files: zero private-key PEM, raw JWT, literal bearer JWT, nonempty quoted-password assignment or listed TLS-bypass matches. These are pattern scans, not an exhaustive secret audit.
- API inline CRUD SQL scan: zero matching application files. Web JWT-validator/SQL-client/localStorage/sessionStorage boundary scan: zero matching source files.
- Only this Phase 11 record is intentionally added. Existing untracked changes are preserved; ordinary Git diff does not cover them, so source/deployment/configuration SHA256 manifests supplement repository checks.

Final integrity: all protected source/release groups matched their baselines, excluding this Phase 11 documentation and temporary harness changes. Existing API/Web/Auth source files, Current contents, release ZIPs, Auth deployment, API runtime JSON/public key, and Auth IIS configuration are unchanged. The API IIS binding intentionally changed only to the validated CA-issued localhost certificate above. All sites/pools remain Started. No release rebuild, deployment, SQL grant, or Auth modification was performed.

`git status --short` and `git diff --check` ran in all three requested repositories; every diff check passed. Existing API/Web untracked project trees and Auth's untracked `docs/plans/Phase-8/` remain. This new record sits inside the already-untracked API docs tree. Its Markdown whitespace/fences also passed explicit checks. No commit was made.

Final SQL read-only check: six original rows remain; ordered full-row SHA256 `0AEA8BC14A054BA0DF3136F84823950CA90210D91B7F70F8B680B26D049A519F` matches Phase 10's preserved baseline. Ledger versions 0/1 retain hashes `58564DD50B49D082DF926267D41F084AB440A5AA0D01B6FDCB8761C686972EE1` and `35708A34D658C1C6308F253DA15A35B7A46229CFF9EEA80A3C4F6B45DC8D5E84`. No Phase 11 database or fixture was created. Final temporary/test-process count: zero. No temporary Phase 11 file was created. No Phase 12 work was started.

**PHASE 11 COMPLETE — FINAL CREDENTIALED END-TO-END ACCEPTANCE VERIFIED.** The approved target, live IIS identity, CA-issued TLS, integrated-security configuration, database mapping, stored-procedure persistence, Web login/session/CRUD/logout, and post-logout denial all passed. API non-SQL tests passed `22/22`, Web tests passed `5/5`, final SQL fixture count is zero, Auth remains unchanged, and no Phase 12 work was started.
