/*
    Módulo: CONFIGURACIÓN (CFG_)
    Tablas: CFG_Countries, CFG_States, CFG_Cities, CFG_Landlords,
            CFG_Permissions, CFG_Roles, CFG_RolePermissions, CFG_Users, CFG_Categories

    NOTA DE ORDEN:
    Los DROP respetan las dependencias de FK (primero las tablas hijas) y los
    CREATE van en el orden inverso (primero las tablas de las que otras dependen).

    NOTA MULTIUSUARIO:
    Landlord es el "negocio" (tenant); ya no guarda credenciales de acceso.
    Cada persona que inicia sesión es un CFG_Users, con un CFG_Roles asignado.
    CFG_Roles agrupa permisos (CFG_Permissions) por medio de CFG_RolePermissions.
    Al registrar un negocio se crea el Landlord + un Rol de sistema "Administrador"
    (con todos los permisos) + un Usuario dueño (IsOwner = 1) con ese rol.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Drops (en orden inverso de dependencia)
-- =========================================================
IF OBJECT_ID(N'dbo.CFG_Categories', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Categories;
GO

IF OBJECT_ID(N'dbo.CFG_Users', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Users;
GO

IF OBJECT_ID(N'dbo.CFG_RolePermissions', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_RolePermissions;
GO

IF OBJECT_ID(N'dbo.CFG_Roles', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Roles;
GO

IF OBJECT_ID(N'dbo.CFG_Permissions', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Permissions;
GO

IF OBJECT_ID(N'dbo.CFG_Landlords', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Landlords;
GO

IF OBJECT_ID(N'dbo.CFG_Cities', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Cities;
GO

IF OBJECT_ID(N'dbo.CFG_States', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_States;
GO

IF OBJECT_ID(N'dbo.CFG_Countries', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Countries;
GO

-- =========================================================
-- Tabla: CFG_Countries
-- Catálogo de países (dropdown en cascada País -> Provincia -> Ciudad).
-- =========================================================
CREATE TABLE dbo.CFG_Countries
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    Name            NVARCHAR(100)      NOT NULL,
    IsoCode         CHAR(2)            NOT NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Countries_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CFG_Countries PRIMARY KEY (Id),
    CONSTRAINT UQ_CFG_Countries_Name UNIQUE (Name),
    CONSTRAINT UQ_CFG_Countries_IsoCode UNIQUE (IsoCode)
);
GO

-- =========================================================
-- Tabla: CFG_States
-- Provincias / estados / departamentos, dependientes de un país.
-- =========================================================
CREATE TABLE dbo.CFG_States
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    CountryId       INT                NOT NULL,
    Name            NVARCHAR(100)      NOT NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_States_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CFG_States PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_States_CFG_Countries FOREIGN KEY (CountryId)
        REFERENCES dbo.CFG_Countries (Id),
    CONSTRAINT UQ_CFG_States_Country_Name UNIQUE (CountryId, Name)
);
GO

CREATE INDEX IX_CFG_States_CountryId ON dbo.CFG_States (CountryId);
GO

-- =========================================================
-- Tabla: CFG_Cities
-- Ciudades principales, dependientes de una provincia/estado.
-- =========================================================
CREATE TABLE dbo.CFG_Cities
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    StateId         INT                NOT NULL,
    Name            NVARCHAR(100)      NOT NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Cities_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CFG_Cities PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_Cities_CFG_States FOREIGN KEY (StateId)
        REFERENCES dbo.CFG_States (Id),
    CONSTRAINT UQ_CFG_Cities_State_Name UNIQUE (StateId, Name)
);
GO

CREATE INDEX IX_CFG_Cities_StateId ON dbo.CFG_Cities (StateId);
GO

-- =========================================================
-- Tabla: CFG_Landlords
-- Negocios (tenants) dados de alta en la plataforma. Ya NO guarda
-- credenciales de acceso: eso vive en CFG_Users (ver más abajo).
-- =========================================================
CREATE TABLE dbo.CFG_Landlords
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_CFG_Landlords_Id DEFAULT NEWID(),
    BusinessName    NVARCHAR(150)      NOT NULL,
    AppName         NVARCHAR(100)      NOT NULL CONSTRAINT DF_CFG_Landlords_AppName DEFAULT 'Alkiman',
    ThemeMode       NVARCHAR(10)       NOT NULL CONSTRAINT DF_CFG_Landlords_ThemeMode DEFAULT 'light',
    AccentColor     NVARCHAR(20)       NOT NULL CONSTRAINT DF_CFG_Landlords_AccentColor DEFAULT 'blue',
    CountryId       INT                NULL,
    StateId         INT                NULL,
    CityId          INT                NULL,
    Address         NVARCHAR(255)      NULL,
    Phone1          NVARCHAR(30)       NULL,
    Phone2          NVARCHAR(30)       NULL,
    TaxId           NVARCHAR(50)       NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Landlords_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_Landlords PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_Landlords_CFG_Countries FOREIGN KEY (CountryId)
        REFERENCES dbo.CFG_Countries (Id),
    CONSTRAINT FK_CFG_Landlords_CFG_States FOREIGN KEY (StateId)
        REFERENCES dbo.CFG_States (Id),
    CONSTRAINT FK_CFG_Landlords_CFG_Cities FOREIGN KEY (CityId)
        REFERENCES dbo.CFG_Cities (Id)
);
GO

CREATE INDEX IX_CFG_Landlords_CountryId ON dbo.CFG_Landlords (CountryId);
CREATE INDEX IX_CFG_Landlords_StateId ON dbo.CFG_Landlords (StateId);
CREATE INDEX IX_CFG_Landlords_CityId ON dbo.CFG_Landlords (CityId);
GO

-- =========================================================
-- Tabla: CFG_Permissions
-- Catálogo global de permisos disponibles en el sistema (fijo,
-- se mantiene sincronizado a mano con Alkiman.Application.Common.Permissions.PermissionCatalog).
-- =========================================================
CREATE TABLE dbo.CFG_Permissions
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    Code            NVARCHAR(50)       NOT NULL,
    Module          NVARCHAR(50)       NOT NULL,
    Description     NVARCHAR(200)      NOT NULL,
    CONSTRAINT PK_CFG_Permissions PRIMARY KEY (Id),
    CONSTRAINT UQ_CFG_Permissions_Code UNIQUE (Code)
);
GO

INSERT INTO dbo.CFG_Permissions (Code, Module, Description) VALUES
    ('assets.view',      'Activos',    'Ver activos'),
    ('assets.manage',    'Activos',    'Crear, editar y eliminar activos'),
    ('customers.view',   'Clientes',   'Ver clientes'),
    ('customers.manage', 'Clientes',   'Crear, editar y eliminar clientes'),
    ('rentals.view',     'Rentas',     'Ver rentas'),
    ('rentals.manage',   'Rentas',     'Crear y editar rentas'),
    ('payments.view',    'Pagos',      'Ver pagos'),
    ('payments.manage',  'Pagos',      'Registrar pagos'),
    ('categories.view',  'Categorías', 'Ver categorías'),
    ('categories.manage','Categorías', 'Crear, editar y eliminar categorías'),
    ('audit.view',       'Bitácora',   'Ver la bitácora de actividad'),
    ('settings.manage',  'Ajustes',    'Editar el perfil y la apariencia del negocio'),
    ('users.manage',     'Usuarios',   'Invitar, editar y eliminar usuarios del negocio'),
    ('roles.manage',     'Roles',      'Crear, editar y eliminar roles y sus permisos'),
    ('assetgroups.view',  'Grupos de Activos', 'Ver grupos de activos'),
    ('assetgroups.manage','Grupos de Activos', 'Crear, editar y eliminar grupos de activos'),
    ('reports.view',      'Reportes',   'Ver métricas y reportes del negocio'),
    ('emails.view',       'Correos',    'Ver correos enviados y recordatorios'),
    ('emails.manage',     'Correos',    'Enviar correos y gestionar recordatorios');
GO

-- =========================================================
-- Tabla: CFG_Roles
-- Roles definidos por cada negocio. El rol "Administrador" (IsSystem = 1)
-- se crea automáticamente al registrar el negocio y no puede editarse ni eliminarse.
-- =========================================================
CREATE TABLE dbo.CFG_Roles
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    Name            NVARCHAR(100)      NOT NULL,
    Description     NVARCHAR(255)      NULL,
    IsSystem        BIT                NOT NULL CONSTRAINT DF_CFG_Roles_IsSystem DEFAULT 0,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Roles_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_Roles PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_Roles_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT UQ_CFG_Roles_Landlord_Name UNIQUE (LandlordId, Name)
);
GO

CREATE INDEX IX_CFG_Roles_LandlordId ON dbo.CFG_Roles (LandlordId);
GO

-- =========================================================
-- Tabla: CFG_RolePermissions
-- Relación N:N entre roles y permisos.
-- =========================================================
CREATE TABLE dbo.CFG_RolePermissions
(
    RoleId          INT                NOT NULL,
    PermissionId    INT                NOT NULL,
    CONSTRAINT PK_CFG_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_CFG_RolePermissions_CFG_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.CFG_Roles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_CFG_RolePermissions_CFG_Permissions FOREIGN KEY (PermissionId)
        REFERENCES dbo.CFG_Permissions (Id)
);
GO

-- =========================================================
-- Tabla: CFG_Users
-- Personas que inician sesión. Cada una pertenece a un negocio (LandlordId)
-- y tiene un rol (RoleId). El usuario dueño (IsOwner = 1) se crea junto con
-- el negocio y no puede eliminarse, desactivarse ni cambiar de rol.
-- =========================================================
CREATE TABLE dbo.CFG_Users
(
    Id                  UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_CFG_Users_Id DEFAULT NEWID(),
    LandlordId          UNIQUEIDENTIFIER   NOT NULL,
    RoleId              INT                NOT NULL,
    FullName            NVARCHAR(150)      NOT NULL,
    Email               NVARCHAR(100)      NOT NULL,
    PasswordHash        NVARCHAR(255)      NOT NULL,
    IsOwner             BIT                NOT NULL CONSTRAINT DF_CFG_Users_IsOwner DEFAULT 0,
    IsActive            BIT                NOT NULL CONSTRAINT DF_CFG_Users_IsActive DEFAULT 1,
    TwoFactorEnabled    BIT                NOT NULL CONSTRAINT DF_CFG_Users_TwoFactorEnabled DEFAULT 0,
    CreatedAt           DATETIME2          NOT NULL CONSTRAINT DF_CFG_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy           NVARCHAR(255)      NOT NULL,
    UpdatedAt           DATETIME2          NULL,
    UpdatedBy           NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_CFG_Users_Email UNIQUE (Email),
    CONSTRAINT FK_CFG_Users_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_CFG_Users_CFG_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.CFG_Roles (Id)
);
GO

CREATE INDEX IX_CFG_Users_LandlordId ON dbo.CFG_Users (LandlordId);
CREATE INDEX IX_CFG_Users_RoleId ON dbo.CFG_Users (RoleId);
GO

-- =========================================================
-- Tabla: CFG_Categories
-- Agrupa los activos para su correcta organización.
-- =========================================================
CREATE TABLE dbo.CFG_Categories
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    Name            NVARCHAR(50)       NOT NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_Categories PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_Categories_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT UQ_CFG_Categories_Landlord_Name UNIQUE (LandlordId, Name)
);
GO

CREATE INDEX IX_CFG_Categories_LandlordId ON dbo.CFG_Categories (LandlordId);
GO
