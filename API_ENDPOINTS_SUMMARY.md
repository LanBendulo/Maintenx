# MAINTENX API ENDPOINTS SUMMARY

## 📊 TOTAL API ENDPOINTS: **108**

---

## 📁 BREAKDOWN BY CONTROLLER

| Controller | Endpoints | Purpose |
|-----------|-----------|---------|
| **DashboardController** | 18 | Admin dashboard, work orders, assets, maintenance requests |
| **MaintenanceRequestsController** | 11 | Maintenance request CRUD and workflow |
| **UserManagementController** | 10 | User account management, roles, permissions |
| **PreventiveMaintenanceController** | 9 | PM schedules, generation, management |
| **PersonnelController** | 9 | Personnel CRUD, linking, archiving |
| **AssetController** | 8 | Asset CRUD, status tracking, history |
| **SuperAdminSubscriptionsController** | 8 | Subscription plans, assignments, billing |
| **AccountController** | 7 | Forgot password, reset password, email verification |
| **SuperAdminCompaniesController** | 6 | Company management, suspension, deletion |
| **PartsController** | 6 | Parts inventory management |
| **TechnicianDashboardController** | 5 | Technician work orders, start/complete work |
| **MaintenanceLogsController** | 4 | Maintenance log tracking |
| **HomeController** | 3 | Home page, privacy, error handling |
| **UserDashboardController** | 3 | User maintenance requests view |
| **SuperAdminDashboardController** | 1 | SuperAdmin platform overview |

---

## 🔐 AUTHENTICATION & AUTHORIZATION

### AccountController (7 endpoints)
- `GET /Account/ForgotPassword` - Forgot password form
- `POST /Account/ForgotPassword` - Send password reset email
- `GET /Account/ForgotPasswordConfirmation` - Reset email sent confirmation
- `GET /Account/ResetPassword` - Reset password form
- `POST /Account/ResetPassword` - Reset password action
- `GET /Account/ResetPasswordConfirmation` - Password reset success
- `GET /Account/ConfirmEmail` - Email verification callback

---

## 👥 USER MANAGEMENT

### UserManagementController (10 endpoints)
- `GET /usermanagement` - List all users
- `GET /usermanagement/{id}` - User details
- `GET /usermanagement/create` - Create user form
- `POST /usermanagement/create` - Create user action
- `GET /usermanagement/{id}/edit` - Edit user form
- `POST /usermanagement/{id}/edit` - Edit user action
- `POST /usermanagement/{id}/deactivate` - Deactivate user
- `POST /usermanagement/{id}/reactivate` - Reactivate user
- `POST /usermanagement/{id}/change-role` - Change user role
- `POST /usermanagement/{id}/reset-password` - Admin reset user password

---

## 👷 PERSONNEL MANAGEMENT

### PersonnelController (9 endpoints)
- `GET /personnel` - List all personnel
- `GET /personnel/{id}` - Personnel details
- `GET /personnel/create` - Create personnel form
- `POST /personnel/create` - Create personnel action
- `GET /personnel/{id}/edit` - Edit personnel form
- `POST /personnel/{id}/edit` - Edit personnel action
- `POST /personnel/{id}/archive` - Archive personnel
- `POST /personnel/{id}/reactivate` - Reactivate personnel
- Additional endpoints for linking/unlinking users

---

## 🏢 ASSET MANAGEMENT

### AssetController (8 endpoints)
- `GET /asset` - List all assets
- `GET /asset/{id}` - Asset details
- `GET /asset/create` - Create asset form
- `POST /asset/create` - Create asset action
- `GET /asset/{id}/edit` - Edit asset form
- `POST /asset/{id}/edit` - Edit asset action
- `POST /asset/{id}/archive` - Archive asset
- `GET /asset/{id}/history` - Asset status history

---

## 🔧 MAINTENANCE REQUESTS

### MaintenanceRequestsController (11 endpoints)
- `GET /maintenancerequests` - List maintenance requests
- `GET /maintenancerequests/{id}` - Request details
- `GET /maintenancerequests/create` - Create request form
- `POST /maintenancerequests/create` - Create request action
- `GET /maintenancerequests/{id}/edit` - Edit request form
- `POST /maintenancerequests/{id}/edit` - Edit request action
- `POST /maintenancerequests/{id}/approve` - Approve request
- `POST /maintenancerequests/{id}/reject` - Reject request
- `POST /maintenancerequests/{id}/convert` - Convert to work order
- `POST /maintenancerequests/{id}/archive` - Archive request
- `GET /maintenancerequests/archived` - View archived requests

---

## 📅 PREVENTIVE MAINTENANCE

### PreventiveMaintenanceController (9 endpoints)
- `GET /preventivemaintenance` - List PM schedules
- `GET /preventivemaintenance/assets/list` - Get assets for dropdown
- `GET /preventivemaintenance/technicians/list` - Get technicians for dropdown
- `POST /preventivemaintenance/create` - Create PM schedule
- `GET /preventivemaintenance/{id}` - Get PM schedule details
- `PUT /preventivemaintenance/{id}/edit` - Edit PM schedule
- `PUT /preventivemaintenance/{id}/toggle-status` - Activate/deactivate schedule
- `DELETE /preventivemaintenance/{id}/delete` - Delete PM schedule
- `POST /preventivemaintenance/{id}/generate` - Manually generate work order

---

## 🛠️ WORK ORDERS & DASHBOARD

### DashboardController (18 endpoints)
**Admin Dashboard:**
- `GET /admin/dashboard` - Admin dashboard overview
- `GET /admin/dashboard/stats` - Dashboard statistics

**Work Orders:**
- `GET /admin/dashboard/work-orders` - List work orders
- `GET /admin/dashboard/work-orders/{id}` - Work order details
- `POST /admin/dashboard/work-orders/create` - Create work order
- `POST /admin/dashboard/work-orders/{id}/edit` - Edit work order
- `POST /admin/dashboard/work-orders/{id}/assign` - Assign technician
- `POST /admin/dashboard/work-orders/{id}/start` - Start work
- `POST /admin/dashboard/work-orders/{id}/complete` - Complete work
- `POST /admin/dashboard/work-orders/{id}/archive` - Archive work order

**Assets:**
- `GET /admin/dashboard/assets` - List assets
- `GET /admin/dashboard/assets/{id}` - Asset details

**Maintenance Requests:**
- `GET /admin/dashboard/maintenance-requests` - List requests
- `GET /admin/dashboard/maintenance-requests/{id}` - Request details

**Cost Tracking:**
- `POST /admin/dashboard/work-orders/{id}/costs/add` - Add cost
- `POST /admin/dashboard/work-orders/{id}/parts/add` - Add part usage
- Additional cost/part management endpoints

---

## 👨‍🔧 TECHNICIAN DASHBOARD

### TechnicianDashboardController (5 endpoints)
- `GET /dashboard` - Technician dashboard overview
- `GET /dashboard/work-orders` - List assigned work orders
- `GET /dashboard/work-orders/{id}` - Work order details
- `POST /dashboard/work-orders/start/{id}` - Start work
- `POST /dashboard/work-orders/complete/{id}` - Complete work

---

## 👤 USER DASHBOARD

### UserDashboardController (3 endpoints)
- `GET /userdashboard` - User dashboard overview
- `GET /userdashboard/maintenance-requests` - List user's requests
- `GET /userdashboard/maintenance-requests/{id}` - Request details

---

## 🔩 PARTS INVENTORY

### PartsController (6 endpoints)
- `GET /parts` - List all parts
- `GET /parts/{id}` - Part details
- `GET /parts/create` - Create part form
- `POST /parts/create` - Create part action
- `GET /parts/{id}/edit` - Edit part form
- `POST /parts/{id}/edit` - Edit part action

---

## 📝 MAINTENANCE LOGS

### MaintenanceLogsController (4 endpoints)
- `GET /maintenancelogs` - List maintenance logs
- `GET /maintenancelogs/{id}` - Log details
- `POST /maintenancelogs/create` - Create log entry
- `GET /maintenancelogs/asset/{assetId}` - Logs for specific asset

---

## 🌐 HOME & PUBLIC

### HomeController (3 endpoints)
- `GET /` - Home page
- `GET /Home/Privacy` - Privacy policy
- `GET /Home/Error` - Error page

---

## 👑 SUPERADMIN - COMPANIES

### SuperAdminCompaniesController (6 endpoints)
- `GET /superadmin/companies` - List all companies
- `GET /superadmin/companies/{id}` - Company details
- `POST /superadmin/companies/{id}/suspend` - Suspend company
- `POST /superadmin/companies/{id}/reactivate` - Reactivate company
- `POST /superadmin/companies/{id}/update` - Update company details
- `POST /superadmin/companies/{id}/delete` - Delete company

---

## 💳 SUPERADMIN - SUBSCRIPTIONS

### SuperAdminSubscriptionsController (8 endpoints)
**Subscription Plans:**
- `GET /superadmin/subscriptions/plans` - List subscription plans
- `POST /superadmin/subscriptions/plans/create` - Create plan
- `POST /superadmin/subscriptions/plans/{id}/update` - Update plan
- `POST /superadmin/subscriptions/plans/{id}/toggle` - Activate/deactivate plan

**Company Subscriptions:**
- `GET /superadmin/subscriptions` - List company subscriptions
- `POST /superadmin/subscriptions/assign` - Assign subscription to company
- `POST /superadmin/subscriptions/{id}/extend` - Extend subscription
- `POST /superadmin/subscriptions/{id}/payment-status` - Update payment status

---

## 📊 SUPERADMIN - DASHBOARD

### SuperAdminDashboardController (1 endpoint)
- `GET /superadmin/dashboard` - Platform overview dashboard

---

## 🔐 AUTHORIZATION LEVELS

### Public (No Authentication Required)
- Home page
- Privacy policy
- Error page
- Forgot password flow
- Email verification

### Authenticated Users
- User dashboard
- Create maintenance requests
- View own requests

### Technician
- Technician dashboard
- View assigned work orders
- Start/complete work

### Admin
- Admin dashboard
- Full work order management
- Asset management
- Personnel management
- Parts inventory
- Maintenance request approval
- User management (limited)

### Owner
- All Admin permissions
- Full user management
- Company settings

### SuperAdmin
- Platform-level access
- Company management
- Subscription management
- Cross-tenant operations

---

## 🛡️ SECURITY FEATURES

### Multi-Tenant Isolation
- All tenant endpoints filter by `CompanyId`
- SuperAdmin bypasses tenant filtering
- Tenant service enforces isolation

### Role-Based Authorization
- `[Authorize]` - Requires authentication
- `[Authorize(Roles = "Admin,Owner")]` - Specific roles
- `[AllowAnonymous]` - Public access

### Anti-Forgery Protection
- `[ValidateAntiForgeryToken]` on POST actions
- CSRF protection enabled

### Input Validation
- Model validation with data annotations
- Server-side validation
- Client-side validation

---

## 📈 API STATISTICS

- **Total Endpoints:** 108
- **Controllers:** 15
- **Authentication Endpoints:** 7
- **Admin Endpoints:** ~60
- **Technician Endpoints:** 5
- **User Endpoints:** 3
- **SuperAdmin Endpoints:** 15
- **Public Endpoints:** 3

---

## 🔄 HTTP METHODS USED

- **GET:** ~50 endpoints (read operations)
- **POST:** ~45 endpoints (create/update operations)
- **PUT:** ~5 endpoints (update operations)
- **DELETE:** ~3 endpoints (delete operations)
- **PATCH:** 0 endpoints

---

## 📝 NOTES

### RESTful Conventions
- Most endpoints follow RESTful patterns
- Some use custom action names for clarity (e.g., `/approve`, `/convert`)

### Response Types
- Views (Razor Pages) for UI endpoints
- JSON for AJAX/API endpoints
- Redirects for form submissions

### Error Handling
- Try-catch blocks in all endpoints
- Structured logging
- User-friendly error messages
- Global error handler

---

**Last Updated:** May 12, 2026  
**MaintenX Version:** 1.0  
**Total API Endpoints:** 108
