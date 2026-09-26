# Phase 8 - Release Packaging Record

Date: 2026-09-22

Status: **COMPLETE.** This record covers packaging only. No deployment, IIS change, production configuration change, production database change, migration execution, commit, tag, push, or GitHub release was performed.

## Release identity

| Field | Value |
| --- | --- |
| Application | LabAPIServer |
| Version | 1.0.0 |
| Artifact | `Releases/v1.0.0/LabAPIServer-1.0.0.zip` |
| SHA256 | `8c4bbdca3e15bc98389fc3dc234c33c78bf98b880de31b4d52ebdeecbe6c1e85` |
| Target framework | `net10.0` |
| Build configuration | Release |
| Artifact file count | 49 |
| Artifact size | 4,947,001 bytes |
| Creation timestamp | 2026-09-21T17:58:42Z |

## Validation

- Restore: PASS.
- Release build: PASS, zero errors and zero warnings.
- Automated tests: PASS, 23/23 (22 non-infrastructure tests plus 1 explicit SQL CRUD/authorization test); 0 failed.
- Test database: `DC01 / LabAPIServer_Test`; migration execution: **NOT RUN**.
- Artifact readable: PASS.
- Artifact extractable into a clean directory: PASS.
- Expected files present: PASS.
- Prohibited files absent: PASS; no `bin`, `obj`, `.git`, `.vscode`, development settings, PDBs, or test output.
- SHA256 recalculated independently and matched `SHA256SUMS.txt`: PASS.
- Byte-for-byte reproducibility was not claimed; source/build inputs and artifact contents were verified.

## Database and security

`database/migrations/0000_MigrationLedger.sql` and `database/migrations/0001_WorkItems.sql` are included as immutable release inputs. The WorkItems migration contains the table, status index, and `List`, `Get`, `Create`, `Update`, and `Delete` procedures. It was not executed.

- Secret scan: PASS.
- Private-key scan: PASS.
- Raw-JWT scan: PASS.
- Web SQL/JWT boundary scan: PASS.
- API direct Work-item CRUD SQL scan: PASS; runtime CRUD uses the stored procedures.
- Production database: **NOT TOUCHED**.

## Explicit boundaries

- Deployment: NOT RUN.
- IIS changes: NOT RUN.
- Phase 9: NOT STARTED BY THIS PHASE 8 TASK.
- Rollback: not executed; existing recovery documentation remains authoritative.
