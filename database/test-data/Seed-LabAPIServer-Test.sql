/*
  Local-only deterministic fixture seed for LabAPIServer_Test.
  Run Reset-LabAPIServer-Test.sql first.
*/
SET NOCOUNT ON;

INSERT INTO app.WorkItems
(
    WorkItemId,
    Name,
    Description,
    Status,
    CreatedAtUtc,
    UpdatedAtUtc,
    CreatedBy,
    UpdatedBy
)
SELECT fixture.WorkItemId,
       fixture.Name,
       fixture.Description,
       fixture.Status,
       fixture.CreatedAtUtc,
       fixture.UpdatedAtUtc,
       N'test-data-fixture',
       N'test-data-fixture'
FROM
(
    VALUES
    ('11111111-1111-1111-1111-111111111101', N'[TEST DATA] Open with description', N'Synthetic fixture description 101.', N'Open',       CONVERT(datetime2(7), '2026-01-10T07:00:00'), CONVERT(datetime2(7), '2026-01-10T08:00:00')),
    ('11111111-1111-1111-1111-111111111102', N'[TEST DATA] InProgress without description', NULL,                                      N'InProgress', CONVERT(datetime2(7), '2026-01-11T08:00:00'), CONVERT(datetime2(7), '2026-01-11T09:00:00')),
    ('11111111-1111-1111-1111-111111111103', N'[TEST DATA] Complete with description', N'Synthetic fixture description 103.', N'Complete',   CONVERT(datetime2(7), '2026-01-12T09:00:00'), CONVERT(datetime2(7), '2026-01-12T10:00:00')),
    ('11111111-1111-1111-1111-111111111104', N'[TEST DATA] Open without description', NULL,                                      N'Open',       CONVERT(datetime2(7), '2026-01-13T10:00:00'), CONVERT(datetime2(7), '2026-01-13T11:00:00')),
    ('11111111-1111-1111-1111-111111111105', N'[TEST DATA] InProgress with description', N'Synthetic fixture description 105.', N'InProgress', CONVERT(datetime2(7), '2026-01-14T11:00:00'), CONVERT(datetime2(7), '2026-01-14T12:00:00')),
    ('11111111-1111-1111-1111-111111111106', N'[TEST DATA] Complete without description', NULL,                                      N'Complete',   CONVERT(datetime2(7), '2026-01-15T12:00:00'), CONVERT(datetime2(7), '2026-01-15T13:00:00'))
) AS fixture(WorkItemId, Name, Description, Status, CreatedAtUtc, UpdatedAtUtc)
WHERE NOT EXISTS
(
    SELECT 1
    FROM app.WorkItems existing
    WHERE existing.WorkItemId = fixture.WorkItemId
);
