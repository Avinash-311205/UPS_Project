using Backend.Modules.Dependencies.DTOs;
using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Dependencies.Interfaces;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Dependencies.Services;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Dependencies.Tests
{
    public class MockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public bool HasTenant => TenantId.HasValue;
    }

    public class InMemorySystemDependencyRepository : ISystemDependencyRepository
    {
        private readonly List<SystemDependency> _dependencies = new();

        public Task<IEnumerable<SystemDependency>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<SystemDependency>>(_dependencies.ToList());
        }

        public Task<SystemDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_dependencies.FirstOrDefault(d => d.Id == id));
        }

        public Task<bool> ExistsAsync(Guid dependentSystemId, Guid dependencySystemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_dependencies.Any(d => d.DependentSystemId == dependentSystemId && d.DependencySystemId == dependencySystemId));
        }

        public Task<SystemDependency> AddAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            _dependencies.Add(dependency);
            return Task.FromResult(dependency);
        }

        public Task<SystemDependency> UpdateAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            var idx = _dependencies.FindIndex(d => d.Id == dependency.Id);
            if (idx >= 0)
            {
                _dependencies[idx] = dependency;
            }
            return Task.FromResult(dependency);
        }

        public Task DeleteAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            _dependencies.RemoveAll(d => d.Id == dependency.Id);
            return Task.CompletedTask;
        }
    }

    public class InMemoryLogisticsSystemRepositoryForDep : ILogisticsSystemRepository
    {
        private readonly List<LogisticsSystem> _systems = new();

        public void Seed(LogisticsSystem system) => _systems.Add(system);

        public Task<IEnumerable<LogisticsSystem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<LogisticsSystem>>(_systems.ToList());
        }

        public Task<LogisticsSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_systems.FirstOrDefault(s => s.Id == id));
        }

        public Task<LogisticsSystem> AddAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _systems.Add(system);
            return Task.FromResult(system);
        }

        public Task<LogisticsSystem> UpdateAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            var index = _systems.FindIndex(s => s.Id == system.Id);
            if (index >= 0) _systems[index] = system;
            return Task.FromResult(system);
        }

        public Task DeleteAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _systems.RemoveAll(s => s.Id == system.Id);
            return Task.CompletedTask;
        }
    }

    public static class SystemDependencyTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — System Dependencies Test Suite");
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

            // 1. Successful dependency creation
            RunTest("Successful dependency creation assigns TenantId and records relationship", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS Core", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS Alpha", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);
                sysRepo.Seed(wms);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (success, error, data) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = wms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Expected success but got: {error}");
                if (data.TenantId != tenantId) throw new Exception("TenantId must be auto-assigned from ITenantContext");
                if (data.DependentSystemId != tms.Id || data.DependencySystemId != wms.Id) throw new Exception("System IDs mismatch");
                if (data.DependencyType != DependencyType.Mandatory) throw new Exception("DependencyType mismatch");
                if (data.DependentSystem?.Name != "TMS Core") throw new Exception("DependentSystem details missing from response");
                if (data.DependencySystem?.Name != "WMS Alpha") throw new Exception("DependencySystem details missing from response");
            });

            // 2. Mandatory and Optional dependency types
            RunTest("Supports both Mandatory and Optional dependency types", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var telem = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Telem", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);
                sysRepo.Seed(wms);
                sysRepo.Seed(telem);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (s1, _, d1) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = wms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                var (s2, _, d2) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = telem.Id,
                    DependencyType = DependencyType.Optional
                }).GetAwaiter().GetResult();

                if (!s1 || d1?.DependencyType != DependencyType.Mandatory) throw new Exception("Mandatory creation failed");
                if (!s2 || d2?.DependencyType != DependencyType.Optional) throw new Exception("Optional creation failed");
            });

            // 3. Duplicate relationship rejection
            RunTest("Duplicate dependency relationships are prohibited", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);
                sysRepo.Seed(wms);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (s1, _, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = wms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (!s1) throw new Exception("First dependency creation should succeed");

                // Try duplicate creation
                var (s2, error2, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = wms.Id,
                    DependencyType = DependencyType.Optional // Even with different type, duplicate is rejected
                }).GetAwaiter().GetResult();

                if (s2) throw new Exception("Duplicate dependency creation should have been rejected!");
                if (!error2!.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Unexpected error message: {error2}");
            });

            // 4. Self-dependency rejection
            RunTest("A system cannot depend on itself", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (success, error, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = tms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Self-dependency should have been rejected!");
                if (!error!.Contains("cannot depend on itself", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"Unexpected error message: {error}");
            });

            // 5. Nonexistent system rejection
            RunTest("Rejects dependency creation when either system does not exist", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var realSys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(realSys);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                // Nonexistent dependent
                var (s1, e1, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = Guid.NewGuid(),
                    DependencySystemId = realSys.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (s1) throw new Exception("Nonexistent dependent should be rejected");
                if (!e1!.Contains("does not exist", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error: {e1}");

                // Nonexistent dependency
                var (s2, e2, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = realSys.Id,
                    DependencySystemId = Guid.NewGuid(),
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (s2) throw new Exception("Nonexistent dependency should be rejected");
                if (!e2!.Contains("does not exist", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error: {e2}");
            });

            // 6. Inactive system rejection
            RunTest("Inactive systems cannot be used to create new dependencies", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var activeSys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Active TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var inactiveSys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Retired WMS", LifecycleStatus = SystemLifecycleStatus.Inactive };
                sysRepo.Seed(activeSys);
                sysRepo.Seed(inactiveSys);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                // Dependent inactive
                var (s1, e1, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = inactiveSys.Id,
                    DependencySystemId = activeSys.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (s1) throw new Exception("Inactive dependent system should be rejected");
                if (!e1!.Contains("inactive", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error: {e1}");

                // Dependency inactive
                var (s2, e2, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = activeSys.Id,
                    DependencySystemId = inactiveSys.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (s2) throw new Exception("Inactive dependency system should be rejected");
                if (!e2!.Contains("inactive", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error: {e2}");
            });

            // 7. Cross-tenant creation rejection
            RunTest("Cross-tenant dependency creation is strictly blocked", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantA };

                var sysA = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Tenant A Sys", LifecycleStatus = SystemLifecycleStatus.Active };
                var sysB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Tenant B Sys", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sysA);
                sysRepo.Seed(sysB);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (success, error, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = sysA.Id,
                    DependencySystemId = sysB.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Cross-tenant dependency creation must be rejected!");
                if (!error!.Contains("Cross-tenant", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error: {error}");
            });

            // 8. Cross-tenant read, update, delete protection
            RunTest("Tenant A cannot read, update, or delete Tenant B dependency", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();

                // Create a dependency belonging to Tenant B
                var depB = new SystemDependency
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantB,
                    DependentSystemId = Guid.NewGuid(),
                    DependencySystemId = Guid.NewGuid(),
                    DependencyType = DependencyType.Mandatory
                };
                depRepo.AddAsync(depB).GetAwaiter().GetResult();

                // Tenant A tries to access
                var contextA = new MockTenantContext { TenantId = tenantA };
                var serviceA = new SystemDependencyService(depRepo, sysRepo, contextA);

                // Read
                var readResult = serviceA.GetByIdAsync(depB.Id).GetAwaiter().GetResult();
                if (readResult != null) throw new Exception("Tenant A should NOT be able to read Tenant B dependency!");

                // Update
                var (updSuccess, _, updNotFound, _) = serviceA.UpdateAsync(depB.Id, new UpdateSystemDependencyDTO
                {
                    DependencyType = DependencyType.Optional
                }).GetAwaiter().GetResult();

                if (updSuccess || !updNotFound) throw new Exception("Tenant A should NOT be able to update Tenant B dependency!");

                // Delete
                var (delSuccess, _, delNotFound) = serviceA.DeleteAsync(depB.Id).GetAwaiter().GetResult();
                if (delSuccess || !delNotFound) throw new Exception("Tenant A should NOT be able to delete Tenant B dependency!");
            });

            // 9. Successful update
            RunTest("Successfully updates DependencyType without altering systems or TenantId", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);
                sysRepo.Seed(wms);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (_, _, created) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = wms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                var (updSuccess, updError, _, updated) = service.UpdateAsync(created!.Id, new UpdateSystemDependencyDTO
                {
                    DependencyType = DependencyType.Optional
                }).GetAwaiter().GetResult();

                if (!updSuccess || updated == null) throw new Exception($"Update failed: {updError}");
                if (updated.DependencyType != DependencyType.Optional) throw new Exception("DependencyType was not updated");
                if (updated.UpdatedAt == null) throw new Exception("UpdatedAt timestamp was not updated");
                if (updated.TenantId != tenantId) throw new Exception("TenantId must remain immutable");
                if (updated.DependentSystemId != tms.Id || updated.DependencySystemId != wms.Id) throw new Exception("System IDs must remain immutable");
            });

            // 10. Successful deletion preserves referenced LogisticsSystems
            RunTest("Deletion removes only the dependency relationship and preserves both LogisticsSystems", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);
                sysRepo.Seed(wms);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (_, _, created) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = wms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                var (delSuccess, delError, _) = service.DeleteAsync(created!.Id).GetAwaiter().GetResult();
                if (!delSuccess) throw new Exception($"Deletion failed: {delError}");

                // Confirm dependency is gone
                var afterDelete = service.GetByIdAsync(created.Id).GetAwaiter().GetResult();
                if (afterDelete != null) throw new Exception("Dependency was not removed from repository");

                // CRITICAL: Verify BOTH referenced LogisticsSystems are completely preserved
                var tmsAfter = sysRepo.GetByIdAsync(tms.Id).GetAwaiter().GetResult();
                var wmsAfter = sysRepo.GetByIdAsync(wms.Id).GetAwaiter().GetResult();
                if (tmsAfter == null) throw new Exception("Dependent LogisticsSystem was accidentally deleted!");
                if (wmsAfter == null) throw new Exception("Dependency LogisticsSystem was accidentally deleted!");
            });

            // 11. Tenant filtering: GetAllAsync returns only caller tenant's dependencies
            RunTest("GetAllAsync returns only dependencies belonging to the caller's tenant", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();

                var depA1 = new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = Guid.NewGuid(), DependencySystemId = Guid.NewGuid() };
                var depA2 = new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = Guid.NewGuid(), DependencySystemId = Guid.NewGuid() };
                var depB1 = new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantB, DependentSystemId = Guid.NewGuid(), DependencySystemId = Guid.NewGuid() };

                depRepo.AddAsync(depA1).GetAwaiter().GetResult();
                depRepo.AddAsync(depA2).GetAwaiter().GetResult();
                depRepo.AddAsync(depB1).GetAwaiter().GetResult();

                var contextA = new MockTenantContext { TenantId = tenantA };
                var serviceA = new SystemDependencyService(depRepo, sysRepo, contextA);

                var listA = serviceA.GetAllAsync().GetAwaiter().GetResult().ToList();
                if (listA.Count != 2) throw new Exception($"Expected 2 dependencies for Tenant A, got {listA.Count}");
                if (listA.Any(d => d.TenantId != tenantA)) throw new Exception("Tenant A received a dependency from Tenant B!");
            });

            // 12. Multiple systems of the same SystemType are allowed when IDs differ
            RunTest("Multiple systems of the same SystemType are allowed when IDs differ", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", SystemType = SystemType.TMS, LifecycleStatus = SystemLifecycleStatus.Active };
                var chicagoWms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Chicago WMS", SystemType = SystemType.WMS, LifecycleStatus = SystemLifecycleStatus.Active };
                var dallasWms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Dallas WMS", SystemType = SystemType.WMS, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);
                sysRepo.Seed(chicagoWms);
                sysRepo.Seed(dallasWms);

                var service = new SystemDependencyService(depRepo, sysRepo, context);

                var (s1, e1, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = chicagoWms.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                var (s2, e2, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = tms.Id,
                    DependencySystemId = dallasWms.Id,
                    DependencyType = DependencyType.Optional
                }).GetAwaiter().GetResult();

                if (!s1) throw new Exception($"First WMS dependency failed: {e1}");
                if (!s2) throw new Exception($"Second WMS dependency failed: {e2}");
            });

            // 13. Null / unauthenticated tenant context strictly fails closed
            RunTest("Operations fail closed when caller has no authenticated tenant context", () =>
            {
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var emptyContext = new MockTenantContext { TenantId = null };
                var service = new SystemDependencyService(depRepo, sysRepo, emptyContext);

                var all = service.GetAllAsync().GetAwaiter().GetResult();
                if (all.Any()) throw new Exception("GetAllAsync should return empty when unauthenticated");

                var one = service.GetByIdAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (one != null) throw new Exception("GetByIdAsync should return null when unauthenticated");

                var (createSuccess, _, _) = service.CreateAsync(new CreateSystemDependencyDTO
                {
                    DependentSystemId = Guid.NewGuid(),
                    DependencySystemId = Guid.NewGuid(),
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();
                if (createSuccess) throw new Exception("CreateAsync should fail when unauthenticated");

                var (updSuccess, _, _, _) = service.UpdateAsync(Guid.NewGuid(), new UpdateSystemDependencyDTO
                {
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();
                if (updSuccess) throw new Exception("UpdateAsync should fail when unauthenticated");

                var (delSuccess, _, _) = service.DeleteAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (delSuccess) throw new Exception("DeleteAsync should fail when unauthenticated");
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
