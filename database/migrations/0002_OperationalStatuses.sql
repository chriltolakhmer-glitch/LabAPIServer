IF OBJECT_ID(N'app.OperationalStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE app.OperationalStatuses
    (
        StatusId uniqueidentifier NOT NULL
            CONSTRAINT PK_OperationalStatuses PRIMARY KEY,
        [Key] nvarchar(100) NOT NULL
            CONSTRAINT UQ_OperationalStatuses_Key UNIQUE,
        [Value] nvarchar(200) NOT NULL,
        Severity nvarchar(16) NOT NULL
            CONSTRAINT CK_OperationalStatuses_Severity CHECK (Severity IN (N'Info', N'Warning', N'Critical')),
        ObservedAtUtc datetime2(7) NOT NULL,
        UpdatedAtUtc datetime2(7) NOT NULL,
        UpdatedBy nvarchar(1024) NOT NULL,
        RowVersion rowversion NOT NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_List
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StatusId, [Key], [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy, RowVersion
    FROM app.OperationalStatuses
    ORDER BY [Key];
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_Get
    @Key nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StatusId, [Key], [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy, RowVersion
    FROM app.OperationalStatuses
    WHERE [Key] = @Key;
END;
GO

CREATE OR ALTER PROCEDURE app.OperationalStatuses_Update
    @Key nvarchar(100),
    @Value nvarchar(200),
    @Severity nvarchar(16),
    @ObservedAtUtc datetime2(7),
    @UpdatedAtUtc datetime2(7),
    @UpdatedBy nvarchar(1024),
    @ExpectedRowVersion binary(8)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE app.OperationalStatuses
    SET [Value] = @Value,
        Severity = @Severity,
        ObservedAtUtc = @ObservedAtUtc,
        UpdatedAtUtc = @UpdatedAtUtc,
        UpdatedBy = @UpdatedBy
    WHERE [Key] = @Key
      AND RowVersion = @ExpectedRowVersion;

    IF @@ROWCOUNT = 1
    BEGIN
        SELECT StatusId, [Key], [Value], Severity, ObservedAtUtc, UpdatedAtUtc, UpdatedBy, RowVersion
        FROM app.OperationalStatuses
        WHERE [Key] = @Key;
    END;
END;
GO