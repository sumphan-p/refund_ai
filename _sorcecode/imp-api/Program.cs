using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using imp_api.DTOs;
using imp_api.Repositories;
using imp_api.Services;

// Register codepage encodings (required for Thai cp874 in ExcelDataReader)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Database — Scoped IDbConnection
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers
builder.Services.AddControllers();

// Custom validation error response (422 with ErrorResponse)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var fieldErrors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.First().ErrorMessage
            );

        var response = new ErrorResponse
        {
            Error = "VALIDATION_ERROR",
            Message = "One or more validation errors occurred.",
            FieldErrors = fieldErrors
        };

        return new UnprocessableEntityObjectResult(response);
    };
});

// OpenAPI / Swagger
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// CORS — allow Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ImpApp", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["ResetPassword:FrontendBaseUrl"] ?? "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// DI — Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IRoleMenuPermissionRepository, RoleMenuPermissionRepository>();
builder.Services.AddScoped<IUserMenuPermissionRepository, UserMenuPermissionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IImportExcelRepository, ImportExcelRepository>();
builder.Services.AddScoped<IExportExcelRepository, ExportExcelRepository>();
builder.Services.AddScoped<IBomM29Repository, BomM29Repository>();
builder.Services.AddScoped<IBomBoiRepository, BomBoiRepository>();
builder.Services.AddScoped<IStockLotRepository, StockLotRepository>();
builder.Services.AddScoped<IStockCuttingRepository, StockCuttingRepository>();

// DI — Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IImportExcelService, ImportExcelService>();
builder.Services.AddScoped<IExportExcelService, ExportExcelService>();
builder.Services.AddScoped<IImportManageService, ImportManageService>();
builder.Services.AddScoped<IExportManageService, ExportManageService>();
builder.Services.AddScoped<IFormulaM29Service, FormulaM29Service>();
builder.Services.AddScoped<IFormulaBoiService, FormulaBoiService>();
builder.Services.AddScoped<IPrivilege19TvisService, Privilege19TvisService>();

// Background — cleanup expired tokens every 6 hours
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var message = "An unexpected error occurred. Please try again later.";

        if (app.Environment.IsDevelopment())
        {
            var exFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exFeature?.Error is not null)
                message = $"{exFeature.Error.GetType().Name}: {exFeature.Error.Message}";
        }

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = "INTERNAL_ERROR",
            Message = message
        });
    });
});

// Swagger (dev only for mapping, available always for now)
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "imp-api v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("ImpApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
