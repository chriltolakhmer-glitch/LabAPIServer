IF SCHEMA_ID(N'app') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA app');
END;

IF OBJECT_ID(N'app.WorkItems', N'U') IS NULL
BEGIN
    CREATE TABLE app.WorkItems
    (
        WorkItemId uniqueidentifier NOT NULL
            CONSTRAINT PK_WorkItems PRIMARY KEY,
        Name nvarchar(200) NOT NULL,
        Description nvarchar(2000) NULL,
        Status nvarchar(32) NOT NULL
            CONSTRAINT CK_WorkItems_Status CHECK (Status IN (N'Open', N'InProgress', N'Complete')),
        CreatedAtUtc datetime2(7) NOT NULL,
        UpdatedAtUtc datetime2(7) NOT NULL,
        CreatedBy nvarchar(1024) NOT NULL,
        UpdatedBy nvarchar(1024) NOT NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'app.WorkItems')
      AND name = N'IX_WorkItems_Status_UpdatedAtUtc'
)
BEGIN
    CREATE INDEX IX_WorkItems_Status_UpdatedAtUtc
        ON app.WorkItems (Status, UpdatedAtUtc DESC);
END;
GO

CREATE OR ALTER PROCEDURE app.WorkItems_List
AS
BEGIN
    SET NOCOUNT ON;

    SELECT WorkItemId, Name, Description, Status, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy
    FROM app.WorkItems
    ORDER BY UpdatedAtUtc DESC, WorkItemId;
END;
GO

CREATE OR ALTER PROCEDURE app.WorkItems_Get
    @WorkItemId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT WorkItemId, Name, Description, Status, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy
    FROM app.WorkItems
    WHERE WorkItemId = @WorkItemId;
END;
GO

CREATE OR ALTER PROCEDURE app.WorkItems_Create
    @WorkItemId uniqueidentifier,
    @Name nvarchar(200),
    @Description nvarchar(2000),
    @Status nvarchar(32),
    @CreatedAtUtc datetime2(7),
    @UpdatedAtUtc datetime2(7),
    @CreatedBy nvarchar(1024),
    @UpdatedBy nvarchar(1024)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO app.WorkItems
        (WorkItemId, Name, Description, Status, CreatedAtUtc, UpdatedAtUtc, CreatedBy, UpdatedBy)
    VALUES
        (@WorkItemId, @Name, @Description, @Status, @CreatedAtUtc, @UpdatedAtUtc, @CreatedBy, @UpdatedBy);
END;
GO

CREATE OR ALTER PROCEDURE app.WorkItems_Update
    @WorkItemId uniqueidentifier,
    @Name nvarchar(200),
    @Description nvarchar(2000),
    @Status nvarchar(32),
    @UpdatedAtUtc datetime2(7),
    @UpdatedBy nvarchar(1024)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE app.WorkItems
    SET Name = @Name,
        Description = @Description,
        Status = @Status,
        UpdatedAtUtc = @UpdatedAtUtc,
        UpdatedBy = @UpdatedBy
    WHERE WorkItemId = @WorkItemId;

    SELECT CONVERT(int, @@ROWCOUNT) AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE app.WorkItems_Delete
    @WorkItemId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM app.WorkItems
    WHERE WorkItemId = @WorkItemId;

    SELECT CONVERT(int, @@ROWCOUNT) AS RowsAffected;
END;
GO
