Inventory Platform (Multi-tenant) — .NET 9 + EF Core + SQLite

A production-style multi-tenant inventory system showcasing:

Multi-tenancy with strict tenant isolation (all tenant-owned entities have TenantId)

Tenant resolved from HTTP header: X-Tenant-Id

JWT authentication + role-based authorization

Catalog → inventory workflow with stock movement ledger

Audit logging with Before/After JSON snapshots

Background job generating low-stock reports

xUnit unit + integration tests


Solution / Projects

Inventory.API — Web API, controllers, middleware, seed, hosted services, Swagger

Inventory.Domain — Domain entities + abstractions (tenancy interfaces, roles, etc.)

Inventory.Infrastructure — EF Core DbContext, migrations, auditing interceptor

Inventory.Tests.Integration — Integration tests using WebApplicationFactory

Inventory.Tests.Unit — Unit(-ish) tests for workflow rules


Prerequisites

.NET SDK 9

(Optional but recommended) EF CLI tools:

dotnet tool install --global dotnet-ef


Setup & Run (Local)
1) Restore

From solution root:

dotnet restore

2) Apply DB migrations (SQLite)
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API

3) Run the API
dotnet run --project Inventory.API

4) Open Swagger

Swagger UI is available at:

/swagger (see console output for the exact local URL)


Database (SQLite)

Connection string: Data Source=inventory.db

A local file inventory.db is created in the working directory of the API process.

Reset DB (dev)

Stop the API

Delete inventory.db

Re-apply migrations:

dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API


Seeding (Demo Tenants + Users + Products + Warehouses)

Seeding runs automatically on startup (commonly in Development) using DbSeeder.

Demo Tenants (deterministic)

Tenant A: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Tenant B: bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb

Seeded Users (per tenant)

Password for all demo users: Pass123!

Admin: admin@demo.com

Manager: manager@demo.com

Clerk: clerk@demo.com

Seeded Warehouses (per tenant)

Main Warehouse

Secondary Warehouse

Seeded Products (per tenant)

SKU-COFFEE-001 — Coffee Beans 1kg — 14.99

SKU-TEA-001 — Green Tea 200g — 6.50


Multi-tenancy
Tenant header (required)

All API requests (except Swagger/OpenAPI endpoints) require:

X-Tenant-Id: <tenant-guid>

If missing/invalid, the API responds with 400 BadRequest.

Tenant isolation enforcement

Isolation is enforced by:

Tenant resolved by middleware (per-request)

EF Core global query filters for tenant-owned entities

SaveChanges guard preventing cross-tenant writes


Authentication & Authorization
Roles

Admin

Manage users

Can read/write products & warehouses (depending on your implementation)

Manager

Manage inventory, products, warehouses

Only Manager can post inventory movements

Clerk

Read-only access (catalog and stock)

Login

POST /api/auth/login

Headers:

X-Tenant-Id: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa (or TenantB)

Body:

{
  "email": "manager@demo.com",
  "password": "Pass123!"
}


Response:

{
  "accessToken": "<JWT>"
}


Use this token for subsequent requests:

Authorization: Bearer <JWT>


Swagger usage (headers + JWT)

Swagger is configured to support:

X-Tenant-Id as an API key header

Bearer JWT

Steps:

Call POST /api/auth/login with X-Tenant-Id to get a token

Click Authorize in Swagger and set:

X-Tenant-Id value (tenant GUID)

Bearer token (JWT)


API Endpoints
Auditing

AuditController

Base route: /api/audit

Example: GET /api/audit?take=50&skip=0&entityType=Product&userId=1

Endpoints:

GET /api/audit (Admin only)
Query params:

take (default 50)

skip (default 0)

entityType (optional, e.g. Product)

userId (optional)

Auth

AuthController

Base route: /api/auth

POST /api/auth/login

Inventory

InventoryController

Base route: /api/inventory

Endpoints:

GET /api/inventory/stock
Returns flat list of stock rows (warehouse + product + qty)
Roles: Admin/Manager/Clerk (read)

POST /api/inventory/movements
Roles: Manager only

Movement types supported:

Purchase

Sale (oversell prevented)

Adjustment

Transfer

Example Purchase:

{
  "type": "Purchase",
  "productId": 1,
  "warehouseId": 1,
  "quantity": 10
}

Products

ProductsController

Base route: /api/products

Endpoints:

GET /api/products — GetAll

GET /api/products/{id} — GetById

POST /api/products — Create

PUT /api/products/{id:int} — Update

DELETE /api/products/{id:int} — Delete

Reports (Low-stock background job)

ReportsController

Base route: /api/reports

Endpoints:

GET /api/reports/low-stock — GetLatestLowStockReport
Roles: Admin/Manager

GET /api/reports/low-stock/history?take=10 — GetLowStockHistory
Roles: Admin/Manager

Users

UsersController

Base route: /api/users

Endpoints:

GET /api/users — list users (Admin only)

POST /api/users — create user (Admin only)

Warehouses

WarehousesController

Base route: /api/warehouses

Endpoints:

GET /api/warehouses — GetAll

GET /api/warehouses/{id} — GetById

POST /api/warehouses — Create

PUT /api/warehouses/{id} — Update

DELETE /api/warehouses/{id} — Delete

Background Job — Low Stock Report

A hosted background service generates low-stock reports periodically.

Config in appsettings.json:

"LowStockJob": {
  "IntervalSeconds": 86400,
  "Threshold": 5
}

API Endpoints
Auditing

AuditController

Base route: /api/audit

Example: GET /api/audit?take=50&skip=0&entityType=Product&userId=1

Endpoints:

GET /api/audit (Admin only)
Query params:

take (default 50)

skip (default 0)

entityType (optional, e.g. Product)

userId (optional)

Auth

AuthController

Base route: /api/auth

POST /api/auth/login

Inventory

InventoryController

Base route: /api/inventory

Endpoints:

GET /api/inventory/stock
Returns flat list of stock rows (warehouse + product + qty)
Roles: Admin/Manager/Clerk (read)

POST /api/inventory/movements
Roles: Manager only

Movement types supported:

Purchase

Sale (oversell prevented)

Adjustment

Transfer

Example Purchase:

{
  "type": "Purchase",
  "productId": 1,
  "warehouseId": 1,
  "quantity": 10
}

Products

ProductsController

Base route: /api/products

Endpoints:

GET /api/products — GetAll

GET /api/products/{id} — GetById

POST /api/products — Create

PUT /api/products/{id:int} — Update

DELETE /api/products/{id:int} — Delete

Reports (Low-stock background job)

ReportsController

Base route: /api/reports

Endpoints:

GET /api/reports/low-stock — GetLatestLowStockReport
Roles: Admin/Manager

GET /api/reports/low-stock/history?take=10 — GetLowStockHistory
Roles: Admin/Manager

Users

UsersController

Base route: /api/users

Endpoints:

GET /api/users — list users (Admin only)

POST /api/users — create user (Admin only)

Warehouses

WarehousesController

Base route: /api/warehouses

Endpoints:

GET /api/warehouses — GetAll

GET /api/warehouses/{id} — GetById

POST /api/warehouses — Create

PUT /api/warehouses/{id} — Update

DELETE /api/warehouses/{id} — Delete

Background Job — Low Stock Report

A hosted background service generates low-stock reports periodically.

Config in appsettings.json:

"LowStockJob": {
  "IntervalSeconds": 86400,
  "Threshold": 5
}


Troubleshooting
EF asks for project

Use explicit flags:

dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API

Pending model changes warning

Add a migration and update:

dotnet ef migrations add <MigrationName> --project Inventory.Infrastructure --startup-project Inventory.API
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API

SQLite quirks (DateTimeOffset ordering)

If you see translation errors ordering by DateTimeOffset, either:

store with a converter, or

order by an integer key (e.g., Id) when returning “latest” rows.

Seed values (reference)

Tenants:

Tenant A: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Tenant B: bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb

Users (per tenant):

admin@demo.com
 / Pass123!

manager@demo.com
 / Pass123!

clerk@demo.com
 / Pass123!

Warehouses (per tenant):

Main Warehouse

Secondary Warehouse

Products (per tenant):

SKU-COFFEE-001 — Coffee Beans 1kg — 14.99

SKU-TEA-001 — Green Tea 200g — 6.50