# Development and release rules

Any new, modified, or removed API endpoint MUST update the corresponding Postman collection in the same change before the work is complete. API changes require Postman updates, including Web client expectations when the consumed contract changes.

Each request must specify the method, URL/path, headers, authentication, credential-free body examples, expected status, and response assertions. Update or retire requests for removed endpoints and verify their removal contract.

Acceptance tests are required before release. Cover success, authentication failure, authorization, validation/error responses, and explicit cleanup of fixtures owned by the test run. Test Reader, Operator, and Administrator allowed/denied behavior separately; do not infer unexecuted role coverage.

Completion requires implemented source, a matching deployment artifact, an updated collection, passing required Postman tests, and verified authorization. Record environment, artifact identity, role, results, and cleanup. Skipped tests and missing credentials are NOT RUN, not PASS. Build/unit tests alone do not constitute release acceptance.

Keep reviewed collection source under `tests/postman/` when imported. No collection was present in these source repositories during GitHub preparation. Existing external Postman collections must be sanitized and reviewed before import; do not commit environments, globals, cookies, real passwords, tokens, or raw run exports. Use placeholders and local secret variables. This rule does not authorize a deployment or destructive testing.

Keep production/runtime configuration, certificates, private keys, binaries, IIS folders, and test results outside Git. Review `git status --short --untracked-files=all` and the exact staged diff before committing. Never use `git add -f` to bypass secret exclusions. Historical phase records describe prior runs and do not establish acceptance of a new artifact.
