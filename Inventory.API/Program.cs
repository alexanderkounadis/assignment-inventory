using Inventory.API;
using Inventory.API.Background;
using Inventory.API.Seed;
using Inventory.API.Services;
using Inventory.API.Tenancy;
using Inventory.Domain.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Tenant header
    c.AddSecurityDefinition("Tenant", new OpenApiSecurityScheme
    {
        Name = "X-Tenant-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Tenant id (GUID). Example: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Tenant" }
            },
            Array.Empty<string>()
        }
    });

    // JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY the JWT token (no 'Bearer ' prefix)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Auth
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Tenancy
builder.Services.AddScoped<TenantProvider>();
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());
builder.Services.AddScoped<TenantResolutionMiddleware>();

builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Inventory.Infrastructure.Auditing.AuditSaveChangesInterceptor>();

// Db
builder.Services.AddDbContext<InventoryDbContext>((sp, opt) => {
    opt.UseSqlite("Data Source=inventory.db");
    opt.AddInterceptors(sp.GetRequiredService<Inventory.Infrastructure.Auditing.AuditSaveChangesInterceptor>());
    });

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services.Configure<LowStockJobOptions>(builder.Configuration.GetSection("LowStockJob"));
builder.Services.AddHostedService<LowStockReportJob>();

var app = builder.Build();

// Seed early (optional: only dev)
if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);
}

// Swagger (dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

