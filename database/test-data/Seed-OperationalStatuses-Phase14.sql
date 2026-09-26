/* Local-only Phase 14 fixture setup for the approved DC01/LabAPIServer_Test database. */
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM app.OperationalStatuses WHERE [Key] = N'phase14-fixture')
BEGIN
    INSERT INTO app.OperationalStatuses
        (StatusId, [Key], Status, [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy)
    VALUES
        ('7e9f320b-9c85-47e9-a7ba-6f0fa1a3f8f9', N'phase14-fixture', N'Open', N'Phase 14 fixture', N'Info', SYSUTCDATETIME(), SYSUTCDATETIME(), N'phase14-fixture');
END;
