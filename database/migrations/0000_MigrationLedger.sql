/* Infrastructure ledger only. No business tables are defined in Phase 2. */
IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaMigrations
    (
        VersionNumber int NOT NULL,
        MigrationName nvarchar(200) NOT NULL,
        ScriptSha256 char(64) NOT NULL,
        AppliedUtc datetime2(7) NOT NULL,
        CONSTRAINT PK_SchemaMigrations PRIMARY KEY (VersionNumber),
        CONSTRAINT UQ_SchemaMigrations_Name UNIQUE (MigrationName),
        CONSTRAINT CK_SchemaMigrations_Hash CHECK (LEN(ScriptSha256) = 64)
    );
END;
