using EWMS.Api.Entities;
using EWMS.Api.DTOs.InboundRequests;

namespace EWMS.Api.Interfaces
{
    public interface IInboundRequestRepository : IGenericRepository<InboundRequest>
    {
        Task<List<int>> GetAssignedWarehouseIdsAsync(int userId);
        Task<bool> IsAssignedToWarehouseAsync(int userId, int warehouseId);
        Task<InboundLookupDto> GetLookupsAsync(int userId);

        // Task 1: Create
        Task<InboundRequest> CreateAsync(
            InboundRequest request,
            List<InboundRequestItem> items);

        Task<List<InboundRequestItem>> GetItemsByRequestIdAsync(
            int inboundRequestId);
        Task<InboundRequest?> GetForUpdateAsync(int id);
        Task<List<InboundRequestItem>> LoadItemsAsync(int requestId, bool tracking);
        Task<string?> GetUserNameAsync(int userId);
        Task<bool> HasGoodsReceiptsAsync(int requestId);
        Task<bool> IsActiveWarehouseAsync(int warehouseId);
        Task<bool> IsActiveSupplierAsync(int supplierId);
        Task<ProductVariant?> GetProductVariantAsync(int variantId);
        Task<bool> IsActiveAssignedUserAsync(int userId, int warehouseId, string role);
        Task SaveCommitmentAsync(InboundRequest request, int supplierId, string status,
            DateTime? expectedDate, IReadOnlyList<(InboundRequestItem Item, int VariantId, int Quantity)> items);
        Task SaveApprovalAsync(InboundRequest request, int managerUserId);
        Task SaveRejectionAsync(InboundRequest request, string reason, int managerUserId);

        // Task 6: Receiving Progress
        Task<List<GoodsReceipt>> GetGoodsReceiptsByRequestIdAsync(
            int inboundRequestId
        );

        Task<List<GoodsReceiptItem>> GetGoodsReceiptItemsByReceiptIdsAsync(
            List<int> goodsReceiptIds
        );


    }
}
