using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Dependencies.Tests;
using Backend.Modules.Graph.Services;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Models;

namespace Backend.Modules.Graph.Tests
{
    public static class GraphEngineTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ResilienceX — Graph Engine Test Suite");
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

            // 1. Direct dependency traversal
            RunTest("Direct dependency traversal identifies immediate dependents at depth 1", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Core ERP", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Chicago WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);

                // WMS depends on ERP (Provider = ERP, Dependent = WMS)
                depRepo.AddAsync(new SystemDependency
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    DependentSystemId = wms.Id,
                    DependencySystemId = erp.Id,
                    DependencyType = DependencyType.Mandatory
                }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, error, notFound, data) = service.GetDownstreamImpactAsync(erp.Id).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Failed: {error}");
                if (data.DisruptedSystem.Id != erp.Id) throw new Exception("Disrupted system mismatch");
                if (data.DirectDependentsCount != 1) throw new Exception($"Expected 1 direct dependent, got {data.DirectDependentsCount}");
                if (data.IndirectDependentsCount != 0) throw new Exception("Expected 0 indirect dependents");
                if (data.TotalAffectedSystemsCount != 1) throw new Exception("Expected 1 total affected system");

                var direct = data.DirectDependents.First();
                if (direct.SystemId != wms.Id) throw new Exception("Direct dependent ID mismatch");
                if (direct.Depth != 1) throw new Exception("Direct dependent depth must be 1");
                if (direct.DependencyType != DependencyType.Mandatory) throw new Exception("DependencyType mismatch");
                if (direct.DependencyPath.Count != 2 || direct.DependencyPath[0] != "Core ERP" || direct.DependencyPath[1] != "Chicago WMS")
                    throw new Exception("Dependency path mismatch");
            });

            // 2. Multi-level dependency traversal
            RunTest("Multi-level traversal identifies direct and indirect dependents with paths", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "ERP", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);
                sysRepo.Seed(tms);

                // WMS depends on ERP (ERP -> WMS)
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = wms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                // TMS depends on WMS (WMS -> TMS)
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = tms.Id, DependencySystemId = wms.Id, DependencyType = DependencyType.Optional }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, error, _, data) = service.GetDownstreamImpactAsync(erp.Id).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Failed: {error}");
                if (data.DirectDependentsCount != 1) throw new Exception("Expected 1 direct dependent (WMS)");
                if (data.IndirectDependentsCount != 1) throw new Exception("Expected 1 indirect dependent (TMS)");
                if (data.TotalAffectedSystemsCount != 2) throw new Exception("Expected 2 total affected systems");

                var direct = data.DirectDependents.First();
                if (direct.SystemId != wms.Id || direct.Depth != 1) throw new Exception("Direct dependent mismatch");

                var indirect = data.IndirectDependents.First();
                if (indirect.SystemId != tms.Id || indirect.Depth != 2) throw new Exception("Indirect dependent mismatch");
                if (indirect.DependencyPath.Count != 3 || indirect.DependencyPath[0] != "ERP" || indirect.DependencyPath[1] != "WMS" || indirect.DependencyPath[2] != "TMS")
                    throw new Exception("Indirect dependency path sequence mismatch");
            });

            // 3. No dependent systems (leaf node)
            RunTest("Leaf system with no dependents returns empty affected lists", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var tms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Isolated TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tms);

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, error, _, data) = service.GetDownstreamImpactAsync(tms.Id).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Failed: {error}");
                if (data.TotalAffectedSystemsCount != 0) throw new Exception("Expected 0 affected systems");
                if (data.DirectDependents.Any() || data.IndirectDependents.Any()) throw new Exception("Lists should be empty");
            });

            // 4. Multiple dependents (branching)
            RunTest("Branching dependencies identifies all parallel direct dependents", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Central ERP", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Warehouse WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var edi = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Partner EDI", LifecycleStatus = SystemLifecycleStatus.Active };
                var telematics = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Fleet Telematics", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);
                sysRepo.Seed(edi);
                sysRepo.Seed(telematics);

                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = wms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = edi.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = telematics.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Optional }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, error, _, data) = service.GetDownstreamImpactAsync(erp.Id).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Failed: {error}");
                if (data.DirectDependentsCount != 3) throw new Exception($"Expected 3 direct dependents, got {data.DirectDependentsCount}");
                if (data.IndirectDependentsCount != 0) throw new Exception("Expected 0 indirect dependents");

                var names = data.DirectDependents.Select(d => d.SystemName).ToHashSet();
                if (!names.Contains("Warehouse WMS") || !names.Contains("Partner EDI") || !names.Contains("Fleet Telematics"))
                    throw new Exception("Missing one or more expected branch dependents");
            });

            // 5. Cyclic dependency protection
            RunTest("Cyclic dependencies terminate safely without infinite loops or duplicate visits", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var sysA = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System A", LifecycleStatus = SystemLifecycleStatus.Active };
                var sysB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System B", LifecycleStatus = SystemLifecycleStatus.Active };
                var sysC = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System C", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sysA);
                sysRepo.Seed(sysB);
                sysRepo.Seed(sysC);

                // Cycle: A -> B -> C -> A
                // B depends on A (A provider for B)
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysB.Id, DependencySystemId = sysA.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                // C depends on B (B provider for C)
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysC.Id, DependencySystemId = sysB.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                // A depends on C (C provider for A)
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysA.Id, DependencySystemId = sysC.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);

                // Run impact analysis with timeout safety
                var task = Task.Run(() => service.GetDownstreamImpactAsync(sysA.Id));
                if (!task.Wait(TimeSpan.FromSeconds(3)))
                {
                    throw new Exception("Cyclic dependency caused infinite loop / timeout!");
                }

                var (success, error, _, data) = task.Result;
                if (!success || data == null) throw new Exception($"Failed: {error}");

                // Should visit B (direct) and C (indirect) exactly once, without re-visiting A
                if (data.TotalAffectedSystemsCount != 2) throw new Exception($"Expected 2 affected systems in cycle, got {data.TotalAffectedSystemsCount}");
                if (data.DirectDependentsCount != 1 || data.DirectDependents.First().SystemId != sysB.Id) throw new Exception("Direct dependent should be B");
                if (data.IndirectDependentsCount != 1 || data.IndirectDependents.First().SystemId != sysC.Id) throw new Exception("Indirect dependent should be C");
            });

            // 6. Duplicate traversal prevention (diamond graph)
            RunTest("Diamond topology prevents duplicate entries and preserves shortest path", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var sysA = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System A", LifecycleStatus = SystemLifecycleStatus.Active };
                var sysB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System B", LifecycleStatus = SystemLifecycleStatus.Active };
                var sysC = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System C", LifecycleStatus = SystemLifecycleStatus.Active };
                var sysD = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "System D", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sysA);
                sysRepo.Seed(sysB);
                sysRepo.Seed(sysC);
                sysRepo.Seed(sysD);

                // Diamond: A -> B -> D and A -> C -> D
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysB.Id, DependencySystemId = sysA.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysC.Id, DependencySystemId = sysA.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysD.Id, DependencySystemId = sysB.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = sysD.Id, DependencySystemId = sysC.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, error, _, data) = service.GetDownstreamImpactAsync(sysA.Id).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception($"Failed: {error}");
                if (data.TotalAffectedSystemsCount != 3) throw new Exception($"Expected exactly 3 affected systems (B, C, D), got {data.TotalAffectedSystemsCount}");
                if (data.DirectDependentsCount != 2) throw new Exception("Expected 2 direct dependents (B, C)");
                if (data.IndirectDependentsCount != 1) throw new Exception("Expected 1 indirect dependent (D visited once)");

                var dCount = data.IndirectDependents.Count(x => x.SystemId == sysD.Id);
                if (dCount != 1) throw new Exception($"System D must appear exactly once, but appeared {dCount} times");
            });

            // 7. Mandatory and Optional dependency preservation
            RunTest("Preserves whether each dependency relationship is Mandatory or Optional", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "ERP", LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                var report = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Reporting", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);
                sysRepo.Seed(report);

                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = wms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = report.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Optional }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, _, _, data) = service.GetDownstreamImpactAsync(erp.Id).GetAwaiter().GetResult();

                if (!success || data == null) throw new Exception("Failed to get impact");

                var wmsAffected = data.DirectDependents.First(d => d.SystemId == wms.Id);
                var reportAffected = data.DirectDependents.First(d => d.SystemId == report.Id);

                if (wmsAffected.DependencyType != DependencyType.Mandatory) throw new Exception("Expected Mandatory dependency type for WMS");
                if (reportAffected.DependencyType != DependencyType.Optional) throw new Exception("Expected Optional dependency type for Reporting");
            });

            // 8. Inactive system exclusion
            RunTest("Inactive systems do not participate in operational impact calculations", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "ERP", LifecycleStatus = SystemLifecycleStatus.Active };
                var inactiveWms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Decommissioned WMS", LifecycleStatus = SystemLifecycleStatus.Inactive };
                var activeTms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Active TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(inactiveWms);
                sysRepo.Seed(activeTms);

                // ERP -> Inactive WMS -> Active TMS
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = inactiveWms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantId, DependentSystemId = activeTms.Id, DependencySystemId = inactiveWms.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();

                var service = new GraphEngineService(depRepo, sysRepo, context);

                // Case A: Traversal ignores inactive intermediate system and its downstream
                var (s1, _, _, data1) = service.GetDownstreamImpactAsync(erp.Id).GetAwaiter().GetResult();
                if (!s1 || data1 == null) throw new Exception("Failed to compute impact");
                if (data1.TotalAffectedSystemsCount != 0) throw new Exception("Inactive systems and their downstream chain must be excluded from operational impact");

                // Case B: Disrupting an inactive system directly returns failure
                var (s2, e2, notFound2, _) = service.GetDownstreamImpactAsync(inactiveWms.Id).GetAwaiter().GetResult();
                if (s2) throw new Exception("Disrupting an inactive system must be rejected");
                if (notFound2) throw new Exception("Inactive system exists so notFound should be false");
                if (!e2!.Contains("inactive", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error: {e2}");
            });

            // 9. Cross-tenant isolation
            RunTest("Cross-tenant isolation ensures Tenant A cannot view or traverse Tenant B graph", () =>
            {
                var tenantA = Guid.NewGuid();
                var tenantB = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();

                var erpA = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "ERP A", LifecycleStatus = SystemLifecycleStatus.Active };
                var wmsA = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "WMS A", LifecycleStatus = SystemLifecycleStatus.Active };
                var erpB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantB, Name = "ERP B", LifecycleStatus = SystemLifecycleStatus.Active };
                var wmsB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantB, Name = "WMS B", LifecycleStatus = SystemLifecycleStatus.Active };

                sysRepo.Seed(erpA);
                sysRepo.Seed(wmsA);
                sysRepo.Seed(erpB);
                sysRepo.Seed(wmsB);

                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = wmsA.Id, DependencySystemId = erpA.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();
                depRepo.AddAsync(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantB, DependentSystemId = wmsB.Id, DependencySystemId = erpB.Id, DependencyType = DependencyType.Mandatory }).GetAwaiter().GetResult();

                var contextA = new MockTenantContext { TenantId = tenantA };
                var serviceA = new GraphEngineService(depRepo, sysRepo, contextA);

                // Tenant A disrupts ERP A
                var (sA, _, _, dataA) = serviceA.GetDownstreamImpactAsync(erpA.Id).GetAwaiter().GetResult();
                if (!sA || dataA == null) throw new Exception("Tenant A impact computation failed");
                if (dataA.DirectDependentsCount != 1 || dataA.DirectDependents.First().SystemId != wmsA.Id) throw new Exception("Tenant A must see only its own dependent");

                // Tenant A tries to request impact for Tenant B's ERP
                var (sB, _, notFoundB, _) = serviceA.GetDownstreamImpactAsync(erpB.Id).GetAwaiter().GetResult();
                if (sB || !notFoundB) throw new Exception("Tenant A must receive NotFound when targeting Tenant B system");
            });

            // 10. Invalid or nonexistent system ID
            RunTest("Nonexistent system ID returns NotFound", () =>
            {
                var tenantId = Guid.NewGuid();
                var depRepo = new InMemorySystemDependencyRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForDep();
                var context = new MockTenantContext { TenantId = tenantId };

                var service = new GraphEngineService(depRepo, sysRepo, context);
                var (success, error, notFound, data) = service.GetDownstreamImpactAsync(Guid.NewGuid()).GetAwaiter().GetResult();

                if (success || !notFound) throw new Exception("Nonexistent system ID must return NotFound");
                if (data != null) throw new Exception("Data must be null on notFound");
                if (!error!.Contains("not found", StringComparison.OrdinalIgnoreCase)) throw new Exception($"Unexpected error message: {error}");
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
