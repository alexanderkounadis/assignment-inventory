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
