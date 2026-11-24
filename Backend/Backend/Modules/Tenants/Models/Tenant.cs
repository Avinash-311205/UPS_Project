using Backend.Modules.Dependencies.Models;
using Backend.Modules.Identity.Models;
using Backend.Modules.Operations.Models;
using Backend.Modules.Systems.Models;

namespace Backend.Modules.Tenants.Models
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<User> Users { get; set; } = new List<User>();

        public ICollection<LogisticsSystem> Systems { get; set; } = new List<LogisticsSystem>();

        public ICollection<SystemDependency> Dependencies { get; set; } = new List<SystemDependency>();

        public ICollection<BusinessOperation> BusinessOperations { get; set; } = new List<BusinessOperation>();

        public ICollection<SystemBusinessOperation> SystemBusinessOperations { get; set; } = new List<SystemBusinessOperation>();

        public ICollection<Backend.Modules.Incidents.Models.Incident> Incidents { get; set; } = new List<Backend.Modules.Incidents.Models.Incident>();
    }
}
