/* Local-only Phase 13 fixture cleanup for the approved LabAPIServer_Test database. */
SET NOCOUNT ON;

DELETE FROM app.OperationalStatuses
WHERE [Key] = N'phase13-fixture';