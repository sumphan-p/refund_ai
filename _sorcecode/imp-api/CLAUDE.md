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
- **SqlScripts/** — Database migration & seed scripts (run in order: 001 → 017)

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
| `Privilege19TvisController` | `api/privilege-19tvis` | `[Authorize]` | Section 19 bis FIFO stock cutting |
| `TestDbController` | `api/testdb` | Public | Database connectivity check |

## DI Registration (Program.cs)

**Repositories**: `IUserRepository`, `IRoleRepository`, `IMenuRepository`, `IRoleMenuPermissionRepository`, `IUserMenuPermissionRepository`, `IRefreshTokenRepository`, `IPasswordResetRepository`, `IImportExcelRepository`, `IExportExcelRepository`, `IBomM29Repository`, `IBomBoiRepository`, `IStockLotRepository`, `IStockCuttingRepository`, `IM29BatchRepository`

**Services**: `IJwtService`, `IEmailService`, `IAuthService`, `IAdminService`, `IImportExcelService`, `IExportExcelService`, `IImportManageService`, `IExportManageService`, `IFormulaM29Service`, `IFormulaBoiService`, `IPrivilege19TvisService`

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

### Section 19 bis FIFO Stock Cutting

**FIFO algorithm** (`Privilege19TvisService.CalculateFifoAsync`):
1. Get BOM formula → for each raw material: QtyRequired = ExportQty × (Ratio + Scrap) / 100
2. Get active lots ordered by ImportDate ASC, filtered: age ≤ 365 days
3. Cut lots sequentially until QtyRequired fulfilled
4. Save cutting records as PENDING in `stock_m29_batch`, update lot balances
5. Confirm → status CONFIRMED + stock card entry; Cancel → delete records + restore lot balances

**Batch document** (`BatchDocNo`): Format `"001/69"` (3-digit running / พ.ศ. 2 หลัก)
- Auto-generated from MAX running in current Buddhist year, or user-specified starting number
- Max 10 export items per BatchDocNo (`BATCH_LIMIT`)
- Cancel-batch cancels all records with same BatchDocNo

**API endpoints** (`api/privilege-19tvis`):
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `exports` | Search export items eligible for cutting |
| GET | `export-lines?declarNo=` | Get export lines by DeclarNo |
| GET | `bom-formula?formulaNo=&exportQty=` | Get BOM formula with calculated quantities |
| GET | `available-lots?rawMaterialCode=` | Get all active lots for a material (FIFO order) |
| GET | `stock-card?rawMaterialCode=` | Get stock card entries |
| GET | `next-doc-no` | Get next batch document number (stock_m29_batch) |
| GET | `cutting-detail` | Get cutting detail for an export item |
| POST | `cut` | Calculate FIFO cutting (creates PENDING records) |
| PUT | `confirm` | Confirm cutting (PENDING → CONFIRMED) |
| PUT | `cancel` | Cancel cutting by export item |
| PUT | `cancel-batch?batchDocNo=` | Cancel entire batch by BatchDocNo |
| POST | `sync-lots` | Sync import_excel → stock_m29_lot |
| GET | `m29-batches` | Search M29 batch headers (paginated) |
| GET | `m29-next-doc-no` | Get next M29 batch document number (m29_batch_header) |
| POST | `m29-batch` | Create M29 batch (select export items into a group) |
| GET | `m29-batch/{batchDocNo}` | Get M29 batch detail (header + items) |
| PUT | `m29-confirm-batch?batchDocNo=` | Confirm M29 batch |
| DELETE | `m29-batch?batchDocNo=` | Cancel/delete M29 batch (CASCADE deletes items) |

## Database Schema (imp schema, 15 tables)

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
| `stock_m29_lot` | Id (INT IDENTITY PK), ImportDeclarNo + ImportItemNo, QtyOriginal/QtyUsed/QtyBalance, DutyPerUnit, Status, ExpiryDate |
| `stock_m29_batch` | Id (INT IDENTITY PK), BatchDocNo, StockLotId (FK→lot), ExportDeclarNo/ItemNo, Ratio/Scrap/QtyRequired/QtyCut, DutyRefund, Status |
| `stock_m29_card` | Id (INT IDENTITY PK), TransactionType (IN/OUT), RawMaterialCode, QtyIn/QtyOut/QtyBalance |
| `stock_m29_on_hand` | Id (INT IDENTITY PK), RawMaterialCode, QtyBalance (aggregated on-hand) |
| `m29_batch_header` | Id (INT IDENTITY PK), BatchDocNo (UNIQUE), Status (DRAFT/PENDING/CONFIRMED), TotalItems, TotalFOBTHB, Remark, CreatedBy/ConfirmedBy/CancelledBy |
| `m29_batch_item` | Id (INT IDENTITY PK), BatchHeaderId (FK→batch_header ON DELETE CASCADE), ExportExcelId (FK→export_excel), ExportDeclarNo, ExportItemNo, SortOrder |

### M29 Batch Management (Export Grouping)

**Batch grouping** (`m29_batch_header` / `m29_batch_item`): Groups export items into batches before FIFO cutting.
- **BatchDocNo**: Format `"001/69"` (running/พ.ศ. 2 หลัก) — separate sequence from `stock_m29_batch.BatchDocNo`
- **Status flow**: DRAFT → PENDING (items selected) → CONFIRMED (or Cancel → DELETE CASCADE)
- **Business rules**: Max 10 DeclarNo per batch, TotalFOBTHB ≤ 10,000,000 THB
- **Remark**: Auto-generated summary: `"เลือกแล้ว {count} รายการ ({declarCount} ใบขน) · น้ำหนัก: {weight} kg · FOB: {fob} บาท"`
- **Sort**: Items sorted by ReleaseDate ASC (LoadingDate/ExportDate)
- **Repository**: `IM29BatchRepository` / `M29BatchRepository` (separate from `IStockCuttingRepository`)
- **Models**: `M29BatchHeader`, `M29BatchItem`
- **DTOs**: `CreateBatchRequest/Response`, `BatchListItem`, `M29BatchDetailResponse`, `M29BatchItemDetail`, `NextDocNoResponse`

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
- **Menus**: DASHBOARD, IMPORT (IMPORT_EXCEL, IMPORT_MANAGE), EXPORT (EXPORT_EXCEL, EXPORT_MANAGE), PRIVILEGE_S29 (PRIVILEGE_S29_MANAGE, PRIVILEGE_S29_BATCH), STOCKCARD, FORMULA, REPORT, ADMIN (ADMIN_USERS, ADMIN_ROLES, ADMIN_PERMISSIONS)
- **Admin permissions**: Full access to all menus

## TODO — Production Hardening

ยังไม่ implement — เพิ่มเมื่อพร้อม:

1. **ย้าย secrets ออกจาก `appsettings.json`** — Connection string (SA password) และ JWT SecretKey อยู่ใน plain text ควรย้ายไป `dotnet user-secrets` (dev) หรือ environment variables (production)
2. **Rate limiting บน auth endpoints** — ป้องกัน brute force / DoS ใช้ ASP.NET Core `AddRateLimiter()` + `[EnableRateLimiting]` บน login, register, forgot-password
3. **Unit tests** — ยังไม่มี test project ควรเพิ่มอย่างน้อย service layer (AuthService, AdminService)
4. **ปิด Swagger ใน production** — ตอนนี้เปิดตลอด ควร wrap ด้วย `if (app.Environment.IsDevelopment())`
