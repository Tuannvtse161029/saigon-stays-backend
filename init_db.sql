IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'homestay')
BEGIN
    CREATE DATABASE homestay;
END
GO
USE homestay;
GO

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
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Address] nvarchar(255) NOT NULL,
        [Hotline] nvarchar(20) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [GoogleId] nvarchar(max) NULL,
        [Email] nvarchar(100) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [AvatarUrl] nvarchar(max) NULL,
        [Role] nvarchar(20) NOT NULL DEFAULT N'GUEST',
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ImageUrl] nvarchar(max) NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'AVAILABLE',
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rooms_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE TABLE [PricingSlots] (
        [Id] uniqueidentifier NOT NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [SlotName] nvarchar(50) NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PricingSlots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PricingSlots_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE TABLE [RoomCheckinInfos] (
        [Id] uniqueidentifier NOT NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [DoorPasscode] nvarchar(50) NOT NULL,
        [WifiSSID] nvarchar(50) NULL,
        [WifiPassword] nvarchar(50) NULL,
        CONSTRAINT [PK_RoomCheckinInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoomCheckinInfos_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] nvarchar(20) NOT NULL,
        [UserId] uniqueidentifier NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [PricingSlotId] uniqueidentifier NOT NULL,
        [GuestName] nvarchar(100) NOT NULL,
        [GuestPhone] nvarchar(20) NOT NULL,
        [GuestEmail] nvarchar(100) NOT NULL,
        [BookingDate] date NOT NULL,
        [TotalPrice] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'PENDING',
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_PricingSlots_PricingSlotId] FOREIGN KEY ([PricingSlotId]) REFERENCES [PricingSlots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_BookingDate] ON [Bookings] ([BookingDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_PricingSlotId] ON [Bookings] ([PricingSlotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_RoomId_PricingSlotId_BookingDate] ON [Bookings] ([RoomId], [PricingSlotId], [BookingDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_Status] ON [Bookings] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_UserId] ON [Bookings] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PricingSlots_RoomId] ON [PricingSlots] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoomCheckinInfos_RoomId] ON [RoomCheckinInfos] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Rooms_BranchId] ON [Rooms] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010071939_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010071939_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

