CREATE DATABASE EnterpriseRagDb;
GO

USE EnterpriseRagDb;
GO

CREATE TABLE Documents (
    DocumentId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    FileName NVARCHAR(260) NOT NULL,
    Domain NVARCHAR(50) NOT NULL, -- 'ClinicalTrial' | 'FinancialReport'
    Status NVARCHAR(30) NOT NULL DEFAULT 'Processing', -- 'Processing' | 'Ready' | 'Failed'
    PageCount INT NOT NULL DEFAULT 0,
    UploadedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE DocumentChunks (
    ChunkId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL
        REFERENCES Documents (DocumentId) ON DELETE CASCADE,
    ChunkIndex INT NOT NULL,
    PageNumber INT NOT NULL,
    SectionTitle NVARCHAR(250) NULL,
    Content NVARCHAR(MAX) NOT NULL,
    EmbeddingJson NVARCHAR(MAX) NOT NULL
);
GO

CREATE INDEX IX_DocumentChunks_DocumentId
    ON DocumentChunks (DocumentId);
GO