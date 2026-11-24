using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Dependencies.Interfaces;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Graph.Services;
using Backend.Modules.ImpactAnalysis.DTOs;
using Backend.Modules.ImpactAnalysis.Enums;
using Backend.Modules.ImpactAnalysis.Services;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Interfaces;
using Backend.Modules.Operations.Models;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.ImpactAnalysis.Tests
{
    public class ImpactMockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public bool HasTenant => TenantId.HasValue;
    }

    public class InMemoryLogisticsSystemRepositoryForImpact : ILogisticsSystemRepository
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
            var idx = _systems.FindIndex(s => s.Id == system.Id);
            if (idx >= 0) _systems[idx] = system;
            return Task.FromResult(system);
        }

        public Task DeleteAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _systems.RemoveAll(s => s.Id == system.Id);
            return Task.CompletedTask;
        }
    }

    public class InMemorySystemDependencyRepositoryForImpact : ISystemDependencyRepository
    {
        private readonly List<SystemDependency> _dependencies = new();

        public void Seed(SystemDependency dep) => _dependencies.Add(dep);

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
            if (idx >= 0) _dependencies[idx] = dependency;
            return Task.FromResult(dependency);
        }

        public Task DeleteAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            _dependencies.RemoveAll(d => d.Id == dependency.Id);
            return Task.CompletedTask;
        }
    }

    public class InMemoryBusinessOperationRepositoryForImpact : IBusinessOperationRepository
    {
        private readonly List<BusinessOperation> _operations = new();
        private readonly List<SystemBusinessOperation> _assignments = new();

        public void SeedOperation(BusinessOperation op) => _operations.Add(op);
        public void SeedAssignment(SystemBusinessOperation assignment)
        {
            _assignments.Add(assignment);
            var op = _operations.FirstOrDefault(o => o.Id == assignment.BusinessOperationId);
            if (op != null && !op.SupportingSystems.Any(s => s.Id == assignment.Id))
            {
                op.SupportingSystems.Add(assignment);
            }
        }

        public Task<IEnumerable<BusinessOperation>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _operations.AsEnumerable();
            if (!includeInactive) query = query.Where(o => o.LifecycleStatus == OperationLifecycleStatus.Active);
            return Task.FromResult<IEnumerable<BusinessOperation>>(query.ToList());
        }

        public Task<BusinessOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_operations.FirstOrDefault(o => o.Id == id));
        }

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _operations.AsQueryable();
            if (excludeId.HasValue) query = query.Where(o => o.Id != excludeId.Value);
            return Task.FromResult(query.Any(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<BusinessOperation> AddAsync(BusinessOperation operation, CancellationToken cancellationToken = default)
        {
            _operations.Add(operation);
            return Task.FromResult(operation);
        }

        public Task<BusinessOperation> UpdateAsync(BusinessOperation operation, CancellationToken cancellationToken = default)
        {
            var idx = _operations.FindIndex(o => o.Id == operation.Id);
            if (idx >= 0) _operations[idx] = operation;
            return Task.FromResult(operation);
        }

        public Task DeleteAsync(BusinessOperation operation, CancellationToken cancellationToken = default)
        {
            _operations.RemoveAll(o => o.Id == operation.Id);
            return Task.CompletedTask;
        }

        public Task<SystemBusinessOperation?> GetAssignmentAsync(Guid operationId, Guid systemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_assignments.FirstOrDefault(a => a.BusinessOperationId == operationId && a.LogisticsSystemId == systemId));
        }

        public Task<bool> AssignmentExistsAsync(Guid operationId, Guid systemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_assignments.Any(a => a.BusinessOperationId == operationId && a.LogisticsSystemId == systemId));
        }

        public Task<SystemBusinessOperation> AddAssignmentAsync(SystemBusinessOperation assignment, CancellationToken cancellationToken = default)
        {
            SeedAssignment(assignment);
            return Task.FromResult(assignment);
        }

        public Task RemoveAssignmentAsync(SystemBusinessOperation assignment, CancellationToken cancellationToken = default)
        {
            _assignments.RemoveAll(a => a.Id == assignment.Id || (a.BusinessOperationId == assignment.BusinessOperationId && a.LogisticsSystemId == assignment.LogisticsSystemId));
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SystemBusinessOperation>> GetSystemsByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default)
        {
            var list = _assignments.Where(a => a.BusinessOperationId == operationId).ToList();
            return Task.FromResult<IEnumerable<SystemBusinessOperation>>(list);
        }

        public Task<IEnumerable<SystemBusinessOperation>> GetOperationsBySystemIdAsync(Guid systemId, CancellationToken cancellationToken = default)
        {
            var list = _assignments.Where(a => a.LogisticsSystemId == systemId).ToList();
            return Task.FromResult<IEnumerable<SystemBusinessOperation>>(list);
        }
    }

    public static class ImpactAnalysisTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("Running Impact Analysis Engine Test Suite");
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
                    Console.WriteLine($"[FAIL] {testName}");
                    Console.WriteLine($"       Error: {ex.Message}");
                    failed++;
                }
            }

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            // 1. Direct and Indirect system impact on operations
            RunTest("Identifies both direct (depth 0) and indirect (downstream) operations distinctly", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                // ERP -> WMS (Mandatory)
                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "ERP Core", Criticality = SystemCriticality.Critical, LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Main WMS", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);

                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = wms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory });

                // Direct operation on ERP
                var directOp = new BusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Financial Billing", Criticality = OperationCriticality.High, AvailabilityRule = AvailabilityRule.AllRequired };
                opRepo.SeedOperation(directOp);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = directOp.Id, LogisticsSystemId = erp.Id });

                // Indirect operation on WMS
                var indirectOp = new BusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Warehouse Picking", Criticality = OperationCriticality.Critical, AvailabilityRule = AvailabilityRule.AllRequired };
                opRepo.SeedOperation(indirectOp);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = indirectOp.Id, LogisticsSystemId = wms.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (success, error, _, data) = impactService.AnalyzeImpactAsync(erp.Id).GetAwaiter().GetResult();

                if (!success) throw new Exception($"AnalyzeImpactAsync failed: {error}");
                if (data == null) throw new Exception("Impact data is null");
                if (data.TotalAffectedSystemsCount != 1) throw new Exception($"Expected 1 downstream affected system, got {data.TotalAffectedSystemsCount}");
                if (data.TotalAffectedOperationsCount != 2) throw new Exception($"Expected 2 affected operations, got {data.TotalAffectedOperationsCount}");

                var directResult = data.AffectedOperations.FirstOrDefault(o => o.OperationId == directOp.Id);
                if (directResult == null) throw new Exception("Direct operation not found in results");
                if (directResult.ImpactType != ImpactType.Direct) throw new Exception($"Expected Direct impact for ERP, got {directResult.ImpactType}");
                if (directResult.ContributingSystemId != erp.Id) throw new Exception("Contributing system should be ERP");

                var indirectResult = data.AffectedOperations.FirstOrDefault(o => o.OperationId == indirectOp.Id);
                if (indirectResult == null) throw new Exception("Indirect operation not found in results");
                if (indirectResult.ImpactType != ImpactType.Indirect) throw new Exception($"Expected Indirect impact for WMS, got {indirectResult.ImpactType}");
                if (indirectResult.ContributingSystemId != wms.Id) throw new Exception("Contributing system should be WMS");
                if (!indirectResult.ContributingDependencyPath.SequenceEqual(new[] { "ERP Core", "Main WMS" }))
                {
                    throw new Exception("Dependency path does not match expected traversal");
                }
            });

            // 2. AllRequired availability evaluation
            RunTest("AllRequired availability evaluation marks operation PotentiallyAffected if ANY supporting system is affected", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var sys1 = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Scanner Gateway", Criticality = SystemCriticality.Medium, LifecycleStatus = SystemLifecycleStatus.Active };
                var sys2 = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Sorting Controller", Criticality = SystemCriticality.Medium, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys1);
                sysRepo.Seed(sys2);

                // Operation requires BOTH sys1 and sys2
                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Automated Parcel Sorting",
                    Criticality = OperationCriticality.High,
                    AvailabilityRule = AvailabilityRule.AllRequired
                };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = sys1.Id });
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = sys2.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                // Disruption on sys1 only
                var (success, _, _, data) = impactService.AnalyzeImpactAsync(sys1.Id).GetAwaiter().GetResult();
                if (!success) throw new Exception("Analysis failed");

                var opResult = data!.AffectedOperations.FirstOrDefault(o => o.OperationId == op.Id);
                if (opResult == null) throw new Exception("Operation not in affected list");
                if (opResult.AvailabilityStatus != OperationAvailabilityStatus.PotentiallyAffected)
                {
                    throw new Exception($"Expected PotentiallyAffected for AllRequired rule, got {opResult.AvailabilityStatus}");
                }
                if (!opResult.AvailabilityReason.Contains("requires all supporting systems"))
                {
                    throw new Exception($"Reason should explain AllRequired rule, got: {opResult.AvailabilityReason}");
                }
            });

            // 3. AnyAvailable availability evaluation: All alternatives affected
            RunTest("AnyAvailable rule: marks PotentiallyAffected when ALL supporting systems are affected", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                // Hub switch causes both primary and secondary systems to fail
                var hub = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Network Hub", Criticality = SystemCriticality.Critical, LifecycleStatus = SystemLifecycleStatus.Active };
                var primary = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Primary TMS", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                var secondary = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Secondary TMS", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(hub);
                sysRepo.Seed(primary);
                sysRepo.Seed(secondary);

                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = primary.Id, DependencySystemId = hub.Id, DependencyType = DependencyType.Mandatory });
                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = secondary.Id, DependencySystemId = hub.Id, DependencyType = DependencyType.Mandatory });

                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Dispatch Routing",
                    Criticality = OperationCriticality.Critical,
                    AvailabilityRule = AvailabilityRule.AnyAvailable
                };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = primary.Id });
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = secondary.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (_, _, _, data) = impactService.AnalyzeImpactAsync(hub.Id).GetAwaiter().GetResult();
                var opResult = data!.AffectedOperations.FirstOrDefault(o => o.OperationId == op.Id);

                if (opResult == null) throw new Exception("Operation not in affected list");
                if (opResult.AvailabilityStatus != OperationAvailabilityStatus.PotentiallyAffected)
                {
                    throw new Exception($"Expected PotentiallyAffected when all AnyAvailable systems fail, got {opResult.AvailabilityStatus}");
                }
                if (!opResult.AvailabilityReason.Contains("All 2 alternative"))
                {
                    throw new Exception($"Expected reason mentioning all alternatives affected, got: {opResult.AvailabilityReason}");
                }
            });

            // 4. AnyAvailable availability evaluation: Partial failure -> RequiresVerification (Unknown health of remaining)
            RunTest("AnyAvailable rule: marks RequiresVerification when only SOME supporting systems fail and health of remaining cannot be confirmed", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var primaryWms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Chicago WMS", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                var backupWms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Dallas WMS", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(primaryWms);
                sysRepo.Seed(backupWms);

                // No dependency between Chicago and Dallas WMS
                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Inventory Fulfillment",
                    Criticality = OperationCriticality.High,
                    AvailabilityRule = AvailabilityRule.AnyAvailable
                };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = primaryWms.Id });
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = backupWms.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                // Chicago WMS fails. Dallas WMS is outside the failure blast radius, but health is not confirmed.
                var (_, _, _, data) = impactService.AnalyzeImpactAsync(primaryWms.Id).GetAwaiter().GetResult();
                var opResult = data!.AffectedOperations.FirstOrDefault(o => o.OperationId == op.Id);

                if (opResult == null) throw new Exception("Operation should be in affected list for verification");
                if (opResult.AvailabilityStatus != OperationAvailabilityStatus.RequiresVerification)
                {
                    throw new Exception($"Expected RequiresVerification for partial AnyAvailable failure, got {opResult.AvailabilityStatus}");
                }
                if (!opResult.AvailabilityReason.Contains("require operational verification"))
                {
                    throw new Exception($"Expected reason to state verification required, got: {opResult.AvailabilityReason}");
                }
            });

            // 5. Weighted severity calculation and scoring dimensions (30% System, 20% Dep, 50% Op)
            RunTest("Weighted severity calculation conforms exactly to 30% System, 20% Dep, 50% Op formula", () =>
            {
                // Test 1: All Critical/Mandatory: (4/4*30) + (4/4*20) + (4/4*50) = 30 + 20 + 50 = 100.0 (Critical)
                var (score1, bd1, sev1) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.Critical, DependencyType.Mandatory, OperationCriticality.Critical);
                if (score1 != 100.0) throw new Exception($"Expected 100.0, got {score1}");
                if (sev1 != OperationImpactSeverity.Critical) throw new Exception($"Expected Critical, got {sev1}");
                if (bd1.SystemCriticalityScore != 30.0 || bd1.DependencyTypeScore != 20.0 || bd1.OperationCriticalityScore != 50.0)
                {
                    throw new Exception("Scoring breakdown component mismatch for 100.0 score");
                }

                // Test 2: All Low/Optional: (1/4*30) + (1/4*20) + (1/4*50) = 7.5 + 5.0 + 12.5 = 25.0 (Low)
                var (score2, bd2, sev2) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.Low, DependencyType.Optional, OperationCriticality.Low);
                if (score2 != 25.0) throw new Exception($"Expected 25.0, got {score2}");
                if (sev2 != OperationImpactSeverity.Low) throw new Exception($"Expected Low, got {sev2}");
                if (bd2.SystemCriticalityScore != 7.5 || bd2.DependencyTypeScore != 5.0 || bd2.OperationCriticalityScore != 12.5)
                {
                    throw new Exception("Scoring breakdown component mismatch for 25.0 score");
                }

                // Test 3: Mixed: Medium (2), Mandatory (4), High (3):
                // System: (2/4)*30 = 15.0
                // Dep: (4/4)*20 = 20.0
                // Op: (3/4)*50 = 37.5
                // Total = 15.0 + 20.0 + 37.5 = 72.5 (High)
                var (score3, _, sev3) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.Medium, DependencyType.Mandatory, OperationCriticality.High);
                if (score3 != 72.5) throw new Exception($"Expected 72.5, got {score3}");
                if (sev3 != OperationImpactSeverity.High) throw new Exception($"Expected High, got {sev3}");
            });

            // 6. Severity threshold boundaries (0–39 Low, 40–59 Medium, 60–79 High, 80–100 Critical)
            RunTest("Severity thresholds correctly classify boundaries: 0-39 Low, 40-59 Medium, 60-79 High, 80-100 Critical", () =>
            {
                // Boundary check: Low (35.0) -> Low
                // Low(1)=7.5 + Optional(1)=5.0 + Medium(2)=25.0 = 37.5 -> Low
                var (sLow, _, sevLow) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.Low, DependencyType.Optional, OperationCriticality.Medium);
                if (sLow != 37.5 || sevLow != OperationImpactSeverity.Low) throw new Exception($"Expected 37.5 Low, got {sLow} {sevLow}");

                // Boundary check: 45.0 -> Medium
                // Medium(2)=15.0 + Optional(1)=5.0 + Medium(2)=25.0 = 45.0 -> Medium
                var (sMed, _, sevMed) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.Medium, DependencyType.Optional, OperationCriticality.Medium);
                if (sMed != 45.0 || sevMed != OperationImpactSeverity.Medium) throw new Exception($"Expected 45.0 Medium, got {sMed} {sevMed}");

                // Boundary check: 60.0 -> High (exact boundary 60.0)
                // Medium(2)=15.0 + Mandatory(4)=20.0 + Medium(2)=25.0 = 60.0 -> High
                var (sHigh, _, sevHigh) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.Medium, DependencyType.Mandatory, OperationCriticality.Medium);
                if (sHigh != 60.0 || sevHigh != OperationImpactSeverity.High) throw new Exception($"Expected 60.0 High, got {sHigh} {sevHigh}");

                // Boundary check: 80.0 -> Critical (exact boundary 80.0)
                // High(3)=22.5 + Mandatory(4)=20.0 + High(3)=37.5 = 80.0 -> Critical
                var (sCrit, _, sevCrit) = ImpactAnalysisService.CalculateSeverityScore(SystemCriticality.High, DependencyType.Mandatory, OperationCriticality.High);
                if (sCrit != 80.0 || sevCrit != OperationImpactSeverity.Critical) throw new Exception($"Expected 80.0 Critical, got {sCrit} {sevCrit}");
            });

            // 7. Multiple affected systems supporting one operation: Deterministic aggregation without double-counting
            RunTest("Deterministic aggregation selects worst-case severity path without double-counting the operation", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                // ERP (Critical, Depth 0) -> WMS (Low, Mandatory, Depth 1)
                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "ERP Core", Criticality = SystemCriticality.Critical, LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Aux WMS", Criticality = SystemCriticality.Low, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);

                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = wms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory });

                // Operation supported by BOTH ERP and WMS
                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Enterprise Reporting",
                    Criticality = OperationCriticality.High,
                    AvailabilityRule = AvailabilityRule.AllRequired
                };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = erp.Id });
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = wms.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (_, _, _, data) = impactService.AnalyzeImpactAsync(erp.Id).GetAwaiter().GetResult();

                // Confirm NO double-counting
                var matchingOps = data!.AffectedOperations.Where(o => o.OperationId == op.Id).ToList();
                if (matchingOps.Count != 1)
                {
                    throw new Exception($"Expected operation to appear exactly once, but appeared {matchingOps.Count} times (duplicate count error)");
                }

                var winning = matchingOps[0];
                // ERP score: (4/4*30) + (4/4*20) + (3/4*50) = 30 + 20 + 37.5 = 87.5 (Critical)
                // WMS score: (1/4*30) + (4/4*20) + (3/4*50) = 7.5 + 20 + 37.5 = 65.0 (High)
                // Worst-case aggregation must choose ERP (87.5 Critical)
                if (winning.ContributingSystemId != erp.Id)
                {
                    throw new Exception($"Expected ERP to be the worst-case contributing system, got {winning.ContributingSystemName}");
                }
                if (winning.NumericSeverityScore != 87.5)
                {
                    throw new Exception($"Expected score 87.5, got {winning.NumericSeverityScore}");
                }
                if (winning.PotentialImpactSeverity != OperationImpactSeverity.Critical)
                {
                    throw new Exception($"Expected Critical severity, got {winning.PotentialImpactSeverity}");
                }
                if (winning.AffectedSupportingSystemsCount != 2)
                {
                    throw new Exception($"Expected 2 affected supporting systems recorded, got {winning.AffectedSupportingSystemsCount}");
                }
            });

            // 8. Cycles and diamond topologies in the graph
            RunTest("Cycle in graph terminates safely and calculates downstream impact without infinite loops", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                // A -> B -> C -> A (cycle)
                var sysA = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "SysA", Criticality = SystemCriticality.Medium, LifecycleStatus = SystemLifecycleStatus.Active };
                var sysB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "SysB", Criticality = SystemCriticality.Medium, LifecycleStatus = SystemLifecycleStatus.Active };
                var sysC = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "SysC", Criticality = SystemCriticality.Medium, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sysA);
                sysRepo.Seed(sysB);
                sysRepo.Seed(sysC);

                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = sysB.Id, DependencySystemId = sysA.Id, DependencyType = DependencyType.Mandatory });
                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = sysC.Id, DependencySystemId = sysB.Id, DependencyType = DependencyType.Mandatory });
                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = sysA.Id, DependencySystemId = sysC.Id, DependencyType = DependencyType.Optional });

                var op = new BusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Cycle Operation", Criticality = OperationCriticality.Medium, AvailabilityRule = AvailabilityRule.AllRequired };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = sysC.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (success, _, _, data) = impactService.AnalyzeImpactAsync(sysA.Id).GetAwaiter().GetResult();
                if (!success) throw new Exception("Cyclic analysis failed");
                if (data!.AffectedOperations.Count != 1) throw new Exception("Expected 1 affected operation in cycle test");
            });

            // 9. Operations with no supporting systems are not impacted
            RunTest("Operations with no supporting systems are excluded from impact results", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Lone System", Criticality = SystemCriticality.Medium, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                // Operation with zero supporting systems
                var emptyOp = new BusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Manual Clipboard Review", Criticality = OperationCriticality.Low, AvailabilityRule = AvailabilityRule.AllRequired };
                opRepo.SeedOperation(emptyOp);

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (_, _, _, data) = impactService.AnalyzeImpactAsync(sys.Id).GetAwaiter().GetResult();
                if (data!.AffectedOperations.Any(o => o.OperationId == emptyOp.Id))
                {
                    throw new Exception("Operation with zero supporting systems should not be affected");
                }
            });

            // 10. Operations whose supporting systems are completely outside blast radius are excluded
            RunTest("Operations supported only by unaffected systems are not marked as affected", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var failedSys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Customs Gateway", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                var unrelatedSys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Warehouse HVAC", Criticality = SystemCriticality.Low, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(failedSys);
                sysRepo.Seed(unrelatedSys);

                var op = new BusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Cold Storage Control", Criticality = OperationCriticality.High, AvailabilityRule = AvailabilityRule.AllRequired };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = unrelatedSys.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (_, _, _, data) = impactService.AnalyzeImpactAsync(failedSys.Id).GetAwaiter().GetResult();
                if (data!.AffectedOperations.Any(o => o.OperationId == op.Id))
                {
                    throw new Exception("Unaffected operation appeared in affected operations list");
                }
            });

            // 11. Cross-tenant isolation prevents analyzing or leaking foreign tenant operations
            RunTest("Cross-tenant impact analysis is strictly blocked (fails closed)", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();

                var tenantASystem = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Tenant A Core", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                var tenantBSystem = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Tenant B Core", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tenantASystem);
                sysRepo.Seed(tenantBSystem);

                var tenantBOp = new BusinessOperation { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Tenant B Secret Op", Criticality = OperationCriticality.Critical, AvailabilityRule = AvailabilityRule.AllRequired };
                opRepo.SeedOperation(tenantBOp);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantB, BusinessOperationId = tenantBOp.Id, LogisticsSystemId = tenantBSystem.Id });

                // Tenant A tries to run impact analysis on Tenant B's system
                var contextA = new ImpactMockTenantContext { TenantId = tenantA };
                var graphServiceA = new GraphEngineService(depRepo, sysRepo, contextA);
                var impactServiceA = new ImpactAnalysisService(graphServiceA, sysRepo, opRepo, contextA);

                var (success, _, notFound, _) = impactServiceA.AnalyzeImpactAsync(tenantBSystem.Id).GetAwaiter().GetResult();
                if (success) throw new Exception("Tenant A was able to run impact analysis on Tenant B system!");
                if (!notFound) throw new Exception("Expected notFound = true for cross-tenant system");

                // Tenant A runs analysis on Tenant A system -> Tenant B operations MUST NOT be included
                var (_, _, _, dataA) = impactServiceA.AnalyzeImpactAsync(tenantASystem.Id).GetAwaiter().GetResult();
                if (dataA!.AffectedOperations.Any(o => o.OperationId == tenantBOp.Id))
                {
                    throw new Exception("Cross-tenant leakage: Tenant B operation appeared in Tenant A impact response!");
                }
            });

            // 12. Inactive system cannot participate in impact calculations
            RunTest("Inactive failed system is rejected from participating in impact analysis", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var inactiveSys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Decommissioned ERP", Criticality = SystemCriticality.Critical, LifecycleStatus = SystemLifecycleStatus.Inactive };
                sysRepo.Seed(inactiveSys);

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (success, error, notFound, _) = impactService.AnalyzeImpactAsync(inactiveSys.Id).GetAwaiter().GetResult();
                if (success) throw new Exception("Expected analysis on inactive system to fail");
                if (notFound) throw new Exception("Inactive system should return 400 Bad Request, not NotFound");
                if (string.IsNullOrWhiteSpace(error) || !error.Contains("inactive"))
                {
                    throw new Exception($"Expected inactive system error message, got: {error}");
                }
            });

            // 13. Missing system ID returns NotFound
            RunTest("Nonexistent system ID returns NotFound", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);

                var (success, _, notFound, _) = impactService.AnalyzeImpactAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (success || !notFound) throw new Exception("Expected notFound = true for random GUID");
            });

            // 14. Unauthenticated caller fails closed
            RunTest("Unauthenticated caller fails closed without tenant context", () =>
            {
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var unauthContext = new ImpactMockTenantContext { TenantId = null };

                var graphService = new GraphEngineService(depRepo, sysRepo, unauthContext);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, unauthContext);

                var (success, error, _, _) = impactService.AnalyzeImpactAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (success) throw new Exception("Unauthenticated call should fail");
                if (string.IsNullOrWhiteSpace(error) || !error.Contains("authenticated tenant context"))
                {
                    throw new Exception($"Expected unauthenticated context error message, got: {error}");
                }
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
