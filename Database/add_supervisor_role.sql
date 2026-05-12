-- =============================================
-- Add Supervisor Role to MaintenX
-- =============================================
-- Description: Adds Supervisor role for operational oversight
-- Date: 2026-05-12
-- =============================================

USE [db50508];
GO

SET QUOTED_IDENTIFIER ON;
GO

PRINT '========================================';
PRINT 'Adding Supervisor Role';
PRINT '========================================';

-- Check if Supervisor role already exists
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Supervisor')
BEGIN
    DECLARE @SupervisorRoleId NVARCHAR(450) = CAST(NEWID() AS NVARCHAR(450));
    
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (
        @SupervisorRoleId,
        'Supervisor',
        'SUPERVISOR',
        CAST(NEWID() AS NVARCHAR(450))
    );
    
    PRINT '✓ Supervisor role created successfully';
    PRINT '  Role ID: ' + @SupervisorRoleId;
END
ELSE
BEGIN
    PRINT '⚠ Supervisor role already exists';
END

PRINT '';
PRINT '========================================';
PRINT 'Supervisor Role Setup Complete';
PRINT '========================================';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Assign Supervisor role to users via User Management';
PRINT '2. Supervisors can access: /supervisor/dashboard';
PRINT '3. Supervisors can approve staged parts usage';
PRINT '4. Supervisors can monitor technician workload';
PRINT '';
PRINT 'SUPERVISOR CAPABILITIES:';
PRINT '- Work Order Oversight';
PRINT '- Parts Approval Workflow';
PRINT '- Technician Monitoring';
PRINT '- Inventory Movement Visibility';
PRINT '- Cost Tracking (Read-Only)';
PRINT '- Maintenance Logs (Read-Only)';
PRINT '- PM Monitoring (Read-Only)';
PRINT '';
PRINT 'SUPERVISOR RESTRICTIONS:';
PRINT '- Cannot manage users/roles';
PRINT '- Cannot manage company settings';
PRINT '- Cannot manage subscription';
PRINT '- Cannot access SuperAdmin features';
PRINT '';

GO
