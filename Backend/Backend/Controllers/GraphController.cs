using Backend.Common.Responses;
using Backend.Modules.Graph.DTOs;
using Backend.Modules.Graph.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/graph")]
    [Authorize]
    public class GraphController : ControllerBase
    {
        private readonly IGraphEngineService _graphService;

        public GraphController(IGraphEngineService graphService)
        {
            _graphService = graphService;
        }

        /// <summary>
        /// Retrieves potential downstream affected systems for a given disrupted logistics system within the authenticated tenant.
        /// Supported paths:
        /// - GET /api/graph/downstream-impact/{systemId}
        /// - GET /api/logistics-systems/{systemId}/downstream-impact
        /// </summary>
        [HttpGet("downstream-impact/{systemId:guid}")]
        [HttpGet("/api/logistics-systems/{systemId:guid}/downstream-impact")]
        [ProducesResponseType(typeof(ResponseDTO<DownstreamImpactResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<DownstreamImpactResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<DownstreamImpactResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDownstreamImpact(Guid systemId, CancellationToken cancellationToken = default)
        {
            var (success, error, notFound, data) = await _graphService.GetDownstreamImpactAsync(systemId, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<DownstreamImpactResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"System with ID '{systemId}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<DownstreamImpactResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to perform disruption analysis.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<DownstreamImpactResponseDTO>
            {
                Success = true,
                Message = "Downstream impact analysis calculated successfully.",
                Data = data
            });
        }
    }
}
