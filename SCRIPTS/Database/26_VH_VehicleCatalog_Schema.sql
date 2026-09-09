-- =============================================================================
-- 26_VH_VehicleCatalog_Schema.sql
-- Catálogo de marcas y modelos de vehículos (prefijo VH_).
-- =============================================================================
USE Alkiman_Dev;
GO

CREATE TABLE dbo.VH_Makes (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(80) NOT NULL,
    IsActive BIT          NOT NULL DEFAULT 1
);

CREATE TABLE dbo.VH_Models (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    MakeId   INT          NOT NULL REFERENCES dbo.VH_Makes(Id),
    Name     NVARCHAR(80) NOT NULL,
    IsActive BIT          NOT NULL DEFAULT 1
);

GO

-- =============================================================================
-- SEED — Marcas y modelos más comunes en República Dominicana
-- =============================================================================

DECLARE @Toyota    INT, @Honda      INT, @Hyundai  INT, @Kia       INT,
        @Chevrolet INT, @Nissan     INT, @Ford     INT, @Jeep      INT,
        @Mitsubishi INT, @Mazda     INT, @VW       INT, @Mercedes  INT,
        @BMW       INT, @Audi       INT, @Dodge    INT, @RAM       INT,
        @Subaru    INT, @Suzuki     INT, @Lexus    INT;

INSERT INTO dbo.VH_Makes (Name) VALUES ('Toyota');     SET @Toyota     = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Honda');      SET @Honda      = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Hyundai');    SET @Hyundai    = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Kia');        SET @Kia        = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Chevrolet');  SET @Chevrolet  = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Nissan');     SET @Nissan     = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Ford');       SET @Ford       = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Jeep');       SET @Jeep       = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Mitsubishi'); SET @Mitsubishi = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Mazda');      SET @Mazda      = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Volkswagen'); SET @VW         = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Mercedes-Benz'); SET @Mercedes = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('BMW');        SET @BMW        = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Audi');       SET @Audi       = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Dodge');      SET @Dodge      = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('RAM');        SET @RAM        = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Subaru');     SET @Subaru     = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Suzuki');     SET @Suzuki     = SCOPE_IDENTITY();
INSERT INTO dbo.VH_Makes (Name) VALUES ('Lexus');      SET @Lexus      = SCOPE_IDENTITY();

-- Toyota
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Toyota, 'Corolla'), (@Toyota, 'Camry'), (@Toyota, 'Yaris'), (@Toyota, 'RAV4'),
  (@Toyota, 'Hilux'), (@Toyota, 'Land Cruiser'), (@Toyota, 'Rush'), (@Toyota, 'Fortuner'),
  (@Toyota, 'Prado'), (@Toyota, 'Highlander');

-- Honda
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Honda, 'Civic'), (@Honda, 'Accord'), (@Honda, 'CR-V'), (@Honda, 'HR-V'),
  (@Honda, 'Pilot'), (@Honda, 'Fit'), (@Honda, 'Odyssey');

-- Hyundai
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Hyundai, 'Elantra'), (@Hyundai, 'Tucson'), (@Hyundai, 'Santa Fe'),
  (@Hyundai, 'Accent'), (@Hyundai, 'Creta'), (@Hyundai, 'Sonata');

-- Kia
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Kia, 'Rio'), (@Kia, 'Sportage'), (@Kia, 'Sorento'),
  (@Kia, 'Picanto'), (@Kia, 'Forte'), (@Kia, 'Carnival');

-- Chevrolet
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Chevrolet, 'Spark'), (@Chevrolet, 'Aveo'), (@Chevrolet, 'Cruze'),
  (@Chevrolet, 'Equinox'), (@Chevrolet, 'Tahoe'), (@Chevrolet, 'Traverse'),
  (@Chevrolet, 'Colorado');

-- Nissan
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Nissan, 'Versa'), (@Nissan, 'Sentra'), (@Nissan, 'Altima'), (@Nissan, 'X-Trail'),
  (@Nissan, 'Murano'), (@Nissan, 'Pathfinder'), (@Nissan, 'Frontier'), (@Nissan, 'NP300');

-- Ford
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Ford, 'EcoSport'), (@Ford, 'Escape'), (@Ford, 'Explorer'),
  (@Ford, 'Ranger'), (@Ford, 'F-150'), (@Ford, 'Fusion');

-- Jeep
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Jeep, 'Wrangler'), (@Jeep, 'Cherokee'), (@Jeep, 'Grand Cherokee'),
  (@Jeep, 'Compass'), (@Jeep, 'Renegade');

-- Mitsubishi
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Mitsubishi, 'Outlander'), (@Mitsubishi, 'Montero'), (@Mitsubishi, 'Galant'),
  (@Mitsubishi, 'Eclipse Cross'), (@Mitsubishi, 'L200');

-- Mazda
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Mazda, 'Mazda 3'), (@Mazda, 'Mazda 6'), (@Mazda, 'CX-5'), (@Mazda, 'CX-30');

-- Volkswagen
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@VW, 'Golf'), (@VW, 'Polo'), (@VW, 'Jetta'), (@VW, 'Tiguan'), (@VW, 'Passat');

-- Mercedes-Benz
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Mercedes, 'Clase C'), (@Mercedes, 'Clase E'), (@Mercedes, 'GLC'), (@Mercedes, 'GLE');

-- BMW
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@BMW, 'Serie 3'), (@BMW, 'Serie 5'), (@BMW, 'X3'), (@BMW, 'X5');

-- Audi
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Audi, 'A4'), (@Audi, 'A6'), (@Audi, 'Q5'), (@Audi, 'Q7');

-- Dodge
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Dodge, 'Charger'), (@Dodge, 'Challenger'), (@Dodge, 'Durango');

-- RAM
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@RAM, '1500'), (@RAM, '2500');

-- Subaru
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Subaru, 'Outback'), (@Subaru, 'Forester'), (@Subaru, 'Impreza');

-- Suzuki
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Suzuki, 'Swift'), (@Suzuki, 'Vitara'), (@Suzuki, 'Jimny');

-- Lexus
INSERT INTO dbo.VH_Models (MakeId, Name) VALUES
  (@Lexus, 'IS'), (@Lexus, 'ES'), (@Lexus, 'RX'), (@Lexus, 'NX');

GO
