# LabAPIServer Agent Instructions

## Required before implementation

- Read the project plan and the current Phase 0 decision record before making changes.
- Preserve the Auth/API/Web boundaries defined by the shared architecture.
- Do not modify LabAuthServer without explicit instruction.
- Do not add unnecessary architecture or framework projects.
- Keep secrets and credentials out of source control.
- Run the relevant tests after changes.
- Update the phase record when implementation work changes status.

## Phase 1 constraints

- Keep the project minimal and buildable.
- Do not create database schema, migrations, or business business logic in Phase 1.
- Do not implement JWT validation, AD authentication, or authorization policies yet.
- Keep configuration placeholders and local-only development values out of source.
- Validate startup and health only; do not claim later-phase functionality.

## Current development and release rules

The Phase 1 constraints above are historical and apply only to that phase. For current work, follow docs/Development-Rules.md. New, changed, or removed API endpoints require Postman updates; acceptance tests are required before release. See README.md for current implemented scope and configuration.

