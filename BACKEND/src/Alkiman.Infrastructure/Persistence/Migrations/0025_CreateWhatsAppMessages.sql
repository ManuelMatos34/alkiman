CREATE TABLE WhatsAppMessages (
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    LandlordId  UNIQUEIDENTIFIER NOT NULL,
    ToPhone     NVARCHAR(30)     NOT NULL,
    TemplateName NVARCHAR(100)   NOT NULL,
    Status      NVARCHAR(20)     NOT NULL,   -- 'Sent' | 'Failed'
    ErrorMessage NVARCHAR(500)   NULL,
    SentAt      DATETIME2        NULL,
    CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_WhatsAppMessages_LandlordId_CreatedAt ON WhatsAppMessages(LandlordId, CreatedAt);
