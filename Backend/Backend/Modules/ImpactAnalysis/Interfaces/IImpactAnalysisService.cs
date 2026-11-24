using Backend.Modules.ImpactAnalysis.DTOs;

namespace Backend.Modules.ImpactAnalysis.Interfaces
{
    public interface IImpactAnalysisService
    {
        Task<(bool Success, string? Error, bool NotFound, ImpactAnalysisResponseDTO? Data)> AnalyzeImpactAsync(
            Guid failedSystemId,
            CancellationToken cancellationToken = default);
    }
}
