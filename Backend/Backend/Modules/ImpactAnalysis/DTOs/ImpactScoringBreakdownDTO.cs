using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Systems.Enums;

namespace Backend.Modules.ImpactAnalysis.DTOs
{
    public class ImpactScoringBreakdownDTO
    {
        public SystemCriticality SystemCriticality { get; set; }

        public int SystemCriticalityWeight { get; set; }

        public double SystemCriticalityScore { get; set; }

        public DependencyType DependencyType { get; set; }

        public int DependencyTypeWeight { get; set; }

        public double DependencyTypeScore { get; set; }

        public OperationCriticality OperationCriticality { get; set; }

        public int OperationCriticalityWeight { get; set; }

        public double OperationCriticalityScore { get; set; }

        public double TotalScore { get; set; }
    }
}
