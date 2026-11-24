using Backend.Common.Responses;
using Backend.Modules.Operations.DTOs;
using Backend.Modules.Operations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/business-operations")]
    [Authorize]
    public class BusinessOperationsController : ControllerBase
    {
        private readonly IBusinessOperationService _operationService;

        public BusinessOperationsController(IBusinessOperationService operationService)
        {
            _operationService = operationService;
        }

        /// <summary>
        /// Retrieves all business operations for the authenticated tenant.
        /// By default, only active operations are returned. Set includeInactive=true to include deactivated ones.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<BusinessOperationResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var operations = await _operationService.GetAllAsync(includeInactive, cancellationToken);
            return Ok(new ResponseDTO<IEnumerable<BusinessOperationResponseDTO>>
            {
                Success = true,
                Message = "Business operations retrieved successfully.",
                Data = operations
            });
        }

        /// <summary>
        /// Retrieves a specific business operation by ID for the authenticated tenant.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var operation = await _operationService.GetByIdAsync(id, cancellationToken);
            if (operation == null)
            {
                return NotFound(new ResponseDTO<BusinessOperationResponseDTO>
                {
                    Success = false,
                    Message = $"Business operation with ID '{id}' was not found.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<BusinessOperationResponseDTO>
            {
                Success = true,
                Message = "Business operation retrieved successfully.",
                Data = operation
            });
        }

        /// <summary>
        /// Creates a new business operation for the authenticated tenant.
        /// TenantId is automatically determined from the security context and cannot be overridden.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateBusinessOperationDTO dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<BusinessOperationResponseDTO>
                {
                    Success = false,
                    Message = "Invalid request payload.",
                    Data = null
                });
            }

            var (success, error, isConflict, data) = await _operationService.CreateAsync(dto, cancellationToken);
            if (!success)
            {
                if (isConflict)
                {
                    return Conflict(new ResponseDTO<BusinessOperationResponseDTO>
                    {
                        Success = false,
                        Message = error ?? "A business operation with this name already exists.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<BusinessOperationResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to create business operation.",
                    Data = null
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = data!.Id },
                new ResponseDTO<BusinessOperationResponseDTO>
                {
                    Success = true,
                    Message = "Business operation created successfully.",
                    Data = data
                });
        }

        /// <summary>
        /// Updates an existing business operation within the authenticated tenant.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseDTO<BusinessOperationResponseDTO>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateBusinessOperationDTO dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<BusinessOperationResponseDTO>
                {
                    Success = false,
                    Message = "Invalid request payload.",
                    Data = null
                });
            }

            var (success, error, notFound, isConflict, data) = await _operationService.UpdateAsync(id, dto, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<BusinessOperationResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"Business operation with ID '{id}' was not found.",
                        Data = null
                    });
                }

                if (isConflict)
                {
                    return Conflict(new ResponseDTO<BusinessOperationResponseDTO>
                    {
                        Success = false,
                        Message = error ?? "A business operation with this name already exists.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<BusinessOperationResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to update business operation.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<BusinessOperationResponseDTO>
            {
                Success = true,
                Message = "Business operation updated successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Deactivates a business operation (soft deactivation).
        /// Preserves the record in the database for impact analysis, incident management, and audit tracking.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var (success, error, notFound) = await _operationService.DeleteAsync(id, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<bool>
                    {
                        Success = false,
                        Message = error ?? $"Business operation with ID '{id}' was not found.",
                        Data = false
                    });
                }

                return BadRequest(new ResponseDTO<bool>
                {
                    Success = false,
                    Message = error ?? "Failed to deactivate business operation.",
                    Data = false
                });
            }

            return Ok(new ResponseDTO<bool>
            {
                Success = true,
                Message = "Business operation deactivated successfully.",
                Data = true
            });
        }

        /// <summary>
        /// Assigns a logistics system to a business operation.
        /// Both must belong to the authenticated tenant. Inactive systems cannot be assigned.
        /// </summary>
        [HttpPost("{id:guid}/systems/{systemId:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<SystemBusinessOperationResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<SystemBusinessOperationResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<SystemBusinessOperationResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseDTO<SystemBusinessOperationResponseDTO>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AssignSystem(
            Guid id,
            Guid systemId,
            CancellationToken cancellationToken)
        {
            var (success, error, notFound, isConflict, data) = await _operationService.AssignSystemAsync(id, systemId, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<SystemBusinessOperationResponseDTO>
                    {
                        Success = false,
                        Message = error ?? "Specified business operation or logistics system was not found.",
                        Data = null
                    });
                }

                if (isConflict)
                {
                    return Conflict(new ResponseDTO<SystemBusinessOperationResponseDTO>
                    {
                        Success = false,
                        Message = error ?? "Logistics system is already assigned to this business operation.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<SystemBusinessOperationResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to assign logistics system to business operation.",
                    Data = null
                });
            }

            return StatusCode(StatusCodes.Status201Created, new ResponseDTO<SystemBusinessOperationResponseDTO>
            {
                Success = true,
                Message = "Logistics system assigned to business operation successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Removes the assignment between a logistics system and a business operation.
        /// Only the relationship record is deleted; neither the system nor the operation is deleted.
        /// </summary>
        [HttpDelete("{id:guid}/systems/{systemId:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveSystem(
            Guid id,
            Guid systemId,
            CancellationToken cancellationToken)
        {
            var (success, error, notFound) = await _operationService.RemoveSystemAsync(id, systemId, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<bool>
                    {
                        Success = false,
                        Message = error ?? "Assignment between business operation and logistics system was not found.",
                        Data = false
                    });
                }

                return BadRequest(new ResponseDTO<bool>
                {
                    Success = false,
                    Message = error ?? "Failed to remove assignment.",
                    Data = false
                });
            }

            return Ok(new ResponseDTO<bool>
            {
                Success = true,
                Message = "Logistics system removed from business operation successfully.",
                Data = true
            });
        }

        /// <summary>
        /// Retrieves all logistics systems that support a specific business operation.
        /// </summary>
        [HttpGet("{id:guid}/systems")]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSystemsForOperation(Guid id, CancellationToken cancellationToken)
        {
            var (success, error, notFound, data) = await _operationService.GetSystemsForOperationAsync(id, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>
                    {
                        Success = false,
                        Message = error ?? $"Business operation with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>
                {
                    Success = false,
                    Message = error ?? "Failed to retrieve systems for business operation.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>
            {
                Success = true,
                Message = "Systems supporting business operation retrieved successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Retrieves all business operations supported by a specific logistics system.
        /// </summary>
        [HttpGet("by-system/{systemId:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOperationsForSystem(Guid systemId, CancellationToken cancellationToken)
        {
            var (success, error, notFound, data) = await _operationService.GetOperationsForSystemAsync(systemId, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>
                    {
                        Success = false,
                        Message = error ?? $"Logistics system with ID '{systemId}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>
                {
                    Success = false,
                    Message = error ?? "Failed to retrieve operations for logistics system.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<IEnumerable<SystemBusinessOperationResponseDTO>>
            {
                Success = true,
                Message = "Business operations for logistics system retrieved successfully.",
                Data = data
            });
        }
    }
}
