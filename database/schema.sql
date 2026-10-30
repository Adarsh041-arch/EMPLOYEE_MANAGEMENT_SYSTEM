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
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [CodePrefix] nchar(1) NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeCodeSequences] (
        [DepartmentId] int NOT NULL,
        [LastNumber] int NOT NULL,
        CONSTRAINT [PK_EmployeeCodeSequences] PRIMARY KEY ([DepartmentId]),
        CONSTRAINT [FK_EmployeeCodeSequences_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeCode] nvarchar(10) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Mobile] nvarchar(10) NOT NULL,
        [Gender] nvarchar(10) NOT NULL,
        [DepartmentId] int NOT NULL,
        [RoleId] int NOT NULL,
        [JoiningDate] date NOT NULL,
        [AnnualSalary] decimal(12,2) NOT NULL,
        [Status] nvarchar(10) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Employees_AnnualSalary] CHECK ([AnnualSalary] > 0),
        CONSTRAINT [CK_Employees_Gender] CHECK ([Gender] IN ('Male','Female','Other')),
        CONSTRAINT [CK_Employees_Status] CHECK ([Status] IN ('Active','Inactive','OnLeave')),
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeePersonalDetails] (
        [EmployeeId] int NOT NULL,
        [DateOfBirth] date NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [AadhaarEncrypted] nvarchar(200) NOT NULL,
        [AadhaarLast4] nchar(4) NOT NULL,
        [PanEncrypted] nvarchar(200) NOT NULL,
        [PanMasked] nvarchar(10) NOT NULL,
        CONSTRAINT [PK_EmployeePersonalDetails] PRIMARY KEY ([EmployeeId]),
        CONSTRAINT [FK_EmployeePersonalDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [PasswordHash] nvarchar(100) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [EmployeeId] int NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeTransferHistory] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [OldDepartmentId] int NOT NULL,
        [NewDepartmentId] int NOT NULL,
        [OldEmployeeCode] nvarchar(10) NOT NULL,
        [NewEmployeeCode] nvarchar(10) NOT NULL,
        [TransferDate] date NOT NULL,
        [Reason] nvarchar(300) NOT NULL,
        [TransferredByUserId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeTransferHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeTransferHistory_Departments_NewDepartmentId] FOREIGN KEY ([NewDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeTransferHistory_Departments_OldDepartmentId] FOREIGN KEY ([OldDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeTransferHistory_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeTransferHistory_Users_TransferredByUserId] FOREIGN KEY ([TransferredByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CodePrefix', N'Name') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] ON;
    EXEC(N'INSERT INTO [Departments] ([Id], [CodePrefix], [Name])
    VALUES (1, N''I'', N''IT''),
    (2, N''H'', N''HR''),
    (3, N''F'', N''Finance''),
    (4, N''S'', N''Sales''),
    (5, N''A'', N''Admin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CodePrefix', N'Name') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [Name])
    VALUES (1, N''Developer''),
    (2, N''Manager''),
    (3, N''Analyst''),
    (4, N''Executive''),
    (5, N''HR Executive'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DepartmentId', N'LastNumber') AND [object_id] = OBJECT_ID(N'[EmployeeCodeSequences]'))
        SET IDENTITY_INSERT [EmployeeCodeSequences] ON;
    EXEC(N'INSERT INTO [EmployeeCodeSequences] ([DepartmentId], [LastNumber])
    VALUES (1, 0),
    (2, 0),
    (3, 0),
    (4, 0),
    (5, 0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DepartmentId', N'LastNumber') AND [object_id] = OBJECT_ID(N'[EmployeeCodeSequences]'))
        SET IDENTITY_INSERT [EmployeeCodeSequences] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_CodePrefix] ON [Departments] ([CodePrefix]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_AnnualSalary] ON [Employees] ([AnnualSalary]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_FullName] ON [Employees] ([FullName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_JoiningDate] ON [Employees] ([JoiningDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_RoleId] ON [Employees] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_Status] ON [Employees] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeTransferHistory_EmployeeId] ON [EmployeeTransferHistory] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeTransferHistory_NewDepartmentId] ON [EmployeeTransferHistory] ([NewDepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeTransferHistory_OldDepartmentId] ON [EmployeeTransferHistory] ([OldDepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeTransferHistory_TransferredByUserId] ON [EmployeeTransferHistory] ([TransferredByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_EmployeeId] ON [Users] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030113653_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030113653_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

