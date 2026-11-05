using EWMS.Api.DTOs.GoodsReceipts;

namespace EWMS.Api.Interfaces
{
    public interface IGoodsReceiptService
    {
        Task<GoodsReceiptSessionDto> GetSessionAsync(int userId);
        Task<List<ReceivableInboundDto>> GetReceivableInboundsAsync(int userId);
        Task<List<ReceiptBinLookupDto>> GetBinsAsync(int warehouseId, int userId);
        Task<GoodsReceiptListDto> GetAllAsync(GoodsReceiptFilterDto filter, int userId);
        Task<GoodsReceiptDetailDto> GetByIdAsync(int id, int userId);
        Task<GoodsReceiptDetailDto> CreateAsync(CreateGoodsReceiptDto dto, int userId);
        Task<GoodsReceiptDetailDto> UpdateDraftAsync(int id, UpdateGoodsReceiptDto dto, int userId);
        Task<GoodsReceiptDetailDto> SubmitAsync(int id, ReceiptVersionDto dto, int userId);
    }
}
