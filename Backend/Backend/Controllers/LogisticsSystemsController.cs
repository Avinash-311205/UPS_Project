using Backend.Common.Responses;
using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/logistics-systems")]
    [Authorize]
    public class LogisticsSystemsController : ControllerBase
    {
        private readonly ILogisticsSystemService _systemService;

        public LogisticsSystemsController(ILogisticsSystemService systemService)
        {
            _systemService = systemService;
        }

        /// <summary>
        /// Retrieves logistics systems belonging to the authenticated tenant.
        /// By default, only active systems are returned to prevent treating inactive systems as active.
        /// Pass includeInactive=true to retrieve retired or deactivated systems.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<LogisticsSystemResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var systems = await _systemService.GetAllAsync(includeInactive, cancellationToken);
            return Ok(new ResponseDTO<IEnumerable<LogisticsSystemResponseDTO>>
            {
                Success = true,
                Message = "Logistics systems retrieved successfully.",
                Data = systems
            });
        }

        /// <summary>
        /// Retrieves a specific logistics system by ID within the authenticated tenant.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var system = await _systemService.GetByIdAsync(id, cancellationToken);
            if (system == null)
            {
                return NotFound(new ResponseDTO<LogisticsSystemResponseDTO>
                {
                    Success = false,
                    Message = $"Logistics system with ID '{id}' was not found.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<LogisticsSystemResponseDTO>
            {
                Success = true,
                Message = "Logistics system retrieved successfully.",
                Data = system
            });
        }

        /// <summary>
        /// Registers a new logistics system for the authenticated tenant.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(
            [FromBody] CreateLogisticsSystemDTO createDto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<LogisticsSystemResponseDTO>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            var (success, error, data) = await _systemService.CreateAsync(createDto, cancellationToken);
            if (!success)
            {
                return BadRequest(new ResponseDTO<LogisticsSystemResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to register logistics system.",
                    Data = null
                });
            }

            return StatusCode(StatusCodes.Status201Created, new ResponseDTO<LogisticsSystemResponseDTO>
            {
                Success = true,
                Message = "Logistics system registered successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Updates an existing logistics system within the authenticated tenant.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<LogisticsSystemResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateLogisticsSystemDTO updateDto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<LogisticsSystemResponseDTO>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            var (success, error, notFound, data) = await _systemService.UpdateAsync(id, updateDto, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<LogisticsSystemResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"Logistics system with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<LogisticsSystemResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to update logistics system.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<LogisticsSystemResponseDTO>
            {
                Success = true,
                Message = "Logistics system updated successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Deactivates a logistics system belonging to the authenticated tenant (soft delete).
        /// Preserves historical references for dependency tracking, incidents, and audit logs.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var (success, error, notFound) = await _systemService.DeleteAsync(id, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<bool>
                    {
                        Success = false,
                        Message = error ?? $"Logistics system with ID '{id}' was not found.",
                        Data = false
                    });
                }

                return BadRequest(new ResponseDTO<bool>
                {
                    Success = false,
                    Message = error ?? "Failed to deactivate logistics system.",
                    Data = false
                });
            }

            return Ok(new ResponseDTO<bool>
            {
                Success = true,
                Message = "Logistics system deactivated successfully.",
                Data = true
            });
        }
    }
}
