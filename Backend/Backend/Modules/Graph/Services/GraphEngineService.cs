using Backend.Modules.Dependencies.DTOs;
using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Dependencies.Interfaces;
using Backend.Modules.Graph.DTOs;
using Backend.Modules.Graph.Interfaces;
using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Graph.Services
{
    public class GraphEngineService : IGraphEngineService
    {
        private readonly ISystemDependencyRepository _dependencyRepository;
        private readonly ILogisticsSystemRepository _systemRepository;
        private readonly ITenantContext _tenantContext;

        public GraphEngineService(
            ISystemDependencyRepository dependencyRepository,
            ILogisticsSystemRepository systemRepository,
            ITenantContext tenantContext)
        {
            _dependencyRepository = dependencyRepository;
            _systemRepository = systemRepository;
            _tenantContext = tenantContext;
        }

        public async Task<(bool Success, string? Error, bool NotFound, DownstreamImpactResponseDTO? Data)> GetDownstreamImpactAsync(
            Guid disruptedSystemId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to perform disruption analysis.", false, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            // 1. Verify disrupted system exists and belongs to current tenant
            var disruptedSystem = await _systemRepository.GetByIdAsync(disruptedSystemId, cancellationToken);
            if (disruptedSystem == null || disruptedSystem.TenantId != currentTenantId)
            {
                return (false, $"System with ID '{disruptedSystemId}' was not found.", true, null);
            }

            // 2. Rule: An inactive system must not participate in new operational impact calculations
            if (disruptedSystem.LifecycleStatus != SystemLifecycleStatus.Active)
            {
                return (false, $"System '{disruptedSystem.Name}' is inactive and cannot participate in operational impact calculations.", false, null);
            }

            // 3. Load all systems for the tenant
            var allSystems = await _systemRepository.GetAllAsync(cancellationToken);
            var systemsMap = allSystems
                .Where(s => s.TenantId == currentTenantId)
                .ToDictionary(s => s.Id);

            // 4. Load all dependencies for the tenant
            var allDependencies = await _dependencyRepository.GetAllAsync(cancellationToken);
            var tenantDependencies = allDependencies
                .Where(d => d.TenantId == currentTenantId);

            // 5. Build in-memory adjacency list: Provider (DependencySystemId) -> Dependent (DependentSystemId)
            var adjacencyList = new Dictionary<Guid, List<GraphEdge>>();
            foreach (var dep in tenantDependencies)
            {
                // Inactive systems must not participate in operational impact calculations
                if (!systemsMap.TryGetValue(dep.DependentSystemId, out var dependent) || dependent.LifecycleStatus != SystemLifecycleStatus.Active)
                {
                    continue;
                }

                if (!systemsMap.TryGetValue(dep.DependencySystemId, out var provider) || provider.LifecycleStatus != SystemLifecycleStatus.Active)
                {
                    continue;
                }

                if (!adjacencyList.ContainsKey(dep.DependencySystemId))
                {
                    adjacencyList[dep.DependencySystemId] = new List<GraphEdge>();
                }

                adjacencyList[dep.DependencySystemId].Add(new GraphEdge
                {
                    DependentSystemId = dep.DependentSystemId,
                    DependentSystem = dependent,
                    DependencyType = dep.DependencyType
                });
            }

            // 6. BFS Traversal
            var directDependents = new List<AffectedSystemDTO>();
            var indirectDependents = new List<AffectedSystemDTO>();

            // Queue item: (SystemId, Depth, PathIds, PathNames, DependencyType)
            var queue = new Queue<(Guid SystemId, int Depth, List<Guid> PathIds, List<string> PathNames, DependencyType DepType)>();
            var visited = new HashSet<Guid> { disruptedSystemId };

            if (adjacencyList.TryGetValue(disruptedSystemId, out var directEdges))
            {
                foreach (var edge in directEdges)
                {
                    if (visited.Add(edge.DependentSystemId))
                    {
                        var pathIds = new List<Guid> { disruptedSystemId, edge.DependentSystemId };
                        var pathNames = new List<string> { disruptedSystem.Name, edge.DependentSystem.Name };

                        var directDto = new AffectedSystemDTO
                        {
                            SystemId = edge.DependentSystemId,
                            SystemName = edge.DependentSystem.Name,
                            SystemType = edge.DependentSystem.SystemType,
                            Criticality = edge.DependentSystem.Criticality,
                            Environment = edge.DependentSystem.Environment,
                            LifecycleStatus = edge.DependentSystem.LifecycleStatus,
                            DependencyType = edge.DependencyType,
                            Depth = 1,
                            DependencyPath = pathNames,
                            DependencyPathIds = pathIds
                        };

                        directDependents.Add(directDto);

                        queue.Enqueue((
                            edge.DependentSystemId,
                            1,
                            pathIds,
                            pathNames,
                            edge.DependencyType
                        ));
                    }
                }
            }

            while (queue.Count > 0)
            {
                var (currentSystemId, currentDepth, currentPathIds, currentPathNames, _) = queue.Dequeue();

                if (adjacencyList.TryGetValue(currentSystemId, out var nextEdges))
                {
                    foreach (var edge in nextEdges)
                    {
                        // Cycle & duplicate protection
                        if (visited.Add(edge.DependentSystemId))
                        {
                            var nextIds = new List<Guid>(currentPathIds) { edge.DependentSystemId };
                            var nextNames = new List<string>(currentPathNames) { edge.DependentSystem.Name };

                            var indirectDto = new AffectedSystemDTO
                            {
                                SystemId = edge.DependentSystemId,
                                SystemName = edge.DependentSystem.Name,
                                SystemType = edge.DependentSystem.SystemType,
                                Criticality = edge.DependentSystem.Criticality,
                                Environment = edge.DependentSystem.Environment,
                                LifecycleStatus = edge.DependentSystem.LifecycleStatus,
                                DependencyType = edge.DependencyType,
                                Depth = currentDepth + 1,
                                DependencyPath = nextNames,
                                DependencyPathIds = nextIds
                            };

                            indirectDependents.Add(indirectDto);

                            queue.Enqueue((
                                edge.DependentSystemId,
                                currentDepth + 1,
                                nextIds,
                                nextNames,
                                edge.DependencyType
                            ));
                        }
                    }
                }
            }

            var result = new DownstreamImpactResponseDTO
            {
                DisruptedSystem = MapToSystemResponse(disruptedSystem),
                TotalAffectedSystemsCount = directDependents.Count + indirectDependents.Count,
                DirectDependentsCount = directDependents.Count,
                IndirectDependentsCount = indirectDependents.Count,
                DirectDependents = directDependents,
                IndirectDependents = indirectDependents
            };

            return (true, null, false, result);
        }

        private static LogisticsSystemResponseDTO MapToSystemResponse(LogisticsSystem system)
        {
            return new LogisticsSystemResponseDTO
            {
                Id = system.Id,
                TenantId = system.TenantId,
                Name = system.Name,
                SystemType = system.SystemType,
                Description = system.Description,
                BusinessPurpose = system.BusinessPurpose,
                Criticality = system.Criticality,
                Environment = system.Environment,
                LifecycleStatus = system.LifecycleStatus,
                CreatedAt = system.CreatedAt,
                UpdatedAt = system.UpdatedAt
            };
        }

        private class GraphEdge
        {
            public Guid DependentSystemId { get; set; }
            public LogisticsSystem DependentSystem { get; set; } = null!;
            public DependencyType DependencyType { get; set; }
        }
    }
}
