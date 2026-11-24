# 06. Systems Management Module

## Objective & Purpose

The **Systems Module** (`Modules/Systems/`) encapsulates the inventory, classification, persistence, and operational metadata of all connected software systems in a 3PL logistics infrastructure (such as Warehouse Management Systems, Transportation Management Systems, Yard Management Systems, Telematics platforms, and EDI customs gateways).

In a cyber-continuity event, knowing exactly which systems exist, their criticality tier, their deployment environment, and their operational status is the foundation for dependency mapping, blast-radius impact analysis, and automated playbook activation.

---

## 1. Module Folder Structure

```text
Backend/
├── Modules/Systems/
│   ├── Models/
│   │   └── LogisticsSystem.cs                   <- Domain entity (ITenantEntity)
│   └── Enums/
│       ├── SystemType.cs                        <- Categorization (TMS, WMS, EDI, etc.)
│       ├── SystemCriticality.cs                 <- Low, Medium, High, Critical
│       ├── SystemEnvironment.cs                 <- Development, Staging, Production
│       └── SystemLifecycleStatus.cs             <- Active, Inactive
└── Infrastructure/Persistence/
    ├── AppDbContext.cs                          <- Includes DbSet<LogisticsSystem> Systems
    ├── AppDbContextFactory.cs                   <- Design-time DbContext factory
    ├── Configurations/
    │   └── LogisticsSystemConfiguration.cs      <- Fluent EF Core mapping
    └── Migrations/
        ├── 20261123053900_AddLogisticsSystems.cs
        └── 20261123053900_AddLogisticsSystems.Designer.cs
```

---

## 2. Enums & Categorization

The module provides four strongly-typed, integer-backed enumerations:

### A. SystemType ([SystemType.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Modules/Systems/Enums/SystemType.cs))
Identifies the operational domain and functionality of the logistics system:
```csharp
namespace Backend.Modules.Systems.Enums
{
    public enum SystemType
    {
        TMS = 1,          // Transportation Management System (routing, dispatch, carrier EDI)
        WMS = 2,          // Warehouse Management System (inventory, binning, fulfillment)
        YMS = 3,          // Yard Management System (dock scheduling, trailer tracking)
        Telematics = 4,   // Fleet telematics (GPS, engine diagnostics, temperature)
        EDI = 5,          // Electronic Data Interchange (customs filings, carrier manifests)
        ERP = 6,          // Enterprise Resource Planning (finance, billing, purchase orders)
        Other = 7         // Custom microservices, IoT hubs, proprietary gateways
    }
}
```

### B. SystemCriticality ([SystemCriticality.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Modules/Systems/Enums/SystemCriticality.cs))
Defines the business criticality tier used to prioritize recovery playbooks during outages:
```csharp
namespace Backend.Modules.Systems.Enums
{
    public enum SystemCriticality
    {
        Low = 1,          // Non-critical reporting, asynchronous analytics
        Medium = 2,       // Operational support systems with manual workarounds
        High = 3,         // Core operational systems; outage delays shipments
        Critical = 4      // Tier 1 mission-critical; outage halts terminal / hub operations
    }
}
```

### C. SystemEnvironment ([SystemEnvironment.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Modules/Systems/Enums/SystemEnvironment.cs))
Distinguishes live production instances from staging and testing environments:
```csharp
namespace Backend.Modules.Systems.Enums
{
    public enum SystemEnvironment
    {
        Development = 1,
        Staging = 2,
        Production = 3
    }
}
```

### D. SystemLifecycleStatus ([SystemLifecycleStatus.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Modules/Systems/Enums/SystemLifecycleStatus.cs))
Reflects whether the system is actively monitored or decommissioned:
```csharp
namespace Backend.Modules.Systems.Enums
{
    public enum SystemLifecycleStatus
    {
        Active = 1,
        Inactive = 2
    }
}
```

---

## 3. Domain Model: `LogisticsSystem.cs`

In Step 5.2, the entity was renamed from `System` to [LogisticsSystem.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Modules/Systems/Models/LogisticsSystem.cs) to eliminate all naming ambiguity with the root C# `System` namespace:

```csharp
using System.ComponentModel.DataAnnotations;
using Backend.Common.Interfaces;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Systems.Models
{
    public class LogisticsSystem : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public SystemType SystemType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? BusinessPurpose { get; set; }

        public SystemCriticality Criticality { get; set; }

        public SystemEnvironment Environment { get; set; }

        public SystemLifecycleStatus LifecycleStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
```

---

## 4. EF Core Configuration & Persistence (Step 5.2)

### Fluent Configuration ([LogisticsSystemConfiguration.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Infrastructure/Persistence/Configurations/LogisticsSystemConfiguration.cs))
```csharp
namespace Backend.Infrastructure.Persistence.Configurations
{
    public class LogisticsSystemConfiguration : IEntityTypeConfiguration<LogisticsSystem>
    {
        public void Configure(EntityTypeBuilder<LogisticsSystem> builder)
        {
            builder.ToTable("LogisticsSystems");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.TenantId).IsRequired();
            builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
            builder.Property(s => s.SystemType).IsRequired().HasConversion<int>();
            builder.Property(s => s.Description).HasMaxLength(500);
            builder.Property(s => s.BusinessPurpose).HasMaxLength(500);
            builder.Property(s => s.Criticality).IsRequired().HasConversion<int>();
            builder.Property(s => s.Environment).IsRequired().HasConversion<int>();
            builder.Property(s => s.LifecycleStatus).IsRequired().HasConversion<int>();
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt);

            // Mandatory relationship to Tenant
            builder.HasOne(s => s.Tenant)
                .WithMany(t => t.Systems)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant-scoped query optimization indexes
            builder.HasIndex(s => s.TenantId);
            builder.HasIndex(s => new { s.TenantId, s.SystemType });
            builder.HasIndex(s => new { s.TenantId, s.LifecycleStatus });
            builder.HasIndex(s => new { s.TenantId, s.Name });
        }
    }
}
```

### Key Mapping Rules:
1. **Multiplicity per SystemType**: No unique index exists on `(TenantId, SystemType)`. Multiple TMS or WMS systems can exist under one tenant (e.g. regional warehouses, multiple carriers).
2. **Integer Enum Storage**: All enums are mapped using `.HasConversion<int>()` for database portability and performance.
3. **Restrictive Referential Integrity**: `DeleteBehavior.Restrict` ensures a Tenant cannot be deleted if active systems are attached to it.

---

## 5. Multi-Tenant Isolation for LogisticsSystem

`LogisticsSystem` implements `ITenantEntity` and is fully integrated into `AppDbContext`:

### Read Isolation: Global Query Filter
```csharp
modelBuilder.Entity<LogisticsSystem>().HasQueryFilter(s =>
    _tenantContext == null || !_tenantContext.HasTenant || s.TenantId == _tenantContext.TenantId!.Value);
```
Every query executed against `_context.Systems` automatically has `WHERE s.TenantId = @currentTenantId` appended when an authenticated user token is present.

### Write Isolation: Mutation Guard Policy
In `AppDbContext.SaveChanges()` / `SaveChangesAsync()`, `EnforceTenantPolicy()` intercepts `ChangeTracker.Entries<ITenantEntity>()`:
* **Auto-Population**: If `system.TenantId` is unassigned (`Guid.Empty`), it is automatically assigned the current user's authenticated `TenantId`.
* **Cross-Tenant Injection Rejection**: If `system.TenantId` does not match `_tenantContext.TenantId`, an `UnauthorizedAccessException` is thrown immediately before sending SQL commands to MySQL.

---

## 6. Generated Migration: `AddLogisticsSystems`

The migration was generated using EF Core CLI tools into [Infrastructure/Persistence/Migrations/20261123053900_AddLogisticsSystems.cs](file:///c:/Users/DELL/Downloads/UPS_Project/Backend/Backend/Infrastructure/Persistence/Migrations/20261123053900_AddLogisticsSystems.cs):

### Schema Operations Overview:
1. `CreateTable("Tenants")`: Creates the `Tenants` table with `PK_Tenants`.
2. `CreateTable("LogisticsSystems")`: Creates the `LogisticsSystems` table with columns, types, lengths, and foreign key `FK_LogisticsSystems_Tenants_TenantId`.
3. `AddColumn<Guid>("TenantId", "Users")`: Adds `TenantId` column to `Users` table with `FK_Users_Tenants_TenantId`.
4. `CreateIndex(...)`: Creates performance indexes for tenant-scoped lookups:
   * `IX_LogisticsSystems_TenantId`
   * `IX_LogisticsSystems_TenantId_SystemType`
   * `IX_LogisticsSystems_TenantId_LifecycleStatus`
   * `IX_LogisticsSystems_TenantId_Name`

> [!IMPORTANT]
> **Pending Execution**: In accordance with the architectural instructions, this migration has **not** been applied to the database (`dotnet ef database update` was not run). It is staged in source control for administrative review.

---

## 7. Implementation Status & Roadmap

| Component | Status | Notes |
| :--- | :--- | :--- |
| `LogisticsSystem.cs` Entity | **Complete** | Renamed from `System`, implements `ITenantEntity` |
| 4 Domain Enums | **Complete** | Explicit integer backing |
| `LogisticsSystemConfiguration` | **Complete** | Table `LogisticsSystems`, indexes, restrict delete |
| `AppDbContext.Systems` DbSet | **Complete** | Registered on context |
| Global Query Filter | **Complete** | Tenant read isolation verified |
| Mutation Protection Guard | **Complete** | Cross-tenant write protection verified |
| Design-Time Factory | **Complete** | `AppDbContextFactory` allows migration CLI operations |
| Generated Migration | **Complete** | `AddLogisticsSystems` generated; not yet applied to database |
| Isolation Tests | **Complete** | 8/8 tests passing (including `LogisticsSystem` test cases) |
| Repositories & Services | *Pending Step 5.3* | `ILogisticsSystemRepository`, `LogisticsSystemService` |
| API Endpoints | *Pending Step 5.4* | `SystemsController` REST endpoints |
