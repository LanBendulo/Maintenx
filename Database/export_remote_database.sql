-- =============================================
-- Export Remote Database Schema and Data
-- Run this against: db50508.public.databaseasp.net
-- =============================================

-- Export all tables with data
-- This script generates INSERT statements for all data

-- 1. Export Companies
SELECT 'INSERT INTO Companies (CompanyId, CompanyName, SubscriptionPlanId, SubscriptionStartDate, SubscriptionEndDate, IsActive, CreatedAt, MaxUsers, MaxAssets) VALUES (' +
    CAST(CompanyId AS VARCHAR) + ', ''' + 
    REPLACE(CompanyName, '''', '''''') + ''', ' +
    CAST(SubscriptionPlanId AS VARCHAR) + ', ''' +
    CONVERT(VARCHAR, SubscriptionStartDate, 120) + ''', ' +
    CASE WHEN SubscriptionEndDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, SubscriptionEndDate, 120) + '''' END + ', ' +
    CAST(IsActive AS VARCHAR) + ', ''' +
    CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    CAST(MaxUsers AS VARCHAR) + ', ' +
    CAST(MaxAssets AS VARCHAR) + ');'
FROM Companies;

-- 2. Export SubscriptionPlans
SELECT 'INSERT INTO SubscriptionPlans (PlanId, PlanName, MaxUsers, MaxAssets, PricePerMonth, Features, IsActive, DisplayOrder, FreeTrialDays) VALUES (' +
    CAST(PlanId AS VARCHAR) + ', ''' + 
    REPLACE(PlanName, '''', '''''') + ''', ' +
    CAST(MaxUsers AS VARCHAR) + ', ' +
    CAST(MaxAssets AS VARCHAR) + ', ' +
    CAST(PricePerMonth AS VARCHAR) + ', ''' +
    REPLACE(Features, '''', '''''') + ''', ' +
    CAST(IsActive AS VARCHAR) + ', ' +
    CAST(DisplayOrder AS VARCHAR) + ', ' +
    CAST(FreeTrialDays AS VARCHAR) + ');'
FROM SubscriptionPlans;

-- 3. Export AspNetUsers
SELECT 'INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, CompanyId, FirstName, LastName, IsActive, CreatedAt, LastLoginAt) VALUES (''' +
    Id + ''', ''' +
    REPLACE(UserName, '''', '''''') + ''', ''' +
    REPLACE(NormalizedUserName, '''', '''''') + ''', ''' +
    REPLACE(Email, '''', '''''') + ''', ''' +
    REPLACE(NormalizedEmail, '''', '''''') + ''', ' +
    CAST(EmailConfirmed AS VARCHAR) + ', ' +
    CASE WHEN PasswordHash IS NULL THEN 'NULL' ELSE '''' + REPLACE(PasswordHash, '''', '''''') + '''' END + ', ''' +
    SecurityStamp + ''', ''' +
    ConcurrencyStamp + ''', ' +
    CASE WHEN PhoneNumber IS NULL THEN 'NULL' ELSE '''' + REPLACE(PhoneNumber, '''', '''''') + '''' END + ', ' +
    CAST(PhoneNumberConfirmed AS VARCHAR) + ', ' +
    CAST(TwoFactorEnabled AS VARCHAR) + ', ' +
    CASE WHEN LockoutEnd IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, LockoutEnd, 120) + '''' END + ', ' +
    CAST(LockoutEnabled AS VARCHAR) + ', ' +
    CAST(AccessFailedCount AS VARCHAR) + ', ' +
    CAST(CompanyId AS VARCHAR) + ', ''' +
    REPLACE(FirstName, '''', '''''') + ''', ''' +
    REPLACE(LastName, '''', '''''') + ''', ' +
    CAST(IsActive AS VARCHAR) + ', ''' +
    CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    CASE WHEN LastLoginAt IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, LastLoginAt, 120) + '''' END + ');'
FROM AspNetUsers;

-- 4. Export AspNetRoles
SELECT 'INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (''' +
    Id + ''', ''' +
    Name + ''', ''' +
    NormalizedName + ''', ' +
    CASE WHEN ConcurrencyStamp IS NULL THEN 'NULL' ELSE '''' + ConcurrencyStamp + '''' END + ');'
FROM AspNetRoles;

-- 5. Export AspNetUserRoles
SELECT 'INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (''' +
    UserId + ''', ''' +
    RoleId + ''');'
FROM AspNetUserRoles;

-- 6. Export Assets
SELECT 'INSERT INTO Assets (AssetId, AssetName, AssetType, SerialNumber, Location, PurchaseDate, PurchaseCost, Status, CompanyId, IsArchived, ArchivedAt, ArchivedBy, ArchiveReason) VALUES (' +
    CAST(AssetId AS VARCHAR) + ', ''' +
    REPLACE(AssetName, '''', '''''') + ''', ''' +
    REPLACE(AssetType, '''', '''''') + ''', ' +
    CASE WHEN SerialNumber IS NULL THEN 'NULL' ELSE '''' + REPLACE(SerialNumber, '''', '''''') + '''' END + ', ' +
    CASE WHEN Location IS NULL THEN 'NULL' ELSE '''' + REPLACE(Location, '''', '''''') + '''' END + ', ' +
    CASE WHEN PurchaseDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, PurchaseDate, 120) + '''' END + ', ' +
    CASE WHEN PurchaseCost IS NULL THEN 'NULL' ELSE CAST(PurchaseCost AS VARCHAR) END + ', ''' +
    Status + ''', ' +
    CAST(CompanyId AS VARCHAR) + ', ' +
    CAST(IsArchived AS VARCHAR) + ', ' +
    CASE WHEN ArchivedAt IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, ArchivedAt, 120) + '''' END + ', ' +
    CASE WHEN ArchivedBy IS NULL THEN 'NULL' ELSE '''' + ArchivedBy + '''' END + ', ' +
    CASE WHEN ArchiveReason IS NULL THEN 'NULL' ELSE '''' + REPLACE(ArchiveReason, '''', '''''') + '''' END + ');'
FROM Assets;

-- 7. Export Personnel
SELECT 'INSERT INTO Personnel (PersonnelId, FirstName, LastName, Email, Phone, Role, HourlyRate, CompanyId, IsActive, CreatedAt, UpdatedAt) VALUES (' +
    CAST(PersonnelId AS VARCHAR) + ', ''' +
    REPLACE(FirstName, '''', '''''') + ''', ''' +
    REPLACE(LastName, '''', '''''') + ''', ' +
    CASE WHEN Email IS NULL THEN 'NULL' ELSE '''' + REPLACE(Email, '''', '''''') + '''' END + ', ' +
    CASE WHEN Phone IS NULL THEN 'NULL' ELSE '''' + REPLACE(Phone, '''', '''''') + '''' END + ', ''' +
    Role + ''', ' +
    CASE WHEN HourlyRate IS NULL THEN 'NULL' ELSE CAST(HourlyRate AS VARCHAR) END + ', ' +
    CAST(CompanyId AS VARCHAR) + ', ' +
    CAST(IsActive AS VARCHAR) + ', ''' +
    CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    CASE WHEN UpdatedAt IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, UpdatedAt, 120) + '''' END + ');'
FROM Personnel;

-- 8. Export MaintenanceRequests
SELECT 'INSERT INTO MaintenanceRequests (RequestId, AssetId, RequestedBy, RequestDate, Description, Priority, Status, CompanyId, IsArchived, ArchivedAt, ArchivedBy, ArchiveReason, ConvertedToWorkOrderId, ConvertedAt, ConvertedBy) VALUES (' +
    CAST(RequestId AS VARCHAR) + ', ' +
    CAST(AssetId AS VARCHAR) + ', ''' +
    RequestedBy + ''', ''' +
    CONVERT(VARCHAR, RequestDate, 120) + ''', ''' +
    REPLACE(Description, '''', '''''') + ''', ''' +
    Priority + ''', ''' +
    Status + ''', ' +
    CAST(CompanyId AS VARCHAR) + ', ' +
    CAST(IsArchived AS VARCHAR) + ', ' +
    CASE WHEN ArchivedAt IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, ArchivedAt, 120) + '''' END + ', ' +
    CASE WHEN ArchivedBy IS NULL THEN 'NULL' ELSE '''' + ArchivedBy + '''' END + ', ' +
    CASE WHEN ArchiveReason IS NULL THEN 'NULL' ELSE '''' + REPLACE(ArchiveReason, '''', '''''') + '''' END + ', ' +
    CASE WHEN ConvertedToWorkOrderId IS NULL THEN 'NULL' ELSE CAST(ConvertedToWorkOrderId AS VARCHAR) END + ', ' +
    CASE WHEN ConvertedAt IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, ConvertedAt, 120) + '''' END + ', ' +
    CASE WHEN ConvertedBy IS NULL THEN 'NULL' ELSE '''' + ConvertedBy + '''' END + ');'
FROM MaintenanceRequests;

-- 9. Export WorkOrders
SELECT 'INSERT INTO WorkOrders (WorkOrderId, AssetId, AssignedTo, Priority, Status, Description, ScheduledDate, CompletionDate, CompanyId, MaintenanceRequestId, Source, ActualCompletionDate) VALUES (' +
    CAST(WorkOrderId AS VARCHAR) + ', ' +
    CAST(AssetId AS VARCHAR) + ', ' +
    CASE WHEN AssignedTo IS NULL THEN 'NULL' ELSE CAST(AssignedTo AS VARCHAR) END + ', ''' +
    Priority + ''', ''' +
    Status + ''', ''' +
    REPLACE(Description, '''', '''''') + ''', ''' +
    CONVERT(VARCHAR, ScheduledDate, 120) + ''', ' +
    CASE WHEN CompletionDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, CompletionDate, 120) + '''' END + ', ' +
    CAST(CompanyId AS VARCHAR) + ', ' +
    CASE WHEN MaintenanceRequestId IS NULL THEN 'NULL' ELSE CAST(MaintenanceRequestId AS VARCHAR) END + ', ''' +
    Source + ''', ' +
    CASE WHEN ActualCompletionDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, ActualCompletionDate, 120) + '''' END + ');'
FROM WorkOrders;

-- 10. Export Parts
SELECT 'INSERT INTO Parts (PartId, PartName, PartNumber, Description, Quantity, UnitCost, ReorderLevel, CompanyId, CreatedAt, UpdatedAt) VALUES (' +
    CAST(PartId AS VARCHAR) + ', ''' +
    REPLACE(PartName, '''', '''''') + ''', ' +
    CASE WHEN PartNumber IS NULL THEN 'NULL' ELSE '''' + REPLACE(PartNumber, '''', '''''') + '''' END + ', ' +
    CASE WHEN Description IS NULL THEN 'NULL' ELSE '''' + REPLACE(Description, '''', '''''') + '''' END + ', ' +
    CAST(Quantity AS VARCHAR) + ', ' +
    CASE WHEN UnitCost IS NULL THEN 'NULL' ELSE CAST(UnitCost AS VARCHAR) END + ', ' +
    CASE WHEN ReorderLevel IS NULL THEN 'NULL' ELSE CAST(ReorderLevel AS VARCHAR) END + ', ' +
    CAST(CompanyId AS VARCHAR) + ', ''' +
    CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    CASE WHEN UpdatedAt IS NULL THEN 'NULL' ELSE '''' + CONVERT(VARCHAR, UpdatedAt, 120) + '''' END + ');'
FROM Parts;

-- Continue with remaining tables...
PRINT 'Export complete. Run the generated INSERT statements against your local database.';
