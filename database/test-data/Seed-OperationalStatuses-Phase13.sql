/* Local-only Phase 13 fixture setup for the approved LabAPIServer_Test database. */
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM app.OperationalStatuses WHERE [Key] = N'phase13-fixture')
BEGIN
    INSERT INTO app.OperationalStatuses
        (StatusId, [Key], [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy)
    VALUES
        ('6e9f320b-9c85-47e9-a7ba-6f0fa1a3f8f9', N'phase13-fixture', N'Phase 13 fixture', N'Info', SYSUTCDATETIME(), SYSUTCDATETIME(), N'phase13-fixture');
END;