using Backend.Common.Responses;
using Backend.Modules.ImpactAnalysis.DTOs;
using Backend.Modules.ImpactAnalysis.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/impact-analysis")]
    [Authorize]
    public class ImpactAnalysisController : ControllerBase
    {
        private readonly IImpactAnalysisService _impactService;

        public ImpactAnalysisController(IImpactAnalysisService impactService)
        {
            _impactService = impactService;
        }

        /// <summary>
        /// Performs operational impact analysis for a failed logistics system within the authenticated tenant.
        /// Identifies affected systems, assesses affected business operations, evaluates availability rules,
        /// and calculates potential impact severity scores.
        /// Supported paths:
        /// - GET /api/impact-analysis/{systemId}
        /// - GET /api/logistics-systems/{systemId}/impact-analysis
        /// </summary>
        [HttpGet("{systemId:guid}")]
        [HttpGet("/api/logistics-systems/{systemId:guid}/impact-analysis")]
        [ProducesResponseType(typeof(ResponseDTO<ImpactAnalysisResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<ImpactAnalysisResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<ImpactAnalysisResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AnalyzeImpact(Guid systemId, CancellationToken cancellationToken = default)
        {
            var (success, error, notFound, data) = await _impactService.AnalyzeImpactAsync(systemId, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<ImpactAnalysisResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"Logistics system with ID '{systemId}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<ImpactAnalysisResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to perform impact analysis.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<ImpactAnalysisResponseDTO>
            {
                Success = true,
                Message = "Impact analysis calculated successfully.",
                Data = data
            });
        }
    }
}
