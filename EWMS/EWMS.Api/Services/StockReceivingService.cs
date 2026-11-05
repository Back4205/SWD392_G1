using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    // Kiểm tra sức chứa và cộng tồn trong cùng transaction duyệt phiếu.
    public class StockReceivingService : IStockService
    {
        private readonly EWMSDbContext _context;

        public StockReceivingService(EWMSDbContext context)
        {
            _context = context;
        }

        public async Task ValidateCapacityAsync(GoodsReceipt receipt, bool requireBins)
        {
            int warehouseId = receipt.InboundRequest.WarehouseId;
            var warehouse = await _context.Warehouses.SingleAsync(x => x.Id == warehouseId);
            // MSG94: nháp được bỏ trống bin, nhưng gửi duyệt/duyệt thì phải chọn.
            if (requireBins && receipt.Items.Any(x => x.ReceivedQty > 0 && !x.BinId.HasValue))
            {
                throw new GoodsReceiptException(400, "MSG94", "Select a bin for every received line.");
            }
            var binIds = receipt.Items.Where(x => x.BinId.HasValue).Select(x => x.BinId!.Value).Distinct().ToList();
            var bins = await _context.StorageBins.Where(x => binIds.Contains(x.Id))
                .Include(x => x.Rack).ThenInclude(x => x.Section).ToListAsync();
            if (bins.Count != binIds.Count || bins.Any(x => x.Rack.Section.WarehouseId != warehouseId))
            {
                throw new GoodsReceiptException(400, "GR_BIN_WAREHOUSE", "Bin must belong to the inbound warehouse.");
            }
            var stocks = await _context.Stocks
                .Where(x => x.Bin.Rack.Section.WarehouseId == warehouseId)
                .Include(x => x.ProductVariant).Include(x => x.Bin).ToListAsync();
            if (stocks.Any(x => x.OnHandQty < 0 || x.ReservedQty < 0 || x.ReservedQty > x.OnHandQty || x.ProductVariant.WeightKg <= 0))
            {
                throw new GoodsReceiptException(409, "GR_INTEGRITY", "Existing stock data is invalid.");
            }
            // Tải trọng = số lượng thực có * cân nặng. Hàng Reserved vẫn chiếm chỗ.
            foreach (var bin in bins)
            {
                decimal currentWeight = stocks.Where(x => x.BinId == bin.Id)
                    .Sum(x => x.OnHandQty * x.ProductVariant.WeightKg);
                decimal incomingWeight = receipt.Items.Where(x => x.BinId == bin.Id)
                    .Sum(x => x.ReceivedQty * x.ProductVariant.WeightKg);
                CheckWeight(currentWeight + incomingWeight, bin.MaxWeightKg, "bin");
            }
            // Cộng gộp cả receipt, tránh từng bin hợp lệ nhưng tổng cùng rack lại quá tải.
            var rackIds = bins.Select(x => x.RackId).Distinct().ToList();
            foreach (int rackId in rackIds)
            {
                var rack = bins.First(x => x.RackId == rackId).Rack;
                var rackBinIds = bins.Where(x => x.RackId == rackId).Select(x => x.Id).ToList();
                decimal currentWeight = stocks.Where(x => x.Bin.RackId == rackId)
                    .Sum(x => x.OnHandQty * x.ProductVariant.WeightKg);
                decimal incomingWeight = receipt.Items.Where(x => x.BinId.HasValue && rackBinIds.Contains(x.BinId.Value))
                    .Sum(x => x.ReceivedQty * x.ProductVariant.WeightKg);
                CheckWeight(currentWeight + incomingWeight, rack.MaxWeightKg, "rack");
            }
            decimal warehouseWeight = stocks.Sum(x => x.OnHandQty * x.ProductVariant.WeightKg);
            decimal receiptWeight = receipt.Items.Where(x => x.BinId.HasValue)
                .Sum(x => x.ReceivedQty * x.ProductVariant.WeightKg);
            CheckWeight(warehouseWeight + receiptWeight, warehouse.MaxWeightKg, "warehouse");
        }

        private void CheckWeight(decimal totalWeight, decimal maxWeight, string level)
        {
            // MSG96: không đủ sức chứa thì giữ nguyên phiếu/tồn kho.
            if (maxWeight <= 0 || totalWeight > maxWeight)
            {
                throw new GoodsReceiptException(409, "MSG96", "Insufficient " + level + " weight capacity.");
            }
        }

        public async Task ApplyApprovedReceiptAsync(GoodsReceipt receipt)
        {
            foreach (var item in receipt.Items.Where(x => x.ReceivedQty > 0))
            {
                var stock = await _context.Stocks.SingleOrDefaultAsync(x =>
                    x.ProductVariantId == item.ProductVariantId && x.BinId == item.BinId);
                if (stock == null)
                {
                    stock = new Stock
                    {
                        ProductVariantId = item.ProductVariantId,
                        BinId = item.BinId!.Value,
                        OnHandQty = 0,
                        ReservedQty = 0
                    };
                    _context.Stocks.Add(stock);
                }
                // Chỉ cộng OnHandQty. Không ghi đè ReservedQty đang có.
                stock.OnHandQty = checked(stock.OnHandQty + item.ReceivedQty);
            }
            await _context.SaveChangesAsync();
            // Không Commit: Approval service còn phải cập nhật Receipt và Inbound.
        }
    }
}
