/* Local-only Phase 14 fixture cleanup for the approved DC01/LabAPIServer_Test database. */
SET NOCOUNT ON;

DELETE history
FROM app.OperationalStatusHistory AS history
INNER JOIN app.OperationalStatuses AS status
    ON status.StatusId = history.StatusId
WHERE status.[Key] = N'phase14-fixture';

DELETE FROM app.OperationalStatuses
WHERE [Key] = N'phase14-fixture';
