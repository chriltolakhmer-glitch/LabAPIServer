# Phase 3 - Auth and JWT Integration

## Scope

Phase 3 adds JWT bearer validation and the protected `GET /api/v1/session` endpoint to LabAPIServer. Web login, business endpoints, and Phase 4 work are out of scope.

## Auth contract

The implementation follows the existing LabAuthServer contract:

- Issuer: `https://DC01.lab.local`
- Audience: `LabAuthServer.API`
- Algorithm: `RS256`
- Access-token lifetime: one hour
- Clock skew: five minutes
- Identity claims: `sub` and `role`
- Accepted roles: `Reader`, `Operator`, `Administrator`

The issuer, audience, algorithm, clock skew, and public validation key are configuration-driven under `JwtValidation`. The API accepts only signed tokens and validates the RSA signature, issuer, audience, lifetime, expiration, and required claims. LabAPIServer contains no private signing key.

## Implementation

- `src/LabAPIServer.Api/Program.cs`
  - Configures JWT bearer authentication.
  - Requires HTTPS metadata, signed tokens, an expiration time, a valid RSA signing key, the configured issuer, audience, lifetime, and algorithm.
  - Maps protected `GET /api/v1/session`, returning `subject`, `role`, and `expiresAt` only after authentication.
- `src/LabAPIServer.Api/Auth/JwtValidationOptions.cs`
  - Loads the public key from `PublicKeyPem` or `PublicKeyPath`.
- `src/LabAPIServer.Api/Auth/JwtValidationOptionsValidator.cs`
  - Rejects missing or invalid HTTPS issuer, audience, algorithm, skew, and RSA public-key configuration.
- `src/LabAPIServer.Api/Auth/JwtBearerValidationService.cs`
  - Provides the same issuer, audience, lifetime, signature, algorithm, and required-claim validation model for direct validation use.

The checked-in default configuration records the real issuer, audience, RS256, and five-minute skew. The public validation key is supplied through deployment/test configuration and is not a private signing key.

## Automated validation evidence

Command executed from `C:\Apps\LabAPIServer\Source\LabAPIServer`:

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-restore
```

Result on 2026-09-21:

- Restore succeeded.
- Release build succeeded.
- `LabAPIServer.Api.Tests`: 10 passed, 0 failed, 0 skipped.
- Automated JWT/session coverage includes:
  - no token -> HTTP 401;
  - valid JWT -> HTTP 200 with verified test identity, role, and expiry;
  - invalid signature -> HTTP 401;
  - wrong issuer -> HTTP 401;
  - wrong audience -> HTTP 401;
  - expired token -> HTTP 401.

These are generated-key automated tests, not real Auth-issued-token tests.

## Endpoint evidence

The current source tests verify:

- `GET /api/v1/health` -> HTTP 200 with `{"status":"Healthy"}`.
- `GET /api/v1/session` without a bearer token -> HTTP 401.

On 2026-09-21, the current source API was started from `src/LabAPIServer.Api` with an external public-only JWT configuration. Direct checks against that process returned:

- `https://localhost:7196/api/v1/health` -> HTTP 200.
- `https://localhost:7196/api/v1/session` without a bearer token -> HTTP 401.

The earlier stale Debug process on `localhost:7196` was identified by executable path and stopped before this current-source check. It was not used as evidence.

## Real Auth -> API integration

**REAL AUTH -> API INTEGRATION: PASS**

Tested on 2026-09-21 using the existing authorized account `test.itd@lab.local` through the secure interactive password prompt. The password was not printed, stored, or recorded.

- Auth endpoint: `https://DC01.lab.local/api/v1/auth/login` -> HTTP 200.
- API endpoint: `https://localhost:7196/api/v1/session` -> HTTP 200.
- Verified subject: `test.itd@lab.local`.
- Verified role: `Operator`.
- Verified expiry: `2026-09-21T16:13:59.0000000Z`.
- Negative case: `/api/v1/session` without a bearer token -> HTTP 401.
- The JWT was held only in memory for the request and was not printed, saved, or documented.

The sanitized token inspection confirmed the real Auth token used `RS256`, issuer `https://DC01.lab.local`, audience `LabAuthServer.API`, and key identifier `lab-jwt-signing-20260907`. The matching public certificate was converted externally to SubjectPublicKeyInfo PEM for the test configuration; no private key was exported or copied.

## Temporary test material

For this verification, public-only PEM files and external API configurations were generated temporarily and then removed, along with the disposable export utility:

- `C:\temp\labapi-auth-public.pem`
- `C:\temp\labapi-phase3-live-config.json`
- `C:\temp\LabApiPublicKeyExport\`

The existing diagnostic and certificate files were not removed because their Phase 3 ownership and production/deployment status could not be established safely. The response files created during the earlier verification were also removed:

- `C:\temp\labapi-health-response.txt`
- `C:\temp\labapi-session-no-token-response.txt`

No private key, password, or raw JWT is recorded in this document.

## Security checks

- Issuer validation: enabled and configuration-driven.
- Audience validation: enabled and configuration-driven.
- Signature validation: enabled with an RSA public key.
- Lifetime validation: enabled with five-minute configured skew.
- Unsigned tokens: rejected through `RequireSignedTokens` and required issuer-signing-key validation.
- Public-key verification: PASS for the configured active Auth certificate thumbprint `BD545BA289EBFC645C8C3DC424311975579D7E09`; the API test configuration used only its exported SubjectPublicKeyInfo public key. The certificate's private key was not exported or copied.
- Real signing-key verification: PASS for Auth key identifier `lab-jwt-signing-20260907`, using existing public certificate thumbprint `94D4AC5345479614B945096CC9CEDE87C48FC51B`. The certificate artifact had no private key; only its public key was converted for API validation.
- Private signing key in LabAPIServer: none found.
- Real JWT in LabAPIServer source or documentation: none found.
- LabAuthServer source was not modified.
- LabWebAppServer was not modified.

## Final status

The Phase 3 implementation, automated validation, public-key trust, and real Auth -> API acceptance are complete. Do not start Phase 4 as part of this task.
