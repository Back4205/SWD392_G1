using EWMS.Api.Entities;

namespace EWMS.Api.Interfaces
{
    // Điểm nối Người 2. Implementation phải dùng cùng DbContext, không Commit riêng.
    public interface IStockService
    {
        Task ValidateCapacityAsync(GoodsReceipt receipt, bool requireBins);
        Task ApplyApprovedReceiptAsync(GoodsReceipt receipt);
    }
}
