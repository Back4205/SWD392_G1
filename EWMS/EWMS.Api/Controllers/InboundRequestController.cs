using System.Security.Claims;
using EWMS.Api.Data;
using EWMS.Api.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Warehouse Staff,Manager,Purchasing Manager")]
    [Route("api/inbound-requests")]
    public class InboundRequestController : ControllerBase
    {
        private readonly IInboundRequestService _service;
        private readonly EWMSDbContext _context;

        public InboundRequestController(
            IInboundRequestService service, EWMSDbContext context)
        {
            _service = service;
            _context = context;
        }

        // Task 1: Create Inbound Request
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(
            [FromBody] CreateInboundRequestDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                if (!await CanUseWarehouseAsync(dto.WarehouseId)) return Forbid();
                int requestedByUserId = UserId();

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

            var warehouses = await AssignedWarehousesAsync();
            requests = requests.Where(x => warehouses.Contains(x.WarehouseId)).ToList();
            if (User.IsInRole("Warehouse Staff"))
                requests = requests.Where(x => CanStaffRead(x.Status)).ToList();
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

            if (!await CanUseWarehouseAsync(request.WarehouseId)) return Forbid();
            if (User.IsInRole("Warehouse Staff") && !CanStaffRead(request.Status)) return Forbid();
            return Ok(request);
        }

        // Task 4: Update Supplier Commitment
        [HttpPut("{id:int}/supplier-commitment")]
        [Authorize(Roles = "Purchasing Manager")]
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
                var request = await _service.GetByIdAsync(id);
                if (request == null) return NotFound(new { message = "Inbound request not found." });
                if (!await CanUseWarehouseAsync(request.WarehouseId)) return Forbid();
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
            catch (GoodsReceiptException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
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
        [Authorize(Roles = "Manager")]
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
                var request = await _service.GetByIdAsync(id);
                if (request == null) return NotFound(new { message = "Inbound request not found." });
                if (!await CanUseWarehouseAsync(request.WarehouseId)) return Forbid();
                int managerUserId = UserId();

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
            catch (GoodsReceiptException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
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
                var request = await _service.GetByIdAsync(id);
                if (request == null) return NotFound(new { message = "Inbound request not found." });
                if (!await CanUseWarehouseAsync(request.WarehouseId)) return Forbid();
                if (User.IsInRole("Warehouse Staff") && !CanStaffRead(request.Status)) return Forbid();
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
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }


        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            var warehouses = await AssignedWarehousesAsync();
            var products = await _context.ProductVariants.AsNoTracking()
                .Where(x => x.Product != null && x.Product.Status == "Active" && x.WeightKg > 0)
                .OrderBy(x => x.SKU)
                .Select(x => new { x.Id, sku = x.SKU, productName = x.Product!.ProductName }).ToListAsync();
            return Ok(new
            {
                warehouses = await _context.Warehouses.AsNoTracking()
                    .Where(x => warehouses.Contains(x.Id) && x.Status == "Active")
                    .OrderBy(x => x.WarehouseName).Select(x => new { x.Id, x.WarehouseName }).ToListAsync(),
                suppliers = await _context.Suppliers.AsNoTracking().Where(x => x.Status == "Active")
                    .OrderBy(x => x.SupplierName).Select(x => new { x.Id, x.SupplierName }).ToListAsync(),
                productVariants = products.Where(x => !string.IsNullOrWhiteSpace(x.sku)).ToList(),
                // SD03: chỉ chọn Staff đang hoạt động và được phân công vào kho đang xem.
                warehouseStaff = await _context.UserWarehouses.AsNoTracking()
                    .Where(x => warehouses.Contains(x.WarehouseId) && x.Warehouse.Status == "Active"
                        && x.User.Status == "Active" && x.User.Role.RoleName == "Warehouse Staff")
                    .OrderBy(x => x.User.FullName)
                    .Select(x => new { x.UserId, fullName = x.User.FullName, x.WarehouseId }).ToListAsync()
            });
        }

        private int UserId() { return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); }
        private Task<List<int>> AssignedWarehousesAsync()
        {
            int userId = UserId();
            return _context.UserWarehouses.Where(x => x.UserId == userId).Select(x => x.WarehouseId).ToListAsync();
        }
        private async Task<bool> CanUseWarehouseAsync(int warehouseId)
        {
            int userId = UserId();
            return await _context.UserWarehouses.AnyAsync(x => x.UserId == userId && x.WarehouseId == warehouseId);
        }
        private static bool CanStaffRead(string status)
        {
            return status == "Approved" || status == "PartiallyReceived" || status == "Completed";
        }

    }
}
