# Phase 10 - Operations and final acceptance

Date: 2026-09-22 (Asia/Bangkok); inspection began at 2026-09-21T19:05:19Z.

**PHASE 10 COMPLETE — IIS SQL RUNTIME VALIDATION NOT RUN.** The resumed recovery acceptance below supersedes the initial blocked inspection. Completion covers the owner-approved, isolated same-host recovery exercise; it does not assert production SQL persistence, production rollback or host-loss recovery.

## Resumed recovery acceptance - 2026-09-22

The owner confirmed the recommended **RPO 24 hours / RTO one working day** and explicitly authorized creating and dropping only `DC01 / LabAPIServer_Phase10_Recovery_20260922`. The populated `DC01 / LabAPIServer_Test` database was preserved. Auth has no corresponding project Phase 10 operations record; its existing deployment/runbook remain unchanged dependencies.

### RPO and RTO

| Requirement | Confirmed target | Observed result | Result and evidence |
| --- | --- | --- | --- |
| RPO | Maximum data loss 24 hours | All six deterministic rows recovered; zero fixture loss, identical ordered full-row SHA256 before/after restore and after CRUD tests | PASS for this exercise. Owner reply to the RPO/RTO question: "use the recommend choice"; shared-plan D5 supplies the recommended values. |
| RTO | Maximum recovery duration one working day | 289.791 seconds (4 minutes 49.791 seconds) from recovery-set/file preparation to restored-database checks/tests and both recovered services' final availability/parity checks | PASS for this representative same-host exercise. No invented working-day hour conversion is needed for this under-five-minute observation. |

These measurements do not establish an operating backup schedule, protection of future business writes, production recovery time or off-host recovery. Before real Work-item data is accepted in IIS, establish ongoing backups consistent with the confirmed RPO. The synthetic backup retained here is not a production-data backup.

| Event | Observed UTC |
| --- | --- |
| Recovery preparation began | 2026-09-21T19:19:05.3377917Z |
| Recovered files/configuration ready | 2026-09-21T19:19:06.4738916Z |
| First API/Web HTTPS checks complete | 2026-09-21T19:20:50.1851296Z |
| Isolated SQL exercise began | 2026-09-21T19:21:28.2859779Z |
| Migrations/fixture complete | 2026-09-21T19:21:29.4265986Z |
| Actual SQL drop/restore verification began | 2026-09-21T19:22:07.9606888Z |
| Restore and database integrity checks complete | 2026-09-21T19:22:08.4450451Z |
| Final recovered Web HTTPS check | 2026-09-21T19:23:54.3310505Z |
| Recovery availability/file parity verification complete | 2026-09-21T19:23:55.1285208Z |

SQL drop/restore/integrity verification took 0.484 seconds wall-clock; SQL's own restore statement reported 0.008 seconds. Cleanup/record finalization happened afterward, including a policy-review interruption, and is not included in the availability measurement. Availability is an observed check time, not a claim of continuous monitoring.

### Actual recovery set

Protected location: `C:\ProgramData\LabAPIServer\Recovery\Phase10-20260922`. Directory inheritance is disabled; only SYSTEM and Administrators have full control. This is a deliberately retained recovery artifact set, not a leftover probe.

- `API/LabAPIServer-1.0.0.zip` and its authoritative checksum file.
- `Web/LabWebAppServer-1.0.0.zip` and its authoritative checksum file.
- `Configuration/labapi-runtime.json` and public-only `auth-signing-public.pem`: copied from current API configuration only after confirming no connection string/password/private key. The original path is retained for same-host restoration.
- `Configuration/pool-environment.json`, `iis-api-web-inventory.json`, `tls-certificate-information.json`: scoped API/Web URL/configuration settings, hosting metadata, binding and certificate references; no complete machine IIS export or Auth secrets.
- SQL migration scripts (versions 0/1) and the verified synthetic `SQL/LabAPIServer_Phase10_Recovery_20260922.bak`.
- Current API/Web runbooks, recovery instructions/evidence, and a SHA256 manifest covering every retained file except the manifest itself.

API/Web ZIP hashes remain those in the initial evidence below. Synthetic SQL backup SHA256: `9631DC84BADDEECC9A70470D2CB0B354574D27F4E225B1E7E7D89C62303CC8F9`.

The set is complete for the exercised same-host API/Web recovery scope. The existing .NET/IIS installation, HTTPS private key in certificate stores and unchanged Auth service are prerequisites, not recovered infrastructure. No certificate private key, Auth configuration/credential, live user ticket or raw token was copied. Auth's release is not part of this unchanged-Auth recovery exercise; its existing deployment is identified as a dependency. Host-loss recovery remains NOT RUN.

### Isolated SQL restore

The first connection to `(localdb)\MSSQLLocalDB / LabAPIServer_Test` using `sqlcmd -E -N` failed with `Encryption not supported on SQL Server`. No TLS weakening was attempted. That connection auto-started the previously stopped LocalDB instance; it was stopped gracefully again. The owner then explicitly authorized the uniquely named DC01 database above.

1. Confirmed the disposable name was absent. Created only that new database, using an administrative connection whose initial catalog was the approved existing test database; neither master nor Auth was used as a test target.
2. Applied unchanged source migrations 0000/0001 within a transaction and exclusive `sp_getapplock`; recorded their exact SHA256 values in `dbo.SchemaMigrations`.
3. Verified `app.WorkItems`, `IX_WorkItems_Status_UpdatedAtUtc` and all five required `app.WorkItems_*` procedures.
4. Created six deterministic synthetic rows using `app.WorkItems_Create`: IDs `10101010-2026-4092-8000-000000000101` through `...106`, names `[RECOVERY TEST] Item 1` through `6`, repeating Open/InProgress/Complete statuses, fixed 2026-01-01 UTC timestamps and subject `phase10-recovery`. No existing fixture-reset script was run against DC01.
5. Made a real `BACKUP DATABASE ... WITH COPY_ONLY,CHECKSUM`, ran `RESTORE VERIFYONLY`, dropped only the disposable database after checking for user sessions, and restored it from that backup with `RECOVERY,CHECKSUM`.
6. `DBCC CHECKDB ... WITH NO_INFOMSGS` passed. The six rows, procedure count and migration hashes survived restoration. Ordered full-row hash before restore, after restore and after integration tests: `C268EE3D80A71CC708A519B6A00B6486DCC1D5F3DA3DE45155CC6898558A9F6B`.
7. Existing full API Release tests passed **23/23**, zero failures/skips, explicitly selecting this restored database. The real `SqlWorkItemStore`/procedure path exercised List/Get/Create/Update/Delete, Reader read and Reader write denial. Synthetic JWT signing keys stayed in the TestServer process, not the recovered or IIS service trust configuration.
8. Dropped only the disposable database after tests/services stopped. Its MDF/LDF and exercise staging backup are absent. The checksum-verified synthetic backup copy remains in the protected recovery set.

The original `DC01 / LabAPIServer_Test` still has six rows and ledger versions 0/1; its before/after ordered full-row hash is `0AEA8BC14A054BA0DF3136F84823950CA90210D91B7F70F8B680B26D049A519F`. No unrelated user database was modified. Normal SQL system backup/catalog metadata is an expected consequence of the explicitly authorized database lifecycle.

### Representative service recovery

Exact copied ZIPs were extracted to `C:\Apps\Temp\Phase10-ServiceRecovery-20260922\API` and `Web`; all 49/197 files matched Current and archive-qualified artifacts. Separate recovered API configuration used the copied public verification key. A later exercise-only override selected the restored disposable SQL database with Windows integrated authentication, `Encrypt=True;TrustServerCertificate=False`. No deployed setting was changed.

Both services ran in Production as foreground Kestrel processes under the inspection identity, using the existing trusted localhost certificate from CurrentUser/My (thumbprint `F392B4D1ED31361D16576700E03CFD26921E263F`). No certificate was exported, replaced or rebound. The recovered Web API URL selected the recovered API. Ports 17196/17153 were separate from deployed IIS ports 7196/7153.

| Recovered HTTPS URL | Result |
| --- | --- |
| `https://localhost:17196/api/v1/health` | 200 |
| `https://localhost:17196/api/v1/session` without token | 401 |
| `https://localhost:17153/` | 200 after login redirect |
| `https://localhost:17153/health` | 200 |
| `https://localhost:17153/Account/Login` | 200 |

All requests used normal HTTPS validation. Both process pairs stopped gracefully via Ctrl+C, with `Application is shutting down` observed. No recovery listener/process remains. This proves representative service startup/availability, not recovery under the actual IIS pool identity or full production rollback.

Authenticated session/role behavior was tested by the existing API tests; application DLLs loaded by tests match the recovered release DLLs. A fresh real Auth-account login against the recovered HTTPS services was **NOT RUN** because no authorized credentials were supplied. No real password/JWT was requested through chat, persisted or printed. This conditional credentialed check is not represented as live Auth acceptance.

### Recovery commands and final integrity

Detailed recovery instructions and exact key commands are retained in the recovery set's `Recovery-Evidence.md`. Automated gates used existing binaries (no application code changes or release rebuild required):

```powershell
# API repository; process-scoped variables in the test shell only.
$env:LABAPI_RUN_SQL_TESTS='1'
$env:LABAPI_TEST_CONNECTION_STRING='Server=DC01;Database=LabAPIServer_Phase10_Recovery_20260922;Integrated Security=True;Encrypt=True;TrustServerCertificate=False'
dotnet test -c Release --no-build --no-restore
# Web repository, separate shell.
dotnet test -c Release --no-build --no-restore
```

API: 23 passed; Web: 5 passed; no failures/skips. Fresh restore/build/publish remains NOT RUN because this phase changed no application implementation and used byte-matching existing binaries.

Automatic approval review rejected an initial combined cleanup command with `blocked by policy`; it did not execute. After read-only verification, smaller commands targeting the single authorized database, the literal validated exercise directory and the hash-verified staging backup succeeded. No security policy or protection was disabled or bypassed.

Final checks: existing source (excluding the four Phase 10 Markdown files), API/Web Current/releases, Auth source/deployment, original API runtime configuration and IIS applicationHost.config match baseline hashes. API/Web/Auth sites and pools remain Started with unchanged paths/bindings. Deployed HTTPS checks still return 200/401/200/200/200. No temporary script/process/directory, disposable database or staging backup remains. All three repository status/diff checks and bounded source/recovery-set security scans are recorded at finalization. Only the four Phase 10 Markdown files and the retained recovery set were changed/created.

> IIS runtime SQL configuration remains unavailable, therefore deployed Work-item persistence has not been live-verified.

Remaining disclosed NOT RUN items: IIS-identity SQL persistence, credentialed recovered-service Auth login, production rollback, off-host/host-loss recovery and optional checker. The approved isolated recovery acceptance is complete; no subsequent phase was started.

Final recovery-set check: **16 retained files verified** against `SHA256SUMS.txt` (manifest excluded from its own entries). Manifest SHA256: `7001DACA137A586FCA8BE785075DCFD21E56E3E93E3D7A2280B4EB48EAB8EEDD`. Retained runbook copies point to the set-local Recovery-Evidence.md; their links were verified. Restricted ACL contains only SYSTEM and Administrators full control, no inherited entries. The original release ZIPs and their recovery copies match. Bounded regex scans across **116 source/test/deployed/recovery-set text files** found zero private-key PEM, raw-JWT, literal bearer-JWT, nonempty quoted-password or listed TLS-bypass matches. Binary backup contents are synthetic by construction and verified by restore, not by treating binary data as text. No secrets were committed; no commit was made.

## Initial acceptance inspection (historical)

The following original inspection/evidence remains for traceability. Its blocked decisions and unavailable-resource statements describe the state before the owner decisions and recovery exercise above.

## Authoritative scope and acceptance

Neither repository contained a dedicated Phase 10 plan. The authoritative scope is [Shared System Architecture and Delivery Plan, Phase 10 - Operations](../Shared-System-Architecture-and-Delivery-Plan.md#phase-10--operations), together with its recovery/operations sections and common Definition of Done. Both project plans point there. The general solo-developer roadmap's differently named Phase 10 is not the project-specific scope.

Objective: make normal operation and recovery practical for one developer. Deliverables: short runbooks/recovery checklists, verified health/config/log locations, backup hashes, isolated SQL/file restoration, rollback artifact/config/schema compatibility and representative scoped recovery evidence. Its DoD states: **"A required unperformed recovery test blocks completion"**. Production rollback/off-host recovery may be recorded NOT RUN when not exercised; checksum parity alone does not prove recovery.

This phase needs operational documentation and validation, not new application architecture or release rebuilds. Read applicable AGENTS.md, both project plans, shared plan, Phase 0 decision record, Phase 9A target plan, Phase 9 controlled-deployment record and both Phase 8 packaging records before edits. Dated later deployment evidence supersedes early planning status. The Phase 9 record's old cleanup-blocked statement is historical: the subsequent authorized cleanup succeeded, and `C:\Apps\Temp\Phase9B-ConfigProbe` is absent.

## Completed

- Added [API runbook/recovery checklist](../../operations/LabAPIServer-Operations-Runbook.md) and [Web runbook/recovery checklist](../../../../../../LabWebAppServer/Source/LabWebAppServer/docs/operations/LabWebAppServer-Operations-Runbook.md).
- API/Web/Auth IIS sites and pools were Started; API/Web use their existing Current paths, Auth remains at `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`. No site/pool was restarted or reconfigured.
- Normal HTTPS checks passed; no certificate validation bypass was used.
- Current API external configuration/public-key files exist with restricted directory access; database connection is absent. Web's two pool URL overrides are present. Source loader precedence/restart requirements and incomplete Web key recovery are recorded in the runbooks.
- IIS access-log locations and endpoint records verified. Daily logging enabled. Historical Windows Application/ANCM diagnostics observed; stdout disabled in both deployed web.config files. Fresh application failure capture/retention was not proved.
- Existing Release tests: API **22 passed, 0 failed, 0 skipped**, with the SQL class explicitly excluded; Web **5 passed, 0 failed, 0 skipped**. Existing application Release DLLs match deployed DLLs. This reused existing test binaries; it is not a fresh source rebuild or a new 23/23 API SQL result.
- Both ZIPs match authoritative adjacent checksum files. All API 49/49 and Web 197/197 files match Current, with no extra deployed files.
- Restored both exact packages into `C:\Apps\Temp\Phase10-FileRestore`, compared each restored/deployed file against its ZIP entry SHA256, then removed only that isolated directory. File restoration is TESTED; no restored application was started.
- Phase 9 probe, Phase 10 restore directory and temporary/test processes are absent after validation.

## HTTPS evidence

| URL | Result |
| --- | --- |
| `https://localhost:7196/api/v1/health` | 200 |
| `https://localhost:7196/api/v1/session` without token | 401 |
| `https://localhost:7153/` | 200 after redirect to `/Account/Login?ReturnUrl=%2F` |
| `https://localhost:7153/health` | 200 |
| `https://localhost:7153/Account/Login` | 200 |

Health proves liveness only. HTTPS evidence is for this machine's existing localhost route and trust, not remote browser/production DNS readiness.

## Artifact and security evidence

| Artifact | SHA256 | Files restored/current |
| --- | --- | --- |
| API v1.0.0 | `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` | 49/49 |
| Web v1.0.0 | `a0133a0025e88b6ead17bbbcbec751a80379d8d5c41648690302b516fb34617b` | 197/197 |

Packaged migration hashes still match Phase 9: version 0 `58564DD50B49D082DF926267D41F084AB440A5AA0D01B6FDCB8761C686972EE1`; version 1 `35708A34D658C1C6308F253DA15A35B7A46229CFF9EEA80A3C4F6B45DC8D5E84`. No live ledger query or migration ran.

API `Program.cs` configures issuer, audience, signature/signing-key, lifetime and configured algorithm validation in bearer middleware; the current external algorithm is RS256. Read policy allows Reader/Operator/Administrator; write policy allows Operator/Administrator. The existing tests include invalid signature/issuer/audience/expiry and role behavior. This is verification of the requested existing boundary, not a claim that every proposed trust-profile/rotation scenario has been tested.

`SqlWorkItemStore` uses typed parameters and `CommandType.StoredProcedure` for exactly `app.WorkItems_List`, `app.WorkItems_Get`, `app.WorkItems_Create`, `app.WorkItems_Update`, `app.WorkItems_Delete`. Those definitions exist in migration 0001. Scan of 14 application C# files found no inline CRUD SQL. Web source/project scan found no JWT validation implementation, SQL client or localStorage/sessionStorage use. Login forwards the opaque token to API session verification; `ServerTicketStore` retains token/ticket server-side.

Bounded .NET regex scans of 101 source/test/deployed text files found no private-key PEM, raw JWT, literal bearer JWT, nonempty quoted password assignment or listed TLS-bypass APIs. No credentials/tokens were obtained. These scans are not an exhaustive secret audit. An initial native-command regex invocation had a quoting error/false matches; the final .NET regex scan corrected that invocation and is the reported evidence.

## NOT RUN and blockers

| Item | Reason / consequence |
| --- | --- |
| SQL runtime validation | **NOT RUN. Reason: IIS runtime SQL configuration remains unavailable.** |
| SQL CRUD integration rerun | Explicitly excluded; prior 23/23 isolated test evidence remains historical. No database connection or data mutation in this phase. |
| Required isolated SQL backup restore/integrity/application reads | No identified approved SQL recovery set, named isolated restore destination or restore identity in the records/inspected app locations. No database created or substituted. Required recovery evidence remains missing. |
| Complete configuration/backup-set verification | ZIP hashes pass, but no complete protected app/configuration/SQL recovery set is identified. Live settings are not a backup. Web effective Data Protection persistence/recovery is unverified. |
| Required representative scoped service recovery and rollback compatibility | No established representative recovery target or complete prior working artifact/configuration/schema recovery set. Current ZIP restoration alone is insufficient. |
| Chosen RPO/RTO | D5's 24-hour RPO/one-working-day RTO and retention are proposals only; no confirmed decision found. Recovery-time/data-loss acceptance cannot be asserted. |
| Production rollback / off-host host-loss recovery | NOT RUN; no live disruption or alternate host configured. |
| Fresh Auth login, authenticated deployed roles/CRUD/browser storage/session journey | No authorized credentials supplied; SQL runtime also absent. No synthetic signing trust injected into IIS. |
| Fresh application diagnostic failure capture / retention | Historical logs observed; no production fault injected or logging changed. |
| Restore/build/publish/package regeneration | NOT RUN; no implementation change or Phase 10 rebuild requirement. Existing binaries used for the smallest supplemental test gate. |
| Optional operational checker | NOT RUN / not implemented; the short manual runbooks suffice. |

## Known limitations and next action

> IIS runtime SQL configuration remains unavailable, therefore deployed Work-item persistence has not been live-verified.

Confirm recovery objectives and identify a complete protected recovery set plus an authorized isolated SQL/service recovery target. Then exercise required restore/read/compatibility checks and record achieved RPO/RTO. This is the remaining Phase 10 work; no subsequent phase was started. No general refactoring or production remediation was performed.

## Repository and deployment integrity

Only Phase 10 documentation/runbooks were added. No existing application source, database, configuration, release, binding, certificate, Current deployment or Auth file was intentionally changed. Before documentation edits, aggregate SHA256 snapshots matched for all source trees (excluding bin/obj/.git), API/Web Current/releases, Auth deployed files, API ProgramData configuration and IIS applicationHost.config. Final comparisons and repository checks are recorded below after documentation validation. No commit/tag/push was made.

## Exact validation commands

Commands were run in PowerShell on the existing host. No script file was left behind. Read-only discovery/source reads preceded these gates. The following records the successful HTTPS, test, restoration, final security and integrity commands; the initial failed HTTP parser invocation and native regex invocation made no deployment/source change.

### HTTPS

```powershell
$urls=@('https://localhost:7196/api/v1/health','https://localhost:7196/api/v1/session','https://localhost:7153/','https://localhost:7153/health','https://localhost:7153/Account/Login')
foreach($url in $urls){try{$r=Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 20; [pscustomobject]@{Url=$url;Status=[int]$r.StatusCode;FinalUrl=$r.BaseResponse.ResponseUri.AbsoluteUri}}catch{if($_.Exception.Response){[pscustomobject]@{Url=$url;Status=[int]$_.Exception.Response.StatusCode;FinalUrl=$_.Exception.Response.ResponseUri.AbsoluteUri}}else{[pscustomobject]@{Url=$url;Error=$_.Exception.Message}}}}
```

### Existing automated tests

From `C:\Apps\LabAPIServer\Source\LabAPIServer`:

```powershell
dotnet test -c Release --no-build --no-restore --filter 'FullyQualifiedName!~LocalDbIntegrationTests'
```

From `C:\Apps\LabWebAppServer\Source\LabWebAppServer`:

```powershell
dotnet test -c Release --no-build --no-restore
```

### Archive integrity, isolated file restoration and bounded cleanup

```powershell
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$restoreRoot='C:\Apps\Temp\Phase10-FileRestore'
if(Test-Path -LiteralPath $restoreRoot){throw 'Restore directory already exists; no changes made.'}
$packages=@(@{Name='LabAPIServer';Expected=49},@{Name='LabWebAppServer';Expected=197})
foreach($package in $packages){
 $release="C:\Apps\$($package.Name)\Releases\v1.0.0"
 $zipPath=Join-Path $release ($package.Name+'-1.0.0.zip')
 $hash=(Get-FileHash -LiteralPath $zipPath).Hash
 $expectedHash=((Get-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Raw).Trim() -split '\s+')[0]
 if($hash -ine $expectedHash){throw 'Release checksum mismatch.'}
 $archive=[IO.Compression.ZipFile]::OpenRead($zipPath)
 try{
  $destination=Join-Path $restoreRoot $package.Name
  $entries=@($archive.Entries | Where-Object {$_.Name -ne ''})
  if($entries.Count -ne $package.Expected){throw 'Unexpected archive file count.'}
  foreach($entry in $archive.Entries){$target=[IO.Path]::GetFullPath((Join-Path $destination $entry.FullName));if(-not $target.StartsWith($destination+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Archive entry escapes restore directory.'}}
  [IO.Compression.ZipFileExtensions]::ExtractToDirectory($archive,$destination)
  $matching=0
  foreach($entry in $entries){
   $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
   try{$entryHash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
   $restored=Join-Path $destination $entry.FullName
   $current=Join-Path "C:\Apps\$($package.Name)\Current" $entry.FullName
   if((Get-FileHash -LiteralPath $restored).Hash -cne $entryHash -or (Get-FileHash -LiteralPath $current).Hash -cne $entryHash){throw 'Restored/current file differs from archive.'}
   $matching++
  }
  $currentCount=@(Get-ChildItem -LiteralPath "C:\Apps\$($package.Name)\Current" -Recurse -Force -File).Count
  if($currentCount -ne $matching){throw 'Extra deployed files detected.'}
  [pscustomobject]@{App=$package.Name;ZipSHA256=$hash;ChecksumMatches=$true;RestoredFiles=$matching;CurrentFiles=$currentCount;ByteParity=$true} | ConvertTo-Json -Compress
 }finally{$archive.Dispose()}
}
$resolvedRestore=(Resolve-Path -LiteralPath $restoreRoot).ProviderPath
if($resolvedRestore -cne 'C:\Apps\Temp\Phase10-FileRestore'){throw 'Unsafe cleanup path.'}
$restoreItems=@(Get-Item -LiteralPath $resolvedRestore -Force)+@(Get-ChildItem -LiteralPath $resolvedRestore -Recurse -Force)
if(@($restoreItems | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Unexpected reparse point; cleanup stopped.'}
$usingProcesses=@(Get-CimInstance Win32_Process | Where-Object {$_.ProcessId -ne $PID -and $_.CommandLine -like '*Phase10-FileRestore*'})
if($usingProcesses.Count){throw 'Process references restore directory; cleanup stopped.'}
Remove-Item -LiteralPath $resolvedRestore -Recurse -ErrorAction Stop
"Restore directory removed: $(-not (Test-Path -LiteralPath $restoreRoot))"
```

### Final secret and inline SQL pattern scans

```powershell
$ErrorActionPreference='Stop'
$api='C:\Apps\LabAPIServer\Source\LabAPIServer';$web='C:\Apps\LabWebAppServer\Source\LabWebAppServer'
$roots=@("$api\src","$api\tests","$web\src","$web\tests",'C:\Apps\LabAPIServer\Current','C:\Apps\LabWebAppServer\Current')
$files=@($roots | ForEach-Object {Get-ChildItem -LiteralPath $_ -Recurse -File} | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in @('.cs','.cshtml','.js','.json','.config','.csproj')})
$patterns=[ordered]@{PrivateKey='-----BEGIN (?:RSA |EC |ENCRYPTED )?PRIVATE KEY-----';RawJwt='eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}';LiteralBearer='Bearer\s+eyJ[A-Za-z0-9_-]+';PasswordAssignment='(?i)(?:password|pwd)\s*[=:]\s*["''][^"'']+|"(?:password|pwd)"\s*:\s*"[^"'']+';TlsBypass='SkipCertificateCheck|ServerCertificateCustomValidationCallback|DangerousAcceptAnyServerCertificateValidator'}
foreach($name in $patterns.Keys){$matches=@($files | Where-Object {[regex]::IsMatch([IO.File]::ReadAllText($_.FullName),$patterns[$name])});[pscustomobject]@{Scan=$name;ScannedFiles=$files.Count;MatchingFiles=$matches.Count;Files=@($matches.FullName)} | ConvertTo-Json -Compress}
$sqlPattern='(?is)\b(select\b[^;"'']*\bfrom|insert\s+into|update\s+(\[?app\]?\.)?\[?WorkItems|delete\s+from|merge\s+into)\b'
$apiFiles=@($files | Where-Object {$_.FullName.StartsWith("$api\src\") -and $_.Extension -eq '.cs'})
$matches=@($apiFiles | Where-Object {[regex]::IsMatch([IO.File]::ReadAllText($_.FullName),$sqlPattern)})
[pscustomobject]@{Scan='ApiInlineCrudSql';ScannedFiles=$apiFiles.Count;MatchingFiles=$matches.Count;Files=@($matches.FullName)} | ConvertTo-Json -Compress
```

Web boundary/source checks:

```powershell
$web='C:\Apps\LabWebAppServer\Source\LabWebAppServer'
$boundaryPatterns=[ordered]@{WebJwtValidation='JwtSecurityTokenHandler|JsonWebTokenHandler|ValidateToken|TokenValidationParameters|AddJwtBearer|Microsoft.IdentityModel';WebSql='Microsoft.Data.SqlClient|System.Data.SqlClient|SqlConnection|DbContext';BrowserStorage='localStorage|sessionStorage'}
foreach($name in $boundaryPatterns.Keys){$matches=@(& rg -l -g '*.cs' -g '*.csproj' -g '*.js' -g '*.cshtml' -g '!**/bin/**' -g '!**/obj/**' -- $boundaryPatterns[$name] "$web\src");[pscustomobject]@{Scan=$name;MatchingFiles=$matches.Count;Files=$matches} | ConvertTo-Json -Compress}
```

### Baseline and final protected-file/repository snapshot

```powershell
$ErrorActionPreference='Stop'
$roots=@('C:\Apps\LabAPIServer\Source\LabAPIServer','C:\Apps\LabWebAppServer\Source\LabWebAppServer','C:\Apps\LabAuthServer\Source\LabAuthServer','C:\Apps\LabAPIServer\Current','C:\Apps\LabWebAppServer\Current','C:\Apps\LabAuthServer\Releases\v1.0.0-20260920','C:\Apps\LabAPIServer\Releases','C:\Apps\LabWebAppServer\Releases','C:\ProgramData\LabAPIServer')
$files=@($roots | ForEach-Object {Get-ChildItem -LiteralPath $_ -Recurse -Force -File} | Where-Object {$_.FullName -notmatch '\\(bin|obj|\.git)\\'})
$files+=Get-Item -LiteralPath "$env:windir\System32\inetsrv\config\applicationHost.config"
$hashes=@($files | Group-Object { if ($_.FullName -match '\\Source\\') { ($_.FullName -split '\\Source\\')[0] + '\Source' } elseif ($_.FullName -match '\\Current\\') { ($_.FullName -split '\\Current\\')[0] + '\Current' } elseif ($_.FullName -match '\\Releases\\') { ($_.FullName -split '\\Releases\\')[0] + '\Releases' } else { $_.DirectoryName } } | ForEach-Object { $entries=@($_.Group | Sort-Object FullName -Unique | ForEach-Object { $_.FullName + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash }); $sha=[Security.Cryptography.SHA256]::Create(); try {$digest=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($entries -join "`n")))).Replace('-','')} finally {$sha.Dispose()}; [pscustomobject]@{Path=$_.Name;Count=$entries.Count;Hash=$digest} })
$repos=@('C:\Apps\LabAPIServer\Source\LabAPIServer','C:\Apps\LabWebAppServer\Source\LabWebAppServer','C:\Apps\LabAuthServer\Source\LabAuthServer')
$git=@(foreach($repo in $repos){[pscustomobject]@{Repo=$repo; Status=(@(& git -C $repo status --short)-join "`n");DiffCheck=(@(& git -C $repo diff --check)-join "`n");DiffExit=$LASTEXITCODE}})
[pscustomobject]@{Time=[DateTime]::UtcNow.ToString('o');Hashes=$hashes;Git=$git;Sites=@(& "$env:windir\System32\inetsrv\appcmd.exe" list site);Pools=@(& "$env:windir\System32\inetsrv\appcmd.exe" list apppool);Vdirs=@(& "$env:windir\System32\inetsrv\appcmd.exe" list vdir)} | ConvertTo-Json -Depth 5 -Compress
```

Snapshot results were retained in tool-session memory, not written into the repositories. Final comparison excludes only the four newly added Phase 10 Markdown files listed below, so existing untracked content remains covered. Aggregate manifests hash sorted full paths plus each file's SHA256. Source bin/obj/.git are excluded; deployed binaries and runtime configuration are included. IIS state/path arrays and Git status/diff results are compared separately.

## Final integrity result

At 2026-09-21T19:13:34Z all ten protected manifest groups matched baseline. Existing source files: API 56, Web 109, Auth 1,678. Deployment files: API 49, Web 197, Auth 52. API/Web release directories (two files each), API external configuration (two files) and IIS applicationHost.config matched. Sites, pools and physical paths were unchanged and remained Started.

Only these four new files were excluded from the final source comparison:

- API `docs/operations/LabAPIServer-Operations-Runbook.md`.
- API `docs/plans/Phase-10/Phase-10-Operations-Acceptance-Record.md`.
- Web `docs/operations/LabWebAppServer-Operations-Runbook.md`.
- Web `docs/plans/Phase-10/Phase-10-Operations-Acceptance-Record.md`.

The snapshot command's final run inserted this filter immediately before building `$hashes`:

```powershell
$phase10Docs=@('C:\Apps\LabAPIServer\Source\LabAPIServer\docs\operations\LabAPIServer-Operations-Runbook.md','C:\Apps\LabWebAppServer\Source\LabWebAppServer\docs\operations\LabWebAppServer-Operations-Runbook.md','C:\Apps\LabAPIServer\Source\LabAPIServer\docs\plans\Phase-10\Phase-10-Operations-Acceptance-Record.md','C:\Apps\LabWebAppServer\Source\LabWebAppServer\docs\plans\Phase-10\Phase-10-Operations-Acceptance-Record.md')
$files=@($files | Where-Object {$_.FullName -notin $phase10Docs})
```

`git status --short` and `git diff --check` ran for all three requested repositories; each diff check returned exit 0. API/Web retain their pre-existing untracked project trees, including `docs/`; Auth retains only its pre-existing untracked `docs/plans/Phase-8/`. Status output is unchanged because new docs are inside already-untracked directories. Ordinary Git diff does not cover those files; explicit source hashes, Markdown link/fence and trailing-whitespace checks supplement it.

No commit, secret persistence, temporary script or temporary application launch occurred. The Phase 10 restore directory and Phase 9 probe are absent; final temporary/test-process count is zero. No database connection, configuration change, deployment, release rebuild or Auth modification was performed. Only the four documentation files above were added.

**Historical initial decision: PHASE 10 BLOCKED.** Superseded by the resumed recovery acceptance above. IIS SQL runtime validation remains explicitly NOT RUN.
