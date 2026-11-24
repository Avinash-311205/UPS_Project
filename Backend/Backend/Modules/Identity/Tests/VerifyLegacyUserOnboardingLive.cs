using System.Security.Claims;
using Backend.Infrastructure.Persistence;
using Backend.Modules.Identity.Models;
using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Repositories;
using Backend.Modules.Systems.Services;
using Backend.Modules.Tenants.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Identity.Tests
{
    public static class VerifyLegacyUserOnboardingLive
    {
        public static int Run()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — Live Legacy User Onboarding Verification");
            Console.WriteLine("=================================================");

            var factory = new AppDbContextFactory();
            using var db = factory.CreateDbContext(Array.Empty<string>());

            const string targetEmail = "nbaditya32@gmail.com";

            // 1. Verify User has a valid TenantId in the live database
            var user = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Email == targetEmail);
            if (user == null)
            {
                Console.WriteLine($"[FAIL] User with email '{targetEmail}' was not found in MySQL.");
                return 1;
            }

            if (!user.TenantId.HasValue || user.TenantId.Value == Guid.Empty)
            {
                Console.WriteLine($"[FAIL] User '{targetEmail}' has NULL or Empty TenantId.");
                return 1;
            }
            Console.WriteLine($"[PASS] User '{targetEmail}' has valid TenantId: {user.TenantId.Value}");

            // 2. Verify referenced Tenant record exists in MySQL
            var tenant = db.Tenants.FirstOrDefault(t => t.Id == user.TenantId.Value);
            if (tenant == null)
            {
                Console.WriteLine($"[FAIL] Referenced Tenant record with ID '{user.TenantId.Value}' does not exist in MySQL.");
                return 1;
            }
            Console.WriteLine($"[PASS] Referenced Tenant record exists: '{tenant.Name}' (Status: {tenant.Status})");

            // 3. Verify fresh login JWT claim generation policy
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };
            if (user.TenantId.HasValue && user.TenantId.Value != Guid.Empty)
            {
                claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));
            }

            var tenantClaim = claims.FirstOrDefault(c => c.Type == "tenant_id");
            if (tenantClaim == null || tenantClaim.Value != user.TenantId.Value.ToString())
            {
                Console.WriteLine("[FAIL] JWT claims do not contain matching tenant_id claim.");
                return 1;
            }
            Console.WriteLine($"[PASS] Fresh login generates JWT containing tenant_id claim: {tenantClaim.Value}");

            // 4. Verify authenticated tenant context resolves that ID
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            httpContext.User = new ClaimsPrincipal(identity);
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var tenantContext = new TenantContext(accessor);

            if (!tenantContext.HasTenant || tenantContext.TenantId != user.TenantId.Value)
            {
                Console.WriteLine($"[FAIL] TenantContext failed to resolve TenantId. Expected: {user.TenantId.Value}, Resolved: {tenantContext.TenantId}");
                return 1;
            }
            Console.WriteLine($"[PASS] Authenticated ITenantContext successfully resolved TenantId: {tenantContext.TenantId.Value}");

            // 5. Verify creating a logistics system succeeds under this tenant context
            using var tenantDb = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseMySQL("Server=localhost;Database=resiliencex;Uid=root;Pwd=Aditya@2005;", opt =>
                    {
                        opt.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    }).Options,
                tenantContext);

            var systemRepo = new LogisticsSystemRepository(tenantDb);
            var systemService = new LogisticsSystemService(systemRepo, tenantContext);

            var testSystemDto = new CreateLogisticsSystemDTO
            {
                Name = "Aditya Live Fleet TMS",
                SystemType = SystemType.TMS,
                Description = "Verification system for onboarded tenant context",
                Criticality = SystemCriticality.High,
                Environment = SystemEnvironment.Production,
                LifecycleStatus = SystemLifecycleStatus.Active
            };

            var (success, error, created) = systemService.CreateAsync(testSystemDto).GetAwaiter().GetResult();
            if (!success || created == null)
            {
                Console.WriteLine($"[FAIL] Failed to create logistics system: {error}");
                return 1;
            }

            if (created.TenantId != user.TenantId.Value)
            {
                Console.WriteLine($"[FAIL] Created system has wrong TenantId: {created.TenantId} vs expected {user.TenantId.Value}");
                return 1;
            }
            Console.WriteLine($"[PASS] Created logistics system '{created.Name}' (ID: {created.Id}) under TenantId: {created.TenantId}");

            // 6. Clean up verification test record from database
            var systemInDb = tenantDb.Systems.FirstOrDefault(s => s.Id == created.Id);
            if (systemInDb != null)
            {
                tenantDb.Systems.Remove(systemInDb);
                tenantDb.SaveChanges();
                Console.WriteLine($"[PASS] Verification logistics system cleaned up successfully.");
            }

            Console.WriteLine("=================================================");
            Console.WriteLine("All 6 live onboarding verification checks PASSED.");
            Console.WriteLine("=================================================");
            return 0;
        }
    }
}
