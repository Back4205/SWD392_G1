---
title: Complete SDS entities
status: completed
priority: P1
effort: medium
branch: main
tags: [database, ef-core, sds]
created: 2026-11-03
---

# Complete SDS entities

Status: Completed

## Scope and acceptance

Implement all 34 database entities from Report 2 SDS section 2.2 in the existing
ASP.NET Core 8 / EF Core 8 SQL Server project. Match field names, SQL types,
nullability, keys and foreign keys. Preserve the existing InitialCreate migration
and GoodsReceipt approval fields. Add an incremental migration, verify compilation,
model/schema parity and SQL generation. Do not apply migrations to a live database
or implement services, endpoints or frontend workflows.

## Reviewed approach

Keep the current entity namespace, class style, singular table names and explicit
Fluent API configuration in EWMSDbContext. Add the 13 missing entity classes and
DbSets, connect SerialNumber's outbound references, and use NoAction on foreign
keys to avoid cascading deletion of warehouse history and SQL Server multiple
cascade paths. Follow the SDS constraint columns; User.Email's description says
unique but its UN cell is blank, so preserve its current nonunique mapping.
Add the explicitly marked nullable unique User.PhoneNumber index.

## Checklist

- [x] Inspect source, migration history and all 34 SDS database tables.
- [x] Review scope, compatibility and implementation approach.
- [x] Implement missing entities and model mappings.
- [x] Generate incremental migration and verify model/schema/SQL.
- [x] Review changes and document migration commands and assumptions.

## Verification report

- `dotnet build EWMS/EWMS.sln --no-restore`: 0 warnings, 0 errors.
- `dotnet ef migrations has-pending-model-changes`: no pending changes.
- Independent EF metadata comparison against original SDS: all 34 tables and
  217 specified columns match SQL types, nullability and primary keys. The 3
  existing GoodsReceipt approval columns are intentionally retained.
- All 72 SDS foreign keys point to the correct tables. Including the existing
  GoodsReceipt approval relation, all 73 foreign keys use NoAction; no shadow
  properties. All 11 SDS non-PK unique columns are indexed appropriately.
- `CompleteSdsEntities.Up`: 13 CreateTable, 42 CreateIndex, 2 AddForeignKey;
  no drop, alter or data operations. Down removes all 13 added tables.
- Full idempotent, incremental and rollback SQL generation passed. Temporary
  validation artifacts are under ignored `EWMS/EWMS.Api/obj/schema-verification/`.
- Independent code review and `git diff --check` passed.
- Migration guide: `docs/database-migrations.md`; decisions recorded in
  `docs/journals/261103-sds-entities.md`.

No live SQL Server migration was executed. Existing phone uniqueness and serial
outbound-reference prerequisites are documented. No unresolved implementation
items or additional phase files remain. No files were staged or committed.
