using Backend.Common.Responses;
using Backend.Modules.ImpactAnalysis.DTOs;
using Backend.Modules.Incidents.DTOs;
using Backend.Modules.Incidents.Enums;
using Backend.Modules.Incidents.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/incidents")]
    [Authorize]
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentsController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        /// <summary>
        /// Retrieves all incidents belonging to the authenticated tenant, with optional status, severity, type, and system filters.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<IncidentResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] IncidentStatus? status = null,
            [FromQuery] IncidentSeverity? severity = null,
            [FromQuery] IncidentType? type = null,
            [FromQuery] Guid? failedSystemId = null,
            CancellationToken cancellationToken = default)
        {
            var incidents = await _incidentService.GetAllAsync(status, severity, type, failedSystemId, cancellationToken);
            return Ok(new ResponseDTO<IEnumerable<IncidentResponseDTO>>
            {
                Success = true,
                Message = "Incidents retrieved successfully.",
                Data = incidents
            });
        }

        /// <summary>
        /// Retrieves a specific incident by ID within the authenticated tenant.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var incident = await _incidentService.GetByIdAsync(id, cancellationToken);
            if (incident == null)
            {
                return NotFound(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = $"Incident with ID '{id}' was not found.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<IncidentResponseDTO>
            {
                Success = true,
                Message = "Incident retrieved successfully.",
                Data = incident
            });
        }

        /// <summary>
        /// Reports a new disruption incident against a registered logistics system.
        /// TenantId is automatically derived from the authenticated context.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateIncidentDTO dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = "Invalid request payload.",
                    Data = null
                });
            }

            var (success, error, notFound, data) = await _incidentService.CreateAsync(dto, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<IncidentResponseDTO>
                    {
                        Success = false,
                        Message = error ?? "Logistics system not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to create incident.",
                    Data = null
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = data!.Id },
                new ResponseDTO<IncidentResponseDTO>
                {
                    Success = true,
                    Message = "Incident created successfully.",
                    Data = data
                });
        }

        /// <summary>
        /// Updates the title, description, type, or severity of an existing incident.
        /// Closed incidents cannot be modified.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateIncidentDTO dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = "Invalid request payload.",
                    Data = null
                });
            }

            var (success, error, notFound, data) = await _incidentService.UpdateAsync(id, dto, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<IncidentResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"Incident with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to update incident.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<IncidentResponseDTO>
            {
                Success = true,
                Message = "Incident updated successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Transitions the lifecycle status of an incident (e.g., Open -> Investigating -> Mitigating -> Resolved -> Closed).
        /// Enforces valid status transition rules and records resolution timestamps.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<IncidentResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateIncidentStatusDTO dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = "Invalid request payload.",
                    Data = null
                });
            }

            var (success, error, notFound, data) = await _incidentService.UpdateStatusAsync(id, dto, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<IncidentResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"Incident with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<IncidentResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to update incident status.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<IncidentResponseDTO>
            {
                Success = true,
                Message = "Incident status updated successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Computes potential operational blast radius and business operation impact for this incident's failed system.
        /// Integrates with the existing Graph Engine and Impact Analysis Engine without duplicating logic.
        /// </summary>
        [HttpGet("{id:guid}/impact-analysis")]
        [ProducesResponseType(typeof(ResponseDTO<ImpactAnalysisResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<ImpactAnalysisResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<ImpactAnalysisResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetImpactAnalysis(Guid id, CancellationToken cancellationToken)
        {
            var (success, error, notFound, data) = await _incidentService.GetImpactAnalysisAsync(id, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<ImpactAnalysisResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"Incident with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<ImpactAnalysisResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to perform impact analysis for incident.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<ImpactAnalysisResponseDTO>
            {
                Success = true,
                Message = "Incident impact analysis calculated successfully.",
                Data = data
            });
        }
    }
}
