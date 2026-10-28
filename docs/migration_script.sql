IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027130033_InitialCreate'
)
BEGIN
    CREATE TABLE [AppSettings] (
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Key])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027130033_InitialCreate'
)
BEGIN
    CREATE TABLE [Holidays] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [From] date NOT NULL,
        [To] date NOT NULL,
        [IsOpeningDay] bit NOT NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027130033_InitialCreate'
)
BEGIN
    CREATE TABLE [Subjects] (
        [Id] int NOT NULL IDENTITY,
        [SortOrder] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Periods] int NOT NULL,
        [Grade] decimal(4,1) NULL,
        [ReservePeriods] int NULL,
        [Note] nvarchar(100) NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027130033_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Holidays_From] ON [Holidays] ([From]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027130033_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subjects_SortOrder] ON [Subjects] ([SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027130033_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027130033_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022453_AddFlexibleExamAndOpeningDay'
)
BEGIN
    ALTER TABLE [Subjects] ADD [CustomExamSlot] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022453_AddFlexibleExamAndOpeningDay'
)
BEGIN
    ALTER TABLE [Subjects] ADD [ExamMode] nvarchar(50) NOT NULL DEFAULT N'Immediately';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022453_AddFlexibleExamAndOpeningDay'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028022453_AddFlexibleExamAndOpeningDay', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE TABLE [PlanActivities] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Type] int NOT NULL,
        [AfterSubjectId] int NULL,
        [SortOrder] int NOT NULL,
        [FromDate] date NULL,
        [ToDate] date NULL,
        [Sessions] int NULL,
        [Periods] int NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_PlanActivities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanActivities_Subjects_AfterSubjectId] FOREIGN KEY ([AfterSubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE TABLE [Students] (
        [Id] int NOT NULL IDENTITY,
        [SortOrder] int NOT NULL,
        [StudentCode] nvarchar(50) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [DateOfBirth] date NULL,
        [Gender] nvarchar(10) NULL,
        [Note] nvarchar(200) NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE TABLE [StudentGrades] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [Score] decimal(4,2) NULL,
        [Note] nvarchar(200) NULL,
        CONSTRAINT [PK_StudentGrades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StudentGrades_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentGrades_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE INDEX [IX_PlanActivities_AfterSubjectId] ON [PlanActivities] ([AfterSubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE INDEX [IX_PlanActivities_SortOrder] ON [PlanActivities] ([SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentGrades_StudentId_SubjectId] ON [StudentGrades] ([StudentId], [SubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE INDEX [IX_StudentGrades_SubjectId] ON [StudentGrades] ([SubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE INDEX [IX_Students_SortOrder] ON [Students] ([SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    CREATE INDEX [IX_Students_StudentCode] ON [Students] ([StudentCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028100932_AddPlanActivitiesAndStudentGrades'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028100932_AddPlanActivitiesAndStudentGrades', N'8.0.11');
END;
GO

COMMIT;
GO

