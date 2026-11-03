
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.Api.Controllers
{
    [ApiController]
    [Route("api/inbound-requests")]
    public class InboundRequestController : ControllerBase
    {
        private readonly IInboundRequestService _service;

        public InboundRequestController(
            IInboundRequestService service)
        {
            _service = service;
        }

        // Task 1: Create Inbound Request
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateInboundRequestDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                // Temporary local test user.
                // Replace with authenticated Manager ID
                // when JWT integration is available.
                const int requestedByUserId = 1;

                var requestId = await _service.CreateAsync(
                    dto, requestedByUserId);

                return StatusCode(StatusCodes.Status201Created,
                    new
                    {
                        message = "Inbound request created successfully.",
                        id = requestId,
                        status = "Pending"
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // Task 2: Get Inbound Request List
        [HttpGet]
        public async Task<ActionResult<List<InboundRequestListDto>>> GetAll()
        {
            var requests = await _service.GetAllAsync();

            return Ok(requests);
        }

        // Task 3: Get Inbound Request Detail
        [HttpGet("{id:int}")]
        public async Task<ActionResult<InboundRequestDetailDto>> GetById(
            int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid inbound request ID."
                });
            }

            var request = await _service.GetByIdAsync(id);

            if (request == null)
            {
                return NotFound(new
                {
                    message = $"Inbound request #{id} was not found."
                });
            }

            return Ok(request);
        }

        // Task 4: Update Supplier Commitment
        [HttpPut("{id:int}/supplier-commitment")]
        public async Task<IActionResult> UpdateSupplierCommitment(
            int id,
            [FromBody] UpdateSupplierCommitmentDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid inbound request ID."
                });
            }

            try
            {
                await _service.UpdateSupplierCommitmentAsync(id, dto);

                return Ok(new
                {
                    message = "Supplier commitment updated successfully.",
                    id = id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> ReviewInboundRequest(
            int id,
            [FromBody] ReviewInboundRequestDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid inbound request ID."
                });
            }

            try
            {
                // TEMPORARY: Replace with authenticated Manager ID
                // when Auth module is integrated.
                // TODO (AUTH INTEGRATION):
                // Replace hardcoded managerUserId with authenticated user ID.
                // Restrict this endpoint to users with Manager role.
                int managerUserId = 1;

                await _service.ReviewInboundRequestAsync(
                    id,
                    dto,
                    managerUserId
                );

                return Ok(new
                {
                    message = "Inbound request reviewed successfully.",
                    id = id,
                    status = dto.Decision.Trim()
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // Task 6: Get Receiving Progress
        [HttpGet("{id:int}/receiving-progress")]
        public async Task<IActionResult> GetReceivingProgress(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid inbound request ID."
                });
            }

            try
            {
                var progress = await _service.GetReceivingProgressAsync(id);

                if (progress == null)
                {
                    return NotFound(new
                    {
                        message = $"Inbound request #{id} was not found."
                    });
                }

                return Ok(progress);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


    }
}
