CREATE TABLE dbo.CFG_LandlordWhatsApp (
    Id            UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    LandlordId    UNIQUEIDENTIFIER NOT NULL UNIQUE,
    PhoneNumberId NVARCHAR(50)     NOT NULL,
    AccessToken   NVARCHAR(500)    NOT NULL,
    PhoneNumber   NVARCHAR(20)     NULL,
    DisplayName   NVARCHAR(100)    NULL,
    IsActive      BIT              NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt     DATETIME2        NULL,
    CreatedBy     NVARCHAR(100)    NULL,
    UpdatedBy     NVARCHAR(100)    NULL
);
