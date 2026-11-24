using Backend.Modules.Graph.DTOs;
using Backend.Modules.Systems.DTOs;

namespace Backend.Modules.ImpactAnalysis.DTOs
{
    public class ImpactAnalysisResponseDTO
    {
        public LogisticsSystemResponseDTO FailedSystem { get; set; } = null!;

        public int TotalAffectedSystemsCount { get; set; }

        public int DirectDependentsCount { get; set; }

        public int IndirectDependentsCount { get; set; }

        public List<AffectedSystemDTO> DirectDependents { get; set; } = new();

        public List<AffectedSystemDTO> IndirectDependents { get; set; } = new();

        public int TotalAffectedOperationsCount { get; set; }

        public int CriticalOperationsCount { get; set; }

        public int HighOperationsCount { get; set; }

        public int MediumOperationsCount { get; set; }

        public int LowOperationsCount { get; set; }

        public int RequiresVerificationOperationsCount { get; set; }

        public List<AffectedOperationDTO> AffectedOperations { get; set; } = new();
    }
}
