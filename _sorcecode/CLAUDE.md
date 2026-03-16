# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Refund AI** — Thai tax benefits information system ("สิทธิประโยชน์ด้านภาษีอากร"). Monorepo with two sub-projects communicating via REST API.

## Sub-Projects

| Project | Tech | Dev Server | Port |
|---------|------|------------|------|
| `imp-api/` | ASP.NET Core 10.0 + Dapper + SQL Server | `dotnet run` | 5209 |
| `imp-app/` | Next.js 16 + React 19 + Tailwind v4 + shadcn/ui | `npm run dev` | 3000 |

Each sub-project has its own `CLAUDE.md` with detailed conventions — read those before working in either project.

## Quick Start

```bash
# Backend
cd imp-api && dotnet run          # http://localhost:5209, Swagger at /swagger

# Frontend
cd imp-app && npm run dev         # http://localhost:3000 (Turbopack)
```

## Architecture

```
imp-app (Next.js) → REST → imp-api (ASP.NET Core) → Dapper → SQL Server (IMP_DB)
```

- **Backend layering**: Controllers → Services → Repositories (Dapper, not EF)
- **Frontend**: App Router, shadcn/ui components, Thai fonts (Anuphan/Prompt), oklch theming
- **Database**: SQL Server at `192.168.10.131\KPIDATAWH`, database `IMP_DB`
- **No test framework** configured in either project yet

## Features

| Feature | Frontend Route | API Route | Description |
|---------|---------------|-----------|-------------|
| Import Excel Upload | `/import/excel` | `api/import-excel` | Upload & upsert import declaration Excel |
| Import Data Manage | `/import/manage` | `api/import-manage` | CRUD for import declaration data |
| Export Excel Upload | `/export/excel` | `api/export-excel` | Upload & upsert export declaration Excel |
| Export Data Manage | `/export/manage` | `api/export-manage` | CRUD for export declaration data |
| **Section 19 bis** | `/privilege/section29/manage` | `api/privilege-19tvis` | **FIFO stock cutting for tax refund** |
| **Section 29 Batch** | `/privilege/section29/manage_batch` | `api/privilege-19tvis/m29-*` | **Batch management for export grouping & simulate** |
| Auth | `/login`, `/register`, etc. | `api/auth` | JWT login, register, password reset |
| Admin | — | `api/admin` | User, role, menu & permission management |

## Section 19 bis — FIFO Stock Cutting (มาตรา 19 ทวิ)

Core feature: cut imported raw material stock (FIFO) against export declarations to calculate duty refund.

### Business Rules
- **FIFO order**: Oldest import lot (by ImportDate ASC) is cut first
- **BOM formula**: QtyRequired = ExportQty × (Ratio + Scrap) / 100
- **Batch document** (BatchDocNo): Format `"001/69"` (running/พ.ศ. 2 หลัก)
  - Max **10** export items per batch
  - Total **FOBTHB ≤ 10,000,000** THB per batch
  - Cancel by BatchDocNo cancels entire batch (restores all lot balances)
- **Lock rules**:
  - Import lots aged > 365 days → locked (red background, cannot be cut)
  - Export items aged > 180 days → locked (red background, cannot be selected)
- **Status flow**: PENDING → CONFIRMED (or Cancel → delete + restore)

### Database Tables (imp schema)
| Table | Purpose |
|-------|---------|
| `stock_m29_lot` | Import lot inventory (qty tracking per import declaration item) |
| `stock_m29_batch` | FIFO cutting records (links export → import lots with qty/duty) |
| `stock_m29_card` | Stock card ledger (IN/OUT transaction history) |
| `stock_m29_on_hand` | Aggregated on-hand balance by material |
| `m29_batch_header` | Batch grouping header (BatchDocNo, Status, TotalItems, TotalFOBTHB, Remark) |
| `m29_batch_item` | Batch items (FK → batch_header, ExportExcelId, sorted by ReleaseDate) |

## Cross-Project Notes

- **Bilingual UI** (Thai/English) — frontend uses i18n with `useLocale()` hook and `t("key")` translation function
- Thai is the default language, English available via `LocaleSwitcher` component
- Backend root namespace is `imp_api` (underscored)
- Frontend uses `@/*` path alias mapped to `./src/*`
- Frontend lint: `npm run lint` (ESLint 9 flat config)
- Backend API routes follow `api/[controller]` convention
- Backend error messages are in Thai
