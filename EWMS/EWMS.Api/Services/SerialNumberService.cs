using System.Data;
using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    public class SerialNumberService
    {
        private readonly EWMSDbContext _context;
        private readonly GoodsReceiptService _receiptService;
        private readonly IConfiguration _configuration;

        public SerialNumberService(EWMSDbContext context, GoodsReceiptService receiptService, IConfiguration configuration)
        {
            _context = context;
            _receiptService = receiptService;
            _configuration = configuration;
        }

        // UC-50: scan hợp lệ -> lưu tạm -> đếm lại số lượng, chưa nhập kho chính thức.
        public async Task<ReceiptSerialDto> AddAsync(int id, int itemId, AddReceiptSerialDto dto, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                await _receiptService.ValidateUserAsync(userId, "Warehouse Staff");
                var receipt = await _receiptService.LoadReceiptAsync(id);
                await _receiptService.ValidateEditAsync(receipt, userId, dto.ExpectedVersion);
                _receiptService.ValidateInbound(receipt.InboundRequest);
                // ItemId phải thuộc chính receipt, không cho sửa dòng của phiếu khác.
                var item = receipt.Items.SingleOrDefault(x => x.Id == itemId);
                if (item == null)
                {
                    throw new GoodsReceiptException(400, "GR_ITEM", "Item does not belong to this receipt.");
                }
                _receiptService.ValidateProduct(item);
                if (dto.SKU != item.ProductVariant.SKU)
                {
                    throw new GoodsReceiptException(400, "GR_SKU", "Scanned SKU does not match receipt item.");
                }
                ValidateFormat(dto.SerialCode);
                bool scannedBefore = await _context.GoodsReceiptSerials
                    .AnyAsync(x => x.GoodsReceiptId == id && x.SerialCode == dto.SerialCode);
                bool alreadyInStock = await _context.SerialNumbers.AnyAsync(x => x.SerialCode == dto.SerialCode);
                // MSG158: trùng trong phiếu hoặc đã có serial chính thức thì không thêm lại.
                if (scannedBefore || alreadyInStock)
                {
                    throw new GoodsReceiptException(409, "MSG158", "Serial/IMEI already exists.");
                }
                var scan = new GoodsReceiptSerial
                {
                    GoodsReceiptId = id,
                    GoodsReceiptItemId = itemId,
                    SerialCode = dto.SerialCode,
                    ScannedAt = DateTime.UtcNow,
                    ScannedByUserId = userId
                };
                item.Serials.Add(scan);
                item.ReceivedQty = item.Serials.Count; // Không cộng số lượng từ payload frontend.
                // Kiểm tra SKU/remaining ngay khi quét. Nháp vẫn được thiếu bin.
                await _receiptService.ValidateReceiptAsync(receipt, false);
                _receiptService.MarkChanged(receipt);
                await _context.SaveChangesAsync();
                var response = new ReceiptSerialDto
                {
                    Id = scan.Id,
                    ItemId = item.Id,
                    SerialCode = scan.SerialCode,
                    ScannedAt = scan.ScannedAt,
                    ReceivedQty = item.ReceivedQty,
                    Version = Convert.ToBase64String(receipt.Version)
                };
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

        public void ValidateFormat(string code)
        {
            // Giữ chữ hoa/thường và số 0 đầu; không ép mọi serial thành IMEI 15 số.
            GoodsReceiptSerialRules.Validate(code, _configuration["GoodsReceipts:SerialPattern"]);
        }
    }
}
