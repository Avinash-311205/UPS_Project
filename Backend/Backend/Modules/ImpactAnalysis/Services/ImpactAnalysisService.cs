using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Graph.Interfaces;
using Backend.Modules.ImpactAnalysis.DTOs;
using Backend.Modules.ImpactAnalysis.Enums;
using Backend.Modules.ImpactAnalysis.Interfaces;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Interfaces;
using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.ImpactAnalysis.Services
{
    public class ImpactAnalysisService : IImpactAnalysisService
    {
        private readonly IGraphEngineService _graphEngineService;
        private readonly ILogisticsSystemRepository _systemRepository;
        private readonly IBusinessOperationRepository _operationRepository;
        private readonly ITenantContext _tenantContext;

        public ImpactAnalysisService(
            IGraphEngineService graphEngineService,
            ILogisticsSystemRepository systemRepository,
            IBusinessOperationRepository operationRepository,
            ITenantContext tenantContext)
        {
            _graphEngineService = graphEngineService;
            _systemRepository = systemRepository;
            _operationRepository = operationRepository;
            _tenantContext = tenantContext;
        }

        public async Task<(bool Success, string? Error, bool NotFound, ImpactAnalysisResponseDTO? Data)> AnalyzeImpactAsync(
            Guid failedSystemId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to perform impact analysis.", false, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            // 1. Verify failed system exists and belongs to current tenant
            var failedSystem = await _systemRepository.GetByIdAsync(failedSystemId, cancellationToken);
            if (failedSystem == null || failedSystem.TenantId != currentTenantId)
            {
                return (false, $"Logistics system with ID '{failedSystemId}' was not found.", true, null);
            }

            // Inactive systems cannot participate in operational impact calculations
            if (failedSystem.LifecycleStatus != SystemLifecycleStatus.Active)
            {
                return (false, $"System '{failedSystem.Name}' is inactive and cannot participate in operational impact calculations.", false, null);
            }

            // 2. Query Graph Engine for downstream system impact
            var (graphSuccess, graphError, graphNotFound, graphData) = await _graphEngineService.GetDownstreamImpactAsync(failedSystemId, cancellationToken);
            if (!graphSuccess)
            {
                return (false, graphError, graphNotFound, null);
            }

            // 3. Compile the full set of affected systems (Failed system at depth 0 + direct & indirect dependents)
            var affectedLookup = new Dictionary<Guid, AffectedSystemContext>();

            // Failed system is the root failure: depth 0, mandatory direct impact
            affectedLookup[failedSystem.Id] = new AffectedSystemContext
            {
                SystemId = failedSystem.Id,
                SystemName = failedSystem.Name,
                Criticality = failedSystem.Criticality,
                DependencyType = DependencyType.Mandatory,
                Depth = 0,
                DependencyPath = new List<string> { failedSystem.Name },
                DependencyPathIds = new List<Guid> { failedSystem.Id },
                IsRootFailure = true
            };

            foreach (var direct in graphData!.DirectDependents)
            {
                affectedLookup[direct.SystemId] = new AffectedSystemContext
                {
                    SystemId = direct.SystemId,
                    SystemName = direct.SystemName,
                    Criticality = direct.Criticality,
                    DependencyType = direct.DependencyType,
                    Depth = direct.Depth,
                    DependencyPath = direct.DependencyPath,
                    DependencyPathIds = direct.DependencyPathIds,
                    IsRootFailure = false
                };
            }

            foreach (var indirect in graphData.IndirectDependents)
            {
                if (!affectedLookup.ContainsKey(indirect.SystemId))
                {
                    affectedLookup[indirect.SystemId] = new AffectedSystemContext
                    {
                        SystemId = indirect.SystemId,
                        SystemName = indirect.SystemName,
                        Criticality = indirect.Criticality,
                        DependencyType = indirect.DependencyType,
                        Depth = indirect.Depth,
                        DependencyPath = indirect.DependencyPath,
                        DependencyPathIds = indirect.DependencyPathIds,
                        IsRootFailure = false
                    };
                }
            }

            // 4. Retrieve all operations for the tenant
            var allOperations = await _operationRepository.GetAllAsync(includeInactive: false, cancellationToken);
            var tenantOperations = allOperations.Where(o => o.TenantId == currentTenantId);

            var affectedOperations = new List<AffectedOperationDTO>();

            // 5. Evaluate each business operation
            foreach (var operation in tenantOperations)
            {
                var totalSupporting = operation.SupportingSystems.Count;
                if (totalSupporting == 0)
                {
                    // Operations without supporting systems cannot be affected by system failures
                    continue;
                }

                // Identify affected supporting systems
                var affectedSupporting = operation.SupportingSystems
                    .Where(sbo => affectedLookup.ContainsKey(sbo.LogisticsSystemId))
                    .Select(sbo => affectedLookup[sbo.LogisticsSystemId])
                    .ToList();

                if (affectedSupporting.Count == 0)
                {
                    // None of the operation's supporting systems are affected
                    continue;
                }

                // 6. Availability Evaluation based on AvailabilityRule
                OperationAvailabilityStatus availabilityStatus;
                string availabilityReason;

                var affectedNames = affectedSupporting.Select(a => a.SystemName).ToList();

                if (operation.AvailabilityRule == AvailabilityRule.AllRequired)
                {
                    // AllRequired: An operation is potentially affected if any required supporting system is potentially affected
                    availabilityStatus = OperationAvailabilityStatus.PotentiallyAffected;
                    availabilityReason = $"Operation requires all supporting systems to be available. {affectedSupporting.Count} of {totalSupporting} supporting system(s) are affected ({string.Join(", ", affectedNames)}).";
                }
                else // AvailabilityRule.AnyAvailable
                {
                    // AnyAvailable: Potentially affected when all its alternative supporting systems are affected
                    if (affectedSupporting.Count == totalSupporting)
                    {
                        availabilityStatus = OperationAvailabilityStatus.PotentiallyAffected;
                        availabilityReason = $"All {totalSupporting} alternative supporting system(s) are affected ({string.Join(", ", affectedNames)}).";
                    }
                    else
                    {
                        // Some supporting systems affected, but alternative systems exist outside the blast radius
                        // Since operational telemetry cannot confirm live health, mark as RequiresVerification
                        availabilityStatus = OperationAvailabilityStatus.RequiresVerification;
                        availabilityReason = $"{affectedSupporting.Count} of {totalSupporting} alternative supporting system(s) are affected ({string.Join(", ", affectedNames)}). Alternative supporting system(s) require operational verification as live health cannot be confirmed.";
                    }
                }

                // 7. Deterministic Severity Scoring & Worst-Case Path Selection
                // Calculate score for each affected supporting system and select the highest-severity path
                var scoredCandidates = affectedSupporting.Select(sysContext =>
                {
                    var (score, breakdown, severity) = CalculateSeverityScore(
                        sysContext.Criticality,
                        sysContext.DependencyType,
                        operation.Criticality);

                    return new
                    {
                        SystemContext = sysContext,
                        Score = score,
                        Breakdown = breakdown,
                        Severity = severity
                    };
                })
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.SystemContext.IsRootFailure ? 0 : 1) // Prioritize direct failure
                .ThenBy(c => c.SystemContext.Depth)                 // Prioritize shorter depth
                .ThenByDescending(c => (int)c.SystemContext.Criticality)
                .ThenBy(c => c.SystemContext.SystemName)
                .ToList();

                var winningCandidate = scoredCandidates.First();
                var winningSystem = winningCandidate.SystemContext;

                var impactType = winningSystem.IsRootFailure ? ImpactType.Direct : ImpactType.Indirect;
                var severityReason = $"Severity score of {winningCandidate.Score:F1} ({winningCandidate.Severity}) was determined by contributing system '{winningSystem.SystemName}' ({winningSystem.Criticality} Criticality, {winningSystem.DependencyType} Dependency, path depth {winningSystem.Depth}) impacting operation with {operation.Criticality} Criticality.";

                affectedOperations.Add(new AffectedOperationDTO
                {
                    OperationId = operation.Id,
                    OperationName = operation.Name,
                    OperationCriticality = operation.Criticality,
                    AvailabilityRule = operation.AvailabilityRule,
                    AvailabilityStatus = availabilityStatus,
                    AvailabilityReason = availabilityReason,
                    PotentialImpactSeverity = winningCandidate.Severity,
                    NumericSeverityScore = winningCandidate.Score,
                    ScoringBreakdown = winningCandidate.Breakdown,
                    ImpactType = impactType,
                    ContributingSystemId = winningSystem.SystemId,
                    ContributingSystemName = winningSystem.SystemName,
                    ContributingDependencyType = winningSystem.DependencyType,
                    ContributingDependencyPath = winningSystem.DependencyPath,
                    ContributingDependencyPathIds = winningSystem.DependencyPathIds,
                    SeverityReason = severityReason,
                    TotalSupportingSystemsCount = totalSupporting,
                    AffectedSupportingSystemsCount = affectedSupporting.Count,
                    AffectedSupportingSystemNames = affectedNames
                });
            }

            // Sort operations deterministically: highest severity score first, then operation criticality, then name
            var sortedOperations = affectedOperations
                .OrderByDescending(o => o.NumericSeverityScore)
                .ThenByDescending(o => (int)o.OperationCriticality)
                .ThenBy(o => o.OperationName)
                .ToList();

            var response = new ImpactAnalysisResponseDTO
            {
                FailedSystem = MapToSystemDTO(failedSystem),
                TotalAffectedSystemsCount = graphData.TotalAffectedSystemsCount,
                DirectDependentsCount = graphData.DirectDependentsCount,
                IndirectDependentsCount = graphData.IndirectDependentsCount,
                DirectDependents = graphData.DirectDependents,
                IndirectDependents = graphData.IndirectDependents,
                TotalAffectedOperationsCount = sortedOperations.Count,
                CriticalOperationsCount = sortedOperations.Count(o => o.PotentialImpactSeverity == OperationImpactSeverity.Critical),
                HighOperationsCount = sortedOperations.Count(o => o.PotentialImpactSeverity == OperationImpactSeverity.High),
                MediumOperationsCount = sortedOperations.Count(o => o.PotentialImpactSeverity == OperationImpactSeverity.Medium),
                LowOperationsCount = sortedOperations.Count(o => o.PotentialImpactSeverity == OperationImpactSeverity.Low),
                RequiresVerificationOperationsCount = sortedOperations.Count(o => o.AvailabilityStatus == OperationAvailabilityStatus.RequiresVerification),
                AffectedOperations = sortedOperations
            };

            return (true, null, false, response);
        }

        /// <summary>
        /// Calculates weighted impact severity score based on approved ResilienceX weights:
        /// - System Criticality: 30% (Low=1, Medium=2, High=3, Critical=4)
        /// - Dependency Type: 20% (Optional=1, Mandatory=4)
        /// - Operation Criticality: 50% (Low=1, Medium=2, High=3, Critical=4)
        /// Thresholds:
        /// - 0–39: Low
        /// - 40–59: Medium
        /// - 60–79: High
        /// - 80–100: Critical
        /// </summary>
        public static (double TotalScore, ImpactScoringBreakdownDTO Breakdown, OperationImpactSeverity Severity)
            CalculateSeverityScore(
                SystemCriticality systemCriticality,
                DependencyType dependencyType,
                OperationCriticality operationCriticality)
        {
            int sysWeight = (int)systemCriticality;
            int depWeight = dependencyType == DependencyType.Mandatory ? 4 : 1;
            int opWeight = (int)operationCriticality;

            double sysScore = (sysWeight / 4.0) * 30.0;
            double depScore = (depWeight / 4.0) * 20.0;
            double opScore = (opWeight / 4.0) * 50.0;
            double totalScore = Math.Round(sysScore + depScore + opScore, 2);

            OperationImpactSeverity severity;
            if (totalScore >= 80.0)
            {
                severity = OperationImpactSeverity.Critical;
            }
            else if (totalScore >= 60.0)
            {
                severity = OperationImpactSeverity.High;
            }
            else if (totalScore >= 40.0)
            {
                severity = OperationImpactSeverity.Medium;
            }
            else
            {
                severity = OperationImpactSeverity.Low;
            }

            var breakdown = new ImpactScoringBreakdownDTO
            {
                SystemCriticality = systemCriticality,
                SystemCriticalityWeight = sysWeight,
                SystemCriticalityScore = sysScore,
                DependencyType = dependencyType,
                DependencyTypeWeight = depWeight,
                DependencyTypeScore = depScore,
                OperationCriticality = operationCriticality,
                OperationCriticalityWeight = opWeight,
                OperationCriticalityScore = opScore,
                TotalScore = totalScore
            };

            return (totalScore, breakdown, severity);
        }

        private static LogisticsSystemResponseDTO MapToSystemDTO(Backend.Modules.Systems.Models.LogisticsSystem system)
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

        private sealed class AffectedSystemContext
        {
            public Guid SystemId { get; set; }
            public string SystemName { get; set; } = string.Empty;
            public SystemCriticality Criticality { get; set; }
            public DependencyType DependencyType { get; set; }
            public int Depth { get; set; }
            public List<string> DependencyPath { get; set; } = new();
            public List<Guid> DependencyPathIds { get; set; } = new();
            public bool IsRootFailure { get; set; }
        }
    }
}
