using EWMS.Api.DTOs.GoodsReceipts;

namespace EWMS.Api.Interfaces
{
    // Điểm nối nhận hàng với Inbound. Chỉ tính receipt Approved.
    public interface IInboundService
    {
        Task<List<ReceivingLineDto>> GetLinesAsync(int inboundId);
        Task RefreshReceivingProgressAsync(int inboundId);
    }
}
