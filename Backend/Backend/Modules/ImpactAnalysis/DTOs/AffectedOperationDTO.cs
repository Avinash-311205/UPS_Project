using Backend.Modules.Dependencies.Enums;
using Backend.Modules.ImpactAnalysis.Enums;
using Backend.Modules.Operations.Enums;

namespace Backend.Modules.ImpactAnalysis.DTOs
{
    public class AffectedOperationDTO
    {
        public Guid OperationId { get; set; }

        public string OperationName { get; set; } = string.Empty;

        public OperationCriticality OperationCriticality { get; set; }

        public AvailabilityRule AvailabilityRule { get; set; }

        public OperationAvailabilityStatus AvailabilityStatus { get; set; }

        public string AvailabilityReason { get; set; } = string.Empty;

        public OperationImpactSeverity PotentialImpactSeverity { get; set; }

        public double NumericSeverityScore { get; set; }

        public ImpactScoringBreakdownDTO ScoringBreakdown { get; set; } = new();

        public ImpactType ImpactType { get; set; }

        public Guid ContributingSystemId { get; set; }

        public string ContributingSystemName { get; set; } = string.Empty;

        public DependencyType ContributingDependencyType { get; set; }

        public List<string> ContributingDependencyPath { get; set; } = new();

        public List<Guid> ContributingDependencyPathIds { get; set; } = new();

        public string SeverityReason { get; set; } = string.Empty;

        public int TotalSupportingSystemsCount { get; set; }

        public int AffectedSupportingSystemsCount { get; set; }

        public List<string> AffectedSupportingSystemNames { get; set; } = new();
    }
}
