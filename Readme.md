# Inventory Platform (Multi-tenant) — .NET 9 + EF Core + SQLite

Production-style **multi-tenant inventory system** showcasing:

- Multi-tenancy with **strict tenant isolation** (tenant-owned entities have `TenantId`)
- Tenant resolved from HTTP header: `X-Tenant-Id`
- JWT authentication + role-based authorization
- Catalog → inventory workflow with stock movement ledger
- Audit logging with Before/After JSON snapshots
- Background job generating low-stock reports
- xUnit unit + integration tests

---

## Table of Contents

- [Quickstart](#quickstart)
- [Solution Structure](#solution-structure)
- [Prerequisites](#prerequisites)
- [Setup & Run (Local)](#setup--run-local)
  - [Restore](#restore)
  - [Apply DB migrations](#apply-db-migrations)
  - [Run the API](#run-the-api)
  - [Open Swagger](#open-swagger)
- [Database (SQLite)](#database-sqlite)
  - [Reset DB (dev)](#reset-db-dev)
- [Seeding (Demo Data)](#seeding-demo-data)
  - [Tenants](#tenants)
  - [Users](#users)
  - [Warehouses](#warehouses)
  - [Products](#products)
- [Multi-tenancy](#multi-tenancy)
  - [Tenant header (required)](#tenant-header-required)
  - [Tenant isolation enforcement](#tenant-isolation-enforcement)
- [Authentication & Authorization](#authentication--authorization)
  - [Roles](#roles)
  - [Login](#login)
  - [Swagger usage (Tenant header + JWT)](#swagger-usage-tenant-header--jwt)
- [API Endpoints](#api-endpoints)
  - [Audit](#audit)
  - [Auth](#auth)
  - [Inventory](#inventory)
  - [Reports (Low-stock)](#reports-low-stock)
  - [Troubleshooting](#troubleshooting)
- [Background Job — Low Stock Report](#background-job--low-stock-report)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
  - [EF CLI asks for project](#ef-cli-asks-for-project)
  - [Pending model changes warning](#pending-model-changes-warning)
  - [SQLite quirks (DateTimeOffset ordering)](#sqlite-quirks-datetimeoffset-ordering)
- [Reference Seed Values](#reference-seed-values)

---

## Quickstart

1) Restore + migrate + run:

```bash
dotnet restore
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
dotnet run --project Inventory.API
Open Swagger:

https://localhost:<port>/swagger (check console output for the exact port)

Authenticate (per tenant):

Add header: X-Tenant-Id: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Call POST /api/auth/login and use returned JWT as:

Authorization: Bearer <token>

Solution Structure
Inventory.API — Web API (controllers, middleware, seed, hosted services, Swagger)

Inventory.Domain — Domain entities + abstractions (tenancy interfaces, roles, etc.)

Inventory.Infrastructure — EF Core DbContext, migrations, auditing interceptor

Inventory.Tests.Integration — Integration tests using WebApplicationFactory

Inventory.Tests.Unit — Unit(-ish) tests for workflow rules

Prerequisites
.NET SDK 9

(Optional but recommended) EF CLI tools:

dotnet tool install --global dotnet-ef
Setup & Run (Local)
Restore
From solution root:

dotnet restore
Apply DB migrations
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
Run the API
dotnet run --project Inventory.API
Open Swagger
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
Seeding (Demo Data)
Seeding runs automatically on startup (commonly in Development) using DbSeeder.

Tenants
Tenant A: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Tenant B: bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb

Users
Password for all demo users: Pass123!

Per tenant:

Admin: admin@demo.com

Manager: manager@demo.com

Clerk: clerk@demo.com

Warehouses
Per tenant:

Main Warehouse

Secondary Warehouse

Products
Per tenant:

SKU-COFFEE-001 — Coffee Beans 1kg — 14.99

SKU-TEA-001 — Green Tea 200g — 6.50

Multi-tenancy
Tenant header (required)
All API requests (except Swagger/OpenAPI endpoints) require:

X-Tenant-Id: <tenant-guid>
If missing/invalid, the API responds with 400 Bad Request.

Tenant isolation enforcement
Isolation is enforced by:

Tenant resolved by middleware (per-request)

EF Core global query filters for tenant-owned entities

SaveChanges guard preventing cross-tenant writes

Authentication & Authorization
Roles
Admin

Manage users

Read/write products & warehouses (depending on implementation)

Manager

Manage inventory, products, warehouses

Only Manager can post inventory movements

Clerk

Read-only access (catalog and stock)

Login
POST /api/auth/login

Headers:

X-Tenant-Id: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
Body:

{ "email": "manager@demo.com", "password": "Pass123!" }
Response:

{ "accessToken": "..." }
Use the token for subsequent requests:

Authorization: Bearer <token>
Swagger usage (Tenant header + JWT)
Swagger is configured to support:

X-Tenant-Id as an API key header

Bearer JWT

Steps:

Call POST /api/auth/login with X-Tenant-Id to get a token

Click Authorize in Swagger and set:

X-Tenant-Id value (tenant GUID)

Bearer token (JWT)

API Endpoints
Note: All endpoints require X-Tenant-Id unless explicitly excluded for Swagger/OpenAPI.

Audit
Base route: /api/audit

Example:

GET /api/audit?take=50&skip=0&entityType=Product&userId=1

Endpoints:

GET /api/audit (Admin only)

Query params:

take (default 50)

skip (default 0)

entityType (optional, e.g. Product)

userId (optional)

Auth
Base route: /api/auth

POST /api/auth/login

Inventory
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

{ "type": "Purchase", "productId": 1, "warehouseId": 1, "quantity": 10 }
Products
Base route: /api/products

GET /api/products — GetAll

GET /api/products/{id} — GetById

POST /api/products — Create

PUT /api/products/{id:int} — Update

DELETE /api/products/{id:int} — Delete

Reports (Low-stock)
Base route: /api/reports

GET /api/reports/low-stock — GetLatestLowStockReport
Roles: Admin/Manager

GET /api/reports/low-stock/history?take=10 — GetLowStockHistory
Roles: Admin/Manager

Users
Base route: /api/users

GET /api/users — list users (Admin only)

POST /api/users — create user (Admin only)

Warehouses
Base route: /api/warehouses

GET /api/warehouses — GetAll

GET /api/warehouses/{id} — GetById

POST /api/warehouses — Create

PUT /api/warehouses/{id} — Update

DELETE /api/warehouses/{id} — Delete

Background Job — Low Stock Report
A hosted background service generates low-stock reports periodically.

Config in appsettings.json:

"LowStockJob": { "IntervalSeconds": 86400, "Threshold": 5 }
Testing
Run all tests:

dotnet test
Run unit tests:

dotnet test Inventory.Tests.Unit
Run integration tests:

dotnet test Inventory.Tests.Integration
Troubleshooting
EF CLI asks for project
Use explicit flags:

dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
Pending model changes warning
Add a migration and update:

dotnet ef migrations add <MigrationName> --project Inventory.Infrastructure --startup-project Inventory.API
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
SQLite quirks (DateTimeOffset ordering)
SQLite may fail translating ORDER BY on DateTimeOffset. Options:

store with a converter, or

order by an integer key (e.g., Id) when returning “latest” rows, or

for small datasets: materialize and order in-memory (LINQ-to-Objects).

Reference Seed Values
Tenants
Tenant A: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Tenant B: bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb

Users (per tenant)
admin@demo.com / Pass123!

manager@demo.com / Pass123!

clerk@demo.com / Pass123!

Warehouses (per tenant)
Main Warehouse

Secondary Warehouse

Products (per tenant)
SKU-COFFEE-001 — Coffee Beans 1kg — 14.99

SKU-TEA-001 — Green Tea 200g — 6.50