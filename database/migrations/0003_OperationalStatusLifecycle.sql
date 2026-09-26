IF COL_LENGTH(N'app.OperationalStatuses', N'Status') IS NULL
BEGIN
    ALTER TABLE app.OperationalStatuses
        ADD Status nvarchar(32) NOT NULL
            CONSTRAINT DF_OperationalStatuses_Status DEFAULT N'Open';
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'app.OperationalStatuses')
      AND name = N'CK_OperationalStatuses_Status'
)
BEGIN
    ALTER TABLE app.OperationalStatuses
        ADD CONSTRAINT CK_OperationalStatuses_Status
        CHECK (Status IN (N'Open', N'InProgress', N'Complete'));
END;
GO

IF OBJECT_ID(N'app.OperationalStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE app.OperationalStatusHistory
    (
        HistoryId uniqueidentifier NOT NULL
            CONSTRAINT PK_OperationalStatusHistory PRIMARY KEY,
        StatusId uniqueidentifier NOT NULL
            CONSTRAINT FK_OperationalStatusHistory_Status
            REFERENCES app.OperationalStatuses(StatusId),
        PreviousStatus nvarchar(32) NOT NULL
            CONSTRAINT CK_OperationalStatusHistory_PreviousStatus
            CHECK (PreviousStatus IN (N'Open', N'InProgress', N'Complete')),
        NewStatus nvarchar(32) NOT NULL
            CONSTRAINT CK_OperationalStatusHistory_NewStatus
            CHECK (NewStatus IN (N'Open', N'InProgress', N'Complete')),
        ChangedBy nvarchar(1024) NOT NULL,
        ChangedAtUtc datetime2(7) NOT NULL,
        CONSTRAINT CK_OperationalStatusHistory_DifferentStatus
            CHECK (PreviousStatus <> NewStatus)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'app.OperationalStatusHistory')
      AND name = N'IX_OperationalStatusHistory_Status_ChangedAtUtc'
)
BEGIN
    CREATE INDEX IX_OperationalStatusHistory_Status_ChangedAtUtc
        ON app.OperationalStatusHistory (StatusId, ChangedAtUtc, HistoryId);
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_List
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StatusId, [Key], Status, [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy, RowVersion
    FROM app.OperationalStatuses
    ORDER BY [Key];
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_Get
    @Key nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StatusId, [Key], Status, [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy, RowVersion
    FROM app.OperationalStatuses
    WHERE [Key] = @Key;
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_Update
    @Key nvarchar(100),
    @Status nvarchar(32),
    @Value nvarchar(200),
    @Severity nvarchar(16),
    @ObservedAtUtc datetime2(7),
    @UpdatedAtUtc datetime2(7),
    @UpdatedBy nvarchar(1024),
    @ExpectedRowVersion binary(8),
    @Result int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Result = 1;

    DECLARE @StatusId uniqueidentifier;
    DECLARE @PreviousStatus nvarchar(32);
    DECLARE @CurrentRowVersion binary(8);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @StatusId = StatusId,
            @PreviousStatus = Status,
            @CurrentRowVersion = RowVersion
        FROM app.OperationalStatuses WITH (UPDLOCK, HOLDLOCK)
        WHERE [Key] = @Key;

        IF @StatusId IS NULL
        BEGIN
            SET @Result = 1;
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @CurrentRowVersion <> @ExpectedRowVersion
        BEGIN
            SET @Result = 2;
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        IF @Status NOT IN (N'Open', N'InProgress', N'Complete')
           OR NOT
           (
               (@PreviousStatus = N'Open' AND @Status = N'InProgress')
               OR (@PreviousStatus = N'InProgress' AND @Status = N'Complete')
           )
        BEGIN
            SET @Result = 3;
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE app.OperationalStatuses
        SET Status = @Status,
            [Value] = @Value,
            Severity = @Severity,
            ObservedAtUtc = @ObservedAtUtc,
            UpdatedAtUtc = @UpdatedAtUtc,
            UpdatedBy = @UpdatedBy
        WHERE StatusId = @StatusId;

        INSERT INTO app.OperationalStatusHistory
            (HistoryId, StatusId, PreviousStatus, NewStatus, ChangedBy, ChangedAtUtc)
        VALUES
            (NEWID(), @StatusId, @PreviousStatus, @Status, @UpdatedBy, @UpdatedAtUtc);

        COMMIT TRANSACTION;
        SET @Result = 0;

        SELECT StatusId, [Key], Status, [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy, RowVersion
        FROM app.OperationalStatuses
        WHERE StatusId = @StatusId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_History
    @Key nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT history.HistoryId, history.StatusId, history.PreviousStatus, history.NewStatus,
           history.ChangedBy, history.ChangedAtUtc
    FROM app.OperationalStatusHistory AS history
    INNER JOIN app.OperationalStatuses AS status
        ON status.StatusId = history.StatusId
    WHERE status.[Key] = @Key
    ORDER BY history.ChangedAtUtc, history.HistoryId;
END;
GO
