/*
  Local-only fixture reset for LabAPIServer_Test.
  Deletes only the six deterministic fixture IDs below.
  This script does not change schema or delete unknown Work-items.
*/
SET NOCOUNT ON;

DELETE FROM app.WorkItems
WHERE WorkItemId IN
(
    '11111111-1111-1111-1111-111111111101',
    '11111111-1111-1111-1111-111111111102',
    '11111111-1111-1111-1111-111111111103',
    '11111111-1111-1111-1111-111111111104',
    '11111111-1111-1111-1111-111111111105',
    '11111111-1111-1111-1111-111111111106'
);
