# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build
dotnet run                # runs on http://localhost:5209
```

Swagger UI available at `http://localhost:5209/swagger` in Development mode.

## Architecture

ASP.NET Core Web API (.NET 10.0) using **Dapper** with **SQL Server** — layered architecture:

- **Controllers/** — API endpoints (`[ApiController]`, explicit route prefix `api/xxx`)
- **Services/** — Business logic (interfaces + implementations)
- **Repositories/** — Data access via Dapper (interfaces + implementations)
- **Models/** — Domain entities (ImportExcel, ExportExcel, User, Role, Menu, etc.)
- **DTOs/** — Request/response data transfer objects (`AuthDtos.cs`, `AdminDtos.cs`, `ImportExcelDtos.cs`, `ExportExcelDtos.cs`)
- **Helpers/** — Utility classes (`PasswordHasher.cs` — BCrypt with workFactor 12)
- **SqlScripts/** — Database migration & seed scripts (run in order: 001 → 006)

## Key Conventions

- **ORM**: Dapper (not Entity Framework). Use `IDbConnection` injected via DI for all data access.
- **DI Registration**: `IDbConnection` is registered as Scoped in `Program.cs`, resolving to `SqlConnection` from `ConnectionStrings:DefaultConnection`.
- **Namespace**: Root namespace is `imp_api` (underscored, per csproj `RootNamespace`).
- **Database**: SQL Server (`IMP_DB`) at `192.168.10.131\KPIDATAWH`, schema `imp`. Connection string in `appsettings.json`.
- **API Testing**: Use `imp-api.http` for quick endpoint testing.
- **Password hashing**: BCrypt via `PasswordHasher.Hash()` / `PasswordHasher.Verify()` (workFactor 12).
- **Validation**: `[Required]`, `[MinLength]`, `[MaxLength(128)]` on password fields. Custom `ApiBehaviorOptions` returns 422 with `ErrorResponse` DTO. Thai error messages in data annotations.
- **Transactions**: DELETE + INSERT operations (role assignment, permission setting) use `IDbConnection.BeginTransaction()`.
- **Exception pattern**: `AppException` (base) → `AuthException` (alias). Used across all services for domain errors (`INVALID_CREDENTIALS`, `USER_NOT_FOUND`, `ROLE_NOT_FOUND`, `MENU_NOT_FOUND`, `LAST_ADMIN`, `CIRCULAR_REFERENCE`, etc.).
- **Error responses**: All endpoints return `ErrorResponse { Error, Message, FieldErrors? }` for consistency.
- **Username normalization**: `.Trim().ToLowerInvariant()` everywhere — prevents duplicate accounts.

## Authentication & Authorization

- **JWT Bearer** authentication configured in `Program.cs` (HS256, settings from `Jwt:*` in appsettings).
- **Auth flow**: `AuthController` → `AuthService` → `UserRepository` / `JwtService`.
- **Endpoints** (`api/auth`): `login`, `register`, `refresh`, `logout`, `forgot-password`, `reset-password`, `change-password`.
- **Refresh token rotation**: User validation performed BEFORE revoking old token (prevents lockout on failure). `rememberMe` preference preserved across refreshes.
- **Password reset**: Validates user `IsActive` before allowing reset. `MarkUsedAsync` uses `AND UsedAt IS NULL` to prevent race condition double-use.
- **Password strength**: Minimum 6 chars, at least 1 letter + 1 digit (validated in AuthService).
- **Forgot-password**: Always returns 200 OK regardless of user existence (prevents username enumeration). Reset link only returned in Development mode.
- **On password change/reset**: All refresh tokens AND password reset tokens for the user are invalidated.
- **userId parsing**: Controllers use `Guid.TryParse()` with fallback to 401 (no `NullReferenceException`).
- **Default admin**: `admin` / `P@ssw0rd` (seeded via `SqlScripts/003_SeedData.sql`).

## API Controllers

| Controller | Route | Auth | Purpose |
|---|---|---|---|
| `AuthController` | `api/auth` | Public (except `change-password`) | Login, register, JWT refresh, password reset |
| `AdminController` | `api/admin` | `[Authorize(Roles = "Admin")]` | User, role, menu & permission management |
| `MenuController` | `api/menu` | `[Authorize]` | Menu tree with permissions for current user |
| `ImportExcelController` | `api/import-excel` | `[Authorize]` | Upload & save import Excel data |
| `ImportManageController` | `api/import-manage` | `[Authorize]` | CRUD for import declaration data |
| `ExportExcelController` | `api/export-excel` | `[Authorize]` | Upload & save export Excel data |
| `ExportManageController` | `api/export-manage` | `[Authorize]` | CRUD for export declaration data |
| `TestDbController` | `api/testdb` | Public | Database connectivity check |

## DI Registration (Program.cs)

**Repositories**: `IUserRepository`, `IRoleRepository`, `IMenuRepository`, `IRoleMenuPermissionRepository`, `IRefreshTokenRepository`, `IPasswordResetRepository`, `IImportExcelRepository`, `IExportExcelRepository`

**Services**: `IJwtService`, `IEmailService`, `IAuthService`, `IAdminService`, `IImportExcelService`, `IExportExcelService`, `IImportManageService`, `IExportManageService`

**Background**: `TokenCleanupService` — deletes expired/revoked tokens every 6 hours

## Key Business Logic Patterns

### Last-Admin Protection
Prevents system lockout — checked in two places:
1. **ToggleUserActiveAsync**: Before deactivating a user with Admin role, verify `GetActiveAdminCountAsync() > 1`
2. **AssignRolesAsync**: Before removing Admin role from a user, verify `GetActiveAdminCountAsync() > 1`

### Circular Reference Prevention (Menus)
When updating a menu's `ParentId`:
- Self-reference check: `ParentId != menuId`
- Ancestor chain traversal with `HashSet` cycle detection
- ParentId existence validation

### MenuCode Uniqueness
Validated at application level (not just DB constraint):
- On create: check `GetByMenuCodeAsync(menuCode)` returns null
- On update: check no OTHER menu has the same MenuCode (exclude self)

### Permission Merging (OR Logic)
When user has multiple roles, most permissive permission wins per menu:
```sql
MAX(CAST(rmp.Visible AS INT)) AS Visible -- etc for each permission bit
```

### Role/Permission Assignment (Transactional)
`SetUserRolesAsync` and `SetPermissionsAsync` use transaction-based DELETE + INSERT pattern for atomicity.

## Database Schema (imp schema, 9 tables)

| Table | Key Columns |
|---|---|
| `Users` | Id (GUID PK), UserName (UNIQUE), PasswordHash, DisplayName, Email, IsActive |
| `Roles` | Id (INT IDENTITY PK), RoleName (UNIQUE), Description, IsActive |
| `UserRoles` | UserId + RoleId (composite PK, CASCADE DELETE) |
| `Menu` | Id (INT IDENTITY PK), ParentId (self-FK), MenuCode (UNIQUE), MenuName, Icon, Route, SortOrder, IsActive |
| `RoleMenuPermissions` | RoleId + MenuId (UNIQUE), Visible, CanCreate, CanEdit, CanReadOnly, CanDelete |
| `RefreshTokens` | Token (indexed), UserId (FK), ExpiresAt, RevokedAt |
| `PasswordResetTokens` | Token (indexed), UserId (FK), ExpiresAt, UsedAt |
| `import_excel` | Id (INT IDENTITY PK), DeclarNo + ItemDeclarNo (UNIQUE), 141 columns for import declaration data |
| `export_excel` | Id (INT IDENTITY PK), DeclarNo + ItemDeclarNo (UNIQUE), export declaration data (exporter, buyer, FOB, status, privileges) |

## Email

- `EmailService` uses MailKit (SMTP). Falls back to logging reset link if SMTP not configured.
- Send to `user.Email` (not `userName`). Config in `Smtp:*` section of appsettings.

## Global Error Handling

- `UseExceptionHandler` returns `ErrorResponse` with generic message in production.
- In Development mode: includes exception type and message for debugging.

## CORS

Allows Next.js frontend (`http://localhost:3000`) via `ImpApp` policy — configured with `AllowCredentials`.

## Seed Data

- **Roles**: Admin ("ผู้ดูแลระบบ"), User ("ผู้ใช้งานทั่วไป")
- **Menus**: DASHBOARD, IMPORT (IMPORT_EXCEL, IMPORT_MANAGE), EXPORT (EXPORT_EXCEL, EXPORT_MANAGE), STOCKCARD, FORMULA, REPORT, ADMIN (ADMIN_USERS, ADMIN_ROLES, ADMIN_PERMISSIONS)
- **Admin permissions**: Full access to all menus

## TODO — Production Hardening

ยังไม่ implement — เพิ่มเมื่อพร้อม:

1. **ย้าย secrets ออกจาก `appsettings.json`** — Connection string (SA password) และ JWT SecretKey อยู่ใน plain text ควรย้ายไป `dotnet user-secrets` (dev) หรือ environment variables (production)
2. **Rate limiting บน auth endpoints** — ป้องกัน brute force / DoS ใช้ ASP.NET Core `AddRateLimiter()` + `[EnableRateLimiting]` บน login, register, forgot-password
3. **Unit tests** — ยังไม่มี test project ควรเพิ่มอย่างน้อย service layer (AuthService, AdminService)
4. **ปิด Swagger ใน production** — ตอนนี้เปิดตลอด ควร wrap ด้วย `if (app.Environment.IsDevelopment())`
