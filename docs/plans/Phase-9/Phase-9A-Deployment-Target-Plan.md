# Phase 9A - Deployment Target Plan

Date: 2026-09-22

## Decision

**PHASE 9A COMPLETE - IIS TARGETS AND TLS READY FOR DEPLOYMENT.**

The simple IIS deployment targets are established and both HTTPS bindings now use an existing trusted localhost development certificate. The sites and pools remain empty and undeployed. No release artifact, database object, test data, source file, or Auth deployment was changed.

## Current environment findings

### IIS and runtime

- IIS sites: `Default Web Site`, `LabAuthServer`, `LabAPIServer`, and `LabWebAppServer`.
- `LabAuthServer`: Started; physical path `C:\Apps\LabAuthServer\Releases\v1.0.0-20260920`.
- Auth bindings: `*:80:DC01.lab.local` and `*:443:DC01.lab.local`.
- Auth pool: `LabAuthServerAppPool`, Started, `LAB\\svc_labauth`, Integrated pipeline, OnDemand.
- API IIS site: `LabAPIServer`, Started, `C:\Apps\LabAPIServer\Current`, pool `LabAPIServerAppPool`.
- Web IIS site: `LabWebAppServer`, Started, `C:\Apps\LabWebAppServer\Current`, pool `LabWebAppServerAppPool`.
- API pool: `LabAPIServerAppPool`, ApplicationPoolIdentity, No Managed Code, Integrated, 64-bit, OnDemand.
- Web pool: `LabWebAppServerAppPool`, ApplicationPoolIdentity, No Managed Code, Integrated, 64-bit, OnDemand.
- ASP.NET Core Module V2 is installed at `C:\Program Files\IIS\Asp.Net Core Module\V2`.
- .NET SDKs `10.0.400` and `10.0.401` and ASP.NET Core runtimes `10.0.11` and `10.0.12` are installed.
- HTTPS listeners are established on localhost ports `7196` and `7153`; the application roots are empty, so no application is deployed.
- No API/Web Windows service or reverse-proxy configuration was found.

### Filesystem conventions

- Auth has established `Current`, `Releases`, `Backups`, `Config`, `Logs`, `Staging`, and `Scripts` roots.
- API has `Current`, `Releases`, `Scripts`, and `Source` roots.
- Web has `Current`, `Releases`, `Scripts`, and `Source` roots.
- API/Web `Current` directories are empty. Configuration, logs, and backups remain external/not established.
- API/Web launch profiles are Development-only local profiles: API `https://localhost:7196`, Web `https://localhost:7153`.
- `CN=localhost`, thumbprint `F392B4D1ED31361D16576700E03CFD26921E263F`, is bound to both sites. It is the existing ASP.NET Core HTTPS development certificate, has Server Authentication EKU, a private key, localhost SAN coverage, and is trusted through `CurrentUser\Root`.

## Hosting model evaluation

| Model | Evidence | Decision |
| --- | --- | --- |
| IIS hosting | ANCM V2 and IIS are installed; Auth provides a reference pattern. API/Web sites, pools, identities, paths, and bindings are now established. | Selected |
| Windows Service/Kestrel | .NET runtime is installed, but no service definitions, service identities, startup/restart policy, or log locations exist. | Not established |
| Reverse proxy plus Kestrel | No proxy process or configuration was found. | Not established |
| Existing local convention | Only Auth IIS is established; API/Web launch profiles are Development-only. | Insufficient |

IIS is the selected simple hosting model. No service, reverse proxy, container, load balancer, or CI/CD machinery was introduced.

## API deployment target

- Site name: `LabAPIServer`.
- Application pool: `LabAPIServerAppPool`, ApplicationPoolIdentity.
- Physical path: `C:\Apps\LabAPIServer\Current`.
- HTTPS binding: `*:7196:localhost`.
- Certificate: `CN=localhost`, thumbprint `F392B4D1ED31361D16576700E03CFD26921E263F`, LocalMachine\\My; chain validation PASS.
- External configuration location: NOT ESTABLISHED.
- Logging location: NOT ESTABLISHED.
- Health endpoint: application contract is `/api/v1/health`; persistent hosting route is not established.
- Startup/restart behavior: NOT ESTABLISHED.

## Web deployment target

- Site name: `LabWebAppServer`.
- Application pool: `LabWebAppServerAppPool`, ApplicationPoolIdentity.
- Physical path: `C:\Apps\LabWebAppServer\Current`.
- HTTPS binding: `*:7153:localhost`.
- Certificate: `CN=localhost`, thumbprint `F392B4D1ED31361D16576700E03CFD26921E263F`, LocalMachine\\My; chain validation PASS.
- External configuration location: NOT ESTABLISHED.
- Logging location: NOT ESTABLISHED.
- Health endpoint: `/health` in the Web application contract; persistent hosting route is not established.
- Startup/restart behavior: NOT ESTABLISHED.

## HTTPS and architecture strategy

The intended topology remains Browser -> Web over HTTPS -> API over HTTPS -> SQL stored procedures, with Auth issuing JWTs. Auth remains unchanged. API retains JWT validation, authorization, business logic, and stored-procedure access. Web retains server-side session/ticket behavior and must not access SQL or validate JWT signatures.

A persistent HTTPS strategy is selected as localhost-only IIS HTTPS on ports 7196 and 7153 using the existing trusted ASP.NET Core development certificate. The certificate was imported into `LocalMachine\\My` for IIS private-key access; its existing `CurrentUser\\Root` trust was not changed. Normal `Invoke-WebRequest` validation succeeded for both endpoints and returned HTTP 404 because the `Current` directories are intentionally empty.

## Database target

The read-only database target is established:

- Server: `DC01`
- Database: `LabAPIServer_Test`
- Authentication: Windows Integrated Authentication as `LAB\\Administrator` during inspection
- Classification: isolated API development/test database
- `app.WorkItems`: present
- Indexes: `PK_WorkItems` and `IX_WorkItems_Status_UpdatedAtUtc` present
- Procedures: `app.WorkItems_List`, `app.WorkItems_Get`, `app.WorkItems_Create`, `app.WorkItems_Update`, `app.WorkItems_Delete` present
- Migration ledger: `dbo.SchemaMigrations` present with versions 0 and 1
- Recorded migration hashes: version 0 `58564DD50B49D082DF926267D41F084AB440A5AA0D01B6FDCB8761C686972EE1`; version 1 `35708A34D658C1C6308F253DA15A35B7A46229CFF9EEA80A3C4F6B45DC8D5E84`
- Database mutation in Phase 9A: NOT RUN
- Test data mutation in Phase 9A: NOT RUN

## Rollback design prerequisite

The intended deployment flow is:

```text
approved current deployment
    -> backup application files, external configuration, and hosting configuration
    -> stage LabAPIServer/Web v1.0.0 in versioned paths
    -> switch only the approved target
    -> smoke test
       PASS -> retain v1.0.0 and previous backup
       FAIL -> restore previous files/configuration/hosting settings
              -> smoke test previous version
```

For the empty targets, the future rollback baseline is the empty `Current` directory plus the IIS site/pool configuration captured before deployment. Before a real deployment, back up the populated `Current` path, external configuration, and IIS configuration; restore those items and restart the affected pool if smoke fails. The database is already at migration versions 0 and 1; a future deployment must preserve compatibility and must not use binary rollback to imply schema rollback.

## Security considerations

- Do not copy credentials, private keys, raw JWTs, or connection strings into source, ZIPs, or checked-in configuration.
- Keep Web free of SQL client access and JWT signature validation.
- Keep API Work-item CRUD stored-procedure based.
- Use external configuration and HTTPS certificate validation.
- Assign separate least-privilege identities for API and Web before deployment.
- Define log locations and ensure tokens, passwords, and connection strings are not logged.
- Do not reuse Auth's identity, configuration, certificate, or deployment directory for API/Web.

## Required owner decisions before Phase 9

1. Preserve the existing localhost-only IIS topology and the two ApplicationPoolIdentity pools.
2. Define external API/Web configuration and log locations before deployment.
3. Confirm the isolated database runtime configuration and migration authority for the later deployment step.

Until the remaining external configuration and deployment authorization decisions are resolved, Phase 9 must not extract artifacts into `Current`, run migrations, create test data, modify Auth, or start Phase 10.

## Validation

- Phase 8 artifact hashes and entry counts rechecked: PASS.
- IIS/filesystem/runtime/database inspection: PASS; target setup performed without deployment.
- HTTPS client trust probe: PASS; API and Web normal-client requests negotiated TLS and returned HTTP 404 from empty `Current` directories.
- X509 chain validation: PASS; bound certificate chain Build `True`, no `UntrustedRoot`, `PartialChain`, or `NotTimeValid`.
- Repository status and `git diff --check`: PASS before this record update.
- Auth source and existing deployment: unchanged.
- API/Web source and release artifacts: unchanged.
- Deployment: NOT RUN; `Current` directories remain empty.
- Phase 10: NOT STARTED.
