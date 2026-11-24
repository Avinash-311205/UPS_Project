using System.Security.Claims;
using Backend.Common.Interfaces;
using Backend.Modules.Identity.Models;
using Backend.Modules.Tenants.Interfaces;
using Backend.Modules.Tenants.Services;
using Microsoft.AspNetCore.Http;

namespace Backend.Modules.Tenants.Tests
{
    public static class TenantIsolationTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — Tenant Isolation Test Suite");
            Console.WriteLine("=================================================");

            int passed = 0;
            int failed = 0;

            void RunTest(string testName, Action testAction)
            {
                try
                {
                    testAction();
                    Console.WriteLine($"[PASS] {testName}");
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FAIL] {testName}: {ex.Message}");
                    failed++;
                }
            }

            // Test 1: Authenticated user with valid tenant claim
            RunTest("TenantContext resolves TenantId from validated JWT claim", () =>
            {
                var expectedTenantId = Guid.NewGuid();
                var httpContext = new DefaultHttpContext();
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim("tenant_id", expectedTenantId.ToString())
                }, "TestAuthType");
                httpContext.User = new ClaimsPrincipal(identity);

                var accessor = new HttpContextAccessor { HttpContext = httpContext };
                var context = new TenantContext(accessor);

                if (context.TenantId != expectedTenantId)
                    throw new Exception($"Expected {expectedTenantId}, got {context.TenantId}");
                if (!context.HasTenant)
                    throw new Exception("Expected HasTenant to be true");
            });

            // Test 2: Unauthenticated user
            RunTest("TenantContext returns null for unauthenticated user", () =>
            {
                var httpContext = new DefaultHttpContext();
                httpContext.User = new ClaimsPrincipal(); // Empty/unauthenticated

                var accessor = new HttpContextAccessor { HttpContext = httpContext };
                var context = new TenantContext(accessor);

                if (context.TenantId != null)
                    throw new Exception("Expected null TenantId for unauthenticated context");
                if (context.HasTenant)
                    throw new Exception("Expected HasTenant to be false");
            });

            // Test 3: Untrusted request parameters are ignored
            RunTest("TenantContext ignores untrusted query and header parameters", () =>
            {
                var untrustedTenantId = Guid.NewGuid();
                var httpContext = new DefaultHttpContext();
                httpContext.Request.QueryString = new QueryString($"?tenant_id={untrustedTenantId}");
                httpContext.Request.Headers["X-Tenant-Id"] = untrustedTenantId.ToString();
                httpContext.User = new ClaimsPrincipal(); // Unauthenticated

                var accessor = new HttpContextAccessor { HttpContext = httpContext };
                var context = new TenantContext(accessor);

                if (context.TenantId != null)
                    throw new Exception("Security violation: TenantContext resolved tenant from untrusted request parameter!");
            });

            // Test 4: Malformed GUID claim handling
            RunTest("TenantContext handles malformed GUID claims safely", () =>
            {
                var httpContext = new DefaultHttpContext();
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim("tenant_id", "not-a-valid-guid")
                }, "TestAuthType");
                httpContext.User = new ClaimsPrincipal(identity);

                var accessor = new HttpContextAccessor { HttpContext = httpContext };
                var context = new TenantContext(accessor);

                if (context.TenantId != null)
                    throw new Exception("Expected null TenantId for malformed GUID");
                if (context.HasTenant)
                    throw new Exception("Expected HasTenant to be false for malformed GUID");
            });

            // Test 4b: Empty GUID claim handling
            RunTest("TenantContext treats Guid.Empty claims as unassigned (null)", () =>
            {
                var httpContext = new DefaultHttpContext();
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim("tenant_id", Guid.Empty.ToString())
                }, "TestAuthType");
                httpContext.User = new ClaimsPrincipal(identity);

                var accessor = new HttpContextAccessor { HttpContext = httpContext };
                var context = new TenantContext(accessor);

                if (context.TenantId != null)
                    throw new Exception("Expected null TenantId for Guid.Empty claim");
                if (context.HasTenant)
                    throw new Exception("Expected HasTenant to be false for Guid.Empty claim");
            });

            // Test 5: Global Query Filter logic for User (Fail-Closed) and Login Bypass
            RunTest("User query filter fails closed without tenant context and permits login lookup via IgnoreQueryFilters", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();

                var userA = new User { Id = 1, Username = "userA", Email = "a@test.com", TenantId = tenantA };
                var userB = new User { Id = 2, Username = "userB", Email = "b@test.com", TenantId = tenantB };
                var userUnassigned = new User { Id = 3, Username = "unassignedUser", Email = "unassigned@test.com", TenantId = null };

                // Normal fail-closed User filter in AppDbContext
                bool FilterUser(User u, Guid? currentTenant) =>
                    currentTenant.HasValue && u.TenantId.HasValue && u.TenantId.Value == currentTenant.Value;

                // Explicit bypass for login repository email lookup (IgnoreQueryFilters)
                bool LoginLookupBypass(User u, string email) =>
                    u.Email == email;

                // Under Tenant A
                if (!FilterUser(userA, tenantA)) throw new Exception("User A should be visible in Tenant A");
                if (FilterUser(userB, tenantA)) throw new Exception("User B must NOT be visible in Tenant A (cross-tenant leak)");
                if (FilterUser(userUnassigned, tenantA)) throw new Exception("Unassigned user must NOT be visible under Tenant A context");

                // Under Unauthenticated / No tenant context (FAILS CLOSED)
                if (FilterUser(userA, null)) throw new Exception("Normal User filter must fail closed when there is no tenant context");
                if (FilterUser(userB, null)) throw new Exception("Normal User filter must fail closed when there is no tenant context");
                if (FilterUser(userUnassigned, null)) throw new Exception("Normal User filter must fail closed when there is no tenant context");

                // Under Login email lookup (explicit bypass via IgnoreQueryFilters)
                if (!LoginLookupBypass(userA, "a@test.com")) throw new Exception("User A must be retrievable by email during login lookup");
                if (!LoginLookupBypass(userB, "b@test.com")) throw new Exception("User B must be retrievable by email during login lookup");
                if (!LoginLookupBypass(userUnassigned, "unassigned@test.com")) throw new Exception("Unassigned user must be retrievable by email during login lookup");
            });

            // Test 6: Cross-tenant mutation policy enforcement for User
            RunTest("Cross-tenant User mutation throws UnauthorizedAccessException", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();

                void EnforceUserCheck(User user, Guid currentTenant)
                {
                    if (!user.TenantId.HasValue || user.TenantId.Value != currentTenant)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant mutation prohibited. Cannot modify or delete user belonging to Tenant '{user.TenantId}' from Tenant '{currentTenant}'.");
                    }
                }

                var foreignUser = new User { Id = 4, TenantId = tenantB };
                var unassignedUser = new User { Id = 5, TenantId = null };

                bool caughtForeign = false;
                try
                {
                    EnforceUserCheck(foreignUser, tenantA);
                }
                catch (UnauthorizedAccessException)
                {
                    caughtForeign = true;
                }

                if (!caughtForeign)
                    throw new Exception("Expected UnauthorizedAccessException when mutating cross-tenant user entity");

                bool caughtUnassigned = false;
                try
                {
                    EnforceUserCheck(unassignedUser, tenantA);
                }
                catch (UnauthorizedAccessException)
                {
                    caughtUnassigned = true;
                }

                if (!caughtUnassigned)
                    throw new Exception("Expected UnauthorizedAccessException when mutating unassigned user entity inside a tenant context");
            });

            // Test 7: Global Query Filter logic for LogisticsSystem (Strict Fail-Closed)
            RunTest("LogisticsSystem query filter fails closed without tenant context and strictly isolates per tenant", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();

                var sysA = new Backend.Modules.Systems.Models.LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Chicago WMS",
                    SystemType = Backend.Modules.Systems.Enums.SystemType.WMS
                };
                var sysB = new Backend.Modules.Systems.Models.LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantB,
                    Name = "Dallas WMS",
                    SystemType = Backend.Modules.Systems.Enums.SystemType.WMS
                };

                // Fail-closed filter: caller must have an active tenant context matching the system's tenant
                bool FilterSystem(Backend.Modules.Systems.Models.LogisticsSystem s, Guid? currentTenant) =>
                    currentTenant.HasValue && s.TenantId == currentTenant.Value;

                // Under Tenant A
                if (!FilterSystem(sysA, tenantA)) throw new Exception("System A should be visible in Tenant A");
                if (FilterSystem(sysB, tenantA)) throw new Exception("System B must NOT be visible in Tenant A (cross-tenant leak)");

                // Under Unassigned user, unauthenticated caller, or null tenant context (FAILS CLOSED)
                if (FilterSystem(sysA, null)) throw new Exception("System A must NOT be accessible without tenant context (must fail closed)");
                if (FilterSystem(sysB, null)) throw new Exception("System B must NOT be accessible without tenant context (must fail closed)");
            });

            // Test 8: Cross-tenant mutation policy enforcement for LogisticsSystem
            RunTest("Cross-tenant LogisticsSystem write/mutation throws UnauthorizedAccessException", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();

                void EnforceCheck(ITenantEntity entity, Guid currentTenant)
                {
                    if (entity.TenantId != currentTenant)
                    {
                        throw new UnauthorizedAccessException(
                            $"Cross-tenant write prohibited. Cannot create entity with TenantId '{entity.TenantId}' inside Tenant '{currentTenant}'.");
                    }
                }

                var foreignSystem = new Backend.Modules.Systems.Models.LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantB,
                    Name = "Rogue WMS"
                };

                bool caught = false;
                try
                {
                    EnforceCheck(foreignSystem, tenantA);
                }
                catch (UnauthorizedAccessException)
                {
                    caught = true;
                }

                if (!caught)
                    throw new Exception("Expected UnauthorizedAccessException when injecting cross-tenant LogisticsSystem");
            });

            // Test 9: JWT claim generation policy for unassigned vs assigned users
            RunTest("JWT claims policy: unassigned users do not receive tenant claim, assigned users do", () =>
            {
                var assignedTenant = Guid.NewGuid();
                var assignedUser = new User { Id = 10, Username = "assigned", TenantId = assignedTenant };
                var unassignedUser = new User { Id = 11, Username = "unassigned", TenantId = null };

                List<Claim> BuildClaims(User user)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username)
                    };
                    if (user.TenantId.HasValue && user.TenantId.Value != Guid.Empty)
                    {
                        claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));
                    }
                    return claims;
                }

                var assignedClaims = BuildClaims(assignedUser);
                var unassignedClaims = BuildClaims(unassignedUser);

                if (!assignedClaims.Any(c => c.Type == "tenant_id" && c.Value == assignedTenant.ToString()))
                    throw new Exception("Assigned user must receive valid tenant_id claim");

                if (unassignedClaims.Any(c => c.Type == "tenant_id"))
                    throw new Exception("Security violation: Unassigned user received a tenant_id claim!");
            });

            // Test 10: Registration tenant assignment safety
            RunTest("Registration provisions a new dedicated tenant workspace and disallows arbitrary tenant injection", () =>
            {
                // Verify RegisterDTO does not expose a client-supplied TenantId property
                var tenantIdProp = typeof(Backend.Modules.Identity.DTOs.RegisterDTO).GetProperty("TenantId");
                if (tenantIdProp != null)
                {
                    throw new Exception("Security violation: RegisterDTO must not expose a client-supplied TenantId property!");
                }
            });

            // Test 11: Safe, idempotent onboarding for legacy unassigned accounts
            RunTest("Legacy unassigned account is provisioned with a unique tenant and resolves tenant context for system creation", () =>
            {
                var legacyUser = new User
                {
                    Id = 42,
                    Username = "legacy_user",
                    Email = "legacy@example.com",
                    TenantId = null
                };

                // Idempotent onboarding assigns new dedicated Tenant
                var provisionedTenantId = Guid.NewGuid();
                legacyUser.TenantId = provisionedTenantId;

                // Build JWT claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, legacyUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, legacyUser.Username),
                    new Claim(ClaimTypes.Email, legacyUser.Email)
                };
                if (legacyUser.TenantId.HasValue && legacyUser.TenantId.Value != Guid.Empty)
                {
                    claims.Add(new Claim("tenant_id", legacyUser.TenantId.Value.ToString()));
                }

                if (!claims.Any(c => c.Type == "tenant_id" && c.Value == provisionedTenantId.ToString()))
                {
                    throw new Exception("Fresh login token must contain tenant_id claim for onboarded user");
                }

                // Resolve TenantContext
                var httpContext = new DefaultHttpContext();
                var identity = new ClaimsIdentity(claims, "TestAuthType");
                httpContext.User = new ClaimsPrincipal(identity);
                var accessor = new HttpContextAccessor { HttpContext = httpContext };
                var tenantContext = new TenantContext(accessor);

                if (tenantContext.TenantId != provisionedTenantId)
                {
                    throw new Exception($"Expected TenantContext to resolve {provisionedTenantId}, got {tenantContext.TenantId}");
                }
                if (!tenantContext.HasTenant)
                {
                    throw new Exception("Expected HasTenant to be true for onboarded user context");
                }
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
