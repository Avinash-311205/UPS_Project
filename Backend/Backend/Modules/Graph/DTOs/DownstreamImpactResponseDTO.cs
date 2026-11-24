using Backend.Modules.Systems.DTOs;

namespace Backend.Modules.Graph.DTOs
{
    public class DownstreamImpactResponseDTO
    {
        public LogisticsSystemResponseDTO DisruptedSystem { get; set; } = null!;

        public int TotalAffectedSystemsCount { get; set; }

        public int DirectDependentsCount { get; set; }

        public int IndirectDependentsCount { get; set; }

        public List<AffectedSystemDTO> DirectDependents { get; set; } = new();

        public List<AffectedSystemDTO> IndirectDependents { get; set; } = new();
    }
}
