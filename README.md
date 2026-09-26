# LabAPIServer

## Purpose

ASP.NET Core (.NET 10) API for Work-items and Operational Statuses. LabAuthServer issues bearer tokens; this API validates RSA signatures, issuer, audience and lifetime, authorizes requests, and owns business data in SQL Server. LabWebAppServer is the separate Razor Pages client.

## Repository layout

- `src/LabAPIServer.Api/`: executable API, JWT validation and stored-procedure adapters.
- `tests/LabAPIServer.Api.Tests/`: unit, HTTP and opt-in SQL integration tests.
- `database/migrations/`: ordered schema and stored-procedure migrations (0000 through 0003).
- `database/test-data/`: synthetic fixtures for the isolated test database.
- `docs/`: configuration examples, development rules, plans and operations records.

Git belongs at this source directory, as in LabAuthServer. Outer `Current`, `Releases`, `Backups` and IIS deployment contents are not repository source. Existing phase records are historical evidence; their environment names and past test counts are not defaults or current release acceptance.

## Build and local development

Install a .NET 10 SDK (preparation validated with 10.0.401) and trust the local ASP.NET Core HTTPS development certificate using `dotnet dev-certs https --trust`. From this repository root:

```powershell
dotnet restore LabAPIServer.slnx
dotnet build LabAPIServer.slnx -c Release --no-restore
dotnet test LabAPIServer.slnx -c Release --no-build --filter 'FullyQualifiedName!~LocalDbIntegrationTests'
Copy-Item src/LabAPIServer.Api/appsettings.Development.example.json src/LabAPIServer.Api/appsettings.Development.json
# Replace every placeholder in the copied, ignored file before starting.
dotnet run --project src/LabAPIServer.Api --launch-profile https
```

Back up existing local settings before copying. The actual project launch profile uses HTTPS port 7196 (HTTP 5282); root `Properties/launchSettings.json` is an older scaffold, not the project launch profile. Root `appsettings.json` is also a historical scaffold; normal `dotnet run --project` loads settings from `src/LabAPIServer.Api/`.

## Configuration and authentication

Use [development settings](src/LabAPIServer.Api/appsettings.Development.example.json) or [external runtime settings](docs/configuration/labapi-runtime.example.json). All deployment-specific values are placeholders. Numeric timeouts and RS256 are documented defaults, not credentials. Local development settings are ignored by Git and excluded from publish output.

`JwtValidation` requires an approved Auth HTTPS issuer, exact audience, RS256 and an external RSA public key (`PublicKeyPath`, or `PublicKeyPem`). Provision public-only trust material separately; never copy Auth private keys or certificates into this repository. The API does not authenticate against AD or issue tokens. Reader can read; Operator and Administrator can perform the implemented writes. `/api/v1/session` returns API-verified identity for Web login.

For runtime use, copy the runtime example outside the repository, replace placeholders, restrict file access, and set `LABAPI_CONFIG_PATH` to its absolute path. The current loader adds external JSON **after** the standard configuration providers: external values override duplicate environment/command-line values. Reload is disabled; restart after changes. Without external JSON, standard ASP.NET Core environment variables such as `JwtValidation__Issuer` apply normally.

## Database requirements

SQL Server is required for business endpoints; health is a liveness check and does not prove SQL connectivity. Use a dedicated API database, Windows integrated authentication where appropriate, `Encrypt=True` and `TrustServerCertificate=False`, with trusted SQL TLS. Apply migrations 0000-0003 in order using a separate migration identity; runtime only needs the documented stored-procedure permissions, not DDL. See [database documentation](database/README.md) and [operations](docs/operations/LabAPIServer-Operations-Runbook.md).

SQL tests are deliberately excluded from the default command above. The existing `LocalDbIntegrationTests` fixture currently guards the named lab server `DC01` and database `LabAPIServer_Test`, despite its historical class name. It requires `LABAPI_RUN_SQL_TESTS=1`, `LABAPI_TEST_CONNECTION_STRING` and the seeded fixtures. Do not point it at another or production database or weaken that guard. SQL acceptance remains NOT RUN until explicitly exercised against its approved isolated target.

## Deployment and release

Publish the validated source with `dotnet publish src/LabAPIServer.Api -c Release -o <EXTERNAL_PUBLISH_DIRECTORY>`. Keep publish output, immutable archives, checksums, IIS configuration and backups outside the repository. Configure a dedicated IIS pool, the matching .NET Hosting Bundle, HTTPS, external runtime JSON and public-key file access. Deploy compatible schema before API, then Web; retain the prior artifact/configuration for rollback. Do not apply migrations automatically at startup.

API changes require Postman updates. Acceptance tests are required before release, including authentication, all roles, validation, persistence and verified fixture cleanup on the exact candidate artifact. Follow [development and release rules](docs/Development-Rules.md). Existing external Postman collections have not been imported or rerun by repository preparation.

## CI and test instructions

The [build workflow](.github/workflows/build.yml) restores, builds and tests on pushes and pull requests targeting `main`, using Windows and .NET SDK 10.0.400. Branch names are not changed by this setup. Repositories still on `master` will not trigger this workflow until work targets `main`.

After the restore/build commands above, use this isolated local validation command:

```powershell
dotnet test LabAPIServer.slnx -c Release --no-build --no-restore --filter "FullyQualifiedName!~LocalDbIntegrationTests"
```

Clear operational test configuration/opt-in variables in the test process first, as the workflow does. See [release process and CI limitations](docs/Release-Process.md) for the exact external-dependency exclusions and required acceptance. CI does not deploy or substitute for live acceptance.

## Architecture role

See [Architecture](docs/Architecture.md) for Browser -> Web -> Auth/AD and API/SQL responsibilities. Auth owns authentication, verified identity and role issuance; API owns business authorization and database access; Web owns UI, server-side sessions/cookies and API clients.

## API/Postman update requirement

New, modified or removed API endpoints require matching Postman updates before completion. Acceptance tests are required before release, covering success, authentication, role-specific authorization, validation/errors and verified cleanup of run-owned fixtures. Web expectations must follow changed API contracts. Keep credentials and tokens out of collections and repository files. Follow [Release Process](docs/Release-Process.md) for tagging, build verification, artifact checksums and rollback readiness.
