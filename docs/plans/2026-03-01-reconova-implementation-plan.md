# Reconova Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a B2B SaaS multi-tenant reconnaissance and compliance platform where businesses register, scan their domains for vulnerabilities, and receive compliance readiness reports.

**Architecture:** Modular monolith with scan worker separation. A single .NET API process hosts both the Control Plane (auth, tenants, billing, feature flags) and Tenant Module (domains, scans, workflows, compliance). Scan Workers run as separate processes consuming from a Redis queue. SvelteKit serves the frontend. Database-per-tenant isolation with PostgreSQL.

**Tech Stack:** .NET 9 (ASP.NET Core), SvelteKit, PostgreSQL, Redis, Stripe, EF Core, Docker

**Design doc:** `docs/plans/2026-03-01-reconova-prd-design.md`

---

## Solution Structure

```
Reconova.sln
├── src/
│   ├── Reconova.Api/                    # ASP.NET Core Web API host (startup, middleware, DI)
│   ├── Reconova.ControlPlane/           # Control plane services (tenants, auth, billing, flags)
│   ├── Reconova.ControlPlane.Data/      # EF Core DbContext + entities for control DB
│   ├── Reconova.Tenant/                 # Tenant module services (domains, scans, workflows)
│   ├── Reconova.Tenant.Data/            # EF Core DbContext + entities for tenant DBs
│   ├── Reconova.ScanWorker/             # Background worker host (consumes Redis queue)
│   ├── Reconova.ScanEngine/             # Scan pipeline, tool adapters, recon modules
│   └── Reconova.Shared/                 # Shared DTOs, interfaces, enums, constants
├── tests/
│   ├── Reconova.ControlPlane.Tests/     # Unit tests for control plane
│   ├── Reconova.Tenant.Tests/           # Unit tests for tenant module
│   ├── Reconova.ScanEngine.Tests/       # Unit tests for scan engine
│   └── Reconova.Integration.Tests/      # Integration tests (real DB, Redis)
├── frontend/                            # SvelteKit application
├── docker/
│   └── docker-compose.yml               # Postgres, Redis, app containers
└── docs/plans/                          # Design docs and plans
```

---

## Phase 1: Project Scaffolding & Infrastructure

### Task 1.1: Initialize Git Repository

**Files:**
- Create: `.gitignore`
- Create: `.editorconfig`

**Step 1: Initialize git repo**

Run:
```bash
cd /Users/blackkspydo/side-projects/reconova
git init
```

**Step 2: Create .gitignore**

Create `.gitignore` with standard .NET, Node.js, and IDE exclusions:
```
## .NET
bin/
obj/
*.user
*.suo
*.cache
*.dll
*.exe
*.pdb
out/
publish/

## Node / SvelteKit
frontend/node_modules/
frontend/.svelte-kit/
frontend/build/

## IDE
.vs/
.vscode/
.idea/
*.swp

## Environment
.env
.env.*
!.env.example
appsettings.Development.json

## OS
.DS_Store
Thumbs.db

## Docker
docker/data/

## OpenSpec (keep)
!openspec/
```

**Step 3: Create .editorconfig**

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{json,yml,yaml,svelte,ts,js,css}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

**Step 4: Commit**

```bash
git add .gitignore .editorconfig
git commit -m "chore: initialize git repo with .gitignore and .editorconfig"
```

---

### Task 1.2: Create .NET Solution and Projects

**Files:**
- Create: `Reconova.sln`
- Create: `src/Reconova.Api/Reconova.Api.csproj`
- Create: `src/Reconova.Shared/Reconova.Shared.csproj`
- Create: `src/Reconova.ControlPlane/Reconova.ControlPlane.csproj`
- Create: `src/Reconova.ControlPlane.Data/Reconova.ControlPlane.Data.csproj`
- Create: `src/Reconova.Tenant/Reconova.Tenant.csproj`
- Create: `src/Reconova.Tenant.Data/Reconova.Tenant.Data.csproj`
- Create: `src/Reconova.ScanWorker/Reconova.ScanWorker.csproj`
- Create: `src/Reconova.ScanEngine/Reconova.ScanEngine.csproj`
- Create: `tests/Reconova.ControlPlane.Tests/Reconova.ControlPlane.Tests.csproj`
- Create: `tests/Reconova.Tenant.Tests/Reconova.Tenant.Tests.csproj`
- Create: `tests/Reconova.ScanEngine.Tests/Reconova.ScanEngine.Tests.csproj`
- Create: `tests/Reconova.Integration.Tests/Reconova.Integration.Tests.csproj`

**Step 1: Create the solution and all projects**

```bash
# Solution
dotnet new sln -n Reconova

# API host
dotnet new webapi -n Reconova.Api -o src/Reconova.Api --no-openapi
dotnet sln add src/Reconova.Api

# Shared library
dotnet new classlib -n Reconova.Shared -o src/Reconova.Shared
dotnet sln add src/Reconova.Shared

# Control Plane
dotnet new classlib -n Reconova.ControlPlane -o src/Reconova.ControlPlane
dotnet new classlib -n Reconova.ControlPlane.Data -o src/Reconova.ControlPlane.Data
dotnet sln add src/Reconova.ControlPlane src/Reconova.ControlPlane.Data

# Tenant
dotnet new classlib -n Reconova.Tenant -o src/Reconova.Tenant
dotnet new classlib -n Reconova.Tenant.Data -o src/Reconova.Tenant.Data
dotnet sln add src/Reconova.Tenant src/Reconova.Tenant.Data

# Scan Worker + Engine
dotnet new worker -n Reconova.ScanWorker -o src/Reconova.ScanWorker
dotnet new classlib -n Reconova.ScanEngine -o src/Reconova.ScanEngine
dotnet sln add src/Reconova.ScanWorker src/Reconova.ScanEngine

# Test projects
dotnet new xunit -n Reconova.ControlPlane.Tests -o tests/Reconova.ControlPlane.Tests
dotnet new xunit -n Reconova.Tenant.Tests -o tests/Reconova.Tenant.Tests
dotnet new xunit -n Reconova.ScanEngine.Tests -o tests/Reconova.ScanEngine.Tests
dotnet new xunit -n Reconova.Integration.Tests -o tests/Reconova.Integration.Tests
dotnet sln add tests/Reconova.ControlPlane.Tests tests/Reconova.Tenant.Tests tests/Reconova.ScanEngine.Tests tests/Reconova.Integration.Tests
```

**Step 2: Add project references**

```bash
# API references everything
dotnet add src/Reconova.Api reference src/Reconova.Shared src/Reconova.ControlPlane src/Reconova.ControlPlane.Data src/Reconova.Tenant src/Reconova.Tenant.Data

# Control Plane references
dotnet add src/Reconova.ControlPlane reference src/Reconova.Shared src/Reconova.ControlPlane.Data
dotnet add src/Reconova.ControlPlane.Data reference src/Reconova.Shared

# Tenant references
dotnet add src/Reconova.Tenant reference src/Reconova.Shared src/Reconova.Tenant.Data
dotnet add src/Reconova.Tenant.Data reference src/Reconova.Shared

# Scan Worker + Engine
dotnet add src/Reconova.ScanWorker reference src/Reconova.Shared src/Reconova.ScanEngine src/Reconova.Tenant.Data src/Reconova.ControlPlane.Data
dotnet add src/Reconova.ScanEngine reference src/Reconova.Shared

# Test references
dotnet add tests/Reconova.ControlPlane.Tests reference src/Reconova.ControlPlane src/Reconova.ControlPlane.Data src/Reconova.Shared
dotnet add tests/Reconova.Tenant.Tests reference src/Reconova.Tenant src/Reconova.Tenant.Data src/Reconova.Shared
dotnet add tests/Reconova.ScanEngine.Tests reference src/Reconova.ScanEngine src/Reconova.Shared
dotnet add tests/Reconova.Integration.Tests reference src/Reconova.Api src/Reconova.ControlPlane src/Reconova.ControlPlane.Data src/Reconova.Tenant src/Reconova.Tenant.Data src/Reconova.Shared
```

**Step 3: Add NuGet packages**

```bash
# EF Core for data projects
dotnet add src/Reconova.ControlPlane.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Reconova.ControlPlane.Data package Microsoft.EntityFrameworkCore.Design
dotnet add src/Reconova.Tenant.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Reconova.Tenant.Data package Microsoft.EntityFrameworkCore.Design

# Redis
dotnet add src/Reconova.Shared package StackExchange.Redis

# Auth
dotnet add src/Reconova.ControlPlane package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Reconova.ControlPlane package BCrypt.Net-Next
dotnet add src/Reconova.ControlPlane package OtpNet

# Stripe
dotnet add src/Reconova.ControlPlane package Stripe.net

# API host
dotnet add src/Reconova.Api package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Reconova.Api package StackExchange.Redis

# Test packages
dotnet add tests/Reconova.ControlPlane.Tests package Moq
dotnet add tests/Reconova.ControlPlane.Tests package FluentAssertions
dotnet add tests/Reconova.Tenant.Tests package Moq
dotnet add tests/Reconova.Tenant.Tests package FluentAssertions
dotnet add tests/Reconova.ScanEngine.Tests package Moq
dotnet add tests/Reconova.ScanEngine.Tests package FluentAssertions
dotnet add tests/Reconova.Integration.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Reconova.Integration.Tests package Testcontainers.PostgreSql
dotnet add tests/Reconova.Integration.Tests package Moq
dotnet add tests/Reconova.Integration.Tests package FluentAssertions
```

**Step 4: Verify solution builds**

Run: `dotnet build Reconova.sln`
Expected: Build succeeded with 0 errors

**Step 5: Commit**

```bash
git add -A
git commit -m "chore: scaffold .NET solution with modular monolith structure"
```

---

### Task 1.3: Docker Compose for Local Development

**Files:**
- Create: `docker/docker-compose.yml`
- Create: `.env.example`

**Step 1: Create docker-compose.yml**

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_USER: reconova
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-reconova_dev}
      POSTGRES_DB: reconova_control
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./init-scripts:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U reconova"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  pgdata:
  redisdata:
```

**Step 2: Create init script for template DB**

Create `docker/init-scripts/01-create-template-db.sql`:
```sql
-- Create the template database that will be cloned for each tenant
CREATE DATABASE reconova_template;
```

**Step 3: Create .env.example**

```env
# Database
POSTGRES_PASSWORD=reconova_dev
DATABASE_HOST=localhost
DATABASE_PORT=5432
DATABASE_USER=reconova
DATABASE_PASSWORD=reconova_dev
CONTROL_DATABASE=reconova_control
TEMPLATE_DATABASE=reconova_template

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379

# JWT
JWT_SECRET=change-this-to-a-secure-random-string-at-least-32-chars
JWT_ISSUER=reconova
JWT_AUDIENCE=reconova-api
JWT_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# Stripe
STRIPE_SECRET_KEY=sk_test_xxx
STRIPE_WEBHOOK_SECRET=whsec_xxx

# Encryption
API_KEY_ENCRYPTION_KEY=change-this-to-a-32-byte-hex-string
```

**Step 4: Start services and verify**

Run: `cd docker && docker compose up -d && docker compose ps`
Expected: Both postgres and redis containers are healthy

**Step 5: Commit**

```bash
git add docker/ .env.example
git commit -m "chore: add Docker Compose with Postgres and Redis for local dev"
```

---

### Task 1.4: Configure API Host with appsettings

**Files:**
- Modify: `src/Reconova.Api/appsettings.json`
- Create: `src/Reconova.Api/appsettings.Development.json`

**Step 1: Update appsettings.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "ControlDatabase": "Host=localhost;Port=5432;Database=reconova_control;Username=reconova;Password=reconova_dev",
    "TemplateDatabase": "Host=localhost;Port=5432;Database=reconova_template;Username=reconova;Password=reconova_dev"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "reconova",
    "Audience": "reconova-api",
    "ExpiryMinutes": 15,
    "RefreshExpiryDays": 7
  },
  "Stripe": {
    "SecretKey": "",
    "WebhookSecret": ""
  }
}
```

**Step 2: Create Development overrides**

`appsettings.Development.json` — already in .gitignore, so this won't be committed. Create it locally with real dev secrets.

**Step 3: Verify API starts**

Run: `dotnet run --project src/Reconova.Api`
Expected: Application starts on https://localhost:5001 or similar

**Step 4: Commit**

```bash
git add src/Reconova.Api/appsettings.json
git commit -m "chore: configure API host with connection strings and settings"
```

---

## Phase 2: Control Database & Multi-Tenancy Core

### Task 2.1: Control DB Entities — Tenants

**Files:**
- Create: `src/Reconova.Shared/Enums/TenantStatus.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/Tenant.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/TenantDatabase.cs`
- Create: `src/Reconova.ControlPlane.Data/ControlDbContext.cs`

**Step 1: Write failing test — Tenant entity has required properties**

Create `tests/Reconova.ControlPlane.Tests/Entities/TenantEntityTests.cs`:
```csharp
using FluentAssertions;
using Reconova.ControlPlane.Data.Entities;
using Reconova.Shared.Enums;

namespace Reconova.ControlPlane.Tests.Entities;

public class TenantEntityTests
{
    [Fact]
    public void Tenant_ShouldHaveRequiredProperties()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Acme Corp",
            Slug = "acme-corp",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme-corp");
        tenant.Status.Should().Be(TenantStatus.Active);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "TenantEntityTests" -v n`
Expected: FAIL — types don't exist yet

**Step 3: Create TenantStatus enum**

`src/Reconova.Shared/Enums/TenantStatus.cs`:
```csharp
namespace Reconova.Shared.Enums;

public enum TenantStatus
{
    Active,
    Suspended,
    Deactivated,
    Provisioning
}
```

**Step 4: Create Tenant entity**

`src/Reconova.ControlPlane.Data/Entities/Tenant.cs`:
```csharp
using Reconova.Shared.Enums;

namespace Reconova.ControlPlane.Data.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public Guid? PlanId { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Step 5: Create TenantDatabase entity**

`src/Reconova.ControlPlane.Data/Entities/TenantDatabase.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class TenantDatabase
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string ConnectionString { get; set; } = string.Empty;
    public string Status { get; set; } = "provisioning";
    public string TemplateVersion { get; set; } = string.Empty;
}
```

**Step 6: Create ControlDbContext**

`src/Reconova.ControlPlane.Data/ControlDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Reconova.ControlPlane.Data.Entities;

namespace Reconova.ControlPlane.Data;

public class ControlDbContext : DbContext
{
    public ControlDbContext(DbContextOptions<ControlDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantDatabase> TenantDatabases => Set<TenantDatabase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<TenantDatabase>(entity =>
        {
            entity.ToTable("tenant_databases");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Tenant).WithOne().HasForeignKey<TenantDatabase>(e => e.TenantId);
            entity.Property(e => e.ConnectionString).IsRequired();
        });
    }
}
```

**Step 7: Run test to verify it passes**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "TenantEntityTests" -v n`
Expected: PASS

**Step 8: Commit**

```bash
git add src/Reconova.Shared/Enums/ src/Reconova.ControlPlane.Data/Entities/ src/Reconova.ControlPlane.Data/ControlDbContext.cs tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add Tenant and TenantDatabase entities with ControlDbContext"
```

---

### Task 2.2: Control DB Entities — Users and Auth

**Files:**
- Create: `src/Reconova.ControlPlane.Data/Entities/User.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Entities/UserEntityTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Reconova.ControlPlane.Data.Entities;

namespace Reconova.ControlPlane.Tests.Entities;

public class UserEntityTests
{
    [Fact]
    public void User_ShouldHaveRequiredProperties()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "user@acme.com",
            PasswordHash = "hashed",
            Role = "tenant_owner",
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow
        };

        user.Email.Should().Be("user@acme.com");
        user.Role.Should().Be("tenant_owner");
    }
}
```

**Step 2: Run test — expect FAIL**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "UserEntityTests" -v n`

**Step 3: Create User entity**

`src/Reconova.ControlPlane.Data/Entities/User.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string Role { get; set; } = "tenant_owner";
    public DateTime CreatedAt { get; set; }
}
```

**Step 4: Add User to ControlDbContext**

Add to `ControlDbContext.cs`:
```csharp
public DbSet<User> Users => Set<User>();
```

And in `OnModelCreating`:
```csharp
modelBuilder.Entity<User>(entity =>
{
    entity.ToTable("users");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
    entity.HasIndex(e => e.Email).IsUnique();
    entity.Property(e => e.PasswordHash).IsRequired();
    entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
    entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
});
```

**Step 5: Run test — expect PASS**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "UserEntityTests" -v n`

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane.Data/ tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add User entity with 2FA fields to control DB"
```

---

### Task 2.3: Control DB Entities — Migration Tracking

**Files:**
- Create: `src/Reconova.ControlPlane.Data/Entities/BaseMigration.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/TenantMigration.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/MigrationScript.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Entities/MigrationEntityTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Reconova.ControlPlane.Data.Entities;

namespace Reconova.ControlPlane.Tests.Entities;

public class MigrationEntityTests
{
    [Fact]
    public void TenantMigration_ShouldTrackTypeAndStatus()
    {
        var migration = new TenantMigration
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Version = "20260301_001",
            Name = "add_custom_field",
            Type = "tenant_specific",
            Status = "applied",
            AppliedAt = DateTime.UtcNow
        };

        migration.Type.Should().Be("tenant_specific");
        migration.Status.Should().Be("applied");
    }

    [Fact]
    public void MigrationScript_ShouldStoreUpAndDownScripts()
    {
        var script = new MigrationScript
        {
            Id = Guid.NewGuid(),
            MigrationId = Guid.NewGuid(),
            UpScript = "ALTER TABLE domains ADD COLUMN custom TEXT;",
            DownScript = "ALTER TABLE domains DROP COLUMN custom;",
            Checksum = "abc123"
        };

        script.UpScript.Should().Contain("ADD COLUMN");
        script.DownScript.Should().Contain("DROP COLUMN");
    }
}
```

**Step 2: Run test — expect FAIL**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "MigrationEntityTests" -v n`

**Step 3: Create migration entities**

`src/Reconova.ControlPlane.Data/Entities/BaseMigration.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class BaseMigration
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScriptHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

`src/Reconova.ControlPlane.Data/Entities/TenantMigration.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class TenantMigration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "base"; // 'base' | 'tenant_specific'
    public string ScriptHash { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
    public string? AppliedBy { get; set; }
    public string Status { get; set; } = "applied"; // 'applied' | 'rolled_back' | 'failed'
}
```

`src/Reconova.ControlPlane.Data/Entities/MigrationScript.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class MigrationScript
{
    public Guid Id { get; set; }
    public Guid MigrationId { get; set; }
    public TenantMigration Migration { get; set; } = null!;
    public string UpScript { get; set; } = string.Empty;
    public string DownScript { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
}
```

**Step 4: Add to ControlDbContext**

Add DbSets:
```csharp
public DbSet<BaseMigration> BaseMigrations => Set<BaseMigration>();
public DbSet<TenantMigration> TenantMigrations => Set<TenantMigration>();
public DbSet<MigrationScript> MigrationScripts => Set<MigrationScript>();
```

Add entity configs in `OnModelCreating`:
```csharp
modelBuilder.Entity<BaseMigration>(entity =>
{
    entity.ToTable("base_migrations");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
    entity.HasIndex(e => e.Version).IsUnique();
});

modelBuilder.Entity<TenantMigration>(entity =>
{
    entity.ToTable("tenant_migrations");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
    entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
    entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
    entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
    entity.HasIndex(e => new { e.TenantId, e.Version }).IsUnique();
});

modelBuilder.Entity<MigrationScript>(entity =>
{
    entity.ToTable("migration_scripts");
    entity.HasKey(e => e.Id);
    entity.HasOne(e => e.Migration).WithOne().HasForeignKey<MigrationScript>(e => e.MigrationId);
});
```

**Step 5: Run test — expect PASS**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "MigrationEntityTests" -v n`

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane.Data/ tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add migration tracking entities (base, tenant-specific, scripts)"
```

---

### Task 2.4: Control DB Entities — Billing

**Files:**
- Create: `src/Reconova.ControlPlane.Data/Entities/SubscriptionPlan.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/TenantSubscription.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/CreditTransaction.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/CreditPack.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/ScanStepPricing.cs`
- Modify: `src/Reconova.ControlPlane.Data/ControlDbContext.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Entities/BillingEntityTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Reconova.ControlPlane.Data.Entities;

namespace Reconova.ControlPlane.Tests.Entities;

public class BillingEntityTests
{
    [Fact]
    public void CreditTransaction_ShouldLogConsumption()
    {
        var tx = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Amount = -5,
            Type = "consumption",
            Description = "vuln_scanning x 1 domain",
            CreatedAt = DateTime.UtcNow
        };

        tx.Amount.Should().BeNegative();
        tx.Type.Should().Be("consumption");
    }

    [Fact]
    public void ScanStepPricing_ShouldVaryByTier()
    {
        var pricing = new ScanStepPricing
        {
            Id = Guid.NewGuid(),
            CheckType = "vulnerability_scanning",
            TierId = Guid.NewGuid(),
            CreditsPerDomain = 3
        };

        pricing.CreditsPerDomain.Should().Be(3);
    }
}
```

**Step 2: Run test — expect FAIL**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "BillingEntityTests" -v n`

**Step 3: Create all billing entities**

`src/Reconova.ControlPlane.Data/Entities/SubscriptionPlan.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StripePriceId { get; set; }
    public int MonthlyCredits { get; set; }
    public int MaxDomains { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceAnnual { get; set; }
    public string FeaturesJson { get; set; } = "{}";
    public string Status { get; set; } = "active";
}
```

`src/Reconova.ControlPlane.Data/Entities/TenantSubscription.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class TenantSubscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public int CreditsRemaining { get; set; }
    public int CreditsUsedThisPeriod { get; set; }
}
```

`src/Reconova.ControlPlane.Data/Entities/CreditTransaction.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class CreditTransaction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int Amount { get; set; }
    public string Type { get; set; } = string.Empty; // allotment, consumption, purchase, refund
    public Guid? ScanJobId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

`src/Reconova.ControlPlane.Data/Entities/CreditPack.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class CreditPack
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StripePriceId { get; set; }
    public int Credits { get; set; }
    public decimal Price { get; set; }
}
```

`src/Reconova.ControlPlane.Data/Entities/ScanStepPricing.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class ScanStepPricing
{
    public Guid Id { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public Guid TierId { get; set; }
    public SubscriptionPlan Tier { get; set; } = null!;
    public int CreditsPerDomain { get; set; }
    public string? Description { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Step 4: Add to ControlDbContext — DbSets and entity configs**

Add DbSets and EF configuration for all five entities with appropriate table names, keys, indexes, and relationships. Map `TenantSubscription` to `tenant_subscriptions`, `CreditTransaction` to `credit_transactions`, etc.

**Step 5: Run test — expect PASS**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "BillingEntityTests" -v n`

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane.Data/ tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add billing entities (plans, subscriptions, credits, pricing)"
```

---

### Task 2.5: Control DB Entities — Feature Flags

**Files:**
- Create: `src/Reconova.ControlPlane.Data/Entities/FeatureFlag.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/PlanFeature.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/TenantFeatureOverride.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Entities/FeatureFlagEntityTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Reconova.ControlPlane.Data.Entities;

namespace Reconova.ControlPlane.Tests.Entities;

public class FeatureFlagEntityTests
{
    [Fact]
    public void FeatureFlag_ShouldSupportSubscriptionAndOperationalTypes()
    {
        var subFlag = new FeatureFlag { Type = "subscription", Module = "vulnerability_scanning" };
        var opsFlag = new FeatureFlag { Type = "operational", Module = "maintenance_mode" };

        subFlag.Type.Should().Be("subscription");
        opsFlag.Type.Should().Be("operational");
    }

    [Fact]
    public void TenantFeatureOverride_ShouldWinOverPlanDefault()
    {
        var ov = new TenantFeatureOverride
        {
            TenantId = Guid.NewGuid(),
            FeatureId = Guid.NewGuid(),
            Enabled = true,
            OverriddenBy = "super_admin",
            Reason = "Beta access granted"
        };

        ov.Enabled.Should().BeTrue();
        ov.OverriddenBy.Should().Be("super_admin");
    }
}
```

**Step 2: Run test — expect FAIL**

**Step 3: Create entities**

`src/Reconova.ControlPlane.Data/Entities/FeatureFlag.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class FeatureFlag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "subscription"; // subscription | operational
    public string Module { get; set; } = string.Empty;
    public bool DefaultEnabled { get; set; }
    public string? Description { get; set; }
}
```

`src/Reconova.ControlPlane.Data/Entities/PlanFeature.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class PlanFeature
{
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public Guid FeatureId { get; set; }
    public FeatureFlag Feature { get; set; } = null!;
    public bool Enabled { get; set; }
}
```

`src/Reconova.ControlPlane.Data/Entities/TenantFeatureOverride.cs`:
```csharp
namespace Reconova.ControlPlane.Data.Entities;

public class TenantFeatureOverride
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid FeatureId { get; set; }
    public FeatureFlag Feature { get; set; } = null!;
    public bool Enabled { get; set; }
    public string? OverriddenBy { get; set; }
    public string? Reason { get; set; }
}
```

**Step 4: Add to ControlDbContext with configs. PlanFeature has composite key (PlanId, FeatureId).**

**Step 5: Run test — expect PASS**

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane.Data/ tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add feature flag entities (flags, plan features, tenant overrides)"
```

---

### Task 2.6: Control DB Entities — Compliance, CVE, API Keys, Audit

**Files:**
- Create: `src/Reconova.ControlPlane.Data/Entities/ComplianceFramework.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/ComplianceControl.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/PlanComplianceAccess.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/ControlCheckMapping.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/CveEntry.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/CveFeedSource.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/PlatformApiKey.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/ApiUsageTracking.cs`
- Create: `src/Reconova.ControlPlane.Data/Entities/AuditLog.cs`
- Modify: `src/Reconova.ControlPlane.Data/ControlDbContext.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Entities/PlatformEntityTests.cs`

**Step 1: Write failing test covering key entities**

```csharp
using FluentAssertions;
using Reconova.ControlPlane.Data.Entities;

namespace Reconova.ControlPlane.Tests.Entities;

public class PlatformEntityTests
{
    [Fact]
    public void AuditLog_ShouldCaptureAllFields()
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "scan.created",
            ResourceType = "scan_job",
            IpAddress = "192.168.1.1",
            IsSuperAdmin = false,
            Timestamp = DateTime.UtcNow
        };

        log.Action.Should().Be("scan.created");
        log.IsSuperAdmin.Should().BeFalse();
    }

    [Fact]
    public void PlatformApiKey_ShouldBelongToProvider()
    {
        var key = new PlatformApiKey
        {
            Id = Guid.NewGuid(),
            Provider = "shodan",
            ApiKeyEncrypted = "encrypted_value",
            RateLimit = 100,
            MonthlyQuota = 10000,
            Status = "active"
        };

        key.Provider.Should().Be("shodan");
    }
}
```

**Step 2: Run test — expect FAIL**

**Step 3: Create all remaining control DB entities**

Create each entity class following the schema in the design doc. Each entity maps to its respective table. Key relationships:
- `ComplianceControl` belongs to `ComplianceFramework`
- `ControlCheckMapping` belongs to `ComplianceControl`
- `PlanComplianceAccess` has composite key `(PlanId, FrameworkId)`
- `ApiUsageTracking` references `PlatformApiKey`
- `AuditLog` is standalone with optional `TenantId` and `UserId`

**Step 4: Add all to ControlDbContext with DbSets and entity configs**

**Step 5: Run test — expect PASS**

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane.Data/ tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add compliance, CVE, API key, and audit entities to control DB"
```

---

### Task 2.7: Generate and Apply EF Core Migrations for Control DB

**Step 1: Install EF Core CLI tool if needed**

Run: `dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef`

**Step 2: Generate initial migration**

Run:
```bash
dotnet ef migrations add InitialControlDb \
  --project src/Reconova.ControlPlane.Data \
  --startup-project src/Reconova.Api \
  --context ControlDbContext \
  --output-dir Migrations
```
Expected: Migration files created in `src/Reconova.ControlPlane.Data/Migrations/`

**Step 3: Ensure Docker Postgres is running**

Run: `cd docker && docker compose up -d postgres`

**Step 4: Apply migration to control database**

Run:
```bash
dotnet ef database update \
  --project src/Reconova.ControlPlane.Data \
  --startup-project src/Reconova.Api \
  --context ControlDbContext
```
Expected: Tables created in `reconova_control` database

**Step 5: Verify tables exist**

Run: `docker exec -it docker-postgres-1 psql -U reconova -d reconova_control -c "\dt"`
Expected: All control DB tables listed

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane.Data/Migrations/
git commit -m "feat: add initial EF Core migration for control database"
```

---

### Task 2.8: Tenant DB Entities (Template Database)

**Files:**
- Create: `src/Reconova.Tenant.Data/Entities/Domain.cs`
- Create: `src/Reconova.Tenant.Data/Entities/Subdomain.cs`
- Create: `src/Reconova.Tenant.Data/Entities/Port.cs`
- Create: `src/Reconova.Tenant.Data/Entities/Technology.cs`
- Create: `src/Reconova.Tenant.Data/Entities/Screenshot.cs`
- Create: `src/Reconova.Tenant.Data/Entities/ScanJob.cs`
- Create: `src/Reconova.Tenant.Data/Entities/ScanResult.cs`
- Create: `src/Reconova.Tenant.Data/Entities/Vulnerability.cs`
- Create: `src/Reconova.Tenant.Data/Entities/ScanSchedule.cs`
- Create: `src/Reconova.Tenant.Data/Entities/Workflow.cs`
- Create: `src/Reconova.Tenant.Data/Entities/WorkflowTemplate.cs`
- Create: `src/Reconova.Tenant.Data/Entities/TenantComplianceSelection.cs`
- Create: `src/Reconova.Tenant.Data/Entities/ComplianceAssessment.cs`
- Create: `src/Reconova.Tenant.Data/Entities/ControlResult.cs`
- Create: `src/Reconova.Tenant.Data/Entities/VulnerabilityAlert.cs`
- Create: `src/Reconova.Tenant.Data/Entities/IntegrationConfig.cs`
- Create: `src/Reconova.Tenant.Data/Entities/NotificationRule.cs`
- Create: `src/Reconova.Tenant.Data/Entities/NotificationHistory.cs`
- Create: `src/Reconova.Tenant.Data/TenantDbContext.cs`
- Test: `tests/Reconova.Tenant.Tests/Entities/TenantDbEntityTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Reconova.Tenant.Data.Entities;

namespace Reconova.Tenant.Tests.Entities;

public class TenantDbEntityTests
{
    [Fact]
    public void Domain_ShouldHaveRequiredProperties()
    {
        var domain = new Domain
        {
            Id = Guid.NewGuid(),
            DomainName = "example.com",
            Status = "active",
            AddedBy = Guid.NewGuid()
        };

        domain.DomainName.Should().Be("example.com");
    }

    [Fact]
    public void ScanJob_ShouldTrackStatusAndWorkflow()
    {
        var job = new ScanJob
        {
            Id = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Status = "running"
        };

        job.Status.Should().Be("running");
    }
}
```

**Step 2: Run test — expect FAIL**

**Step 3: Create all tenant DB entities following the template schema in the design doc**

Each entity maps to its corresponding table. Key relationships:
- `Subdomain` → `Domain`
- `Port` → `Subdomain`
- `Technology` → `Subdomain`
- `Screenshot` → `Subdomain`
- `ScanJob` → `Domain`, `Workflow`
- `ScanResult` → `ScanJob`
- `Vulnerability` → `ScanResult`
- `VulnerabilityAlert` → `Domain`
- `ComplianceAssessment` → `ScanJob`
- `ControlResult` → `ComplianceAssessment`
- `NotificationRule` → `IntegrationConfig`
- `NotificationHistory` → `NotificationRule`

**Step 4: Create TenantDbContext with all DbSets and entity configs**

`src/Reconova.Tenant.Data/TenantDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Reconova.Tenant.Data.Entities;

namespace Reconova.Tenant.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<Subdomain> Subdomains => Set<Subdomain>();
    public DbSet<Port> Ports => Set<Port>();
    // ... all DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure all entities with table names, keys, indexes, relationships
    }
}
```

**Step 5: Run test — expect PASS**

**Step 6: Commit**

```bash
git add src/Reconova.Tenant.Data/ tests/Reconova.Tenant.Tests/
git commit -m "feat: add all tenant database entities and TenantDbContext"
```

---

### Task 2.9: Generate Tenant Template DB Migration

**Step 1: Generate migration**

Run:
```bash
dotnet ef migrations add InitialTenantTemplate \
  --project src/Reconova.Tenant.Data \
  --startup-project src/Reconova.Api \
  --context TenantDbContext \
  --output-dir Migrations
```

Note: The API startup must register `TenantDbContext` with the template DB connection string for migration generation purposes. Add a temporary registration in `Program.cs`:

```csharp
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TemplateDatabase")));
```

**Step 2: Apply migration to template database**

Run:
```bash
dotnet ef database update \
  --project src/Reconova.Tenant.Data \
  --startup-project src/Reconova.Api \
  --context TenantDbContext
```

**Step 3: Verify tables in template DB**

Run: `docker exec -it docker-postgres-1 psql -U reconova -d reconova_template -c "\dt"`
Expected: All tenant tables listed

**Step 4: Commit**

```bash
git add src/Reconova.Tenant.Data/Migrations/ src/Reconova.Api/
git commit -m "feat: add initial migration for tenant template database"
```

---

### Task 2.10: Tenant Provisioning Service

**Files:**
- Create: `src/Reconova.ControlPlane/Services/ITenantProvisioningService.cs`
- Create: `src/Reconova.ControlPlane/Services/TenantProvisioningService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/TenantProvisioningServiceTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Moq;
using Reconova.ControlPlane.Services;
using Reconova.ControlPlane.Data;

namespace Reconova.ControlPlane.Tests.Services;

public class TenantProvisioningServiceTests
{
    [Fact]
    public async Task ProvisionTenant_ShouldCreateDatabaseAndRecordConnection()
    {
        // This will be an integration test later;
        // for now test the slug generation logic
        var slug = TenantProvisioningService.GenerateSlug("Acme Corp!");
        slug.Should().Be("acme-corp");
    }

    [Fact]
    public void GenerateSlug_ShouldHandleSpecialCharacters()
    {
        TenantProvisioningService.GenerateSlug("My Company (2026)").Should().Be("my-company-2026");
        TenantProvisioningService.GenerateSlug("  Spaces  Everywhere  ").Should().Be("spaces-everywhere");
        TenantProvisioningService.GenerateSlug("UPPER-case").Should().Be("upper-case");
    }
}
```

**Step 2: Run test — expect FAIL**

**Step 3: Create interface**

`src/Reconova.ControlPlane/Services/ITenantProvisioningService.cs`:
```csharp
namespace Reconova.ControlPlane.Services;

public interface ITenantProvisioningService
{
    Task<Guid> ProvisionTenantAsync(string name, Guid userId);
    static abstract string GenerateSlug(string name);
}
```

**Step 4: Create implementation**

`src/Reconova.ControlPlane/Services/TenantProvisioningService.cs`:
```csharp
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Reconova.ControlPlane.Data;
using Reconova.ControlPlane.Data.Entities;
using Reconova.Shared.Enums;

namespace Reconova.ControlPlane.Services;

public partial class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ControlDbContext _controlDb;
    private readonly string _templateConnectionString;

    public TenantProvisioningService(
        ControlDbContext controlDb,
        string templateConnectionString)
    {
        _controlDb = controlDb;
        _templateConnectionString = templateConnectionString;
    }

    public static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = NonAlphanumericRegex().Replace(slug, "-");
        slug = MultipleDashRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        return slug;
    }

    public async Task<Guid> ProvisionTenantAsync(string name, Guid userId)
    {
        var slug = GenerateSlug(name);

        // 1. Create tenant record
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Status = TenantStatus.Provisioning,
            CreatedAt = DateTime.UtcNow
        };
        _controlDb.Tenants.Add(tenant);
        await _controlDb.SaveChangesAsync();

        // 2. Clone template database
        var dbName = $"tenant_{slug}";
        await CloneDatabaseAsync(dbName);

        // 3. Record connection
        var connString = BuildConnectionString(dbName);
        var tenantDb = new TenantDatabase
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ConnectionString = connString,
            Status = "ready",
            TemplateVersion = "1.0"
        };
        _controlDb.TenantDatabases.Add(tenantDb);

        // 4. Activate tenant
        tenant.Status = TenantStatus.Active;
        await _controlDb.SaveChangesAsync();

        return tenant.Id;
    }

    private async Task CloneDatabaseAsync(string newDbName)
    {
        // Use raw SQL to clone template DB
        // CREATE DATABASE newdb TEMPLATE reconova_template
        await using var conn = new Npgsql.NpgsqlConnection(_templateConnectionString);
        await conn.OpenAsync();
        // Must use NpgsqlCommand directly — EF doesn't support CREATE DATABASE
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{newDbName}\" TEMPLATE reconova_template";
        await cmd.ExecuteNonQueryAsync();
    }

    private string BuildConnectionString(string dbName)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_templateConnectionString)
        {
            Database = dbName
        };
        return builder.ConnectionString;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashRegex();
}
```

**Step 5: Run test — expect PASS**

Run: `dotnet test tests/Reconova.ControlPlane.Tests --filter "TenantProvisioningServiceTests" -v n`

**Step 6: Commit**

```bash
git add src/Reconova.ControlPlane/ tests/Reconova.ControlPlane.Tests/
git commit -m "feat: add tenant provisioning service with DB cloning from template"
```

---

### Task 2.11: Tenant Resolution Middleware

**Files:**
- Create: `src/Reconova.Shared/TenantContext.cs`
- Create: `src/Reconova.Api/Middleware/TenantResolutionMiddleware.cs`
- Create: `src/Reconova.Tenant.Data/TenantDbContextFactory.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Middleware/TenantResolutionTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Reconova.Shared;

namespace Reconova.ControlPlane.Tests.Middleware;

public class TenantResolutionTests
{
    [Fact]
    public void TenantContext_ShouldHoldTenantIdAndConnectionString()
    {
        var ctx = new TenantContext
        {
            TenantId = Guid.NewGuid(),
            ConnectionString = "Host=localhost;Database=tenant_acme"
        };

        ctx.TenantId.Should().NotBeEmpty();
        ctx.ConnectionString.Should().Contain("tenant_acme");
    }
}
```

**Step 2: Run test — expect FAIL**

**Step 3: Create TenantContext**

`src/Reconova.Shared/TenantContext.cs`:
```csharp
namespace Reconova.Shared;

public class TenantContext
{
    public Guid TenantId { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
```

**Step 4: Create TenantResolutionMiddleware**

`src/Reconova.Api/Middleware/TenantResolutionMiddleware.cs`:
```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Reconova.ControlPlane.Data;
using Reconova.Shared;
using StackExchange.Redis;

namespace Reconova.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ControlDbContext controlDb,
        TenantContext tenantContext, IConnectionMultiplexer redis)
    {
        // Extract tenant ID from JWT claims
        var tenantClaim = context.User.FindFirstValue("tenant_id");
        if (tenantClaim == null || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            await _next(context);
            return;
        }

        // Check Redis cache first
        var db = redis.GetDatabase();
        var cachedConn = await db.StringGetAsync($"tenant:conn:{tenantId}");

        string connectionString;
        if (cachedConn.HasValue)
        {
            connectionString = cachedConn!;
        }
        else
        {
            var tenantDb = await controlDb.TenantDatabases
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

            if (tenantDb == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Tenant not found");
                return;
            }

            connectionString = tenantDb.ConnectionString;
            await db.StringSetAsync($"tenant:conn:{tenantId}", connectionString,
                TimeSpan.FromMinutes(30));
        }

        tenantContext.TenantId = tenantId;
        tenantContext.ConnectionString = connectionString;

        await _next(context);
    }
}
```

**Step 5: Create TenantDbContextFactory**

`src/Reconova.Tenant.Data/TenantDbContextFactory.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Reconova.Shared;

namespace Reconova.Tenant.Data;

public class TenantDbContextFactory
{
    private readonly TenantContext _tenantContext;

    public TenantDbContextFactory(TenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public TenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(_tenantContext.ConnectionString)
            .Options;

        return new TenantDbContext(options);
    }
}
```

**Step 6: Run test — expect PASS**

**Step 7: Commit**

```bash
git add src/Reconova.Shared/ src/Reconova.Api/Middleware/ src/Reconova.Tenant.Data/ tests/
git commit -m "feat: add tenant resolution middleware with Redis caching and DB context factory"
```

---

## Phase 3: Authentication

### Task 3.1: User Registration Service

**Files:**
- Create: `src/Reconova.ControlPlane/Services/IAuthService.cs`
- Create: `src/Reconova.ControlPlane/Services/AuthService.cs`
- Create: `src/Reconova.Shared/DTOs/RegisterRequest.cs`
- Create: `src/Reconova.Shared/DTOs/AuthResponse.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/AuthServiceTests.cs`

**Step 1: Write failing test**

```csharp
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Reconova.ControlPlane.Services;
using Reconova.ControlPlane.Data;

namespace Reconova.ControlPlane.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_ShouldHashPasswordAndCreateUser()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new ControlDbContext(options);
        var authService = new AuthService(db, /* jwt config */ null!);

        var result = await authService.RegisterAsync(new Shared.DTOs.RegisterRequest
        {
            Email = "user@test.com",
            Password = "SecureP@ss123",
            TenantName = "Test Corp"
        });

        result.Should().NotBeNull();
        var user = await db.Users.FirstAsync(u => u.Email == "user@test.com");
        user.PasswordHash.Should().NotBe("SecureP@ss123"); // should be hashed
        user.TenantId.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_ShouldRejectDuplicateEmail()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new ControlDbContext(options);
        var authService = new AuthService(db, null!);

        await authService.RegisterAsync(new Shared.DTOs.RegisterRequest
        {
            Email = "dup@test.com", Password = "Pass123!", TenantName = "T1"
        });

        var act = () => authService.RegisterAsync(new Shared.DTOs.RegisterRequest
        {
            Email = "dup@test.com", Password = "Pass456!", TenantName = "T2"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

**Step 2: Run test — expect FAIL**

**Step 3: Create DTOs**

`src/Reconova.Shared/DTOs/RegisterRequest.cs`:
```csharp
namespace Reconova.Shared.DTOs;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}
```

`src/Reconova.Shared/DTOs/AuthResponse.cs`:
```csharp
namespace Reconova.Shared.DTOs;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public bool RequiresTwoFactor { get; set; }
}
```

**Step 4: Create IAuthService and AuthService**

The `AuthService` should:
- Validate email uniqueness
- Hash password with BCrypt
- Create tenant (using `TenantProvisioningService` or inline for now)
- Create user linked to tenant
- Return `AuthResponse` (token generation comes in next task)

**Step 5: Run test — expect PASS**

**Step 6: Commit**

```bash
git add src/ tests/
git commit -m "feat: add user registration with password hashing and tenant creation"
```

---

### Task 3.2: JWT Token Generation

**Files:**
- Create: `src/Reconova.ControlPlane/Services/IJwtService.cs`
- Create: `src/Reconova.ControlPlane/Services/JwtService.cs`
- Create: `src/Reconova.Shared/Config/JwtConfig.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/JwtServiceTests.cs`

Test that the JWT service generates tokens with `user_id`, `tenant_id`, and `role` claims, and that tokens can be validated. Test refresh token generation returns a unique opaque string.

**Commit message:** `feat: add JWT token generation with tenant and role claims`

---

### Task 3.3: Login Service

**Files:**
- Create: `src/Reconova.Shared/DTOs/LoginRequest.cs`
- Modify: `src/Reconova.ControlPlane/Services/AuthService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/AuthServiceLoginTests.cs`

Test login with correct password returns JWT. Wrong password returns null/error. Login for user with 2FA enabled returns `RequiresTwoFactor = true` without a token.

**Commit message:** `feat: add login with password verification and 2FA check`

---

### Task 3.4: TOTP 2FA Enrollment and Verification

**Files:**
- Create: `src/Reconova.ControlPlane/Services/ITwoFactorService.cs`
- Create: `src/Reconova.ControlPlane/Services/TwoFactorService.cs`
- Create: `src/Reconova.Shared/DTOs/TwoFactorSetupResponse.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/TwoFactorServiceTests.cs`

Test that enrollment generates a secret and QR URI. Test that verification with a valid TOTP code succeeds. Test that verification with an invalid code fails.

Use the `OtpNet` library for TOTP generation/verification.

**Commit message:** `feat: add TOTP 2FA enrollment and verification`

---

### Task 3.5: Auth API Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/AuthController.cs`
- Test: `tests/Reconova.Integration.Tests/AuthControllerTests.cs`

Endpoints:
- `POST /api/auth/register` — Register user + create tenant
- `POST /api/auth/login` — Login, returns JWT or 2FA prompt
- `POST /api/auth/2fa/setup` — Generate 2FA secret (authenticated)
- `POST /api/auth/2fa/verify` — Verify TOTP code, complete login
- `POST /api/auth/refresh` — Refresh JWT token

Register DI services in `Program.cs`. Add JWT authentication middleware.

**Commit message:** `feat: add auth API endpoints (register, login, 2FA, refresh)`

---

### Task 3.6: Super Admin Seeding

**Files:**
- Create: `src/Reconova.Api/Data/DbSeeder.cs`

Create a seeder that runs on startup. If no super_admin users exist, create one from environment variables (`SUPER_ADMIN_EMAIL`, `SUPER_ADMIN_PASSWORD`). Enforce 2FA setup on first login.

**Commit message:** `feat: add super admin seeding from environment variables`

---

## Phase 4: Billing & Stripe Integration

### Task 4.1: Subscription Plan Management

**Files:**
- Create: `src/Reconova.ControlPlane/Services/ISubscriptionService.cs`
- Create: `src/Reconova.ControlPlane/Services/SubscriptionService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/SubscriptionServiceTests.cs`

Service to CRUD subscription plans (super admin only). Seed default plans (Starter, Pro, Enterprise) on startup.

**Commit message:** `feat: add subscription plan management service`

---

### Task 4.2: Stripe Checkout Integration

**Files:**
- Create: `src/Reconova.ControlPlane/Services/IStripeService.cs`
- Create: `src/Reconova.ControlPlane/Services/StripeService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/StripeServiceTests.cs`

Implement: `CreateCheckoutSession`, `CreateCustomerPortalSession`. Map internal plan IDs to Stripe price IDs.

**Commit message:** `feat: add Stripe checkout session and customer portal integration`

---

### Task 4.3: Stripe Webhook Handler

**Files:**
- Create: `src/Reconova.Api/Controllers/StripeWebhookController.cs`
- Modify: `src/Reconova.ControlPlane/Services/SubscriptionService.cs`

Handle events: `checkout.session.completed`, `invoice.paid`, `customer.subscription.deleted`, `customer.subscription.updated`. On `invoice.paid`: reset monthly credits. On `subscription.deleted`: downgrade tenant.

**Commit message:** `feat: add Stripe webhook handler for subscription lifecycle`

---

### Task 4.4: Credit System Service

**Files:**
- Create: `src/Reconova.ControlPlane/Services/ICreditService.cs`
- Create: `src/Reconova.ControlPlane/Services/CreditService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/CreditServiceTests.cs`

Methods: `DeductCredits(tenantId, steps, domainCount)`, `HasSufficientCredits(tenantId, steps, domainCount)`, `GetBalance(tenantId)`, `AddCredits(tenantId, amount, type)`. Deduction calculates cost from `ScanStepPricing` based on tenant's tier. All changes logged to `CreditTransaction`.

**Commit message:** `feat: add credit system with per-step per-domain consumption`

---

### Task 4.5: Billing API Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/BillingController.cs`

Endpoints:
- `GET /api/billing/plans` — List available plans
- `POST /api/billing/checkout` — Create Stripe checkout session
- `GET /api/billing/portal` — Get Stripe customer portal URL
- `GET /api/billing/credits` — Get current credit balance
- `POST /api/billing/credits/purchase` — Purchase credit pack

**Commit message:** `feat: add billing API endpoints`

---

## Phase 5: Feature Flags

### Task 5.1: Feature Flag Evaluation Service

**Files:**
- Create: `src/Reconova.ControlPlane/Services/IFeatureFlagService.cs`
- Create: `src/Reconova.ControlPlane/Services/FeatureFlagService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/FeatureFlagServiceTests.cs`

Logic: Check operational flags → check plan features → check tenant overrides. Cache results in Redis. Methods: `IsEnabledAsync(tenantId, featureName)`, `GetAllForTenantAsync(tenantId)`, `InvalidateCacheAsync(tenantId)`.

Test cases:
- Operational flag disabled → returns false regardless of plan
- Plan feature enabled, no override → returns true
- Plan feature disabled, override enabled → returns true (override wins)
- Cache invalidation works

**Commit message:** `feat: add feature flag evaluation service with Redis caching`

---

### Task 5.2: Feature Flag Seed Data and Admin Endpoints

**Files:**
- Modify: `src/Reconova.Api/Data/DbSeeder.cs`
- Create: `src/Reconova.Api/Controllers/Admin/FeatureFlagController.cs`

Seed all feature flags from design doc. Admin endpoints to list flags, toggle per-tenant overrides, toggle operational flags.

**Commit message:** `feat: seed feature flags and add admin management endpoints`

---

## Phase 6: Domain Management (Tenant Module)

### Task 6.1: Domain CRUD Service

**Files:**
- Create: `src/Reconova.Tenant/Services/IDomainService.cs`
- Create: `src/Reconova.Tenant/Services/DomainService.cs`
- Test: `tests/Reconova.Tenant.Tests/Services/DomainServiceTests.cs`

Methods: `AddDomainAsync`, `ListDomainsAsync`, `DeleteDomainAsync`. Enforce max domains per plan. Validate domain format.

**Commit message:** `feat: add domain CRUD service with plan limit enforcement`

---

### Task 6.2: Domain API Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/DomainController.cs`

Endpoints:
- `POST /api/domains` — Add domain
- `GET /api/domains` — List domains
- `DELETE /api/domains/{id}` — Remove domain

All endpoints require tenant context (resolved by middleware).

**Commit message:** `feat: add domain management API endpoints`

---

## Phase 7: Scan Engine & Workflows

### Task 7.1: Workflow Template Service

**Files:**
- Create: `src/Reconova.Tenant/Services/IWorkflowService.cs`
- Create: `src/Reconova.Tenant/Services/WorkflowService.cs`
- Create: `src/Reconova.Shared/DTOs/WorkflowStepDefinition.cs`
- Test: `tests/Reconova.Tenant.Tests/Services/WorkflowServiceTests.cs`

Seed system workflow templates (Quick Recon, Full Scan, Web App Scan, Compliance Check, Continuous Monitor). `steps_json` is a JSON array of `WorkflowStepDefinition` objects:
```csharp
public class WorkflowStepDefinition
{
    public string CheckType { get; set; } = string.Empty; // subdomain_enum, port_scan, etc.
    public int Order { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

Methods: `ListTemplatesAsync`, `CreateCustomWorkflowAsync`, `DuplicateTemplateAsync`.

**Commit message:** `feat: add workflow template service with system presets`

---

### Task 7.2: Scan Job Creation and Queuing

**Files:**
- Create: `src/Reconova.Tenant/Services/IScanService.cs`
- Create: `src/Reconova.Tenant/Services/ScanService.cs`
- Create: `src/Reconova.Shared/Messages/ScanJobMessage.cs`
- Test: `tests/Reconova.Tenant.Tests/Services/ScanServiceTests.cs`

Flow:
1. Validate domain belongs to tenant
2. Check feature flags for each workflow step
3. Calculate credit cost via `CreditService`
4. Deduct credits
5. Create `ScanJob` record with status `queued`
6. Push `ScanJobMessage` to Redis stream/list
7. Return scan job ID

`ScanJobMessage`:
```csharp
public class ScanJobMessage
{
    public Guid ScanJobId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantConnectionString { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public List<WorkflowStepDefinition> Steps { get; set; } = new();
}
```

**Commit message:** `feat: add scan job creation with credit deduction and Redis queuing`

---

### Task 7.3: Scan Worker Host

**Files:**
- Modify: `src/Reconova.ScanWorker/Program.cs`
- Create: `src/Reconova.ScanWorker/ScanJobConsumer.cs`
- Test: `tests/Reconova.ScanEngine.Tests/ScanJobConsumerTests.cs`

The worker is a .NET `BackgroundService` that:
1. Listens on Redis for `ScanJobMessage`
2. Deserializes the message
3. Creates a `TenantDbContext` using the connection string from the message
4. Executes workflow steps in order via `IScanStepExecutor`
5. Updates `ScanJob.Status` after each step
6. Marks complete when done

**Commit message:** `feat: add scan worker host consuming jobs from Redis queue`

---

### Task 7.4: Scan Step Executor Framework

**Files:**
- Create: `src/Reconova.ScanEngine/IScanStepExecutor.cs`
- Create: `src/Reconova.ScanEngine/ScanPipeline.cs`
- Create: `src/Reconova.ScanEngine/Steps/SubdomainEnumStep.cs` (stub)
- Create: `src/Reconova.ScanEngine/Steps/PortScanStep.cs` (stub)
- Create: `src/Reconova.ScanEngine/Steps/HttpProbeStep.cs` (stub)
- Create: `src/Reconova.ScanEngine/Steps/TechDetectionStep.cs` (stub)
- Create: `src/Reconova.ScanEngine/Steps/VulnScanStep.cs` (stub)
- Create: `src/Reconova.ScanEngine/Steps/ScreenshotStep.cs` (stub)
- Create: `src/Reconova.ScanEngine/Steps/ContentDiscoveryStep.cs` (stub)
- Test: `tests/Reconova.ScanEngine.Tests/ScanPipelineTests.cs`

```csharp
public interface IScanStepExecutor
{
    string CheckType { get; }
    Task ExecuteAsync(ScanStepContext context, CancellationToken ct);
}

public class ScanStepContext
{
    public Guid ScanJobId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public TenantDbContext TenantDb { get; set; } = null!;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> PreviousStepResults { get; set; } = new();
}
```

`ScanPipeline` resolves the correct `IScanStepExecutor` for each step type and runs them in order, passing results between steps.

**Commit message:** `feat: add scan step executor framework with pipeline orchestration`

---

### Task 7.5: Scan API Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/ScanController.cs`
- Create: `src/Reconova.Api/Controllers/WorkflowController.cs`

Endpoints:
- `POST /api/scans` — Start a scan (domain + workflow)
- `GET /api/scans` — List scan jobs for tenant
- `GET /api/scans/{id}` — Get scan job details + results
- `GET /api/scans/{id}/results` — Get scan results
- `GET /api/workflows` — List workflow templates
- `POST /api/workflows` — Create custom workflow
- `GET /api/workflows/{id}` — Get workflow details

**Commit message:** `feat: add scan and workflow API endpoints`

---

## Phase 8: Recon Module Implementations

### Task 8.1: Subdomain Enumeration Step

**Files:**
- Modify: `src/Reconova.ScanEngine/Steps/SubdomainEnumStep.cs`
- Create: `src/Reconova.ScanEngine/Tools/IToolRunner.cs`
- Create: `src/Reconova.ScanEngine/Tools/ToolRunner.cs`
- Test: `tests/Reconova.ScanEngine.Tests/Steps/SubdomainEnumStepTests.cs`

Implement `SubdomainEnumStep`:
1. Run `subfinder -d {domain} -json -silent` via `ToolRunner` (wraps `Process.Start`)
2. Parse JSON output line by line
3. Store each subdomain in `tenant_db.subdomains`
4. Pass discovered subdomains to next step via `PreviousStepResults`

`ToolRunner` is a wrapper around `System.Diagnostics.Process` that captures stdout/stderr and supports timeouts.

**Commit message:** `feat: implement subdomain enumeration step with subfinder`

---

### Task 8.2: Port Scanning Step

**Files:**
- Modify: `src/Reconova.ScanEngine/Steps/PortScanStep.cs`
- Test: `tests/Reconova.ScanEngine.Tests/Steps/PortScanStepTests.cs`

Run `nmap -sV -T4 --top-ports 1000 -oX - {targets}` or use masscan for speed. Parse XML/JSON output. Store ports in `tenant_db.ports`.

**Commit message:** `feat: implement port scanning step with nmap`

---

### Task 8.3: HTTP Probing Step

Implement using `httpx -json -silent` or built-in `HttpClient`. Probe discovered subdomains for live HTTP services. Store results.

**Commit message:** `feat: implement HTTP probing step`

---

### Task 8.4: Technology Detection Step

Detect technologies from HTTP response headers, body patterns (Wappalyzer-style). Store in `tenant_db.technologies`.

**Commit message:** `feat: implement technology detection step`

---

### Task 8.5: Vulnerability Scanning Step

Run `nuclei -t {templates} -target {targets} -json`. Parse results, store in `tenant_db.vulnerabilities`. Map severity levels.

**Commit message:** `feat: implement vulnerability scanning step with nuclei`

---

### Task 8.6: Screenshot Capture Step

Use a headless browser (Playwright or httpx screenshots) to capture website screenshots. Store images to disk/S3, save paths in `tenant_db.screenshots`.

**Commit message:** `feat: implement screenshot capture step`

---

### Task 8.7: Platform API Key Integration (Shodan)

**Files:**
- Create: `src/Reconova.ScanEngine/Integrations/IShodanClient.cs`
- Create: `src/Reconova.ScanEngine/Integrations/ShodanClient.cs`
- Create: `src/Reconova.ScanEngine/Integrations/PlatformApiKeyProvider.cs`

`PlatformApiKeyProvider` fetches a shared API key from control DB (cached in Redis), tracks usage per tenant. `ShodanClient` enriches scan results with Shodan data for host info, banners, CVEs.

**Commit message:** `feat: add Shodan API integration with platform key management`

---

## Phase 9: Compliance Engine

### Task 9.1: Compliance Framework Management

**Files:**
- Create: `src/Reconova.ControlPlane/Services/IComplianceFrameworkService.cs`
- Create: `src/Reconova.ControlPlane/Services/ComplianceFrameworkService.cs`
- Test: `tests/Reconova.ControlPlane.Tests/Services/ComplianceFrameworkServiceTests.cs`

CRUD for compliance frameworks + controls + check mappings. Super admin only. Seed SOC 2 Type II and NIST CSF 2.0 frameworks with key controls and mappings.

**Commit message:** `feat: add compliance framework management with SOC 2 and NIST seeds`

---

### Task 9.2: Compliance Assessment Service

**Files:**
- Create: `src/Reconova.Tenant/Services/IComplianceService.cs`
- Create: `src/Reconova.Tenant/Services/ComplianceService.cs`
- Test: `tests/Reconova.Tenant.Tests/Services/ComplianceServiceTests.cs`

After a scan completes, run compliance mapping:
1. Load tenant's selected frameworks
2. For each framework, evaluate each control against scan results using `control_check_mappings`
3. Generate `ComplianceAssessment` with `ControlResult` entries
4. Calculate overall score

**Commit message:** `feat: add compliance assessment service mapping scan results to controls`

---

### Task 9.3: Compliance Report Generation

**Files:**
- Create: `src/Reconova.Tenant/Services/IReportService.cs`
- Create: `src/Reconova.Tenant/Services/ReportService.cs`

Generate PDF/HTML compliance reports using a templating engine (e.g., QuestPDF for PDF). Include: executive summary, per-control results with evidence, remediation recommendations, historical trend.

**Commit message:** `feat: add compliance report generation (PDF/HTML)`

---

### Task 9.4: Compliance API Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/ComplianceController.cs`

Endpoints:
- `GET /api/compliance/frameworks` — List available frameworks (tier-filtered)
- `POST /api/compliance/selections` — Select frameworks to assess against
- `GET /api/compliance/assessments` — List assessments
- `GET /api/compliance/assessments/{id}` — Get assessment detail
- `GET /api/compliance/reports/{assessmentId}` — Download report (PDF/HTML)

**Commit message:** `feat: add compliance API endpoints`

---

## Phase 10: CVE Monitoring

### Task 10.1: CVE Feed Ingestion Service

**Files:**
- Create: `src/Reconova.ScanWorker/CveIngestionService.cs`
- Create: `src/Reconova.ScanEngine/Integrations/INvdClient.cs`
- Create: `src/Reconova.ScanEngine/Integrations/NvdClient.cs`
- Test: `tests/Reconova.ScanEngine.Tests/CveIngestionTests.cs`

Background service that periodically fetches new CVEs from NVD API. Stores in `control_db.cve_database`. Configurable feed sources.

**Commit message:** `feat: add CVE feed ingestion from NVD`

---

### Task 10.2: CVE Matching and Alerting

**Files:**
- Create: `src/Reconova.ScanWorker/CveMatchingService.cs`
- Test: `tests/Reconova.ScanEngine.Tests/CveMatchingTests.cs`

When new CVEs arrive:
1. Query all tenants' `technologies` tables for affected products
2. Create `VulnerabilityAlert` in affected tenant DBs
3. Trigger notification via notification service

**Commit message:** `feat: add CVE matching against tenant tech stacks with alerts`

---

## Phase 11: Notification Integrations

### Task 11.1: Notification Service

**Files:**
- Create: `src/Reconova.Tenant/Services/INotificationService.cs`
- Create: `src/Reconova.Tenant/Services/NotificationService.cs`
- Create: `src/Reconova.Tenant/Notifications/INotificationChannel.cs`
- Create: `src/Reconova.Tenant/Notifications/EmailChannel.cs`
- Create: `src/Reconova.Tenant/Notifications/SlackChannel.cs`
- Create: `src/Reconova.Tenant/Notifications/WebhookChannel.cs`
- Test: `tests/Reconova.Tenant.Tests/Services/NotificationServiceTests.cs`

Load tenant's `integration_configs` and `notification_rules`. For each matching event+rule, dispatch to the appropriate channel. Log in `notification_history`.

**Commit message:** `feat: add notification service with email, Slack, and webhook channels`

---

### Task 11.2: Notification Config API Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/IntegrationController.cs`

Endpoints:
- `GET /api/integrations` — List configured integrations
- `POST /api/integrations` — Add integration (Slack, email, webhook)
- `PUT /api/integrations/{id}` — Update config
- `DELETE /api/integrations/{id}` — Remove integration
- `GET /api/integrations/{id}/history` — Notification history

**Commit message:** `feat: add integration configuration API endpoints`

---

## Phase 12: Super Admin API

### Task 12.1: Tenant Management Admin Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/Admin/TenantAdminController.cs`

Endpoints:
- `GET /api/admin/tenants` — List all tenants with stats
- `GET /api/admin/tenants/{id}` — Tenant detail
- `POST /api/admin/tenants/{id}/suspend` — Suspend tenant
- `POST /api/admin/tenants/{id}/activate` — Activate tenant
- `POST /api/admin/tenants/{id}/impersonate` — Get impersonation token
- `PUT /api/admin/tenants/{id}/credits` — Adjust credits

**Commit message:** `feat: add super admin tenant management endpoints`

---

### Task 12.2: Platform Configuration Admin Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/Admin/PlanAdminController.cs`
- Create: `src/Reconova.Api/Controllers/Admin/PricingAdminController.cs`
- Create: `src/Reconova.Api/Controllers/Admin/ApiKeyAdminController.cs`
- Create: `src/Reconova.Api/Controllers/Admin/ComplianceAdminController.cs`

Endpoints for managing: plans, scan step pricing, platform API keys, compliance frameworks, CVE feed sources, workflow templates.

**Commit message:** `feat: add super admin platform configuration endpoints`

---

### Task 12.3: Audit Log Endpoints

**Files:**
- Create: `src/Reconova.Api/Controllers/Admin/AuditLogController.cs`
- Create: `src/Reconova.ControlPlane/Services/IAuditService.cs`
- Create: `src/Reconova.ControlPlane/Services/AuditService.cs`

Endpoints:
- `GET /api/admin/audit-logs` — Query audit logs (filterable by tenant, user, action, date range)
- `GET /api/admin/monitoring` — System health dashboard data

**Commit message:** `feat: add audit log and monitoring endpoints`

---

## Phase 13: SvelteKit Frontend

### Task 13.1: Initialize SvelteKit Project

**Step 1: Create SvelteKit app**

```bash
cd /Users/blackkspydo/side-projects/reconova
npx sv create frontend
# Select: SvelteKit minimal, TypeScript, Tailwind CSS
cd frontend && npm install
```

**Step 2: Install dependencies**

```bash
npm install @sveltejs/adapter-node
npm install -D @types/node
```

**Step 3: Verify dev server starts**

Run: `npm run dev`
Expected: SvelteKit app running on localhost:5173

**Step 4: Commit**

```bash
git add frontend/
git commit -m "chore: initialize SvelteKit frontend with TypeScript and Tailwind"
```

---

### Task 13.2: Auth Pages

**Files:**
- Create: `frontend/src/routes/auth/register/+page.svelte`
- Create: `frontend/src/routes/auth/login/+page.svelte`
- Create: `frontend/src/routes/auth/2fa-setup/+page.svelte`
- Create: `frontend/src/routes/auth/2fa-verify/+page.svelte`
- Create: `frontend/src/lib/api.ts` — API client with JWT handling
- Create: `frontend/src/lib/stores/auth.ts` — Auth state store

Registration page: email, password, tenant name. Login page: email, password. 2FA setup: show QR code, verify code. Auto-redirect to 2FA setup after first login.

**Commit message:** `feat: add auth pages (register, login, 2FA setup/verify)`

---

### Task 13.3: Dashboard Layout and Domain Management

**Files:**
- Create: `frontend/src/routes/(app)/+layout.svelte` — Authenticated layout with sidebar
- Create: `frontend/src/routes/(app)/dashboard/+page.svelte`
- Create: `frontend/src/routes/(app)/domains/+page.svelte`
- Create: `frontend/src/routes/(app)/domains/[id]/+page.svelte`

Dashboard: overview cards (domains count, recent scans, credit balance, compliance score). Domains page: list, add, delete domains.

**Commit message:** `feat: add dashboard and domain management pages`

---

### Task 13.4: Scan Management Pages

**Files:**
- Create: `frontend/src/routes/(app)/scans/+page.svelte` — List scans
- Create: `frontend/src/routes/(app)/scans/new/+page.svelte` — Start new scan
- Create: `frontend/src/routes/(app)/scans/[id]/+page.svelte` — Scan details + results
- Create: `frontend/src/routes/(app)/workflows/+page.svelte` — Manage workflows

Scan creation: select domain, pick workflow, show credit cost estimate, confirm. Scan results: tabbed view by check type, severity indicators, export.

**Commit message:** `feat: add scan management and results pages`

---

### Task 13.5: Compliance Pages

**Files:**
- Create: `frontend/src/routes/(app)/compliance/+page.svelte` — Framework selection + assessments
- Create: `frontend/src/routes/(app)/compliance/[id]/+page.svelte` — Assessment detail
- Create: `frontend/src/routes/(app)/compliance/reports/+page.svelte` — Report download

Compliance dashboard: per-framework compliance scores, control status breakdown, trend chart, download report button.

**Commit message:** `feat: add compliance dashboard and report pages`

---

### Task 13.6: Billing Pages

**Files:**
- Create: `frontend/src/routes/(app)/billing/+page.svelte` — Current plan, credit balance
- Create: `frontend/src/routes/(app)/billing/plans/+page.svelte` — Plan comparison + upgrade

Show current plan, credit usage, purchase credits button (Stripe Checkout redirect), manage subscription (Stripe Portal redirect).

**Commit message:** `feat: add billing and plan management pages`

---

### Task 13.7: Settings and Integrations Pages

**Files:**
- Create: `frontend/src/routes/(app)/settings/+page.svelte` — Account settings, 2FA
- Create: `frontend/src/routes/(app)/settings/integrations/+page.svelte` — Configure notifications

**Commit message:** `feat: add settings and integration configuration pages`

---

### Task 13.8: Super Admin Panel

**Files:**
- Create: `frontend/src/routes/(admin)/admin/+layout.svelte` — Admin layout (requires super_admin role)
- Create: `frontend/src/routes/(admin)/admin/tenants/+page.svelte` — Tenant list
- Create: `frontend/src/routes/(admin)/admin/tenants/[id]/+page.svelte` — Tenant detail
- Create: `frontend/src/routes/(admin)/admin/plans/+page.svelte` — Plan management
- Create: `frontend/src/routes/(admin)/admin/features/+page.svelte` — Feature flags
- Create: `frontend/src/routes/(admin)/admin/compliance/+page.svelte` — Framework management
- Create: `frontend/src/routes/(admin)/admin/api-keys/+page.svelte` — Platform API keys
- Create: `frontend/src/routes/(admin)/admin/pricing/+page.svelte` — Scan step pricing
- Create: `frontend/src/routes/(admin)/admin/audit/+page.svelte` — Audit log viewer
- Create: `frontend/src/routes/(admin)/admin/monitoring/+page.svelte` — System health

**Commit message:** `feat: add super admin panel with tenant and platform management`

---

## Phase 14: Integration Testing & Polish

### Task 14.1: End-to-End Integration Tests

**Files:**
- Modify: `tests/Reconova.Integration.Tests/`

Test the full flow with Testcontainers (Postgres + Redis):
1. Register user → create tenant → tenant DB provisioned
2. Add domain → create workflow → start scan → verify job queued
3. Verify credit deduction
4. Verify feature flag enforcement

**Commit message:** `test: add end-to-end integration tests for core flows`

---

### Task 14.2: Docker Compose for Full Stack

**Files:**
- Modify: `docker/docker-compose.yml`

Add services for: API, Scan Worker, SvelteKit (all with Dockerfiles). Production-ready compose with health checks, networking, environment variables.

**Commit message:** `chore: add Docker Compose for full stack local deployment`

---

### Task 14.3: API Documentation

**Files:**
- Modify: `src/Reconova.Api/Program.cs`

Add OpenAPI/Swagger documentation with `Swashbuckle` or `NSwag`. Group endpoints by controller. Document auth requirements.

**Commit message:** `docs: add OpenAPI documentation for all API endpoints`

---

## Dependency Graph

```
Phase 1 (Scaffolding)
  └── Phase 2 (Multi-Tenancy Core)
        ├── Phase 3 (Auth)
        │     └── Phase 5 (Feature Flags)
        │           └── Phase 7 (Scan Engine)
        │                 └── Phase 8 (Recon Modules)
        │                 └── Phase 9 (Compliance)
        │                       └── Phase 10 (CVE Monitoring)
        │                 └── Phase 11 (Notifications)
        ├── Phase 4 (Billing)
        └── Phase 6 (Domain Management)
              └── Phase 7 (Scan Engine)

Phase 12 (Super Admin) — depends on Phases 3-11
Phase 13 (Frontend) — can start after Phase 3, grows with each backend phase
Phase 14 (Testing & Polish) — after all phases
```
