using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Systems.Enums;

namespace Backend.Modules.Graph.DTOs
{
    public class AffectedSystemDTO
    {
        public Guid SystemId { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public SystemType SystemType { get; set; }

        public SystemCriticality Criticality { get; set; }

        public SystemEnvironment Environment { get; set; }

        public SystemLifecycleStatus LifecycleStatus { get; set; }

        public DependencyType DependencyType { get; set; }

        public int Depth { get; set; }

        public List<string> DependencyPath { get; set; } = new();

        public List<Guid> DependencyPathIds { get; set; } = new();
    }
}
