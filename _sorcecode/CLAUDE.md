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
| Auth | `/login`, `/register`, etc. | `api/auth` | JWT login, register, password reset |
| Admin | — | `api/admin` | User, role, menu & permission management |

## Cross-Project Notes

- **Bilingual UI** (Thai/English) — frontend uses i18n with `useLocale()` hook and `t("key")` translation function
- Thai is the default language, English available via `LocaleSwitcher` component
- Backend root namespace is `imp_api` (underscored)
- Frontend uses `@/*` path alias mapped to `./src/*`
- Frontend lint: `npm run lint` (ESLint 9 flat config)
- Backend API routes follow `api/[controller]` convention
- Backend error messages are in Thai
