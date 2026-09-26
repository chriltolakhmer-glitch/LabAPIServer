# Release process

## Source version and build verification

Use `vMAJOR.MINOR.PATCH` tags. Record the exact source commit, SDK, dependency restore, Release build, test results and compatible Auth/API/Web versions. Create the release tag only at that tested commit; verify `git rev-parse <TAG>^{commit}` matches the recorded source SHA. Published tags and packages are immutable; corrections receive a new version.

`.github/workflows/build.yml` runs on pushes and pull requests targeting `main`, with read-only repository permissions on Windows. It checks out source, installs .NET SDK 10.0.400, restores, builds Release and runs the tests described below. Local verification may use a compatible .NET 10 SDK; Auth's `global.json` permits latest patch in its feature band. CI is validation only: it does not create tags, publish releases or deploy. A successful local run is not evidence that the hosted workflow ran.

## Test scope and external dependencies

CI runs all unit and isolated HTTP tests with `--filter "FullyQualifiedName!~LocalDbIntegrationTests"`. The two excluded SQL cases require the existing explicitly guarded lab SQL target and seeded fixtures, unavailable on a hosted runner. The workflow prints this limitation. Set `LABAPI_RUN_SQL_TESTS=1` and `LABAPI_TEST_CONNECTION_STRING` only in an approved lab acceptance session following README and database test-data instructions; do not weaken the guard. Live Auth/Postman acceptance is separate.

Excluded/unsupported acceptance must be recorded as NOT RUN, never PASS. Provision explicitly authorized isolated targets before running infrastructure acceptance. Do not inject production credentials into pull-request workflows or bypass TLS/target guards. Required acceptance remains a release requirement even when hosted build validation passes.

## API and Postman acceptance

Every new, modified or removed API endpoint requires the corresponding Postman collection update before completion. Include method/path, headers, authentication, credential-free request examples and response/status assertions; retire obsolete requests and verify the removal contract. Web client expectations must follow consumed API contract changes.

Before release, run the relevant collections against the exact candidate artifact in an approved environment. Verify success, authentication failures, validation/errors and allowed/denied behavior separately for Reader, Operator and Administrator. Create only run-owned fixtures and verify cleanup. Record environment, artifact identity, executed role, assertion counts and cleanup outcome. Missing credentials or skipped cases remain NOT RUN. Keep secrets, tokens, cookies and raw sensitive run exports outside Git. Do not infer full role coverage from a single successful run.

## Artifact verification and deployment

Publish the tested source once to an external staging directory. Verify the package contains the intended binaries/static assets and no local runtime settings, private keys, credentials or test output. Record its SHA256 and verify the same checksum and contents at deployment; do not rebuild between acceptance and deployment. Keep production configuration external and protected. Database/schema compatibility is API-owned; Auth audit storage is separate and Web has no direct database dependency.

Follow the existing deployment runbook for the application. Confirm HTTPS, external configuration, runtime identity access and version compatibility. Keep source CI separate from IIS/deployment operations. A release needs both artifact verification and environment-specific smoke/acceptance evidence.

## Rollback requirement

Before deployment, retain the previous immutable package and checksum, compatible external configuration, and a documented rollback procedure. Identify the rollback trigger and verify the recovery path is available. Account for schema compatibility and required backups; application rollback does not automatically reverse database migrations. Web recycle/rollback loses in-memory sessions and requires login again. Verify health and the critical authenticated journey after rollback, and record whether rollback was exercised or only prepared.

See [development rules](Development-Rules.md) and the [API operations runbook](operations/LabAPIServer-Operations-Runbook.md).
