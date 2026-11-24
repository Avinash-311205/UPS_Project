using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Graph.Services;
using Backend.Modules.ImpactAnalysis.Enums;
using Backend.Modules.ImpactAnalysis.Services;
using Backend.Modules.ImpactAnalysis.Tests;
using Backend.Modules.Incidents.DTOs;
using Backend.Modules.Incidents.Enums;
using Backend.Modules.Incidents.Interfaces;
using Backend.Modules.Incidents.Models;
using Backend.Modules.Incidents.Services;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Models;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Incidents.Tests
{
    public class IncidentMockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public bool HasTenant => TenantId.HasValue;
    }

    public class InMemoryIncidentRepository : IIncidentRepository
    {
        private readonly List<Incident> _incidents = new();

        public List<Incident> RawIncidents => _incidents;

        public Task<IEnumerable<Incident>> GetAllAsync(
            IncidentStatus? status = null,
            IncidentSeverity? severity = null,
            IncidentType? type = null,
            Guid? failedSystemId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _incidents.AsEnumerable();

            if (status.HasValue) query = query.Where(i => i.Status == status.Value);
            if (severity.HasValue) query = query.Where(i => i.Severity == severity.Value);
            if (type.HasValue) query = query.Where(i => i.IncidentType == type.Value);
            if (failedSystemId.HasValue) query = query.Where(i => i.FailedSystemId == failedSystemId.Value);

            return Task.FromResult<IEnumerable<Incident>>(query.OrderByDescending(i => i.CreatedAt).ToList());
        }

        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var incident = _incidents.FirstOrDefault(i => i.Id == id);
            return Task.FromResult(incident);
        }

        public Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            _incidents.Add(incident);
            return Task.FromResult(incident);
        }

        public Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            var idx = _incidents.FindIndex(i => i.Id == incident.Id);
            if (idx >= 0) _incidents[idx] = incident;
            return Task.FromResult(incident);
        }

        public Task DeleteAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            _incidents.RemoveAll(i => i.Id == incident.Id);
            return Task.CompletedTask;
        }
    }

    public static class IncidentTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("Running Incident Management Engine Test Suite");
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

            // 1. Incident creation and retrieval
            RunTest("Create incident assigns TenantId automatically and sets status to Open", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var system = new LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Fleet Dispatch Core",
                    Criticality = SystemCriticality.Critical,
                    LifecycleStatus = SystemLifecycleStatus.Active
                };
                sysRepo.Seed(system);

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);
                var incidentService = new IncidentService(incidentRepo, sysRepo, impactService, context);

                var (success, error, _, data) = incidentService.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = system.Id,
                    Title = "Ransomware Detected on Fleet Dispatch",
                    Description = "Host isolation initiated due to suspicious encryption activity.",
                    IncidentType = IncidentType.Ransomware,
                    Severity = IncidentSeverity.Critical
                }).GetAwaiter().GetResult();

                if (!success) throw new Exception($"Create failed: {error}");
                if (data == null) throw new Exception("Response data is null");
                if (data.TenantId != tenantA) throw new Exception($"TenantId mismatch: {data.TenantId}");
                if (data.FailedSystemId != system.Id) throw new Exception("FailedSystemId mismatch");
                if (data.FailedSystemName != "Fleet Dispatch Core") throw new Exception($"FailedSystemName mismatch: {data.FailedSystemName}");
                if (data.Status != IncidentStatus.Open) throw new Exception($"Expected initial status Open, got {data.Status}");
                if (data.Severity != IncidentSeverity.Critical) throw new Exception("Severity mismatch");

                // Retrieve by ID
                var retrieved = incidentService.GetByIdAsync(data.Id).GetAwaiter().GetResult();
                if (retrieved == null) throw new Exception("Failed to retrieve incident by ID");
                if (retrieved.Title != "Ransomware Detected on Fleet Dispatch") throw new Exception("Retrieved title mismatch");
            });

            // 2. Title required validation
            RunTest("Validation rejects empty or whitespace incident title", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var system = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Sys", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(system);

                var incidentService = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (success, error, _, _) = incidentService.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = system.Id,
                    Title = "   ",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.High
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Expected creation to fail for empty title");
                if (string.IsNullOrWhiteSpace(error) || !error.Contains("title")) throw new Exception("Expected title error message");
            });

            // 3. Rejection of inactive systems
            RunTest("Cannot create an incident for an inactive logistics system", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var inactiveSystem = new LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Retired WMS",
                    LifecycleStatus = SystemLifecycleStatus.Inactive
                };
                sysRepo.Seed(inactiveSystem);

                var incidentService = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (success, error, notFound, _) = incidentService.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = inactiveSystem.Id,
                    Title = "Outage on Retired WMS",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.Low
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Expected creation to fail for inactive system");
                if (notFound) throw new Exception("Inactive system should return 400 Bad Request, not NotFound");
                if (string.IsNullOrWhiteSpace(error) || !error.Contains("inactive")) throw new Exception($"Expected inactive error message, got: {error}");
            });

            // 4. Rejection of nonexistent system
            RunTest("Creating incident for nonexistent system returns NotFound", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };
                var incidentService = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (success, _, notFound, _) = incidentService.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = Guid.NewGuid(),
                    Title = "Ghost Outage",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.Medium
                }).GetAwaiter().GetResult();

                if (success || !notFound) throw new Exception("Expected notFound = true for missing system");
            });

            // 5. Cross-tenant system rejection during creation
            RunTest("Tenant A cannot report an incident against Tenant B's logistics system", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();

                var tenantBSystem = new LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantB,
                    Name = "Tenant B Proprietary TMS",
                    LifecycleStatus = SystemLifecycleStatus.Active
                };
                sysRepo.Seed(tenantBSystem);

                var contextA = new IncidentMockTenantContext { TenantId = tenantA };
                var incidentServiceA = new IncidentService(incidentRepo, sysRepo, null!, contextA);

                var (success, _, notFound, _) = incidentServiceA.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = tenantBSystem.Id,
                    Title = "Cross-tenant intrusion attempt",
                    IncidentType = IncidentType.CyberAttack,
                    Severity = IncidentSeverity.Critical
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Tenant A was able to create incident for Tenant B system!");
                if (!notFound) throw new Exception("Cross-tenant system should return NotFound");
            });

            // 6. Cross-tenant retrieval, update, status change, and impact analysis blocked
            RunTest("Cross-tenant incident access is strictly prevented across all operations", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();

                var sysB = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Sys B", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sysB);

                var contextB = new IncidentMockTenantContext { TenantId = tenantB };
                var incidentServiceB = new IncidentService(incidentRepo, sysRepo, null!, contextB);

                var (_, _, _, incidentB) = incidentServiceB.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = sysB.Id,
                    Title = "Tenant B Incident",
                    IncidentType = IncidentType.HardwareFailure,
                    Severity = IncidentSeverity.Medium
                }).GetAwaiter().GetResult();

                var contextA = new IncidentMockTenantContext { TenantId = tenantA };
                var incidentServiceA = new IncidentService(incidentRepo, sysRepo, null!, contextA);

                // Tenant A tries to read Tenant B incident
                var readRes = incidentServiceA.GetByIdAsync(incidentB!.Id).GetAwaiter().GetResult();
                if (readRes != null) throw new Exception("Tenant A was able to read Tenant B incident!");

                // Tenant A tries to update Tenant B incident
                var (updSuccess, _, updNotFound, _) = incidentServiceA.UpdateAsync(incidentB.Id, new UpdateIncidentDTO
                {
                    Title = "Tampered Title",
                    IncidentType = IncidentType.Other,
                    Severity = IncidentSeverity.Low
                }).GetAwaiter().GetResult();
                if (updSuccess || !updNotFound) throw new Exception("Tenant A was able to update Tenant B incident!");

                // Tenant A tries to update status of Tenant B incident
                var (statusSuccess, _, statusNotFound, _) = incidentServiceA.UpdateStatusAsync(incidentB.Id, new UpdateIncidentStatusDTO
                {
                    Status = IncidentStatus.Closed
                }).GetAwaiter().GetResult();
                if (statusSuccess || !statusNotFound) throw new Exception("Tenant A was able to change status of Tenant B incident!");

                // Tenant A tries to get impact analysis of Tenant B incident
                var (impactSuccess, _, impactNotFound, _) = incidentServiceA.GetImpactAnalysisAsync(incidentB.Id).GetAwaiter().GetResult();
                if (impactSuccess || !impactNotFound) throw new Exception("Tenant A was able to analyze impact of Tenant B incident!");
            });

            // 7. Update incident details
            RunTest("UpdateAsync updates incident metadata and sets UpdatedAt timestamp", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "EDI Gateway", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                var service = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (_, _, _, created) = service.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = sys.Id,
                    Title = "EDI Slowness",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.Low
                }).GetAwaiter().GetResult();

                var (updSuccess, updErr, _, updated) = service.UpdateAsync(created!.Id, new UpdateIncidentDTO
                {
                    Title = "EDI Gateway Hard Crash",
                    Description = "Kernel panic detected on host 10.0.1.5",
                    IncidentType = IncidentType.HardwareFailure,
                    Severity = IncidentSeverity.High
                }).GetAwaiter().GetResult();

                if (!updSuccess) throw new Exception($"Update failed: {updErr}");
                if (updated!.Title != "EDI Gateway Hard Crash") throw new Exception("Title not updated");
                if (updated.Severity != IncidentSeverity.High) throw new Exception("Severity not updated");
                if (!updated.UpdatedAt.HasValue) throw new Exception("UpdatedAt should be set");
            });

            // 8. Lifecycle status transitions: Open -> Investigating -> Mitigating -> Resolved -> Closed
            RunTest("Valid lifecycle transitions succeed and record ResolvedAt timestamp on resolution and closure", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "TMS Core", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                var service = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (_, _, _, incident) = service.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = sys.Id,
                    Title = "Database Connection Timeout",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.Medium
                }).GetAwaiter().GetResult();

                // Open -> Investigating
                var (s1, e1, _, d1) = service.UpdateStatusAsync(incident!.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Investigating }).GetAwaiter().GetResult();
                if (!s1 || d1!.Status != IncidentStatus.Investigating) throw new Exception($"Failed transition to Investigating: {e1}");

                // Investigating -> Mitigating
                var (s2, e2, _, d2) = service.UpdateStatusAsync(incident.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Mitigating }).GetAwaiter().GetResult();
                if (!s2 || d2!.Status != IncidentStatus.Mitigating) throw new Exception($"Failed transition to Mitigating: {e2}");

                // Mitigating -> Resolved
                var (s3, e3, _, d3) = service.UpdateStatusAsync(incident.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Resolved, ResolutionNotes = "Failover node promoted." }).GetAwaiter().GetResult();
                if (!s3 || d3!.Status != IncidentStatus.Resolved) throw new Exception($"Failed transition to Resolved: {e3}");
                if (!d3.ResolvedAt.HasValue) throw new Exception("ResolvedAt must be set when transitioning to Resolved");

                // Resolved -> Closed
                var (s4, e4, _, d4) = service.UpdateStatusAsync(incident.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Closed, ResolutionNotes = "Root cause documented." }).GetAwaiter().GetResult();
                if (!s4 || d4!.Status != IncidentStatus.Closed) throw new Exception($"Failed transition to Closed: {e4}");
                if (!d4.ResolvedAt.HasValue) throw new Exception("ResolvedAt must remain set when Closed");
            });

            // 9. Reopening an incident clears ResolvedAt
            RunTest("Reopening a Resolved incident back to Investigating clears ResolvedAt timestamp", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "WMS Dispatch", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                var service = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (_, _, _, incident) = service.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = sys.Id,
                    Title = "Intermittent Lockout",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.High
                }).GetAwaiter().GetResult();

                // Resolve
                service.UpdateStatusAsync(incident!.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Resolved }).GetAwaiter().GetResult();

                // Reopen to Investigating
                var (reopenSuccess, reopenErr, _, reopened) = service.UpdateStatusAsync(incident.Id, new UpdateIncidentStatusDTO
                {
                    Status = IncidentStatus.Investigating,
                    ResolutionNotes = "Issue recurred after reboot."
                }).GetAwaiter().GetResult();

                if (!reopenSuccess) throw new Exception($"Reopen failed: {reopenErr}");
                if (reopened!.Status != IncidentStatus.Investigating) throw new Exception("Status not changed to Investigating");
                if (reopened.ResolvedAt.HasValue) throw new Exception("ResolvedAt must be cleared when reopened to Investigating");
            });

            // 10. Closed is a terminal state
            RunTest("Closed is a terminal state and prohibits any further status transitions or edits", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Archived Hub", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                var service = new IncidentService(incidentRepo, sysRepo, null!, context);

                var (_, _, _, incident) = service.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = sys.Id,
                    Title = "To Close",
                    IncidentType = IncidentType.Other,
                    Severity = IncidentSeverity.Low
                }).GetAwaiter().GetResult();

                // Close incident
                service.UpdateStatusAsync(incident!.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Closed }).GetAwaiter().GetResult();

                // Attempt transition from Closed back to Open
                var (transSuccess, transErr, _, _) = service.UpdateStatusAsync(incident.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Open }).GetAwaiter().GetResult();
                if (transSuccess) throw new Exception("Expected transition from Closed to fail");
                if (string.IsNullOrWhiteSpace(transErr) || !transErr.Contains("Closed"))
                {
                    throw new Exception($"Expected Closed terminal error, got: {transErr}");
                }

                // Attempt editing a Closed incident
                var (updSuccess, updErr, _, _) = service.UpdateAsync(incident.Id, new UpdateIncidentDTO
                {
                    Title = "Edited After Closed",
                    IncidentType = IncidentType.Other,
                    Severity = IncidentSeverity.Low
                }).GetAwaiter().GetResult();
                if (updSuccess) throw new Exception("Editing a Closed incident must be rejected");
            });

            // 11. Filtering incidents
            RunTest("GetAllAsync filters incidents by status, severity, and failedSystemId", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var context = new ImpactMockTenantContext { TenantId = tenantA };

                var sys1 = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Sys 1", LifecycleStatus = SystemLifecycleStatus.Active };
                var sys2 = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Sys 2", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys1);
                sysRepo.Seed(sys2);

                var service = new IncidentService(incidentRepo, sysRepo, null!, new IncidentMockTenantContext { TenantId = tenantA });

                var (_, _, _, inc1) = service.CreateAsync(new CreateIncidentDTO { FailedSystemId = sys1.Id, Title = "Inc 1", IncidentType = IncidentType.CyberAttack, Severity = IncidentSeverity.Critical }).GetAwaiter().GetResult();
                var (_, _, _, inc2) = service.CreateAsync(new CreateIncidentDTO { FailedSystemId = sys2.Id, Title = "Inc 2", IncidentType = IncidentType.HardwareFailure, Severity = IncidentSeverity.Low }).GetAwaiter().GetResult();

                // Resolve inc2
                service.UpdateStatusAsync(inc2!.Id, new UpdateIncidentStatusDTO { Status = IncidentStatus.Resolved }).GetAwaiter().GetResult();

                // Filter by status Open
                var openIncidents = service.GetAllAsync(status: IncidentStatus.Open).GetAwaiter().GetResult().ToList();
                if (openIncidents.Count != 1 || openIncidents[0].Id != inc1!.Id)
                {
                    throw new Exception($"Expected 1 Open incident, got {openIncidents.Count}");
                }

                // Filter by severity Critical
                var critIncidents = service.GetAllAsync(severity: IncidentSeverity.Critical).GetAwaiter().GetResult().ToList();
                if (critIncidents.Count != 1 || critIncidents[0].Id != inc1!.Id)
                {
                    throw new Exception($"Expected 1 Critical incident, got {critIncidents.Count}");
                }

                // Filter by system sys2
                var sys2Incidents = service.GetAllAsync(failedSystemId: sys2.Id).GetAwaiter().GetResult().ToList();
                if (sys2Incidents.Count != 1 || sys2Incidents[0].Id != inc2.Id)
                {
                    throw new Exception($"Expected 1 incident for sys2, got {sys2Incidents.Count}");
                }
            });

            // 12. Impact Analysis integration
            RunTest("GetImpactAnalysisAsync calculates full downstream system and business operation impact from incident", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var depRepo = new InMemorySystemDependencyRepositoryForImpact();
                var opRepo = new InMemoryBusinessOperationRepositoryForImpact();
                var context = new IncidentMockTenantContext { TenantId = tenantA };

                // ERP -> WMS (Mandatory)
                var erp = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "ERP Central", Criticality = SystemCriticality.Critical, LifecycleStatus = SystemLifecycleStatus.Active };
                var wms = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Warehouse WMS", Criticality = SystemCriticality.High, LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(erp);
                sysRepo.Seed(wms);

                depRepo.Seed(new SystemDependency { Id = Guid.NewGuid(), TenantId = tenantA, DependentSystemId = wms.Id, DependencySystemId = erp.Id, DependencyType = DependencyType.Mandatory });

                // Operation supported by WMS
                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Shipment Packing",
                    Criticality = OperationCriticality.Critical,
                    AvailabilityRule = AvailabilityRule.AllRequired
                };
                opRepo.SeedOperation(op);
                opRepo.SeedAssignment(new SystemBusinessOperation { Id = Guid.NewGuid(), TenantId = tenantA, BusinessOperationId = op.Id, LogisticsSystemId = wms.Id });

                var graphService = new GraphEngineService(depRepo, sysRepo, context);
                var impactService = new ImpactAnalysisService(graphService, sysRepo, opRepo, context);
                var incidentService = new IncidentService(incidentRepo, sysRepo, impactService, context);

                // Incident reported on ERP
                var (_, _, _, incident) = incidentService.CreateAsync(new CreateIncidentDTO
                {
                    FailedSystemId = erp.Id,
                    Title = "ERP Outage",
                    IncidentType = IncidentType.SystemOutage,
                    Severity = IncidentSeverity.Critical
                }).GetAwaiter().GetResult();

                // Call incident impact analysis endpoint
                var (impactSuccess, impactErr, _, impactData) = incidentService.GetImpactAnalysisAsync(incident!.Id).GetAwaiter().GetResult();

                if (!impactSuccess) throw new Exception($"GetImpactAnalysisAsync failed: {impactErr}");
                if (impactData == null) throw new Exception("Impact data is null");
                if (impactData.FailedSystem.Id != erp.Id) throw new Exception("Failed system in impact response mismatch");
                if (impactData.TotalAffectedSystemsCount != 1) throw new Exception($"Expected 1 downstream affected system, got {impactData.TotalAffectedSystemsCount}");
                if (impactData.TotalAffectedOperationsCount != 1) throw new Exception($"Expected 1 affected operation, got {impactData.TotalAffectedOperationsCount}");

                var affectedOp = impactData.AffectedOperations[0];
                if (affectedOp.OperationId != op.Id) throw new Exception("Affected operation mismatch");
                if (affectedOp.PotentialImpactSeverity != OperationImpactSeverity.Critical)
                {
                    throw new Exception($"Expected Critical impact severity, got {affectedOp.PotentialImpactSeverity}");
                }
            });

            // 13. Unauthenticated access fails closed
            RunTest("Unauthenticated caller fails closed across all incident operations", () =>
            {
                var incidentRepo = new InMemoryIncidentRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForImpact();
                var unauthContext = new IncidentMockTenantContext { TenantId = null };
                var service = new IncidentService(incidentRepo, sysRepo, null!, unauthContext);

                var all = service.GetAllAsync().GetAwaiter().GetResult();
                if (all.Any()) throw new Exception("GetAllAsync should return empty when unauthenticated");

                var one = service.GetByIdAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (one != null) throw new Exception("GetByIdAsync should return null when unauthenticated");

                var (cSuccess, _, _, _) = service.CreateAsync(new CreateIncidentDTO { FailedSystemId = Guid.NewGuid(), Title = "Unauth", IncidentType = IncidentType.Other, Severity = IncidentSeverity.Low }).GetAwaiter().GetResult();
                if (cSuccess) throw new Exception("CreateAsync must fail when unauthenticated");

                var (uSuccess, _, _, _) = service.UpdateAsync(Guid.NewGuid(), new UpdateIncidentDTO { Title = "Unauth", IncidentType = IncidentType.Other, Severity = IncidentSeverity.Low }).GetAwaiter().GetResult();
                if (uSuccess) throw new Exception("UpdateAsync must fail when unauthenticated");

                var (sSuccess, _, _, _) = service.UpdateStatusAsync(Guid.NewGuid(), new UpdateIncidentStatusDTO { Status = IncidentStatus.Closed }).GetAwaiter().GetResult();
                if (sSuccess) throw new Exception("UpdateStatusAsync must fail when unauthenticated");

                var (iSuccess, _, _, _) = service.GetImpactAnalysisAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (iSuccess) throw new Exception("GetImpactAnalysisAsync must fail when unauthenticated");
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
