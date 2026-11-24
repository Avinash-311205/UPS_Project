using System.Reflection;
using Backend.Common.Interfaces;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Identity.Models;
using Backend.Modules.Incidents.Models;
using Backend.Modules.Operations.Models;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;
using Backend.Modules.Tenants.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantContext? _tenantContext;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantContext? tenantContext = null) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LogisticsSystem> Systems => Set<LogisticsSystem>();
        public DbSet<SystemDependency> Dependencies => Set<SystemDependency>();
        public DbSet<BusinessOperation> BusinessOperations => Set<BusinessOperation>();
        public DbSet<SystemBusinessOperation> SystemBusinessOperations => Set<SystemBusinessOperation>();
        public DbSet<Incident> Incidents => Set<Incident>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Global Query Filters (Fail-Closed):
            // Normal User queries fail closed when there is no authenticated tenant context.
            // Login lookup explicitly bypasses this filter in UserRepository via IgnoreQueryFilters().
            modelBuilder.Entity<User>().HasQueryFilter(u =>
                _tenantContext != null && _tenantContext.HasTenant && u.TenantId.HasValue && u.TenantId.Value == _tenantContext.TenantId!.Value);

            // Strict Tenant Isolation (Fail-Closed):
            // LogisticsSystems only return records belonging to the authenticated tenant.
            // Fails closed if _tenantContext is null or has no tenant claim.
            modelBuilder.Entity<LogisticsSystem>().HasQueryFilter(s =>
                _tenantContext != null && _tenantContext.HasTenant && s.TenantId == _tenantContext.TenantId!.Value);

            // Strict Tenant Isolation (Fail-Closed):
            // SystemDependencies only return records belonging to the authenticated tenant.
            // Fails closed if _tenantContext is null or has no tenant claim.
            modelBuilder.Entity<SystemDependency>().HasQueryFilter(d =>
                _tenantContext != null && _tenantContext.HasTenant && d.TenantId == _tenantContext.TenantId!.Value);

            // Strict Tenant Isolation (Fail-Closed):
            // BusinessOperations only return records belonging to the authenticated tenant.
            modelBuilder.Entity<BusinessOperation>().HasQueryFilter(o =>
                _tenantContext != null && _tenantContext.HasTenant && o.TenantId == _tenantContext.TenantId!.Value);

            // Strict Tenant Isolation (Fail-Closed):
            // SystemBusinessOperations only return records belonging to the authenticated tenant.
            modelBuilder.Entity<SystemBusinessOperation>().HasQueryFilter(sbo =>
                _tenantContext != null && _tenantContext.HasTenant && sbo.TenantId == _tenantContext.TenantId!.Value);

            // Strict Tenant Isolation (Fail-Closed):
            // Incidents only return records belonging to the authenticated tenant.
            modelBuilder.Entity<Incident>().HasQueryFilter(i =>
                _tenantContext != null && _tenantContext.HasTenant && i.TenantId == _tenantContext.TenantId!.Value);
        }

        public override int SaveChanges()
        {
            EnforceTenantPolicy();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnforceTenantPolicy();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void EnforceTenantPolicy()
        {
            if (_tenantContext == null || !_tenantContext.HasTenant)
            {
                return;
            }

            var currentTenantId = _tenantContext.TenantId!.Value;

            // Guard tenant-owned business entities implementing ITenantEntity
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = currentTenantId;
                    }
                    else if (entry.Entity.TenantId != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant write prohibited. Cannot create entity with TenantId '{entry.Entity.TenantId}' inside Tenant '{currentTenantId}'.");
                    }
                }
                else if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    if (entry.Entity.TenantId != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant mutation prohibited. Cannot modify or delete entity belonging to Tenant '{entry.Entity.TenantId}' from Tenant '{currentTenantId}'.");
                    }
                }
            }

            // Guard User mutations within a tenant context
            foreach (var entry in ChangeTracker.Entries<User>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (!entry.Entity.TenantId.HasValue || entry.Entity.TenantId.Value == Guid.Empty)
                    {
                        entry.Entity.TenantId = currentTenantId;
                    }
                    else if (entry.Entity.TenantId.Value != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant write prohibited. Cannot create user with TenantId '{entry.Entity.TenantId}' inside Tenant '{currentTenantId}'.");
                    }

                    // Enforce exactly one Root per Tenant
                    if (entry.Entity.AccountType == Backend.Modules.Identity.Enums.AccountType.Root && entry.Entity.TenantId.HasValue)
                    {
                        var hasExistingRoot = Users.IgnoreQueryFilters().Any(u =>
                            u.TenantId == entry.Entity.TenantId &&
                            u.AccountType == Backend.Modules.Identity.Enums.AccountType.Root &&
                            u.Id != entry.Entity.Id);

                        if (hasExistingRoot)
                        {
                            throw new InvalidOperationException(
                                $"Tenant '{entry.Entity.TenantId}' already has a Root account. Only one Root account is permitted per Tenant.");
                        }
                    }

                    entry.Entity.RootTenantId = entry.Entity.AccountType == Backend.Modules.Identity.Enums.AccountType.Root
                        ? entry.Entity.TenantId
                        : null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (!entry.Entity.TenantId.HasValue || entry.Entity.TenantId.Value != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant mutation prohibited. Cannot modify or delete user belonging to Tenant '{entry.Entity.TenantId}' from Tenant '{currentTenantId}'.");
                    }

                    if (entry.Entity.AccountType == Backend.Modules.Identity.Enums.AccountType.Root && entry.Entity.TenantId.HasValue)
                    {
                        var hasExistingRoot = Users.IgnoreQueryFilters().Any(u =>
                            u.TenantId == entry.Entity.TenantId &&
                            u.AccountType == Backend.Modules.Identity.Enums.AccountType.Root &&
                            u.Id != entry.Entity.Id);

                        if (hasExistingRoot)
                        {
                            throw new InvalidOperationException(
                                $"Tenant '{entry.Entity.TenantId}' already has a Root account. Only one Root account is permitted per Tenant.");
                        }
                    }

                    entry.Entity.RootTenantId = entry.Entity.AccountType == Backend.Modules.Identity.Enums.AccountType.Root
                        ? entry.Entity.TenantId
                        : null;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    if (!entry.Entity.TenantId.HasValue || entry.Entity.TenantId.Value != currentTenantId)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant mutation prohibited. Cannot modify or delete user belonging to Tenant '{entry.Entity.TenantId}' from Tenant '{currentTenantId}'.");
                    }
                }
            }

            // Guard SystemDependency-specific rules
            foreach (var entry in ChangeTracker.Entries<SystemDependency>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    // 1. Prevent self-dependency
                    if (entry.Entity.DependentSystemId == entry.Entity.DependencySystemId)
                    {
                        throw new InvalidOperationException(
                            $"A system cannot depend on itself. DependentSystemId and DependencySystemId cannot both be '{entry.Entity.DependentSystemId}'.");
                    }

                    // 2. Both referenced systems must belong to the same tenant as the dependency
                    if (entry.Entity.DependentSystem != null && entry.Entity.DependentSystem.TenantId != entry.Entity.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant dependency prohibited. DependentSystem '{entry.Entity.DependentSystemId}' belongs to tenant '{entry.Entity.DependentSystem.TenantId}', but dependency belongs to tenant '{entry.Entity.TenantId}'.");
                    }

                    if (entry.Entity.DependencySystem != null && entry.Entity.DependencySystem.TenantId != entry.Entity.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant dependency prohibited. DependencySystem '{entry.Entity.DependencySystemId}' belongs to tenant '{entry.Entity.DependencySystem.TenantId}', but dependency belongs to tenant '{entry.Entity.TenantId}'.");
                    }

                    // Verify against local tracked or persisted systems if navigations not explicitly set
                    if (entry.Entity.DependentSystem == null || entry.Entity.DependencySystem == null)
                    {
                        var systemIds = new[] { entry.Entity.DependentSystemId, entry.Entity.DependencySystemId };
                        var tracked = Systems.Local.Where(s => systemIds.Contains(s.Id)).ToList();
                        foreach (var sys in tracked)
                        {
                            if (sys.TenantId != entry.Entity.TenantId)
                            {
                                throw new InvalidOperationException(
                                    $"Cross-tenant dependency prohibited. System '{sys.Id}' belongs to tenant '{sys.TenantId}', but dependency belongs to tenant '{entry.Entity.TenantId}'.");
                            }
                        }

                        var missingIds = systemIds.Except(tracked.Select(s => s.Id)).ToList();
                        if (missingIds.Count > 0)
                        {
                            var dbTenants = Systems.IgnoreQueryFilters()
                                .Where(s => missingIds.Contains(s.Id))
                                .Select(s => new { s.Id, s.TenantId })
                                .ToList();

                            foreach (var sys in dbTenants)
                            {
                                if (sys.TenantId != entry.Entity.TenantId)
                                {
                                    throw new InvalidOperationException(
                                        $"Cross-tenant dependency prohibited. System '{sys.Id}' belongs to tenant '{sys.TenantId}', but dependency belongs to tenant '{entry.Entity.TenantId}'.");
                                }
                            }
                        }
                    }
                }
            }

            // Guard SystemBusinessOperation-specific rules
            foreach (var entry in ChangeTracker.Entries<SystemBusinessOperation>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    if (entry.Entity.LogisticsSystem != null && entry.Entity.LogisticsSystem.TenantId != entry.Entity.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant operation assignment prohibited. System '{entry.Entity.LogisticsSystemId}' belongs to tenant '{entry.Entity.LogisticsSystem.TenantId}', but assignment belongs to tenant '{entry.Entity.TenantId}'.");
                    }

                    if (entry.Entity.BusinessOperation != null && entry.Entity.BusinessOperation.TenantId != entry.Entity.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant operation assignment prohibited. Operation '{entry.Entity.BusinessOperationId}' belongs to tenant '{entry.Entity.BusinessOperation.TenantId}', but assignment belongs to tenant '{entry.Entity.TenantId}'.");
                    }

                    if (entry.Entity.LogisticsSystem == null)
                    {
                        var sysTenantId = Systems.Local.FirstOrDefault(s => s.Id == entry.Entity.LogisticsSystemId)?.TenantId
                            ?? Systems.IgnoreQueryFilters().Where(s => s.Id == entry.Entity.LogisticsSystemId).Select(s => (Guid?)s.TenantId).FirstOrDefault();
                        if (sysTenantId.HasValue && sysTenantId.Value != entry.Entity.TenantId)
                        {
                            throw new InvalidOperationException(
                                $"Cross-tenant operation assignment prohibited. System '{entry.Entity.LogisticsSystemId}' belongs to tenant '{sysTenantId.Value}', but assignment belongs to tenant '{entry.Entity.TenantId}'.");
                        }
                    }

                    if (entry.Entity.BusinessOperation == null)
                    {
                        var opTenantId = BusinessOperations.Local.FirstOrDefault(o => o.Id == entry.Entity.BusinessOperationId)?.TenantId
                            ?? BusinessOperations.IgnoreQueryFilters().Where(o => o.Id == entry.Entity.BusinessOperationId).Select(o => (Guid?)o.TenantId).FirstOrDefault();
                        if (opTenantId.HasValue && opTenantId.Value != entry.Entity.TenantId)
                        {
                            throw new InvalidOperationException(
                                $"Cross-tenant operation assignment prohibited. Operation '{entry.Entity.BusinessOperationId}' belongs to tenant '{opTenantId.Value}', but assignment belongs to tenant '{entry.Entity.TenantId}'.");
                        }
                    }
                }
            }

            // Guard Incident-specific rules
            foreach (var entry in ChangeTracker.Entries<Incident>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    if (entry.Entity.FailedSystem != null && entry.Entity.FailedSystem.TenantId != entry.Entity.TenantId)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant incident prohibited. FailedSystem '{entry.Entity.FailedSystemId}' belongs to tenant '{entry.Entity.FailedSystem.TenantId}', but incident belongs to tenant '{entry.Entity.TenantId}'.");
                    }

                    if (entry.Entity.FailedSystem == null)
                    {
                        var sysTenantId = Systems.Local.FirstOrDefault(s => s.Id == entry.Entity.FailedSystemId)?.TenantId
                            ?? Systems.IgnoreQueryFilters().Where(s => s.Id == entry.Entity.FailedSystemId).Select(s => (Guid?)s.TenantId).FirstOrDefault();
                        if (sysTenantId.HasValue && sysTenantId.Value != entry.Entity.TenantId)
                        {
                            throw new InvalidOperationException(
                                $"Cross-tenant incident prohibited. FailedSystem '{entry.Entity.FailedSystemId}' belongs to tenant '{sysTenantId.Value}', but incident belongs to tenant '{entry.Entity.TenantId}'.");
                        }
                    }
                }
            }
        }
    }
}
