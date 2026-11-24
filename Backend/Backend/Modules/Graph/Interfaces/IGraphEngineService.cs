using Backend.Modules.Graph.DTOs;

namespace Backend.Modules.Graph.Interfaces
{
    public interface IGraphEngineService
    {
        Task<(bool Success, string? Error, bool NotFound, DownstreamImpactResponseDTO? Data)> GetDownstreamImpactAsync(
            Guid disruptedSystemId,
            CancellationToken cancellationToken = default);
    }
}
