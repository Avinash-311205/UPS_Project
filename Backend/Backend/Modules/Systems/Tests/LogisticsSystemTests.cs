using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Systems.Services;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Systems.Tests
{
    public class MockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public bool HasTenant => TenantId.HasValue;
    }

    public class InMemoryLogisticsSystemRepository : ILogisticsSystemRepository
    {
        private readonly List<LogisticsSystem> _systems = new();

        public Task<IEnumerable<LogisticsSystem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<LogisticsSystem>>(_systems.ToList());
        }

        public Task<LogisticsSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var system = _systems.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(system);
        }

        public Task<LogisticsSystem> AddAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _systems.Add(system);
            return Task.FromResult(system);
        }

        public Task<LogisticsSystem> UpdateAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            var index = _systems.FindIndex(s => s.Id == system.Id);
            if (index >= 0)
            {
                _systems[index] = system;
            }
            return Task.FromResult(system);
        }

        public Task DeleteAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _systems.RemoveAll(s => s.Id == system.Id);
            return Task.CompletedTask;
        }
    }

    public static class LogisticsSystemTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — Systems Management CRUD Test Suite");
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

            // Test 1: Create system and verify TenantId is automatically assigned from context
            RunTest("Create system automatically assigns TenantId from authenticated context", () =>
            {
                var tenantA = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = tenantA };
                var service = new LogisticsSystemService(repo, context);

                var dto = new CreateLogisticsSystemDTO
                {
                    Name = "Chicago Hub WMS",
                    SystemType = SystemType.WMS,
                    Description = "Primary central warehouse system",
                    BusinessPurpose = "Inventory tracking and dispatch",
                    Criticality = SystemCriticality.Critical,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                };

                var (success, error, data) = service.CreateAsync(dto).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Creation failed: {error}");
                if (data.TenantId != tenantA) throw new Exception($"Expected TenantId {tenantA}, got {data.TenantId}");
                if (data.Name != "Chicago Hub WMS") throw new Exception("Name mismatch");
                if (data.SystemType != SystemType.WMS) throw new Exception("SystemType mismatch");
                if (data.CreatedAt == default) throw new Exception("CreatedAt should be populated");
            });

            // Test 2: Unauthenticated create attempt is rejected
            RunTest("Create system fails when caller has no authenticated tenant context", () =>
            {
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = null };
                var service = new LogisticsSystemService(repo, context);

                var dto = new CreateLogisticsSystemDTO
                {
                    Name = "Unauthorized WMS",
                    SystemType = SystemType.WMS
                };

                var (success, error, data) = service.CreateAsync(dto).GetAwaiter().GetResult();
                if (success) throw new Exception("Creation must fail without tenant context!");
                if (string.IsNullOrWhiteSpace(error)) throw new Exception("Expected error message for unauthenticated create");
            });

            // Test 3: Name validation
            RunTest("Create system rejects empty or whitespace name", () =>
            {
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = Guid.NewGuid() };
                var service = new LogisticsSystemService(repo, context);

                var dto = new CreateLogisticsSystemDTO
                {
                    Name = "   ",
                    SystemType = SystemType.TMS
                };

                var (success, error, _) = service.CreateAsync(dto).GetAwaiter().GetResult();
                if (success) throw new Exception("Creation must fail for empty name");
            });

            // Test 4: Enum validation
            RunTest("Create system rejects invalid enum values", () =>
            {
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = Guid.NewGuid() };
                var service = new LogisticsSystemService(repo, context);

                var dto = new CreateLogisticsSystemDTO
                {
                    Name = "Valid Name",
                    SystemType = (SystemType)999
                };

                var (success, error, _) = service.CreateAsync(dto).GetAwaiter().GetResult();
                if (success) throw new Exception("Creation must fail for invalid SystemType enum");
            });

            // Test 5: Retrieve all systems
            RunTest("GetAllAsync returns systems for caller tenant", () =>
            {
                var tenantA = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = tenantA };
                var service = new LogisticsSystemService(repo, context);

                service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "System 1",
                    SystemType = SystemType.TMS,
                    Criticality = SystemCriticality.High,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "System 2",
                    SystemType = SystemType.WMS,
                    Criticality = SystemCriticality.Medium,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                var systems = service.GetAllAsync().GetAwaiter().GetResult().ToList();
                if (systems.Count != 2) throw new Exception($"Expected 2 systems, found {systems.Count}");
            });

            // Test 6: Retrieve by ID
            RunTest("GetByIdAsync returns correct system by ID", () =>
            {
                var tenantA = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = tenantA };
                var service = new LogisticsSystemService(repo, context);

                var (_, _, created) = service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Dallas TMS",
                    SystemType = SystemType.TMS,
                    Criticality = SystemCriticality.High,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                var retrieved = service.GetByIdAsync(created!.Id).GetAwaiter().GetResult();
                if (retrieved == null) throw new Exception("Failed to retrieve created system by ID");
                if (retrieved.Name != "Dallas TMS") throw new Exception("Retrieved data mismatch");
            });

            // Test 7: Tenant isolation - Tenant A cannot read Tenant B system
            RunTest("Tenant A cannot retrieve Tenant B system by ID (cross-tenant read blocked)", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();

                // Tenant B creates system
                var contextB = new MockTenantContext { TenantId = tenantB };
                var serviceB = new LogisticsSystemService(repo, contextB);
                var (_, _, sysB) = serviceB.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Tenant B Secret System",
                    SystemType = SystemType.ERP,
                    Criticality = SystemCriticality.Critical,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                // Tenant A attempts to access Tenant B's system
                var contextA = new MockTenantContext { TenantId = tenantA };
                var serviceA = new LogisticsSystemService(repo, contextA);

                var leakAttempt = serviceA.GetByIdAsync(sysB!.Id).GetAwaiter().GetResult();
                if (leakAttempt != null) throw new Exception("Security violation: Tenant A was able to read Tenant B's system!");
            });

            // Test 8: Update system
            RunTest("Update system updates mutable fields and sets UpdatedAt without altering TenantId", () =>
            {
                var tenantA = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = tenantA };
                var service = new LogisticsSystemService(repo, context);

                var (_, _, created) = service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Original Name",
                    SystemType = SystemType.TMS,
                    Criticality = SystemCriticality.Low,
                    Environment = SystemEnvironment.Development,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                var updateDto = new UpdateLogisticsSystemDTO
                {
                    Name = "Renamed TMS",
                    SystemType = SystemType.TMS,
                    Description = "Updated description",
                    Criticality = SystemCriticality.Critical,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                };

                var (success, error, notFound, updated) = service.UpdateAsync(created!.Id, updateDto).GetAwaiter().GetResult();

                if (!success || updated == null) throw new Exception($"Update failed: {error}");
                if (updated.Name != "Renamed TMS") throw new Exception("Name was not updated");
                if (updated.Criticality != SystemCriticality.Critical) throw new Exception("Criticality was not updated");
                if (updated.TenantId != tenantA) throw new Exception("TenantId must remain unchanged");
                if (!updated.UpdatedAt.HasValue) throw new Exception("UpdatedAt must be populated");
            });

            // Test 9: Tenant A cannot update Tenant B system
            RunTest("Tenant A cannot update Tenant B system (cross-tenant update blocked)", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();

                // Tenant B creates system
                var serviceB = new LogisticsSystemService(repo, new MockTenantContext { TenantId = tenantB });
                var (_, _, sysB) = serviceB.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Tenant B System",
                    SystemType = SystemType.WMS,
                    Criticality = SystemCriticality.High,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                // Tenant A attempts to update
                var serviceA = new LogisticsSystemService(repo, new MockTenantContext { TenantId = tenantA });
                var (success, _, notFound, _) = serviceA.UpdateAsync(sysB!.Id, new UpdateLogisticsSystemDTO
                {
                    Name = "Hacked System",
                    SystemType = SystemType.WMS,
                    Criticality = SystemCriticality.Low,
                    Environment = SystemEnvironment.Development,
                    LifecycleStatus = SystemLifecycleStatus.Inactive
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Security violation: Tenant A was able to update Tenant B's system!");
                if (!notFound) throw new Exception("Expected notFound to be true for cross-tenant update");

                // Verify Tenant B's data is untouched
                var untouched = repo.GetByIdAsync(sysB.Id).GetAwaiter().GetResult();
                if (untouched!.Name != "Tenant B System") throw new Exception("Tenant B's system was mutated!");
            });

            // Test 10: Soft delete (deactivation) verifies record remains in database with Inactive status
            RunTest("Delete system performs soft deactivation: sets LifecycleStatus to Inactive and preserves DB record", () =>
            {
                var tenantA = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = tenantA };
                var service = new LogisticsSystemService(repo, context);

                var (_, _, created) = service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "To Deactivate",
                    SystemType = SystemType.Telematics,
                    Criticality = SystemCriticality.Low,
                    Environment = SystemEnvironment.Development,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                var (success, error, notFound) = service.DeleteAsync(created!.Id).GetAwaiter().GetResult();
                if (!success) throw new Exception($"Deactivation failed: {error}");

                // Verify the record STILL EXISTS in the database
                var check = repo.GetByIdAsync(created.Id).GetAwaiter().GetResult();
                if (check == null) throw new Exception("Record was physically deleted from database! Soft deactivation required.");
                if (check.LifecycleStatus != SystemLifecycleStatus.Inactive) throw new Exception($"Expected Inactive status, got {check.LifecycleStatus}");
                if (!check.UpdatedAt.HasValue) throw new Exception("UpdatedAt timestamp was not set during deactivation");
            });

            // Test 10b: Inactive system filtering prevents treating inactive systems as active
            RunTest("Inactive systems are excluded from default queries to prevent accidental active usage", () =>
            {
                var tenantA = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();
                var context = new MockTenantContext { TenantId = tenantA };
                var service = new LogisticsSystemService(repo, context);

                var (s1, e1, activeSys) = service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Active System",
                    SystemType = SystemType.WMS,
                    Criticality = SystemCriticality.Medium,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();
                if (!s1 || activeSys == null) throw new Exception($"Failed to create activeSys: {e1}");

                var (s2, e2, deactSys) = service.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Deactivated System",
                    SystemType = SystemType.TMS,
                    Criticality = SystemCriticality.Low,
                    Environment = SystemEnvironment.Development,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();
                if (!s2 || deactSys == null) throw new Exception($"Failed to create deactSys: {e2}");

                // Deactivate the second system
                service.DeleteAsync(deactSys!.Id).GetAwaiter().GetResult();

                // Default GetAllAsync: must ONLY return active systems
                var activeList = service.GetAllAsync(includeInactive: false).GetAwaiter().GetResult().ToList();
                if (activeList.Count != 1) throw new Exception($"Expected 1 active system, got {activeList.Count}");
                if (activeList[0].Id != activeSys!.Id) throw new Exception("Deactivated system was included in active queries!");

                // With includeInactive: true: must return both systems for historical auditing
                var allList = service.GetAllAsync(includeInactive: true).GetAwaiter().GetResult().ToList();
                if (allList.Count != 2) throw new Exception($"Expected 2 systems when includeInactive=true, got {allList.Count}");

                // Direct GetById returns the inactive record so audit trail & incident history can inspect it
                var directGet = service.GetByIdAsync(deactSys.Id).GetAwaiter().GetResult();
                if (directGet == null) throw new Exception("Deactivated system could not be retrieved by ID for historical audit");
                if (directGet.LifecycleStatus != SystemLifecycleStatus.Inactive) throw new Exception("Retrieved system should have LifecycleStatus=Inactive");
            });

            // Test 11: Tenant A cannot delete/deactivate Tenant B system
            RunTest("Tenant A cannot deactivate Tenant B system (cross-tenant deactivation blocked)", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var repo = new InMemoryLogisticsSystemRepository();

                var serviceB = new LogisticsSystemService(repo, new MockTenantContext { TenantId = tenantB });
                var (_, _, sysB) = serviceB.CreateAsync(new CreateLogisticsSystemDTO
                {
                    Name = "Protected System",
                    SystemType = SystemType.EDI,
                    Criticality = SystemCriticality.Medium,
                    Environment = SystemEnvironment.Production,
                    LifecycleStatus = SystemLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                var serviceA = new LogisticsSystemService(repo, new MockTenantContext { TenantId = tenantA });
                var (success, _, notFound) = serviceA.DeleteAsync(sysB!.Id).GetAwaiter().GetResult();

                if (success) throw new Exception("Security violation: Tenant A was able to deactivate Tenant B's system!");
                if (!notFound) throw new Exception("Expected notFound to be true for cross-tenant deactivation");

                var check = repo.GetByIdAsync(sysB.Id).GetAwaiter().GetResult();
                if (check == null) throw new Exception("Tenant B's system was deleted!");
                if (check.LifecycleStatus != SystemLifecycleStatus.Active) throw new Exception("Tenant B's system status was modified!");
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
