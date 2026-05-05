-- ═══════════════════════════════════════════════════════════════
-- ADD MAINTENANCE LOG TABLE
-- Creates read-only historical records of completed work orders
-- ═══════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaintenanceLog')
BEGIN
    CREATE TABLE MaintenanceLog (
        log_id INT IDENTITY(1,1) PRIMARY KEY,
        company_id INT NOT NULL,
        work_order_id INT NOT NULL,
        asset_id INT NULL,
        title NVARCHAR(200) NOT NULL,
        description NVARCHAR(MAX) NULL,
        completed_by_personnel_id INT NULL,
        completed_date DATETIME NOT NULL,
        notes NVARCHAR(MAX) NULL,
        created_at DATETIME NOT NULL DEFAULT GETDATE(),
        
        -- Foreign Keys
        CONSTRAINT FK_MaintenanceLog_Company FOREIGN KEY (company_id) REFERENCES Company(company_id),
        CONSTRAINT FK_MaintenanceLog_WorkOrder FOREIGN KEY (work_order_id) REFERENCES Work_Order(work_order_id),
        CONSTRAINT FK_MaintenanceLog_Asset FOREIGN KEY (asset_id) REFERENCES Asset(asset_id),
        CONSTRAINT FK_MaintenanceLog_Personnel FOREIGN KEY (completed_by_personnel_id) REFERENCES Personnel(personnel_id),
        
        -- Unique constraint: one log per work order
        CONSTRAINT UQ_MaintenanceLog_WorkOrder UNIQUE (work_order_id)
    );
    
    PRINT '✓ MaintenanceLog table created successfully';
END
ELSE
BEGIN
    PRINT '⚠ MaintenanceLog table already exists';
END

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaintenanceLog_CompanyId')
BEGIN
    CREATE INDEX IX_MaintenanceLog_CompanyId ON MaintenanceLog(company_id);
    PRINT '✓ Created index on company_id';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaintenanceLog_AssetId')
BEGIN
    CREATE INDEX IX_MaintenanceLog_AssetId ON MaintenanceLog(asset_id);
    PRINT '✓ Created index on asset_id';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaintenanceLog_CompletedDate')
BEGIN
    CREATE INDEX IX_MaintenanceLog_CompletedDate ON MaintenanceLog(completed_date);
    PRINT '✓ Created index on completed_date';
END

PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'Maintenance Log table setup complete!';
PRINT '═══════════════════════════════════════════════════════════════';
