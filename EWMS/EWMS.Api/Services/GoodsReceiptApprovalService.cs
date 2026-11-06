using System.Data;
using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    public class GoodsReceiptApprovalService
    {
        private readonly EWMSDbContext _context;
        private readonly GoodsReceiptService _receiptService;
        private readonly IStockService _stockService;
        private readonly IInboundService _inboundService;

        public GoodsReceiptApprovalService(EWMSDbContext context, GoodsReceiptService receiptService,
            IStockService stockService, IInboundService inboundService)
        {
            _context = context;
            _receiptService = receiptService;
            _stockService = stockService;
            _inboundService = inboundService;
        }

        // UC-56 / SDS 3.3.2.6: đọc từ trên xuống đúng thứ tự sequence.
        public async Task<GoodsReceiptDetailDto> ApproveAsync(int id, ReceiptVersionDto dto, int userId)
        {
            // Nhiều bảng cùng thay đổi: thành công tất cả hoặc rollback tất cả.
            // Serializable kết hợp khóa SQL ngăn hai request cùng chiếm remaining/capacity.
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                // 1. Đúng Manager, đúng kho, PendingApproval, Version mới nhất.
                await _receiptService.ValidateUserAsync(userId, "Manager");
                var receipt = await _receiptService.LoadReceiptAsync(id);
                await _receiptService.ValidateWarehouseAsync(userId, receipt.InboundRequest.WarehouseId);
                _receiptService.ValidateStateAndVersion(receipt, GoodsReceiptStatus.PendingApproval, dto.ExpectedVersion);

                // 2. Kiểm tra lại serial, remaining và sức chứa vì có thể đổi sau Submit.
                await _receiptService.ValidateReceiptAsync(receipt, true);

                // 3. Đăng ký serial chính thức và lịch sử nhập từng thiết bị.
                foreach (var item in receipt.Items)
                {
                    foreach (var scan in item.Serials)
                    {
                        var serial = new SerialNumber
                        {
                            ProductVariantId = item.ProductVariantId,
                            OriginGoodsReceiptItemId = item.Id,
                            CurrentBinId = item.BinId,
                            SerialCode = scan.SerialCode,
                            CurrentStatus = "IN_STOCK"
                        };
                        _context.SerialNumbers.Add(serial);
                        _context.SerialMovements.Add(new SerialMovement
                        {
                            SerialNumber = serial,
                            FromBinId = null,
                            ToBinId = item.BinId,
                            PerformedByUserId = userId,
                            MovementType = "RECEIPT",
                            RefType = "GOODS_RECEIPT",
                            RefId = receipt.Id,
                            MovementDate = DateTime.UtcNow
                        });
                    }
                }
                await _context.SaveChangesAsync();

                // 4. Cộng tồn kho qua hợp đồng Người 2.
                await _stockService.ApplyApprovedReceiptAsync(receipt);

                // 5. Lưu Approved trước để bước tính tiến độ thấy phiếu vừa duyệt.
                receipt.Status = GoodsReceiptStatus.Approved;
                // UC-56 không yêu cầu ghi chú duyệt; giữ cột cũ để đọc lịch sử.
                receipt.ApprovalNote = null;
                receipt.RejectionReason = null;
                receipt.ApprovedByUserId = userId;
                receipt.ApprovalDate = DateTime.UtcNow;
                _receiptService.MarkChanged(receipt);
                await _context.SaveChangesAsync();

                // 6. Nhận một phần -> PartiallyReceived; đủ mọi SKU -> Completed.
                await _inboundService.RefreshReceivingProgressAsync(receipt.InboundRequestId);
                var response = await _receiptService.MapToDtoAsync(receipt);

                // SaveChanges chưa kết thúc transaction. Commit mới xác nhận toàn bộ.
                await transaction.CommitAsync();
                return response;
            }
            catch
            {
                // Ví dụ Inbound lỗi sau khi Stock tăng: hủy cả Stock, Serial, Movement, Approved.
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<GoodsReceiptDetailDto> RejectAsync(int id, RejectGoodsReceiptDto dto, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                await _receiptService.ValidateUserAsync(userId, "Manager");
                var receipt = await _receiptService.LoadReceiptAsync(id);
                await _receiptService.ValidateWarehouseAsync(userId, receipt.InboundRequest.WarehouseId);
                _receiptService.ValidateStateAndVersion(receipt, GoodsReceiptStatus.PendingApproval, dto.ExpectedVersion);
                // Từ chối bắt buộc có lý do và không được vượt độ dài cột database.
                string reason = dto.Reason == null ? "" : dto.Reason.Trim();
                if (reason.Length == 0 || reason.Length > 255)
                {
                    throw new GoodsReceiptException(400, "GR_REJECTION_REASON", "A rejection reason of 1-255 characters is required.");
                }
                receipt.Status = GoodsReceiptStatus.Rejected;
                receipt.ApprovalNote = null;
                receipt.RejectionReason = reason;
                receipt.ApprovedByUserId = userId;
                receipt.ApprovalDate = DateTime.UtcNow;
                _receiptService.MarkChanged(receipt);
                await _context.SaveChangesAsync();
                // Reject chỉ lưu quyết định; không gọi Stock hay tạo serial chính thức.
                var response = await _receiptService.MapToDtoAsync(receipt);
                await transaction.CommitAsync();
                return response;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }
    }
}
