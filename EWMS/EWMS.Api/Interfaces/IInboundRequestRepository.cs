using EWMS.Api.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EWMS.Api.Interfaces
{
    public interface IInboundRequestRepository
    {
        // Task 1: Create
        Task<InboundRequest> CreateAsync(
            InboundRequest request,
            List<InboundRequestItem> items);

        Task<bool> WarehouseExistsAsync(int warehouseId);

        Task<bool> ProductVariantExistsAsync(int productVariantId);

        Task<bool> UserExistsAsync(int userId);

        // Task 2: List
        Task<List<InboundRequest>> GetAllAsync();

        // Task 3: Detail
        Task<InboundRequest?> GetByIdAsync(int id);

        Task<List<InboundRequestItem>> GetItemsByRequestIdAsync(
            int inboundRequestId);
        Task UpdateAsync(InboundRequest request);

        Task<bool> SupplierExistsAsync(int supplierId);

        // Task 6: Receiving Progress
        Task<List<GoodsReceipt>> GetGoodsReceiptsByRequestIdAsync(
            int inboundRequestId
        );

        Task<List<GoodsReceiptItem>> GetGoodsReceiptItemsByReceiptIdsAsync(
            List<int> goodsReceiptIds
        );


    }
}
