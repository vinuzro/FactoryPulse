-- FactoryPulse database initialization
-- Runs on first container start

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FactoryPulseAuth')
    CREATE DATABASE FactoryPulseAuth;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FactoryPulseInspection')
    CREATE DATABASE FactoryPulseInspection;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FactoryPulseReporting')
    CREATE DATABASE FactoryPulseReporting;
GO

USE FactoryPulseInspection;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Equipment')
CREATE TABLE Equipment (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100)   NOT NULL,
    Location    NVARCHAR(100)   NOT NULL,
    Status      NVARCHAR(20)    NOT NULL DEFAULT 'Online',  -- Online, Offline, Maintenance, Fault
    StatusFlags INT             NOT NULL DEFAULT 0,
    LastUpdated DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Inspections')
CREATE TABLE Inspections (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    EquipmentId     INT             NOT NULL REFERENCES Equipment(Id),
    InspectorUserId NVARCHAR(50)    NOT NULL,
    Notes           NVARCHAR(MAX),
    Result          NVARCHAR(20)    NOT NULL,  -- Pass, Fail, NeedsAttention
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
CREATE TABLE AuditLog (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    EntityType  NVARCHAR(50)    NOT NULL,
    EntityId    INT             NOT NULL,
    Action      NVARCHAR(50)    NOT NULL,
    UserId      NVARCHAR(50)    NOT NULL,
    OldValue    NVARCHAR(MAX),
    NewValue    NVARCHAR(MAX),
    Timestamp   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);

-- Seed some equipment
IF NOT EXISTS (SELECT TOP 1 1 FROM Equipment)
BEGIN
    INSERT INTO Equipment (Name, Location, Status) VALUES
        ('Conveyor Belt A1',  'Floor 1 - Section A', 'Online'),
        ('Hydraulic Press B2','Floor 1 - Section B', 'Online'),
        ('Welding Station C1','Floor 2 - Section C', 'Maintenance'),
        ('CNC Mill D3',       'Floor 2 - Section D', 'Online'),
        ('Packaging Line E1', 'Floor 3 - Section E', 'Online'),
        ('Compressor F2',     'Utility Room',        'Fault');
END
GO
