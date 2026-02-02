# Inventory Platform (Multi-tenant) — .NET 9 + EF Core + SQLite

A production-style **multi-tenant inventory system** demonstrating:

- **Strict tenant isolation** (tenant-owned entities contain `TenantId`)
- **Tenant resolution** via HTTP header: `X-Tenant-Id`
- **JWT authentication** + **role-based authorization**
- **Catalog → inventory workflow** with a **stock movement ledger**
- **Audit logging** with **Before/After JSON snapshots**
- **Background job** generating **low-stock reports**
- **xUnit** unit + integration tests

---

## Table of Contents

- [Solution Structure](#solution-structure)
- [Prerequisites](#prerequisites)
- [Setup & Run (Local)](#setup--run-local)
  - [Restore](#restore)
  - [Apply DB Migrations](#apply-db-migrations)
  - [Run the API](#run-the-api)
  - [Swagger UI](#swagger-ui)
- [Database (SQLite)](#database-sqlite)
  - [Reset DB (Dev)](#reset-db-dev)
- [Seeding (Demo Tenants + Users + Products + Warehouses)](#seeding-demo-tenants--users--products--warehouses)
  - [Demo Tenants](#demo-tenants)
  - [Seeded Users](#seeded-users)
  - [Seeded Warehouses](#seeded-warehouses)
  - [Seeded Products](#seeded-products)
- [Multi-tenancy](#multi-tenancy)
  - [Tenant Header (Required)](#tenant-header-required)
  - [Tenant Isolation Enforcement](#tenant-isolation-enforcement)
- [Authentication & Authorization](#authentication--authorization)
  - [Roles](#roles)
  - [Login](#login)
  - [Swagger Usage (Tenant Header + JWT)](#swagger-usage-tenant-header--jwt)
- [API Endpoints](#api-endpoints)
  - [Audit](#audit)
  - [Auth](#auth)
  - [Inventory](#inventory)
  - [Products](#products)
  - [Reports (Low-stock)](#reports-low-stock)
  - [Users](#users)
  - [Warehouses](#warehouses)
- [Background Job — Low Stock Report](#background-job--low-stock-report)
- [Testing](#testing)
  - [Unit Tests](#unit-tests)
  - [Integration Tests](#integration-tests)
- [Troubleshooting](#troubleshooting)
  - [EF CLI asks for project](#ef-cli-asks-for-project)
  - [Pending model changes warning](#pending-model-changes-warning)
  - [SQLite quirks (DateTimeOffset ordering)](#sqlite-quirks-datetimeoffset-ordering)
- [Reference Seed Values](#reference-seed-values)

---

## Solution Structure

- **Inventory.API** — Web API (controllers, middleware, seed, hosted services, Swagger)
- **Inventory.Domain** — Domain entities + abstractions (tenancy interfaces, roles, etc.)
- **Inventory.Infrastructure** — EF Core `DbContext`, migrations, auditing interceptor
- **Inventory.Tests.Integration** — Integration tests using `WebApplicationFactory`
- **Inventory.Tests.Unit** — Unit(-ish) tests for workflow rules

---

## Prerequisites

- **.NET SDK 9**
- *(Optional but recommended)* EF CLI tools:
  ```bash
  dotnet tool install --global dotnet-ef
Setup & Run (Local)
Restore
From solution root:

dotnet restore
Apply DB Migrations
dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
Run the API
dotnet run --project Inventory.API
Swagger UI
Swagger UI is available at:

/swagger (see console output for the exact local URL)

Database (SQLite)
Connection string: Data Source=inventory.db

A local file inventory.db is created in the working directory of the API process.

Reset DB (Dev)
Stop the API

Delete inventory.db

Re-apply migrations:

dotnet ef database update --project Inventory.Infrastructure --startup-project Inventory.API
Seeding (Demo Tenants + Users + Products + Warehouses)
Seeding runs automatically on startup (commonly in Development) using DbSeeder.

Demo Tenants
Tenant A: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Tenant B: bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb

Seeded Users
Password for all demo users: Pass123!

Per tenant:

Admin: admin@demo.com

Manager: manager@demo.com

Clerk: clerk@demo.com

Seeded Warehouses
Per tenant:

Main Warehouse

Secondary Warehouse

Seeded Products
Per tenant:

SKU-COFFEE-001 — Coffee Beans 1kg — 14.99

SKU-TEA-001 — Green Tea 200g — 6.50

Multi-tenancy
Tenant Header (Required)
All API requests (except Swagger/OpenAPI endpoints) require:

Header: X-Tenant-Id: <tenant-guid>

If missing/invalid, the API responds with:

400 Bad Request

Tenant Isolation Enforcement
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
Swagger Usage (Tenant Header + JWT)
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
AuditController
Base route: /api/audit

Example:
GET /api/audit?take=50&skip=0&entityType=Product&userId=1

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
ProductsController
Base route: /api/products

GET /api/products — GetAll

GET /api/products/{id} — GetById

POST /api/products — Create

PUT /api/products/{id:int} — Update

DELETE /api/products/{id:int} — Delete

Reports (Low-stock)
ReportsController
Base route: /api/reports

GET /api/reports/low-stock — GetLatestLowStockReport
Roles: Admin/Manager

GET /api/reports/low-stock/history?take=10 — GetLowStockHistory
Roles: Admin/Manager

Users
UsersController
Base route: /api/users

GET /api/users — list users (Admin only)

POST /api/users — create user (Admin only)

Warehouses
WarehousesController
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
Unit Tests
dotnet test Inventory.Tests.Unit
Integration Tests
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

store timestamp with a converter (e.g., as TEXT/INTEGER), or

order by a supported key (e.g., Id) when returning “latest” rows, or

for small datasets: order in memory after materializing results.

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