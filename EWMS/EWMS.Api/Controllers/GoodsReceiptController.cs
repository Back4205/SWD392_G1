using System.Security.Claims;
using EWMS.Api.Common;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Interfaces;
using EWMS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Warehouse Staff,Manager")]
    [Route("api/goods-receipts")]
    public class GoodsReceiptController : ControllerBase
    {
        private readonly IGoodsReceiptService _service;
        private readonly SerialNumberService _serialService;
        private readonly GoodsReceiptApprovalService _approvalService;
        private readonly ILogger<GoodsReceiptController> _logger;

        public GoodsReceiptController(IGoodsReceiptService service, SerialNumberService serialService,
            GoodsReceiptApprovalService approvalService, ILogger<GoodsReceiptController> logger)
        {
            _service = service;
            _serialService = serialService;
            _approvalService = approvalService;
            _logger = logger;
        }

        [HttpGet("session")]
        public async Task<IActionResult> GetSession()
        {
            try
            {
                var session = await _service.GetSessionAsync(GetUserId());
                return Ok(session);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("lookups/inbounds")]
        public async Task<IActionResult> GetReceivableInbounds()
        {
            try
            {
                var inbounds = await _service.GetReceivableInboundsAsync(GetUserId());
                return Ok(inbounds);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("lookups/bins")]
        public async Task<IActionResult> GetBins([FromQuery] int warehouseId)
        {
            try
            {
                var bins = await _service.GetBinsAsync(warehouseId, GetUserId());
                return Ok(bins);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-15: GET /api/goods-receipts?status=PendingApproval&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GoodsReceiptFilterDto filter)
        {
            try
            {
                var receipts = await _service.GetAllAsync(filter, GetUserId());
                return Ok(receipts);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-16: chi tiết gồm sản phẩm, scan, bin, tiến độ và lý do từ chối.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var receipt = await _service.GetByIdAsync(id, GetUserId());
                return Ok(receipt);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-17: chỉ tạo Draft, chưa nhập tồn kho.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGoodsReceiptDto dto)
        {
            try
            {
                var receipt = await _service.CreateAsync(dto, GetUserId());
                return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-18 SAVE_DRAFT: lưu vị trí của các dòng đã có.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] UpdateGoodsReceiptDto dto)
        {
            try
            {
                var receipt = await _service.UpdateDraftAsync(id, dto, GetUserId());
                return Ok(receipt);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-18 SUBMIT: chuyển PendingApproval để Manager xem ngay.
        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id, [FromBody] ReceiptVersionDto dto)
        {
            try
            {
                var receipt = await _service.SubmitAsync(id, dto, GetUserId());
                return Ok(receipt);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-50: một request là một scan, server tự tính số lượng.
        [HttpPost("{id:int}/items/{itemId:int}/serials")]
        public async Task<IActionResult> AddSerial(int id, int itemId, [FromBody] AddReceiptSerialDto dto)
        {
            try
            {
                var serial = await _serialService.AddAsync(id, itemId, dto, GetUserId());
                return Ok(serial);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-56 APPROVE: chỉ Manager, toàn bộ transaction nằm trong service.
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ReceiptVersionDto dto)
        {
            try
            {
                var receipt = await _approvalService.ApproveAsync(id, dto, GetUserId());
                return Ok(receipt);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // UC-56 REJECT: bắt buộc Reason, không tăng Stock.
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectGoodsReceiptDto dto)
        {
            try
            {
                var receipt = await _approvalService.RejectAsync(id, dto, GetUserId());
                return Ok(receipt);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        private int GetUserId()
        {
            string? value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(value, out int userId) && userId > 0) return userId;
            throw new GoodsReceiptException(401, "GR_UNAUTHENTICATED", "Authentication is required.");
        }

        private IActionResult HandleError(Exception ex)
        {
            if (ex is GoodsReceiptException error)
            {
                return StatusCode(error.StatusCode, new { code = error.Code, message = error.Message });
            }
            if (ex is DbUpdateConcurrencyException)
            {
                return Conflict(new { code = "GR_STALE_VERSION", message = "Receipt changed. Reload before trying again." });
            }
            var sql = ex as SqlException ?? ex.InnerException as SqlException;
            if (sql != null && (sql.Number == 2601 || sql.Number == 2627))
            {
                return Conflict(new { code = "GR_DUPLICATE", message = "A serial or stock key already exists." });
            }
            if (sql != null && (sql.Number == 1205 || sql.Number == 1222 || sql.Number == 51001))
            {
                return Conflict(new { code = "GR_CONCURRENT_WRITE", message = "Concurrent operation. Reload and try again." });
            }
            _logger.LogError(ex, "Goods receipt operation failed");
            return StatusCode(500, new { code = "GR_INTERNAL", message = "The operation failed and was not completed." });
        }
    }
}
