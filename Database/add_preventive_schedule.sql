-- ═══════════════════════════════════════════════════════════════
-- ADD PREVENTIVE SCHEDULE TABLE
-- Creates the preventive maintenance scheduling system
-- ═══════════════════════════════════════════════════════════════

-- Check if table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PreventiveSchedule')
BEGIN
    CREATE TABLE PreventiveSchedule (
        schedule_id INT IDENTITY(1,1) PRIMARY KEY,
        company_id INT NOT NULL,
        asset_id INT NOT NULL,
        title NVARCHAR(200) NOT NULL,
        description NVARCHAR(MAX) NULL,
        frequency_days INT NOT NULL,
        next_due_date DATE NOT NULL,
        last_completed_date DATE NULL,
        is_active BIT NOT NULL DEFAULT 1,
        default_technician_id INT NULL,
        created_at DATETIME NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_PreventiveSchedule_Company FOREIGN KEY (company_id) REFERENCES Company(company_id),
        CONSTRAINT FK_PreventiveSchedule_Asset FOREIGN KEY (asset_id) REFERENCES Asset(asset_id),
        CONSTRAINT FK_PreventiveSchedule_Technician FOREIGN KEY (default_technician_id) REFERENCES Personnel(personnel_id),
        
        -- Constraints
        CONSTRAINT CHK_PreventiveSchedule_FrequencyDays CHECK (frequency_days > 0)
    );
    
    PRINT '✓ PreventiveSchedule table created successfully';
END
ELSE
BEGIN
    PRINT '⚠ PreventiveSchedule table already exists';
    
    -- Add columns if they don't exist (for migration from old schema)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PreventiveSchedule') AND name = 'title')
    BEGIN
        -- Rename schedule_name to title if it exists
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PreventiveSchedule') AND name = 'schedule_name')
        BEGIN
            EXEC sp_rename 'PreventiveSchedule.schedule_name', 'title', 'COLUMN';
            PRINT '✓ Renamed schedule_name to title';
        END
        ELSE
        BEGIN
            ALTER TABLE PreventiveSchedule ADD title NVARCHAR(200) NOT NULL DEFAULT 'Preventive Maintenance';
            PRINT '✓ Added title column';
        END
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PreventiveSchedule') AND name = 'default_technician_id')
    BEGIN
        ALTER TABLE PreventiveSchedule ADD default_technician_id INT NULL;
        
        -- Add foreign key constraint
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PreventiveSchedule_Technician')
        BEGIN
            ALTER TABLE PreventiveSchedule 
            ADD CONSTRAINT FK_PreventiveSchedule_Technician 
            FOREIGN KEY (default_technician_id) REFERENCES Personnel(personnel_id);
        END
        
        PRINT '✓ Added default_technician_id column';
    END
    
    -- Remove created_by if it exists (replaced by default_technician_id)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PreventiveSchedule') AND name = 'created_by')
    BEGIN
        -- Drop all foreign key constraints that reference created_by
        DECLARE @ConstraintName NVARCHAR(200);
        DECLARE constraint_cursor CURSOR FOR
        SELECT fk.name
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
        WHERE fk.parent_object_id = OBJECT_ID('PreventiveSchedule')
        AND c.name = 'created_by';
        
        OPEN constraint_cursor;
        FETCH NEXT FROM constraint_cursor INTO @ConstraintName;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE PreventiveSchedule DROP CONSTRAINT ' + @ConstraintName;
            EXEC sp_executesql @SQL;
            PRINT '✓ Dropped constraint: ' + @ConstraintName;
            FETCH NEXT FROM constraint_cursor INTO @ConstraintName;
        END
        
        CLOSE constraint_cursor;
        DEALLOCATE constraint_cursor;
        
        ALTER TABLE PreventiveSchedule DROP COLUMN created_by;
        PRINT '✓ Removed created_by column';
    END
END

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PreventiveSchedule_CompanyId')
BEGIN
    CREATE INDEX IX_PreventiveSchedule_CompanyId ON PreventiveSchedule(company_id);
    PRINT '✓ Created index on company_id';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PreventiveSchedule_AssetId')
BEGIN
    CREATE INDEX IX_PreventiveSchedule_AssetId ON PreventiveSchedule(asset_id);
    PRINT '✓ Created index on asset_id';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PreventiveSchedule_NextDueDate')
BEGIN
    CREATE INDEX IX_PreventiveSchedule_NextDueDate ON PreventiveSchedule(next_due_date);
    PRINT '✓ Created index on next_due_date';
END

PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'Preventive Schedule table setup complete!';
PRINT '═══════════════════════════════════════════════════════════════';
