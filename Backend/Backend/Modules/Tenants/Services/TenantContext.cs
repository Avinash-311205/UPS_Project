using System.Security.Claims;
using Backend.Modules.Tenants.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Backend.Modules.Tenants.Services
{
    /// <summary>
    /// Resolves TenantId strictly from authenticated and cryptographically validated JWT claims.
    /// Never resolves TenantId from query parameters, request bodies, or route segments.
    /// </summary>
    public class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? TenantId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                var tenantClaim = user.FindFirst("tenant_id")?.Value
                    ?? user.FindFirst("tenantId")?.Value
                    ?? user.FindFirst(ClaimTypes.GroupSid)?.Value;

                if (Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty)
                {
                    return tenantId;
                }

                return null;
            }
        }

        public bool HasTenant => TenantId.HasValue;
    }
}
