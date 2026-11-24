namespace Backend.Modules.Tenants.Interfaces
{
    public interface ITenantContext
    {
        Guid? TenantId { get; }

        bool HasTenant { get; }
    }
}
