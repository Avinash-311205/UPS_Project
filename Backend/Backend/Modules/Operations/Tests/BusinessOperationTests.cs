using Backend.Modules.Operations.DTOs;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Interfaces;
using Backend.Modules.Operations.Models;
using Backend.Modules.Operations.Services;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Operations.Tests
{
    public class OpsMockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
        public bool HasTenant => TenantId.HasValue;
    }

    public class InMemoryBusinessOperationRepository : IBusinessOperationRepository
    {
        private readonly List<BusinessOperation> _operations = new();
        private readonly List<SystemBusinessOperation> _assignments = new();

        public List<BusinessOperation> RawOperations => _operations;
        public List<SystemBusinessOperation> RawAssignments => _assignments;

        public Task<IEnumerable<BusinessOperation>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _operations.AsEnumerable();
            if (!includeInactive)
            {
                query = query.Where(o => o.LifecycleStatus == OperationLifecycleStatus.Active);
            }
            return Task.FromResult<IEnumerable<BusinessOperation>>(query.ToList());
        }

        public Task<BusinessOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var op = _operations.FirstOrDefault(o => o.Id == id);
            return Task.FromResult(op);
        }

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _operations.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(o => o.Id != excludeId.Value);
            }
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
            if (idx >= 0)
            {
                _operations[idx] = operation;
            }
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
            _assignments.Add(assignment);
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

    public class InMemoryLogisticsSystemRepositoryForOps : ILogisticsSystemRepository
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
            if (idx >= 0)
            {
                _systems[idx] = system;
            }
            return Task.FromResult(system);
        }

        public Task DeleteAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _systems.RemoveAll(s => s.Id == system.Id);
            return Task.CompletedTask;
        }
    }

    public static class BusinessOperationTests
    {
        public static int RunAllTests()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("Running Business Operations Management Test Suite");
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

            // 1. Create business operation successfully with tenant auto-assignment
            RunTest("Create business operation assigns authenticated TenantId automatically", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (success, error, isConflict, data) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Order Fulfillment",
                    Description = "Pick, pack, and ship operations"
                }).GetAwaiter().GetResult();

                if (!success) throw new Exception($"Create failed: {error}");
                if (data == null) throw new Exception("Response data is null");
                if (data.TenantId != tenantA) throw new Exception($"TenantId mismatch: expected {tenantA}, got {data.TenantId}");
                if (data.Name != "Order Fulfillment") throw new Exception($"Name mismatch: {data.Name}");
                if (data.LifecycleStatus != OperationLifecycleStatus.Active) throw new Exception($"Expected Active, got {data.LifecycleStatus}");
                if (data.Criticality != OperationCriticality.Medium) throw new Exception($"Expected default Criticality Medium, got {data.Criticality}");
                if (data.AvailabilityRule != AvailabilityRule.AllRequired) throw new Exception($"Expected default AvailabilityRule AllRequired, got {data.AvailabilityRule}");
            });

            // 2. Name is required and trimmed
            RunTest("Validation rejects empty or whitespace operation name", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (success, error, _, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "   ",
                    Description = "No name"
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Expected creation to fail for whitespace name");
                if (string.IsNullOrWhiteSpace(error)) throw new Exception("Expected error message");
            });

            // 3. Unique name enforced per tenant (Conflict 409)
            RunTest("Tenant-scoped uniqueness prevents duplicate operation names in same tenant", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (s1, _, _, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Inbound Receiving"
                }).GetAwaiter().GetResult();
                if (!s1) throw new Exception("Initial creation failed");

                var (s2, err2, isConflict, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "inbound receiving" // Case-insensitive duplicate
                }).GetAwaiter().GetResult();

                if (s2) throw new Exception("Duplicate name creation should have failed");
                if (!isConflict) throw new Exception("Expected IsConflict = true for duplicate name");
            });

            // 4. Same operation name allowed across different tenants (Tenant isolation)
            RunTest("Same operation name is permitted across different tenants", () =>
            {
                var opRepoA = new InMemoryBusinessOperationRepository();
                var opRepoB = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();

                var serviceA = new BusinessOperationService(opRepoA, sysRepo, new OpsMockTenantContext { TenantId = tenantA });
                var serviceB = new BusinessOperationService(opRepoB, sysRepo, new OpsMockTenantContext { TenantId = tenantB });

                var (sA, _, _, _) = serviceA.CreateAsync(new CreateBusinessOperationDTO { Name = "Cross-Docking" }).GetAwaiter().GetResult();
                var (sB, _, _, _) = serviceB.CreateAsync(new CreateBusinessOperationDTO { Name = "Cross-Docking" }).GetAwaiter().GetResult();

                if (!sA || !sB) throw new Exception("Cross-tenant duplicate name should succeed across different tenants");
            });

            // 5. List operations with active/inactive filtering
            RunTest("GetAllAsync filters inactive operations by default unless includeInactive=true", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (s1, _, _, op1) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Active Op" }).GetAwaiter().GetResult();
                var (s2, _, _, op2) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Deactivated Op" }).GetAwaiter().GetResult();

                // Deactivate op2
                service.DeleteAsync(op2!.Id).GetAwaiter().GetResult();

                var activeOnly = service.GetAllAsync(includeInactive: false).GetAwaiter().GetResult().ToList();
                if (activeOnly.Count != 1 || activeOnly[0].Id != op1!.Id)
                {
                    throw new Exception($"Expected 1 active op, got {activeOnly.Count}");
                }

                var all = service.GetAllAsync(includeInactive: true).GetAwaiter().GetResult().ToList();
                if (all.Count != 2)
                {
                    throw new Exception($"Expected 2 total ops with includeInactive=true, got {all.Count}");
                }
            });

            // 6. GetByIdAsync respects tenant boundary
            RunTest("GetByIdAsync returns operation for owning tenant and null for foreign tenant", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();

                var serviceA = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantA });
                var serviceB = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantB });

                var (_, _, _, opA) = serviceA.CreateAsync(new CreateBusinessOperationDTO { Name = "Tenant A Op" }).GetAwaiter().GetResult();

                var retrievedA = serviceA.GetByIdAsync(opA!.Id).GetAwaiter().GetResult();
                if (retrievedA == null) throw new Exception("Tenant A should retrieve its own operation");

                var retrievedB = serviceB.GetByIdAsync(opA.Id).GetAwaiter().GetResult();
                if (retrievedB != null) throw new Exception("Tenant B should NOT be able to retrieve Tenant A's operation");
            });

            // 7. Update business operation
            RunTest("UpdateAsync updates fields while preserving tenant ownership and rejecting duplicate names", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (_, _, _, op1) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Op One" }).GetAwaiter().GetResult();
                var (_, _, _, op2) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Op Two" }).GetAwaiter().GetResult();

                // Attempt to rename op2 to "Op One" -> duplicate conflict
                var (conflictSuccess, _, _, isConflict, _) = service.UpdateAsync(op2!.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Op One",
                    LifecycleStatus = OperationLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                if (conflictSuccess || !isConflict) throw new Exception("Expected conflict when renaming to an existing name");

                // Valid update keeping same name or unique name
                var (updSuccess, updErr, _, _, updatedData) = service.UpdateAsync(op2.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Op Two Updated",
                    Description = "Updated description",
                    LifecycleStatus = OperationLifecycleStatus.Active
                }).GetAwaiter().GetResult();

                if (!updSuccess) throw new Exception($"Update failed: {updErr}");
                if (updatedData!.Name != "Op Two Updated") throw new Exception("Name was not updated");
                if (updatedData.TenantId != tenantA) throw new Exception("TenantId must not be modified during update");
            });

            // 8. Soft deactivation on Delete
            RunTest("DeleteAsync performs soft deactivation: LifecycleStatus becomes Inactive and record is preserved", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (_, _, _, op) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "To Deactivate" }).GetAwaiter().GetResult();

                var (delSuccess, delErr, notFound) = service.DeleteAsync(op!.Id).GetAwaiter().GetResult();
                if (!delSuccess) throw new Exception($"DeleteAsync failed: {delErr}");

                // Confirm record STILL EXISTS in the repository
                var rawRecord = opRepo.RawOperations.FirstOrDefault(o => o.Id == op.Id);
                if (rawRecord == null) throw new Exception("Record was physically deleted from repository! Must be soft-deactivated.");
                if (rawRecord.LifecycleStatus != OperationLifecycleStatus.Inactive)
                {
                    throw new Exception($"Expected LifecycleStatus Inactive, got {rawRecord.LifecycleStatus}");
                }
                if (!rawRecord.UpdatedAt.HasValue) throw new Exception("UpdatedAt should be set during soft deactivation");
            });

            // 9. Assign operation to active logistics system
            RunTest("AssignSystemAsync successfully links active system to active operation", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var activeSystem = new LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Warehouse TMS",
                    LifecycleStatus = SystemLifecycleStatus.Active
                };
                sysRepo.Seed(activeSystem);

                var (_, _, _, op) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Route Optimization" }).GetAwaiter().GetResult();

                var (assignSuccess, assignErr, _, _, assignData) = service.AssignSystemAsync(op!.Id, activeSystem.Id).GetAwaiter().GetResult();
                if (!assignSuccess) throw new Exception($"AssignSystemAsync failed: {assignErr}");
                if (assignData == null) throw new Exception("Assignment data is null");
                if (assignData.BusinessOperationId != op.Id) throw new Exception("BusinessOperationId mismatch");
                if (assignData.LogisticsSystemId != activeSystem.Id) throw new Exception("LogisticsSystemId mismatch");
                if (assignData.TenantId != tenantA) throw new Exception("Assignment TenantId mismatch");
            });

            // 10. Multiple operations per system and multiple systems per operation
            RunTest("A system supports multiple operations, and an operation is supported by multiple systems", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var sys1 = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "ERP Core", LifecycleStatus = SystemLifecycleStatus.Active };
                var sys2 = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "WMS Facility", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys1);
                sysRepo.Seed(sys2);

                var (_, _, _, opA) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Inventory Tracking" }).GetAwaiter().GetResult();
                var (_, _, _, opB) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Order Billing" }).GetAwaiter().GetResult();

                // sys1 supports opA and opB (system supporting multiple operations)
                var (r1, _, _, _, _) = service.AssignSystemAsync(opA!.Id, sys1.Id).GetAwaiter().GetResult();
                var (r2, _, _, _, _) = service.AssignSystemAsync(opB!.Id, sys1.Id).GetAwaiter().GetResult();
                if (!r1 || !r2) throw new Exception("Failed to assign multiple operations to system 1");

                // opA supported by sys1 and sys2 (operation supported by multiple systems)
                var (r3, _, _, _, _) = service.AssignSystemAsync(opA.Id, sys2.Id).GetAwaiter().GetResult();
                if (!r3) throw new Exception("Failed to assign system 2 to operation A");

                // Verify counts
                var (_, _, _, sys1Ops) = service.GetOperationsForSystemAsync(sys1.Id).GetAwaiter().GetResult();
                if (sys1Ops!.Count() != 2) throw new Exception($"Expected 2 operations for sys1, got {sys1Ops!.Count()}");

                var (_, _, _, opASystems) = service.GetSystemsForOperationAsync(opA.Id).GetAwaiter().GetResult();
                if (opASystems!.Count() != 2) throw new Exception($"Expected 2 systems for opA, got {opASystems!.Count()}");
            });

            // 11. Prevent duplicate system-operation relationship (409 Conflict)
            RunTest("Prevent duplicate system-operation assignment with 409 Conflict", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Dispatch TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                var (_, _, _, op) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Dispatch Planning" }).GetAwaiter().GetResult();

                var (firstSuccess, _, _, _, _) = service.AssignSystemAsync(op!.Id, sys.Id).GetAwaiter().GetResult();
                if (!firstSuccess) throw new Exception("First assignment failed");

                var (secondSuccess, secondErr, _, isConflict, _) = service.AssignSystemAsync(op.Id, sys.Id).GetAwaiter().GetResult();
                if (secondSuccess) throw new Exception("Duplicate assignment must fail");
                if (!isConflict) throw new Exception("Expected isConflict = true for duplicate assignment");
            });

            // 12. Inactive system cannot be assigned new operations
            RunTest("Inactive systems cannot be assigned new business operations", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var inactiveSystem = new LogisticsSystem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Legacy WMS",
                    LifecycleStatus = SystemLifecycleStatus.Inactive // Inactive!
                };
                sysRepo.Seed(inactiveSystem);

                var (_, _, _, op) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Inventory Count" }).GetAwaiter().GetResult();

                var (assignSuccess, assignErr, notFound, isConflict, _) = service.AssignSystemAsync(op!.Id, inactiveSystem.Id).GetAwaiter().GetResult();
                if (assignSuccess) throw new Exception("Expected assignment to inactive system to fail");
                if (string.IsNullOrWhiteSpace(assignErr) || !assignErr.Contains("inactive"))
                {
                    throw new Exception($"Expected inactive system error message, got: {assignErr}");
                }
            });

            // 13. Inactive operation cannot be assigned
            RunTest("Inactive business operation cannot be assigned to systems", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var activeSystem = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Active TMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(activeSystem);

                var (_, _, _, op) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Archived Op" }).GetAwaiter().GetResult();
                service.DeleteAsync(op!.Id).GetAwaiter().GetResult(); // Deactivate

                var (assignSuccess, assignErr, _, _, _) = service.AssignSystemAsync(op.Id, activeSystem.Id).GetAwaiter().GetResult();
                if (assignSuccess) throw new Exception("Expected assignment to inactive operation to fail");
            });

            // 14. Cross-tenant assignment protection
            RunTest("Cross-tenant assignment is strictly prohibited", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();

                // System belongs to Tenant B
                var tenantBSystem = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Tenant B WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(tenantBSystem);

                // Operation created by Tenant A
                var serviceA = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantA });
                var (_, _, _, opA) = serviceA.CreateAsync(new CreateBusinessOperationDTO { Name = "Tenant A Ops" }).GetAwaiter().GetResult();

                // Tenant A tries to assign Tenant B's system
                var (assignSuccess, _, notFound, _, _) = serviceA.AssignSystemAsync(opA!.Id, tenantBSystem.Id).GetAwaiter().GetResult();
                if (assignSuccess) throw new Exception("Cross-tenant assignment must not succeed");
                if (!notFound) throw new Exception("Cross-tenant system should be treated as not found");
            });

            // 15. Removing system-operation link preserves both system and operation
            RunTest("RemoveSystemAsync removes only the link, preserving system and operation records", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var sys = new LogisticsSystem { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Hub WMS", LifecycleStatus = SystemLifecycleStatus.Active };
                sysRepo.Seed(sys);

                var (_, _, _, op) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Hub Sorting" }).GetAwaiter().GetResult();
                service.AssignSystemAsync(op!.Id, sys.Id).GetAwaiter().GetResult();

                // Confirm link exists
                if (opRepo.RawAssignments.Count != 1) throw new Exception("Expected 1 assignment before remove");

                // Remove link
                var (removeSuccess, removeErr, _) = service.RemoveSystemAsync(op.Id, sys.Id).GetAwaiter().GetResult();
                if (!removeSuccess) throw new Exception($"RemoveSystemAsync failed: {removeErr}");

                // Confirm link is gone
                if (opRepo.RawAssignments.Count != 0) throw new Exception("Assignment was not removed");

                // Confirm both system and operation STILL EXIST
                var systemStillExists = sysRepo.GetByIdAsync(sys.Id).GetAwaiter().GetResult();
                if (systemStillExists == null) throw new Exception("LogisticsSystem was deleted! It must be preserved.");

                var operationStillExists = opRepo.GetByIdAsync(op.Id).GetAwaiter().GetResult();
                if (operationStillExists == null) throw new Exception("BusinessOperation was deleted! It must be preserved.");
            });

            // 16. Unauthenticated caller fails closed
            RunTest("Unauthenticated caller fails closed across all operation actions", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var unauthContext = new OpsMockTenantContext { TenantId = null };
                var service = new BusinessOperationService(opRepo, sysRepo, unauthContext);

                var all = service.GetAllAsync().GetAwaiter().GetResult();
                if (all.Any()) throw new Exception("GetAllAsync should return empty when unauthenticated");

                var one = service.GetByIdAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (one != null) throw new Exception("GetByIdAsync should return null when unauthenticated");

                var (createSuccess, _, _, _) = service.CreateAsync(new CreateBusinessOperationDTO { Name = "Unauth" }).GetAwaiter().GetResult();
                if (createSuccess) throw new Exception("CreateAsync must fail when unauthenticated");

                var (updateSuccess, _, _, _, _) = service.UpdateAsync(Guid.NewGuid(), new UpdateBusinessOperationDTO { Name = "Unauth" }).GetAwaiter().GetResult();
                if (updateSuccess) throw new Exception("UpdateAsync must fail when unauthenticated");

                var (delSuccess, _, _) = service.DeleteAsync(Guid.NewGuid()).GetAwaiter().GetResult();
                if (delSuccess) throw new Exception("DeleteAsync must fail when unauthenticated");

                var (assignSuccess, _, _, _, _) = service.AssignSystemAsync(Guid.NewGuid(), Guid.NewGuid()).GetAwaiter().GetResult();
                if (assignSuccess) throw new Exception("AssignSystemAsync must fail when unauthenticated");

                var (removeSuccess, _, _) = service.RemoveSystemAsync(Guid.NewGuid(), Guid.NewGuid()).GetAwaiter().GetResult();
                if (removeSuccess) throw new Exception("RemoveSystemAsync must fail when unauthenticated");
            });

            // 17. Creating operations with each valid OperationCriticality
            RunTest("Create business operation supports all valid OperationCriticality levels (Low, Medium, High, Critical)", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var levels = new[]
                {
                    (Name: "Low Priority Task", Criticality: OperationCriticality.Low),
                    (Name: "Medium Priority Task", Criticality: OperationCriticality.Medium),
                    (Name: "High Priority Task", Criticality: OperationCriticality.High),
                    (Name: "Critical Priority Task", Criticality: OperationCriticality.Critical)
                };

                foreach (var (name, expectedCrit) in levels)
                {
                    var (success, error, _, data) = service.CreateAsync(new CreateBusinessOperationDTO
                    {
                        Name = name,
                        Criticality = expectedCrit
                    }).GetAwaiter().GetResult();

                    if (!success) throw new Exception($"Failed to create operation with {expectedCrit}: {error}");
                    if (data!.Criticality != expectedCrit) throw new Exception($"Expected {expectedCrit}, got {data.Criticality}");

                    // Confirm GetByIdAsync also returns the criticality
                    var fetched = service.GetByIdAsync(data.Id).GetAwaiter().GetResult();
                    if (fetched == null || fetched.Criticality != expectedCrit)
                    {
                        throw new Exception($"GetByIdAsync returned mismatched criticality for {expectedCrit}");
                    }
                }
            });

            // 18. Updating operation criticality
            RunTest("UpdateAsync successfully updates operation criticality from Low to Critical", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (_, _, _, created) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Fleet Tracking",
                    Criticality = OperationCriticality.Low
                }).GetAwaiter().GetResult();

                if (created!.Criticality != OperationCriticality.Low) throw new Exception("Initial criticality not Low");

                var (updSuccess, updErr, _, _, updated) = service.UpdateAsync(created.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Fleet Tracking",
                    LifecycleStatus = OperationLifecycleStatus.Active,
                    Criticality = OperationCriticality.Critical
                }).GetAwaiter().GetResult();

                if (!updSuccess) throw new Exception($"Update failed: {updErr}");
                if (updated!.Criticality != OperationCriticality.Critical)
                {
                    throw new Exception($"Expected Critical, got {updated.Criticality}");
                }

                // Verify persistence in repo
                var inRepo = opRepo.RawOperations.FirstOrDefault(o => o.Id == created.Id);
                if (inRepo == null || inRepo.Criticality != OperationCriticality.Critical)
                {
                    throw new Exception("Criticality was not persisted in repository");
                }
            });

            // 19. Rejecting invalid enum values on create
            RunTest("CreateAsync rejects invalid OperationCriticality values", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (success, error, _, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Invalid Crit Task",
                    Criticality = (OperationCriticality)999
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Expected creation to fail for invalid criticality (999)");
                if (string.IsNullOrWhiteSpace(error) || !error.Contains("Criticality"))
                {
                    throw new Exception($"Expected error regarding Criticality, got: {error}");
                }

                var (zeroSuccess, zeroErr, _, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Zero Crit Task",
                    Criticality = (OperationCriticality)0
                }).GetAwaiter().GetResult();

                if (zeroSuccess) throw new Exception("Expected creation to fail for invalid criticality (0)");
            });

            // 20. Rejecting invalid enum values on update
            RunTest("UpdateAsync rejects invalid OperationCriticality values", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (_, _, _, created) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Standard Op",
                    Criticality = OperationCriticality.Medium
                }).GetAwaiter().GetResult();

                var (updSuccess, updErr, _, _, _) = service.UpdateAsync(created!.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Standard Op",
                    LifecycleStatus = OperationLifecycleStatus.Active,
                    Criticality = (OperationCriticality)88
                }).GetAwaiter().GetResult();

                if (updSuccess) throw new Exception("Expected update to fail for invalid criticality (88)");
                if (string.IsNullOrWhiteSpace(updErr) || !updErr.Contains("Criticality"))
                {
                    throw new Exception($"Expected error regarding Criticality, got: {updErr}");
                }
            });

            // 21. Existing records receive safe default of Medium
            RunTest("Existing records without explicit criticality receive safe default of Medium", () =>
            {
                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Legacy Pre-Migration Operation"
                };

                if (op.Criticality != OperationCriticality.Medium)
                {
                    throw new Exception($"Expected entity default to be Medium, got {op.Criticality}");
                }
            });

            // 22. Cross-tenant update prevention
            RunTest("Cross-tenant criticality update is strictly prohibited", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();

                var serviceA = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantA });
                var serviceB = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantB });

                var (_, _, _, opA) = serviceA.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Tenant A Mission Critical",
                    Criticality = OperationCriticality.Critical
                }).GetAwaiter().GetResult();

                // Tenant B attempts to downgrade Tenant A's operation
                var (updSuccess, _, notFound, _, _) = serviceB.UpdateAsync(opA!.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Tenant A Mission Critical",
                    LifecycleStatus = OperationLifecycleStatus.Active,
                    Criticality = OperationCriticality.Low
                }).GetAwaiter().GetResult();

                if (updSuccess) throw new Exception("Tenant B must not be able to update Tenant A's operation");
                if (!notFound) throw new Exception("Expected notFound = true for cross-tenant update");

                // Verify Tenant A's operation remains Critical
                var verify = serviceA.GetByIdAsync(opA.Id).GetAwaiter().GetResult();
                if (verify!.Criticality != OperationCriticality.Critical)
                {
                    throw new Exception("Tenant A's operation criticality was modified by Tenant B!");
                }
            });

            // 23. Default AvailabilityRule when omitted on create
            RunTest("CreateBusinessOperationDTO defaults AvailabilityRule to AllRequired when omitted", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (success, error, _, data) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Default Rule Op"
                }).GetAwaiter().GetResult();

                if (!success) throw new Exception($"Create failed: {error}");
                if (data!.AvailabilityRule != AvailabilityRule.AllRequired)
                {
                    throw new Exception($"Expected AllRequired, got {data.AvailabilityRule}");
                }
            });

            // 24. Creating operations with both valid AvailabilityRule values
            RunTest("Create business operation supports both valid AvailabilityRule options (AllRequired, AnyAvailable)", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var rules = new[]
                {
                    (Name: "Strict Operation", Rule: AvailabilityRule.AllRequired),
                    (Name: "Redundant Operation", Rule: AvailabilityRule.AnyAvailable)
                };

                foreach (var (name, rule) in rules)
                {
                    var (success, error, _, data) = service.CreateAsync(new CreateBusinessOperationDTO
                    {
                        Name = name,
                        AvailabilityRule = rule
                    }).GetAwaiter().GetResult();

                    if (!success) throw new Exception($"Failed to create with {rule}: {error}");
                    if (data!.AvailabilityRule != rule) throw new Exception($"Expected {rule}, got {data.AvailabilityRule}");

                    var fetched = service.GetByIdAsync(data.Id).GetAwaiter().GetResult();
                    if (fetched == null || fetched.AvailabilityRule != rule)
                    {
                        throw new Exception($"GetByIdAsync returned mismatched AvailabilityRule for {rule}");
                    }
                }
            });

            // 25. Updating AvailabilityRule
            RunTest("UpdateAsync successfully updates AvailabilityRule from AllRequired to AnyAvailable", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (_, _, _, created) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Carrier Dispatch",
                    AvailabilityRule = AvailabilityRule.AllRequired
                }).GetAwaiter().GetResult();

                if (created!.AvailabilityRule != AvailabilityRule.AllRequired) throw new Exception("Initial rule not AllRequired");

                var (updSuccess, updErr, _, _, updated) = service.UpdateAsync(created.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Carrier Dispatch",
                    LifecycleStatus = OperationLifecycleStatus.Active,
                    Criticality = OperationCriticality.High,
                    AvailabilityRule = AvailabilityRule.AnyAvailable
                }).GetAwaiter().GetResult();

                if (!updSuccess) throw new Exception($"Update failed: {updErr}");
                if (updated!.AvailabilityRule != AvailabilityRule.AnyAvailable)
                {
                    throw new Exception($"Expected AnyAvailable, got {updated.AvailabilityRule}");
                }

                // Verify persistence in repo
                var inRepo = opRepo.RawOperations.FirstOrDefault(o => o.Id == created.Id);
                if (inRepo == null || inRepo.AvailabilityRule != AvailabilityRule.AnyAvailable)
                {
                    throw new Exception("AvailabilityRule was not persisted in repository");
                }
            });

            // 26. Rejecting invalid AvailabilityRule values on create
            RunTest("CreateAsync rejects invalid AvailabilityRule values", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (success, error, _, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Invalid Rule Task",
                    AvailabilityRule = (AvailabilityRule)999
                }).GetAwaiter().GetResult();

                if (success) throw new Exception("Expected creation to fail for invalid AvailabilityRule (999)");
                if (string.IsNullOrWhiteSpace(error) || !error.Contains("AvailabilityRule"))
                {
                    throw new Exception($"Expected error regarding AvailabilityRule, got: {error}");
                }

                var (zeroSuccess, zeroErr, _, _) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Zero Rule Task",
                    AvailabilityRule = (AvailabilityRule)0
                }).GetAwaiter().GetResult();

                if (zeroSuccess) throw new Exception("Expected creation to fail for invalid AvailabilityRule (0)");
            });

            // 27. Rejecting invalid AvailabilityRule values on update
            RunTest("UpdateAsync rejects invalid AvailabilityRule values", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();
                var context = new OpsMockTenantContext { TenantId = tenantA };
                var service = new BusinessOperationService(opRepo, sysRepo, context);

                var (_, _, _, created) = service.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Valid Task"
                }).GetAwaiter().GetResult();

                var (updSuccess, updErr, _, _, _) = service.UpdateAsync(created!.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Valid Task",
                    LifecycleStatus = OperationLifecycleStatus.Active,
                    AvailabilityRule = (AvailabilityRule)77
                }).GetAwaiter().GetResult();

                if (updSuccess) throw new Exception("Expected update to fail for invalid AvailabilityRule (77)");
                if (string.IsNullOrWhiteSpace(updErr) || !updErr.Contains("AvailabilityRule"))
                {
                    throw new Exception($"Expected error regarding AvailabilityRule, got: {updErr}");
                }
            });

            // 28. Existing records receive safe default of AllRequired
            RunTest("Existing records receive safe default of AllRequired", () =>
            {
                var op = new BusinessOperation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Name = "Pre-Existing Operation"
                };

                if (op.AvailabilityRule != AvailabilityRule.AllRequired)
                {
                    throw new Exception($"Expected entity default to be AllRequired, got {op.AvailabilityRule}");
                }
            });

            // 29. Cross-tenant AvailabilityRule update is strictly prohibited
            RunTest("Cross-tenant AvailabilityRule update is strictly prohibited", () =>
            {
                var opRepo = new InMemoryBusinessOperationRepository();
                var sysRepo = new InMemoryLogisticsSystemRepositoryForOps();

                var serviceA = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantA });
                var serviceB = new BusinessOperationService(opRepo, sysRepo, new OpsMockTenantContext { TenantId = tenantB });

                var (_, _, _, opA) = serviceA.CreateAsync(new CreateBusinessOperationDTO
                {
                    Name = "Tenant A Rule Op",
                    AvailabilityRule = AvailabilityRule.AllRequired
                }).GetAwaiter().GetResult();

                // Tenant B attempts to change Tenant A's operation rule
                var (updSuccess, _, notFound, _, _) = serviceB.UpdateAsync(opA!.Id, new UpdateBusinessOperationDTO
                {
                    Name = "Tenant A Rule Op",
                    LifecycleStatus = OperationLifecycleStatus.Active,
                    AvailabilityRule = AvailabilityRule.AnyAvailable
                }).GetAwaiter().GetResult();

                if (updSuccess) throw new Exception("Tenant B must not be able to update Tenant A's operation");
                if (!notFound) throw new Exception("Expected notFound = true for cross-tenant update");

                // Verify Tenant A's operation remains AllRequired
                var verify = serviceA.GetByIdAsync(opA.Id).GetAwaiter().GetResult();
                if (verify!.AvailabilityRule != AvailabilityRule.AllRequired)
                {
                    throw new Exception("Tenant A's operation AvailabilityRule was modified by Tenant B!");
                }
            });

            Console.WriteLine("=================================================");
            Console.WriteLine($"Results: {passed} passed, {failed} failed.");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }
    }
}
