# Implementation Plan: Login & Authorization System

## Overview

ระบบ Authentication & Authorization สำหรับ imp-api โดยใช้ต้นแบบจาก `_phototype/next_api`
รองรับ Login, Register, Remember Me, Reset Password, Change Password
พร้อมระบบกำหนดสิทธิ์การใช้งานตามเมนู (Role-Based Access Control)

---

## Phase 1: Database Schema Design

Database: `IMP_DB` | Schema: `imp`

### Tables

#### 1. imp.Users — ผู้ใช้งาน

| Column       | Type                 | Constraint              |
|--------------|----------------------|-------------------------|
| Id           | UNIQUEIDENTIFIER     | PK, DEFAULT NEWSEQUENTIALID() |
| UserName     | NVARCHAR(256)        | UNIQUE, NOT NULL        |
| PasswordHash | NVARCHAR(512)        | NOT NULL                |
| DisplayName  | NVARCHAR(100)        | NOT NULL                |
| Email        | NVARCHAR(256)        | NULL                    |
| IsActive     | BIT                  | DEFAULT 1               |
| CreatedAt    | DATETIME2            | DEFAULT SYSUTCDATETIME() |
| UpdatedAt    | DATETIME2            | DEFAULT SYSUTCDATETIME() |

#### 2. imp.Roles — บทบาท

| Column      | Type                 | Constraint              |
|-------------|----------------------|-------------------------|
| Id          | UNIQUEIDENTIFIER     | PK, DEFAULT NEWSEQUENTIALID() |
| RoleName    | NVARCHAR(100)        | UNIQUE, NOT NULL        |
| Description | NVARCHAR(256)        | NULL                    |
| CreatedAt   | DATETIME2            | DEFAULT SYSUTCDATETIME() |

#### 3. imp.UserRoles — ผู้ใช้ ↔ บทบาท (M:N)

| Column | Type             | Constraint         |
|--------|------------------|--------------------|
| UserId | UNIQUEIDENTIFIER | FK → Users, PK     |
| RoleId | UNIQUEIDENTIFIER | FK → Roles, PK     |

#### 4. imp.Menus — เมนู / หน้าในระบบ

| Column    | Type             | Constraint              |
|-----------|------------------|-------------------------|
| Id        | INT IDENTITY     | PK                      |
| MenuCode  | NVARCHAR(50)     | UNIQUE, NOT NULL        |
| MenuName  | NVARCHAR(100)    | NOT NULL (ชื่อภาษาไทย)  |
| ParentId  | INT              | FK → self, NULL         |
| Route     | NVARCHAR(256)    | NULL (frontend route)   |
| Icon      | NVARCHAR(50)     | NULL (lucide icon name) |
| SortOrder | INT              | DEFAULT 0               |
| IsActive  | BIT              | DEFAULT 1               |

#### 5. imp.RoleMenuPermissions — สิทธิ์ตามบทบาท ↔ เมนู

| Column    | Type             | Constraint              |
|-----------|------------------|-------------------------|
| Id        | INT IDENTITY     | PK                      |
| RoleId    | UNIQUEIDENTIFIER | FK → Roles              |
| MenuId    | INT              | FK → Menus              |
| Visible   | BIT              | DEFAULT 0 — มองเห็นเมนู |
| CanCreate | BIT              | DEFAULT 0 — สร้างข้อมูลได้ |
| CanEdit   | BIT              | DEFAULT 0 — แก้ไขได้     |
| CanDelete | BIT              | DEFAULT 0 — ลบได้        |
| ReadOnly  | BIT              | DEFAULT 1 — ดูอย่างเดียว |
| UNIQUE    |                  | (RoleId, MenuId)        |

#### 6. imp.RefreshTokens

| Column    | Type             | Constraint              |
|-----------|------------------|-------------------------|
| Id        | UNIQUEIDENTIFIER | PK                      |
| UserId    | UNIQUEIDENTIFIER | FK → Users (CASCADE)    |
| Token     | NVARCHAR(512)    | NOT NULL                |
| ExpiresAt | DATETIME2        | NOT NULL                |
| CreatedAt | DATETIME2        | DEFAULT SYSUTCDATETIME() |
| RevokedAt | DATETIME2        | NULL                    |

#### 7. imp.PasswordResetTokens

| Column    | Type             | Constraint              |
|-----------|------------------|-------------------------|
| Id        | UNIQUEIDENTIFIER | PK                      |
| UserId    | UNIQUEIDENTIFIER | FK → Users (CASCADE)    |
| Token     | NVARCHAR(256)    | NOT NULL                |
| ExpiresAt | DATETIME2        | NOT NULL                |
| CreatedAt | DATETIME2        | DEFAULT SYSUTCDATETIME() |
| UsedAt    | DATETIME2        | NULL                    |

---

## Phase 2: Backend Project Structure

นำโครงสร้างจาก `_phototype/next_api` มาปรับใช้ใน `imp-api/`

```
imp-api/
├── Controllers/
│   ├── AuthController.cs         — Login, Register, Refresh, Logout,
│   │                               ForgotPassword, ResetPassword, ChangePassword
│   ├── UsersController.cs        — CRUD ผู้ใช้ (Admin)
│   ├── RolesController.cs        — CRUD บทบาท (Admin)
│   └── MenusController.cs        — จัดการเมนู + กำหนดสิทธิ์ (Admin)
├── Services/
│   ├── IAuthService.cs / AuthService.cs
│   ├── IJwtService.cs / JwtService.cs
│   ├── IEmailService.cs / EmailService.cs
│   ├── IUserService.cs / UserService.cs
│   ├── IRoleService.cs / RoleService.cs
│   └── IMenuService.cs / MenuService.cs
├── Repositories/
│   ├── IUserRepository.cs / UserRepository.cs
│   ├── IRefreshTokenRepository.cs / RefreshTokenRepository.cs
│   ├── IPasswordResetRepository.cs / PasswordResetRepository.cs
│   ├── IRoleRepository.cs / RoleRepository.cs
│   └── IMenuRepository.cs / MenuRepository.cs
├── Models/
│   ├── User.cs
│   ├── Role.cs
│   ├── Menu.cs
│   ├── UserRole.cs
│   ├── RoleMenuPermission.cs
│   ├── RefreshToken.cs
│   └── PasswordResetToken.cs
├── DTOs/
│   ├── Auth/
│   │   ├── LoginRequest.cs
│   │   ├── RegisterRequest.cs
│   │   ├── AuthResponse.cs
│   │   ├── RefreshRequest.cs
│   │   ├── LogoutRequest.cs
│   │   ├── ForgotPasswordRequest.cs
│   │   ├── ResetPasswordRequest.cs
│   │   ├── ChangePasswordRequest.cs
│   │   ├── ErrorResponse.cs
│   │   └── MessageResponse.cs
│   ├── User/
│   │   ├── CreateUserRequest.cs
│   │   ├── UpdateUserRequest.cs
│   │   └── UserDto.cs
│   ├── Role/
│   │   ├── CreateRoleRequest.cs
│   │   ├── RoleDto.cs
│   │   └── RolePermissionDto.cs
│   └── Menu/
│       ├── MenuDto.cs
│       └── MenuPermissionRequest.cs
├── Helpers/
│   └── PasswordHasher.cs         — BCrypt (work factor 12)
└── SqlScripts/
    ├── 001_CreateSchema.sql
    ├── 002_CreateTables.sql
    └── 003_SeedData.sql          — Admin role + default admin user
```

---

## Phase 3: API Endpoints

### Auth (Public / Authenticated)

| Method | Endpoint                     | Auth     | Description              |
|--------|------------------------------|----------|--------------------------|
| POST   | `/api/auth/login`            | Public   | เข้าสู่ระบบ (รองรับ remember me) |
| POST   | `/api/auth/register`         | Public   | สมัครสมาชิก               |
| POST   | `/api/auth/refresh`          | Public   | ต่ออายุ token             |
| POST   | `/api/auth/logout`           | Auth     | ออกจากระบบ                |
| POST   | `/api/auth/forgot-password`  | Public   | ขอ reset password        |
| POST   | `/api/auth/reset-password`   | Public   | ตั้งรหัสผ่านใหม่           |
| POST   | `/api/auth/change-password`  | Auth     | เปลี่ยนรหัสผ่าน (ต้องใส่รหัสเดิม) |
| GET    | `/api/auth/me`               | Auth     | ข้อมูล user + permissions |

### User Management (Admin only)

| Method | Endpoint              | Description        |
|--------|-----------------------|--------------------|
| GET    | `/api/users`          | รายการผู้ใช้ทั้งหมด   |
| GET    | `/api/users/{id}`     | ข้อมูลผู้ใช้         |
| POST   | `/api/users`          | สร้างผู้ใช้ใหม่      |
| PUT    | `/api/users/{id}`     | แก้ไขผู้ใช้          |
| DELETE | `/api/users/{id}`     | ลบ/ปิดการใช้งานผู้ใช้ |

### Role Management (Admin only)

| Method | Endpoint              | Description        |
|--------|-----------------------|--------------------|
| GET    | `/api/roles`          | รายการบทบาททั้งหมด   |
| POST   | `/api/roles`          | สร้างบทบาทใหม่      |
| PUT    | `/api/roles/{id}`     | แก้ไขบทบาท          |
| DELETE | `/api/roles/{id}`     | ลบบทบาท            |

### Menu & Permission Management (Admin only)

| Method | Endpoint                          | Description          |
|--------|-----------------------------------|----------------------|
| GET    | `/api/menus`                      | รายการเมนูทั้งหมด (tree) |
| PUT    | `/api/menus/{id}`                 | แก้ไขเมนู             |
| GET    | `/api/menus/permissions/{roleId}` | ดูสิทธิ์ของบทบาท       |
| PUT    | `/api/menus/permissions/{roleId}` | กำหนดสิทธิ์ให้บทบาท    |

---

## Phase 4: Remember Me

- **remember = true** → RefreshToken หมดอายุ **30 วัน**
- **remember = false** → RefreshToken หมดอายุ **7 วัน**
- AccessToken คงเดิมที่ **15 นาที** ทั้ง 2 กรณี

---

## Phase 5: Security (จาก Prototype)

- **Password Hashing**: BCrypt with work factor 12
- **JWT**: HMAC-SHA256, AccessToken 15 min, claims: sub, userName, displayName, roles
- **Refresh Token Rotation**: revoke token เก่าทุกครั้งที่ refresh
- **Password Reset**: GUID token, 1 ชั่วโมง, ใช้ได้ครั้งเดียว, revoke token ทั้งหมดเมื่อ reset สำเร็จ
- **Username Enumeration Prevention**: forgot-password ตอบ 200 เสมอ
- **SQL Injection Prevention**: Dapper parameterized queries ทุก query

---

## Phase 6: Seed Data

```
Role: Admin       — full access ทุกเมนู (Visible, CanCreate, CanEdit, CanDelete)
Role: User        — ตามที่ Admin กำหนด
User: admin / P@ssw0rd  — role = Admin
```

---

## Implementation Order

```
Step 1 → สร้าง SQL Scripts (schema + tables + indexes + seed)
Step 2 → สร้าง Models + DTOs
Step 3 → สร้าง Repositories (Dapper)
Step 4 → สร้าง Services (business logic)
Step 5 → สร้าง Controllers + JWT config ใน Program.cs
Step 6 → ทดสอบ API ผ่าน Swagger / .http file
```

---

## Key Packages Required (เพิ่มจาก prototype)

| Package                                         | Version  | Purpose           |
|--------------------------------------------------|----------|-------------------|
| BCrypt.Net-Next                                  | 4.1.0    | Password hashing  |
| Microsoft.AspNetCore.Authentication.JwtBearer    | 10.0.3   | JWT authentication |
| System.IdentityModel.Tokens.Jwt                  | 8.16.0   | JWT token creation |
| MailKit                                          | 4.15.0   | Email sending     |

---

## Reference

- Prototype source: `_phototype/next_api/`
- Target project: `_sorcecode/imp-api/`
- Database: `IMP_DB` at `192.168.10.131\KPIDATAWH`
