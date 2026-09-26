# LabAPIServer operations and recovery

Verified 2026-09-22 (Asia/Bangkok). See the [Phase 10 evidence and limitations](../plans/Phase-10/Phase-10-Operations-Acceptance-Record.md). Same-host representative API/Web startup and isolated synthetic SQL backup/restore are TESTED. Production IIS SQL/rollback and host-loss recovery are NOT RUN.

## Active deployment

- IIS site `LabAPIServer`, site ID 3; pool `LabAPIServerAppPool`; both Started.
- Active physical path: `C:\Apps\LabAPIServer\Current`.
- Pool: ApplicationPoolIdentity, one worker, `loadUserProfile=false`.
- Release: `C:\Apps\LabAPIServer\Releases\v1.0.0\LabAPIServer-1.0.0.zip`; 49 files. Check against the adjacent `SHA256SUMS.txt` before use.
- HTTPS: `https://localhost:7196`, existing trusted localhost development certificate. Evidence is local to this host/client; a remote production hostname/client route is not verified.
- Auth remains separately deployed and unchanged. Do not restart or change Auth to troubleshoot this API.

## Health and diagnosis

Run with normal certificate validation:

```powershell
Invoke-WebRequest -Uri https://localhost:7196/api/v1/health -UseBasicParsing -TimeoutSec 20
Invoke-WebRequest -Uri https://localhost:7196/api/v1/session -UseBasicParsing -TimeoutSec 20
& "$env:windir\System32\inetsrv\appcmd.exe" list site LabAPIServer
& "$env:windir\System32\inetsrv\appcmd.exe" list apppool LabAPIServerAppPool
```

Health should return 200. Anonymous session should return 401 (Windows PowerShell reports the latter as an HTTP exception). Health is liveness only: it does not open SQL or prove Work-item persistence. Preserve the status/time and sanitized error, never a bearer token.

IIS W3C access logs are enabled, daily, at `C:\inetpub\logs\LogFiles\W3SVC3`; endpoint records were observed in `u_ex260921.log`. Windows Application log contains historical `IIS AspNetCore Module V2` and `.NET Runtime` startup/error events. Inspect timestamps before associating an old error with a current request. Packaged ANCM stdout logging is disabled; `.\logs\stdout` is only an inactive setting. Application warning/error capture and retention were not independently exercised. Do not enable verbose/body/token logging as a routine diagnostic step.

## Effective configuration

Pool variable `LABAPI_CONFIG_PATH` selects `C:\ProgramData\LabAPIServer\labapi-runtime.json`. JWT verification uses the public-only `C:\ProgramData\LabAPIServer\auth-signing-public.pem`, issuer `https://DC01.lab.local`, audience `LabAuthServer.API`, RS256, and 300-second skew. No private signing key belongs here.

The implemented loader adds this JSON after default configuration providers; its duplicate values therefore override default-provider/environment values. Reload is disabled. Apply any separately authorized settings change with a scoped pool restart. Directory ACL inspection found SYSTEM/Administrators full control and `IIS AppPool\LabAPIServerAppPool` read/execute.

`Database:ConnectionString` is supplied by the external runtime JSON and targets `tcp:DC01,1433/LabAPIServer_Test` with Windows Integrated Security, `Encrypt=True`, and `TrustServerCertificate=False`. The live worker identity is `IIS APPPOOL\LabAPIServerAppPool`; it has `CONNECT` plus `EXECUTE` on the five `app.WorkItems_*` procedures and no direct Work-items table DML permissions. Do not substitute `master`, the Auth database, or an administrator's test connection. The target is the approved isolated API database.

## Scoped restart and recovery checklist

These are operator instructions, **not actions performed in Phase 10**. Restart only for a diagnosed problem or planned maintenance; a restart cannot repair absent SQL configuration.

```powershell
& "$env:windir\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:LabAPIServerAppPool
& "$env:windir\System32\inetsrv\appcmd.exe" start apppool /apppool.name:LabAPIServerAppPool
```

Afterward repeat health/anonymous-session checks and inspect new logs. Do not use `iisreset`.

1. Before rollback, identify the exact prior artifact, checksum, matching external configuration/public trust, IIS settings and schema compatibility. The current-version same-host recovery set is `C:\ProgramData\LabAPIServer\Recovery\Phase10-20260922`; validate its SHA256SUMS.txt. It does not supply a previous working release or justify rolling back real data.
2. Retain immutable ZIP/checksum and matching settings in restricted recovery storage. Never place configuration secrets in source/releases or copy Auth private keys. Current live configuration is not a backup.
3. Restore files to isolation and compare every file with the archive. The 49-file API package was recovered with copied external configuration/public key and started on trusted `https://localhost:17196`; health 200 and anonymous session 401 passed. The existing host certificate/private key and runtime were prerequisites, not restored infrastructure.
4. The owner-approved disposable `DC01 / LabAPIServer_Phase10_Recovery_20260922` was migrated, seeded through procedures, backed up, dropped/restored, checked with DBCC CHECKDB and tested through API CRUD/role authorization. It has been removed. The retained backup contains only six synthetic recovery fixtures, not operational data. A future replay must again explicitly select a disposable target and guard against an existing database; never restore it over the populated test or production database.
5. Current migration versions are 0/1; both restored and original test ledgers matched recorded hashes. API tests passed 23/23 on the restored database. Compatibility with an older API is not established. Do not reverse migrations or overwrite later data/audit as binary rollback.
6. A first-install fallback disables only the new API site/pool while retaining data/evidence; it is an outage fallback, not restoration of a previous working service. Phase 9's historical empty-Current rollback is not a complete recovery set for today's deployment. Do not execute this fallback during routine acceptance.

## Maintenance and remaining decision

Weekly: verify trusted health, active paths/version, new failed requests and backup completion. Monthly: review certificate expiry, supported runtime/package patches, access, backup readability/retention and an isolated restore when an approved set exists. No scheduled task or monitoring platform was created.

The owner confirmed **RPO 24 hours and RTO one working day**. The isolated drill lost zero fixture rows and reached final recovered service verification in 4 minutes 49.791 seconds. File restoration, synthetic SQL backup restore and representative same-host service startup are TESTED. Production rollback and host-loss recovery are NOT RUN. No operating backup schedule was installed; establish and verify continuing backups against the confirmed RPO before accepting valuable production data. The retained synthetic backup is not a substitute for business-data protection.
