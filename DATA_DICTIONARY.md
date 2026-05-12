# MaintenX Database - Data Dictionary

**Database Name:** db50508 (MaintenX)  
**Database Type:** Microsoft SQL Server  
**Generated:** May 13, 2026  
**Normalization:** 3rd Normal Form (3NF)  
**Architecture:** Multi-Tenant SaaS with Company-level isolation

---

## ⚠️ ACTUAL DATABASE TABLES (29 Tables)

**Identity & Authentication (8 tables):**
- AspNetUsers, AspNetRoles, AspNetUserClaims, AspNetUserRoles, AspNetRoleClaims, AspNetUserLogins, AspNetUserTokens, __EFMigrationsHistory

**Core Business Tables (21 tables):**
- Company, SubscriptionPlan, CompanySubscription
- Personnel, Asset, AssetStatusHistory, Category
- Maintenance_Request, Work_Order, MaintenanceLog, Maintenance_Log (legacy)
- PreventiveSchedule, Maintenance_Schedule (legacy)
- Part, Spare_Part (legacy), WorkOrderPart, WorkOrder_Parts (legacy)
- WorkOrderCost, Maintenance_Cost (legacy)
- InventoryMovement, Inventory_Transaction (legacy)

**Note:** Some tables have both modern (PascalCase) and legacy (snake_case) versions due to migration history.

---

## Table of Contents
1. [AspNetUsers (Identity)](#aspnetusers-table)
2. [Company](#company-table)
3. [SubscriptionPlan](#subscriptionplan-table)
4. [CompanySubscription](#companysubscription-table)
5. [Personnel](#personnel-table)
6. [Category](#category-table)
7. [Asset](#asset-table)
8. [AssetStatusHistory](#assetstatushistory-table)
9. [Maintenance_Request](#maintenance_request-table)
10. [Work_Order](#work_order-table)
11. [MaintenanceLog](#maintenancelog-table)
12. [PreventiveSchedule](#preventiveschedule-table)
13. [Part](#part-table)
14. [WorkOrderPart](#workorderpart-table)
15. [WorkOrderCost](#workordercost-table)
16. [InventoryMovement](#inventorymovement-table)
17. [Legacy Tables](#legacy-tables)

---

## AspNetUsers table

**Description:** ASP.NET Core Identity table for user authentication and authorization. Extended with multi-tenant support.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| Id-PK | NVARCHAR | 450 | User's unique identifier (GUID) |
| UserName | NVARCHAR | 256 | User's login username or email |
| NormalizedUserName | NVARCHAR | 256 | Uppercase username for lookups |
| Email | NVARCHAR | 256 | User's email address |
| NormalizedEmail | NVARCHAR | 256 | Uppercase email for lookups |
| EmailConfirmed | BIT | 1 | Email verification status |
| PasswordHash | NVARCHAR | MAX | Hashed password (PBKDF2) |
| SecurityStamp | NVARCHAR | MAX | Security token for password changes |
| ConcurrencyStamp | NVARCHAR | MAX | Concurrency control token |
| PhoneNumber | NVARCHAR | 50 | User's phone number |
| PhoneNumberConfirmed | BIT | 1 | Phone verification status |
| TwoFactorEnabled | BIT | 1 | 2FA enabled flag |
| LockoutEnd | DATETIMEOFFSET | - | Account lockout expiration |
| LockoutEnabled | BIT | 1 | Lockout feature enabled |
| AccessFailedCount | INT | 4 | Failed login attempts counter |
| CompanyId-FK | INT | 4 | Company (tenant) ID - NULL for SuperAdmin |
| FullName | NVARCHAR | 200 | User's full name |
| IsActive | BIT | 1 | Account active status |
| LastLoginAt | DATETIME | 8 | Last successful login timestamp |
| CreatedAt | DATETIME | 8 | Account creation timestamp |
| UpdatedAt | DATETIME | 8 | Last account update timestamp |

**Foreign Keys:**
- CompanyId → Company(company_id)

**Indexes:**
- PK_AspNetUsers (Id)
- IX_AspNetUsers_CompanyId
- IX_AspNetUsers_NormalizedUserName (UNIQUE)
- IX_AspNetUsers_NormalizedEmail

---

## Company table

**Description:** Represents a company (tenant) in the multi-tenant SaaS architecture. All business data is isolated by CompanyId.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| company_id-PK | INT-AI | 4 | Company's unique identifier |
| company_name | NVARCHAR | 200 | Company's legal or business name |
| subscription_plan | NVARCHAR | 50 | Current subscription plan name |
| subscription_expiry | DATETIME | 8 | Subscription expiration date |
| is_active | BIT | 1 | Company active status |
| created_at | DATETIME | 8 | Company registration timestamp |
| updated_at | DATETIME | 8 | Last company update timestamp |
| contact_email | NVARCHAR | 255 | Primary contact email |
| contact_phone | NVARCHAR | 50 | Primary contact phone |
| address | NVARCHAR | 500 | Company physical address |
| billing_email | NVARCHAR | 255 | Billing contact email |
| max_users | INT | 4 | Maximum allowed users |
| max_assets | INT | 4 | Maximum allowed assets |

**Indexes:**
- PK_Company (company_id)

---

## SubscriptionPlan table

**Description:** Platform-level subscription plans managed by SuperAdmin. Defines pricing and resource limits.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| plan_id-PK | INT-AI | 4 | Plan's unique identifier |
| name | NVARCHAR | 100 | Plan name (Starter, Professional, Enterprise) |
| description | NVARCHAR | 500 | Plan description and features summary |
| monthly_price | DECIMAL(10,2) | 8 | Monthly subscription price |
| yearly_price | DECIMAL(10,2) | 8 | Yearly subscription price |
| max_users | INT | 4 | Maximum users allowed (NULL = unlimited) |
| max_assets | INT | 4 | Maximum assets allowed (NULL = unlimited) |
| max_work_orders_per_month | INT | 4 | Maximum work orders per month (NULL = unlimited) |
| features_json | NVARCHAR | MAX | JSON array of plan features |
| is_active | BIT | 1 | Plan availability status |
| created_at | DATETIME | 8 | Plan creation timestamp |
| updated_at | DATETIME | 8 | Last plan update timestamp |

**Indexes:**
- PK_SubscriptionPlan (plan_id)

---

## CompanySubscription table

**Description:** Links companies to subscription plans with billing and trial information.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| subscription_id-PK | INT-AI | 4 | Subscription's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| plan_id-FK | INT | 4 | Subscription plan identifier |
| start_date | DATETIME | 8 | Subscription start date |
| end_date | DATETIME | 8 | Subscription end date |
| is_trial | BIT | 1 | Trial subscription flag |
| is_active | BIT | 1 | Subscription active status |
| payment_status | NVARCHAR | 50 | Payment status (Pending, Paid, Failed, Cancelled) |
| external_payment_id | NVARCHAR | 200 | External payment gateway transaction ID |
| last_payment_date | DATETIME | 8 | Last successful payment date |
| created_at | DATETIME | 8 | Subscription creation timestamp |
| updated_at | DATETIME | 8 | Last subscription update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- plan_id → SubscriptionPlan(plan_id)

**Indexes:**
- PK_CompanySubscription (subscription_id)
- IX_CompanySubscription_CompanyId
- IX_CompanySubscription_PlanId

---

## Personnel table

**Description:** Workforce personnel (technicians, contractors, supervisors). Can exist with or without a user account.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| personnel_id-PK | INT-AI | 4 | Personnel's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| user_id-FK | NVARCHAR | 450 | Optional link to AspNetUsers |
| employee_id | NVARCHAR | 50 | Company employee ID number |
| first_name | NVARCHAR | 100 | Personnel's first name |
| middle_name | NVARCHAR | 100 | Personnel's middle name |
| last_name | NVARCHAR | 100 | Personnel's last name |
| email | NVARCHAR | 256 | Personnel's email address |
| phone_number | NVARCHAR | 50 | Personnel's phone number |
| address | NVARCHAR | 500 | Personnel's physical address |
| position | NVARCHAR | 100 | Job position/title |
| department | NVARCHAR | 100 | Department name |
| employment_type | NVARCHAR | 50 | Employment type (FullTime, PartTime, Contractor, Intern, Temporary) |
| hire_date | DATETIME | 8 | Employment start date |
| termination_date | DATETIME | 8 | Employment end date |
| status | NVARCHAR | 50 | Personnel status (Active, Inactive, OnLeave, Retired, Terminated) |
| notes | NVARCHAR | MAX | Additional notes |
| role | NVARCHAR | 50 | Job role (Technician, Supervisor, etc.) |
| skill_set | NVARCHAR | 255 | Skills and certifications |
| hourly_rate | DECIMAL(10,2) | 8 | Hourly labor rate |
| emergency_contact_name | NVARCHAR | 200 | Emergency contact full name |
| emergency_contact_phone | NVARCHAR | 50 | Emergency contact phone |
| profile_photo_url | NVARCHAR | 500 | Profile photo URL |
| is_active | BIT | 1 | Active status flag |
| created_at | DATETIME | 8 | Record creation timestamp |
| updated_at | DATETIME | 8 | Last record update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- user_id → AspNetUsers(Id)

**Indexes:**
- PK_Personnel (personnel_id)
- IX_Personnel_CompanyId
- IX_Personnel_UserId
- IX_Personnel_IsActive

---

## Category table

**Description:** Asset categories (HVAC, Electrical, Plumbing, etc.) for classification.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| category_id-PK | INT-AI | 4 | Category's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| category_name | NVARCHAR | 100 | Category name |

**Foreign Keys:**
- company_id → Company(company_id)

**Indexes:**
- PK_Category (category_id)
- IX_Category_CompanyId

---

## Asset table

**Description:** Equipment and assets requiring maintenance tracking.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| asset_id-PK | INT-AI | 4 | Asset's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| asset_name | NVARCHAR | 100 | Asset name or description |
| asset_code | NVARCHAR | 50 | Asset code or serial number |
| category_id-FK | INT | 4 | Asset category identifier |
| location | NVARCHAR | 150 | Physical location of asset |
| description | NVARCHAR | MAX | Detailed asset description |
| status | NVARCHAR | 30 | Operational status (Operational, Down, UnderMaintenance, Retired) |
| purchase_date | DATE | 3 | Asset purchase date |
| created_at | DATETIME | 8 | Record creation timestamp |
| updated_at | DATETIME | 8 | Last record update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- category_id → Category(category_id)

**Indexes:**
- PK_Asset (asset_id)
- IX_Asset_CompanyId
- IX_Asset_CategoryId

---

## AssetStatusHistory table

**Description:** Audit trail for asset operational status changes.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| history_id-PK | INT-AI | 4 | History record's unique identifier |
| asset_id-FK | INT | 4 | Asset identifier |
| company_id-FK | INT | 4 | Company identifier |
| old_status | NVARCHAR | 30 | Previous status value |
| new_status | NVARCHAR | 30 | New status value |
| changed_by_user_id-FK | NVARCHAR | 450 | User who made the change |
| work_order_id-FK | INT | 4 | Related work order (if applicable) |
| reason | NVARCHAR | 500 | Reason for status change |
| changed_at | DATETIME | 8 | Status change timestamp |

**Foreign Keys:**
- asset_id → Asset(asset_id)
- company_id → Company(company_id)
- work_order_id → Work_Order(work_order_id)
- changed_by_user_id → AspNetUsers(Id)

**Indexes:**
- PK_AssetStatusHistory (history_id)
- IX_AssetStatusHistory_AssetId
- IX_AssetStatusHistory_CompanyId

---

## Maintenance_Request table

**Description:** Maintenance requests submitted by users - entry point of CMMS workflow.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| request_id-PK | INT-AI | 4 | Request's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| request_number | NVARCHAR | 50 | Human-readable request number |
| title | NVARCHAR | 100 | Request title/summary |
| description | NVARCHAR | MAX | Detailed request description |
| asset_id-FK | INT | 4 | Asset requiring maintenance |
| priority | NVARCHAR | 20 | Priority level (Low, Medium, High) |
| status | NVARCHAR | 30 | Request status (Pending, Approved, Rejected, Converted) |
| requested_by-FK | INT | 4 | Personnel who submitted request |
| category | NVARCHAR | 50 | Request category |
| location | NVARCHAR | 200 | Asset location |
| attachment_url | NVARCHAR | 500 | Attachment file URL |
| created_at | DATETIME | 8 | Request creation timestamp |
| updated_at | DATETIME | 8 | Last request update timestamp |
| is_archived | BIT | 1 | Archive status flag |
| archived_at | DATETIME | 8 | Archive timestamp |
| archived_by_user_id-FK | NVARCHAR | 450 | User who archived request |
| converted_work_order_id-FK | INT | 4 | Work order created from request |
| converted_at | DATETIME | 8 | Conversion timestamp |
| converted_by_user_id-FK | NVARCHAR | 450 | User who converted request |
| closed_at | DATETIME | 8 | Request closure timestamp |
| closed_by_user_id-FK | NVARCHAR | 450 | User who closed request |

**Foreign Keys:**
- company_id → Company(company_id)
- asset_id → Asset(asset_id)
- requested_by → Personnel(personnel_id)
- archived_by_user_id → AspNetUsers(Id)
- converted_by_user_id → AspNetUsers(Id)
- closed_by_user_id → AspNetUsers(Id)

**Indexes:**
- PK_Maintenance_Request (request_id)
- IX_MaintenanceRequest_CompanyId
- IX_MaintenanceRequest_AssetId
- IX_MaintenanceRequest_Status
- UQ_MaintenanceRequest_RequestNumber (UNIQUE)

---

## Work_Order table

**Description:** Maintenance work orders - core entity for maintenance execution.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| work_order_id-PK | INT-AI | 4 | Work order's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| maintenance_request_id-FK | INT | 4 | Source maintenance request (if applicable) |
| preventive_schedule_id-FK | INT | 4 | Source preventive schedule (if applicable) |
| source | NVARCHAR | 50 | Work order source (Manual, Request, Preventive) |
| asset_id-FK | INT | 4 | Asset being maintained |
| assigned_to-FK | INT | 4 | Technician assigned to work order |
| created_by-FK | INT | 4 | Personnel who created work order |
| status | NVARCHAR | 30 | Work order status (Open, InProgress, Completed, Cancelled) |
| priority | NVARCHAR | 20 | Priority level (Low, Medium, High) |
| description | NVARCHAR | MAX | Work order description |
| date_created | DATE | 3 | Work order creation date |
| due_date | DATE | 3 | Work order due date |
| actual_completion | DATE | 3 | Actual completion date |
| is_archived | BIT | 1 | Archive status flag |
| archived_at | DATETIME | 8 | Archive timestamp |
| archived_by_user_id-FK | NVARCHAR | 450 | User who archived work order |
| archive_reason | NVARCHAR | 500 | Reason for archiving |

**Foreign Keys:**
- company_id → Company(company_id)
- asset_id → Asset(asset_id)
- assigned_to → Personnel(personnel_id)
- created_by → Personnel(personnel_id)
- maintenance_request_id → Maintenance_Request(request_id)
- preventive_schedule_id → PreventiveSchedule(schedule_id)
- archived_by_user_id → AspNetUsers(Id)

**Indexes:**
- PK_Work_Order (work_order_id)
- IX_WorkOrder_CompanyId
- IX_WorkOrder_AssetId
- IX_WorkOrder_AssignedTo
- IX_WorkOrder_Status
- IX_WorkOrder_MaintenanceRequestId
- IX_WorkOrder_PreventiveScheduleId

---

## MaintenanceLog table

**Description:** Immutable completion records for work orders - maintenance history audit trail.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| log_id-PK | INT-AI | 4 | Log's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| work_order_id-FK | INT | 4 | Related work order |
| asset_id-FK | INT | 4 | Asset maintained |
| title | NVARCHAR | 200 | Log entry title |
| description | NVARCHAR | MAX | Maintenance work performed |
| completed_by_personnel_id-FK | INT | 4 | Technician who completed work |
| completed_date | DATETIME | 8 | Work completion date |
| notes | NVARCHAR | MAX | Additional notes |
| labor_cost | DECIMAL(10,2) | 8 | Labor cost snapshot |
| parts_cost | DECIMAL(10,2) | 8 | Parts cost snapshot |
| other_cost | DECIMAL(10,2) | 8 | Other costs snapshot |
| total_cost | DECIMAL(10,2) | 8 | Total cost snapshot |
| created_at | DATETIME | 8 | Log creation timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- work_order_id → Work_Order(work_order_id)
- asset_id → Asset(asset_id)
- completed_by_personnel_id → Personnel(personnel_id)

**Indexes:**
- PK_MaintenanceLog (log_id)
- IX_MaintenanceLog_CompanyId
- IX_MaintenanceLog_WorkOrderId
- IX_MaintenanceLog_AssetId

---

## PreventiveSchedule table

**Description:** Preventive maintenance schedules for automatic work order generation.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| schedule_id-PK | INT-AI | 4 | Schedule's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| asset_id-FK | INT | 4 | Asset to be maintained |
| title | NVARCHAR | 200 | Schedule title |
| description | NVARCHAR | MAX | Schedule description |
| frequency_days | INT | 4 | Maintenance frequency in days |
| next_due_date | DATE | 3 | Next scheduled maintenance date |
| last_completed_date | DATE | 3 | Last completion date |
| is_active | BIT | 1 | Schedule active status |
| default_technician_id-FK | INT | 4 | Default assigned technician |
| priority | NVARCHAR | 20 | Default priority (Low, Medium, High) |
| last_generated_date | DATE | 3 | Last work order generation date |
| last_generated_work_order_id-FK | INT | 4 | Last generated work order ID |
| last_generation_attempt | DATETIME | 8 | Last generation attempt timestamp |
| last_generation_error | NVARCHAR | 500 | Last generation error message |
| created_at | DATETIME | 8 | Schedule creation timestamp |
| updated_at | DATETIME | 8 | Last schedule update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- asset_id → Asset(asset_id)
- default_technician_id → Personnel(personnel_id)

**Indexes:**
- PK_PreventiveSchedule (schedule_id)
- IX_PreventiveSchedule_CompanyId
- IX_PreventiveSchedule_AssetId
- IX_PreventiveSchedule_NextDueDate

---

## Part table

**Description:** Spare parts and inventory items for maintenance operations.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| part_id-PK | INT-AI | 4 | Part's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| part_name | NVARCHAR | 200 | Part name or description |
| part_number | NVARCHAR | 100 | Part number or SKU |
| description | NVARCHAR | MAX | Detailed part description |
| quantity | INT | 4 | Current stock quantity |
| unit_cost | DECIMAL(10,2) | 8 | Cost per unit |
| reorder_level | INT | 4 | Minimum stock level for reorder alert |
| location | NVARCHAR | 200 | Storage location |
| is_active | BIT | 1 | Part active status |
| created_at | DATETIME | 8 | Record creation timestamp |
| updated_at | DATETIME | 8 | Last record update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)

**Indexes:**
- PK_Part (part_id)
- IX_Part_CompanyId
- IX_Part_CompanyId_PartNumber (UNIQUE)

---

## WorkOrderPart table

**Description:** Parts used in work orders - junction table with lifecycle governance.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| id-PK | INT-AI | 4 | Record's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| work_order_id-FK | INT | 4 | Work order identifier |
| part_id-FK | INT | 4 | Part identifier |
| quantity_used | INT | 4 | Quantity consumed |
| unit_cost | DECIMAL(10,2) | 8 | Unit cost snapshot |
| total_cost | DECIMAL(10,2) | 8 | Total cost (quantity × unit_cost) |
| usage_status | NVARCHAR | 50 | Usage status (Pending, Approved, Consumed, Rejected) |
| added_by_personnel_id-FK | INT | 4 | Technician who added part usage |
| approved_by_user_id-FK | NVARCHAR | 450 | User who approved consumption |
| consumed_at | DATETIME | 8 | Inventory deduction timestamp |
| created_at | DATETIME | 8 | Record creation timestamp |
| updated_at | DATETIME | 8 | Last record update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- work_order_id → Work_Order(work_order_id)
- part_id → Part(part_id)
- added_by_personnel_id → Personnel(personnel_id)
- approved_by_user_id → AspNetUsers(Id)

**Indexes:**
- PK_WorkOrderPart (id)
- IX_WorkOrderPart_CompanyId_WorkOrderId
- IX_WorkOrderPart_CompanyId_PartId

---

## WorkOrderCost table

**Description:** Cost tracking for work orders - labor, parts, and other costs.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| cost_id-PK | INT-AI | 4 | Cost record's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| work_order_id-FK | INT | 4 | Work order identifier |
| labor_cost | DECIMAL(10,2) | 8 | Labor cost amount |
| parts_cost | DECIMAL(10,2) | 8 | Parts cost amount |
| other_cost | DECIMAL(10,2) | 8 | Other costs amount |
| total_cost | DECIMAL(10,2) | 8 | Total cost (sum of all costs) |
| notes | NVARCHAR | MAX | Cost notes or breakdown |
| created_at | DATETIME | 8 | Record creation timestamp |
| updated_at | DATETIME | 8 | Last record update timestamp |

**Foreign Keys:**
- company_id → Company(company_id)
- work_order_id → Work_Order(work_order_id)

**Indexes:**
- PK_WorkOrderCost (cost_id)
- IX_WorkOrderCost_CompanyId_WorkOrderId

---

## InventoryMovement table

**Description:** Immutable audit log for all inventory stock changes - complete traceability.

| Field Names | Datatype | Length | Description |
|------------|----------|--------|-------------|
| movement_id-PK | INT-AI | 4 | Movement's unique identifier |
| company_id-FK | INT | 4 | Company identifier |
| part_id-FK | INT | 4 | Part identifier |
| quantity_changed | INT | 4 | Quantity change (+ or -) |
| previous_quantity | INT | 4 | Stock level before movement |
| new_quantity | INT | 4 | Stock level after movement |
| movement_type | NVARCHAR | 50 | Movement type (Consumption, Adjustment, Restock, Return, Correction, InitialStock, Transfer) |
| work_order_id-FK | INT | 4 | Related work order (if applicable) |
| work_order_part_id-FK | INT | 4 | Related WorkOrderPart record |
| performed_by_user_id-FK | NVARCHAR | 450 | User who performed movement |
| unit_cost_snapshot | DECIMAL(10,2) | 8 | Unit cost at time of movement |
| total_cost | DECIMAL(10,2) | 8 | Total cost of movement |
| notes | NVARCHAR | MAX | Movement notes or reason |
| created_at | DATETIME | 8 | Movement timestamp (immutable) |

**Foreign Keys:**
- company_id → Company(company_id)
- part_id → Part(part_id)
- work_order_id → Work_Order(work_order_id)
- work_order_part_id → WorkOrderPart(id)
- performed_by_user_id → AspNetUsers(Id)

**Indexes:**
- PK_InventoryMovement (movement_id)
- IX_InventoryMovement_CompanyId_PartId
- IX_InventoryMovement_WorkOrderId
- IX_InventoryMovement_MovementType
- IX_InventoryMovement_CreatedAt
- IX_InventoryMovement_PerformedBy

---

## Legacy Tables

**Note:** The following tables exist in the database but may be deprecated or replaced by newer versions:

### Maintenance_Log (Legacy)
- **Status:** Potentially replaced by `MaintenanceLog` (PascalCase version)
- **Description:** Original maintenance log table from initial schema
- **Recommendation:** Verify which version is actively used in application

### Maintenance_Schedule (Legacy)
- **Status:** Replaced by `PreventiveSchedule`
- **Description:** Original preventive maintenance schedule table
- **Recommendation:** Data may need migration to PreventiveSchedule

### Maintenance_Cost (Legacy)
- **Status:** Replaced by `WorkOrderCost`
- **Description:** Original cost tracking table
- **Recommendation:** Data may need migration to WorkOrderCost

### Spare_Part (Legacy)
- **Status:** Replaced by `Part`
- **Description:** Original parts inventory table
- **Recommendation:** Data may need migration to Part table

### WorkOrder_Parts (Legacy)
- **Status:** Replaced by `WorkOrderPart`
- **Description:** Original work order parts junction table
- **Recommendation:** Data may need migration to WorkOrderPart

### Inventory_Transaction (Legacy)
- **Status:** Replaced by `InventoryMovement`
- **Description:** Original inventory transaction log
- **Recommendation:** Data may need migration to InventoryMovement

### __EFMigrationsHistory
- **Status:** Active - Entity Framework Core migration tracking
- **Description:** Tracks applied EF Core migrations
- **Do Not Modify:** System table managed by Entity Framework

---

## Database Relationships Summary

### Multi-Tenant Architecture
- All business tables contain `company_id` for tenant isolation
- SuperAdmin users have `CompanyId = NULL` in AspNetUsers
- All queries must filter by `CompanyId` except SuperAdmin operations

### Core Relationships
1. **Company** → Users, Assets, WorkOrders, Personnel, MaintenanceRequests
2. **Asset** → WorkOrders, MaintenanceRequests, PreventiveSchedules
3. **Personnel** → WorkOrders (assigned_to, created_by), MaintenanceRequests
4. **MaintenanceRequest** → WorkOrder (one-to-one conversion)
5. **PreventiveSchedule** → WorkOrder (one-to-many generation)
6. **WorkOrder** → MaintenanceLog, WorkOrderPart, WorkOrderCost
7. **Part** → WorkOrderPart, InventoryMovement
8. **WorkOrderPart** → InventoryMovement (consumption tracking)

### Audit Trail Tables
- **AssetStatusHistory**: Tracks asset status changes
- **InventoryMovement**: Tracks all inventory mutations
- **MaintenanceLog**: Immutable work completion records

---

## Data Types Legend

| Abbreviation | Full Type | Description |
|-------------|-----------|-------------|
| INT-AI | INT IDENTITY(1,1) | Auto-incrementing integer primary key |
| INT | INT | 32-bit integer |
| BIT | BIT | Boolean (0 or 1) |
| NVARCHAR | NVARCHAR(n) | Unicode variable-length string |
| VARCHAR | VARCHAR(n) | ASCII variable-length string |
| DECIMAL(10,2) | DECIMAL(10,2) | Decimal with 10 digits, 2 decimal places |
| DATETIME | DATETIME | Date and time |
| DATE | DATE | Date only |
| MAX | NVARCHAR(MAX) | Maximum length text field |

---

## Naming Conventions

- **Tables**: PascalCase or snake_case (mixed due to legacy)
- **Columns**: snake_case in database, PascalCase in C# models
- **Primary Keys**: `table_name_id` or `Id`
- **Foreign Keys**: `referenced_table_id` or `ReferencedTableId`
- **Indexes**: `IX_TableName_ColumnName`
- **Unique Constraints**: `UQ_TableName_ColumnName`
- **Foreign Key Constraints**: `FK_ChildTable_ParentTable`

---

## Security & Compliance

### Multi-Tenant Isolation
- All business data filtered by `CompanyId`
- Row-level security enforced at application layer
- SuperAdmin bypass for platform management

### Audit Trails
- All critical operations logged with user ID and timestamp
- Immutable audit tables (InventoryMovement, MaintenanceLog, AssetStatusHistory)
- Soft deletes with archive flags and timestamps

### Data Integrity
- Foreign key constraints enforce referential integrity
- Check constraints validate enum values
- Unique constraints prevent duplicates
- NOT NULL constraints ensure data completeness

---

**Document Version:** 1.1  
**Last Updated:** May 13, 2026  
**Database Verified:** Yes - Connected to actual db50508 database  
**Total Tables:** 29 (8 Identity + 21 Business)  
**Maintained By:** MaintenX Development Team

---

## Database Health Notes

### Active Tables (Currently Used)
✅ Company, SubscriptionPlan, CompanySubscription  
✅ Personnel, Asset, AssetStatusHistory, Category  
✅ Maintenance_Request, Work_Order  
✅ MaintenanceLog, PreventiveSchedule  
✅ Part, WorkOrderPart, WorkOrderCost, InventoryMovement  
✅ All AspNetUsers/Identity tables  

### Legacy Tables (May Need Cleanup)
⚠️ Maintenance_Log, Maintenance_Schedule, Maintenance_Cost  
⚠️ Spare_Part, WorkOrder_Parts, Inventory_Transaction  

**Recommendation:** Audit legacy tables to determine if they contain data that needs migration or if they can be safely dropped.
