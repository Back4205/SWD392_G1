using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    public class InboundRequestService : IInboundRequestService
    {
        private readonly IInboundRequestRepository _repository;
        private readonly EWMSDbContext _context;

        public InboundRequestService(IInboundRequestRepository repository,
            EWMSDbContext context, IConfiguration configuration)
        {
            _repository = repository;
            _context = context;
        }

        public Task<List<int>> GetAssignedWarehouseIdsAsync(int userId) =>
            _repository.GetAssignedWarehouseIdsAsync(userId);

        public Task<bool> CanUseWarehouseAsync(int userId, int warehouseId) =>
            _repository.IsAssignedToWarehouseAsync(userId, warehouseId);

        public Task<InboundLookupDto> GetLookupsAsync(int userId) =>
            _repository.GetLookupsAsync(userId);

        // UC11 / SD03: Manager chọn kho, nhân viên kho, SKU và số lượng rồi lưu Pending.
        public async Task<int> CreateAsync(CreateInboundRequestDto dto, int requestedByUserId)
        {
            await ValidateManagerAsync(requestedByUserId, dto.WarehouseId);
            await ValidateWarehouseAsync(dto.WarehouseId);
            if (!dto.AssignedStaffUserId.HasValue)
                throw new ArgumentException("Select the assigned warehouse staff.");
            await ValidateStaffAsync(dto.AssignedStaffUserId.Value, dto.WarehouseId);
            if (dto.Items == null || dto.Items.Count == 0)
                throw new ArgumentException("At least one product is required.");
            var variantIds = new HashSet<int>();
            var items = new List<InboundRequestItem>();
            foreach (var item in dto.Items)
            {
                if (item == null || !variantIds.Add(item.ProductVariantId))
                    throw new ArgumentException("Every product variant must appear exactly once.");
                await ValidateItemAsync(item.ProductVariantId, item.ExpectedQty);
                var requestItem = new InboundRequestItem();
                requestItem.UpdateConfirmedDetails(item.ProductVariantId, item.ExpectedQty);
                items.Add(requestItem);
            }
            var request = new InboundRequest
            {
                WarehouseId = dto.WarehouseId,
                RequestedByUserId = requestedByUserId,
                AssignedStaffUserId = dto.AssignedStaffUserId,
                CreatedDate = DateTime.UtcNow
            };
            request.UpdateStatus("Pending");
            // Repository hiện có lưu header và các dòng trong cùng transaction.
            var created = await _repository.CreateAsync(request, items);
            return created.Id;
        }

        public async Task<List<InboundRequestListDto>> GetAllAsync()
        {
            var requests = await _repository.GetAllAsync();
            var result = new List<InboundRequestListDto>();
            foreach (var request in requests)
            {
                result.Add(new InboundRequestListDto
                {
                    Id = request.Id, WarehouseId = request.WarehouseId,
                    RequestedByUserId = request.RequestedByUserId, SupplierId = request.SupplierId,
                    Status = request.Status, CreatedDate = request.CreatedDate,
                    ExpectedDeliveryDate = request.ExpectedDeliveryDate
                });
            }
            return result;
        }

        // Map thủ công để thấy rõ dữ liệu dùng cho màn hình và kiểm tra phiên bản nội dung.
        public async Task<InboundRequestDetailDto?> GetByIdAsync(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            if (request == null) return null;
            var items = await LoadItemsAsync(id, false);
            var result = new InboundRequestDetailDto
            {
                Id = request.Id, WarehouseId = request.WarehouseId, SupplierId = request.SupplierId,
                RequestedByUserId = request.RequestedByUserId,
                AssignedStaffUserId = request.AssignedStaffUserId,
                AssignedStaffName = request.AssignedStaffUserId.HasValue
                    ? await _repository.GetUserNameAsync(request.AssignedStaffUserId.Value)
                    : null,
                ApprovedByUserId = request.ApprovedByUserId,
                DecidedByName = request.ApprovedByUserId.HasValue
                    ? await _repository.GetUserNameAsync(request.ApprovedByUserId.Value)
                    : null,
                DecisionDate = request.DecisionDate,
                SupplierCommitmentStatus = request.SupplierCommitmentStatus,
                HasGoodsReceipts = await _repository.HasGoodsReceiptsAsync(id),
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                RejectionReason = request.RejectionReason,
                ApprovalNote = request.ApprovalNote,
                Status = request.Status, CreatedDate = request.CreatedDate,
                Revision = CalculateRevision(request, items)
            };
            foreach (var item in items)
            {
                result.Items.Add(new InboundRequestDetailItemDto
                {
                    Id = item.Id, ProductVariantId = item.ProductVariantId,
                    ExpectedQty = item.ExpectedQty, SKU = item.ProductVariant.SKU,
                    ProductName = item.ProductVariant.Product?.ProductName ?? ""
                });
            }
            return result;
        }

        // SD04: NCC chấp nhận -> cập nhật mặt hàng/ngày giao; từ chối -> Canceled.
        public async Task UpdateSupplierCommitmentAsync(int requestId, UpdateSupplierCommitmentDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Cùng khóa với Receipt: không cho hai thao tác thay đổi trạng thái xen kẽ nhau.
                await _context.LockGoodsReceiptAsync();
                var request = await _repository.GetForUpdateAsync(requestId);
                if (request == null) throw new KeyNotFoundException("Inbound request was not found.");
                var items = await LoadItemsAsync(requestId, true);
                ValidateRevision(request, items, dto.ExpectedRevision);
                if (request.Status != "Pending")
                    throw new InvalidOperationException("Only pending inbound requests can be updated.");
                if (!dto.SupplierId.HasValue || !await _repository.IsActiveSupplierAsync(dto.SupplierId.Value))
                    throw new GoodsReceiptException(400, "IN_SUPPLIER_INACTIVE", "Select an active supplier.");
                string status = dto.CommitmentStatus?.Trim() ?? "";
                // Confirmed chỉ là alias tương thích client/dữ liệu cũ; giá trị mới luôn là Accepted.
                if (status == "Confirmed") status = "Accepted";
                if (status != "Accepted" && status != "Rejected")
                    throw new ArgumentException("Commitment status must be Accepted or Rejected.");
                if (status == "Accepted")
                {
                    await ValidateWarehouseAsync(request.WarehouseId);
                    if (!dto.ExpectedDeliveryDate.HasValue || dto.ExpectedDeliveryDate.Value.Date < DateTime.Today)
                        throw new ArgumentException("An expected delivery date today or later is required.");
                    // Mỗi dòng hiện có được cập nhật đúng một lần như updateConfirmedDetails trong SD04.
                    if (items.Count == 0 || dto.Items == null || dto.Items.Count != items.Count)
                        throw new ArgumentException("Confirm every requested item exactly once.");
                    var itemIds = new HashSet<int>();
                    var variantIds = new HashSet<int>();
                    foreach (var confirmed in dto.Items)
                    {
                        if (confirmed == null || !itemIds.Add(confirmed.ItemId)
                            || !items.Any(x => x.Id == confirmed.ItemId))
                            throw new ArgumentException("Confirmed item does not belong to this request or is duplicated.");
                        if (!variantIds.Add(confirmed.ProductVariantId))
                            throw new ArgumentException("Every product variant must appear exactly once.");
                        await ValidateItemAsync(confirmed.ProductVariantId, confirmed.ExpectedQty);
                    }
                }
                var confirmedItems = status == "Accepted"
                    ? dto.Items!.Select(x => (items.Single(item => item.Id == x.ItemId), x.ProductVariantId, x.ExpectedQty)).ToList()
                    : new List<(InboundRequestItem, int, int)>();
                // Không tạo/xóa bảng serial dự kiến: tài liệu chỉ yêu cầu serial thực nhận ở UC50.
                await _repository.SaveCommitmentAsync(request, dto.SupplierId.Value, status,
                    dto.ExpectedDeliveryDate, confirmedItems);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        // SD05: Manager kiểm tra cam kết NCC và xác nhận duyệt; nhánh reject bắt buộc lý do.
        public async Task ReviewInboundRequestAsync(int requestId, ReviewInboundRequestDto dto, int managerUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                var request = await _repository.GetForUpdateAsync(requestId);
                if (request == null) throw new KeyNotFoundException("Inbound request was not found.");
                var items = await LoadItemsAsync(requestId, true);
                ValidateRevision(request, items, dto.ExpectedRevision);
                if (request.Status != "Pending")
                    throw new InvalidOperationException("Only pending inbound requests can be reviewed.");
                await ValidateManagerAsync(managerUserId, request.WarehouseId);
                string decision = dto.Decision?.Trim() ?? "";
                string reason = dto.RejectionReason?.Trim() ?? "";
                if (decision != "Approved" && decision != "Rejected")
                    throw new ArgumentException("Decision must be Approved or Rejected.");
                if (decision == "Rejected" && (reason.Length == 0 || reason.Length > 255))
                    throw new GoodsReceiptException(400, "IN_REJECTION_REASON", "A rejection reason of 1-255 characters is required.");
                if (decision == "Approved")
                {
                    if (request.SupplierCommitmentStatus != "Accepted" && request.SupplierCommitmentStatus != "Confirmed")
                        throw new GoodsReceiptException(409, "IN_SUPPLIER_NOT_ACCEPTED", "Supplier acceptance is required before approval.");
                    if (!request.SupplierId.HasValue || !await _repository.IsActiveSupplierAsync(request.SupplierId.Value))
                        throw new GoodsReceiptException(400, "IN_SUPPLIER_INACTIVE", "The supplier must remain active.");
                    await ValidateWarehouseAsync(request.WarehouseId);
                    if (!request.ExpectedDeliveryDate.HasValue || items.Count == 0)
                        throw new ArgumentException("Expected delivery date and requested items are required.");
                    var variantIds = new HashSet<int>();
                    foreach (var item in items)
                    {
                        if (!variantIds.Add(item.ProductVariantId))
                            throw new ArgumentException("Duplicate product variants are not allowed.");
                        await ValidateItemAsync(item.ProductVariantId, item.ExpectedQty);
                    }
                    if (request.AssignedStaffUserId.HasValue)
                        await ValidateStaffAsync(request.AssignedStaffUserId.Value, request.WarehouseId);
                }
                if (decision == "Approved")
                    await _repository.SaveApprovalAsync(request, managerUserId);
                else
                    await _repository.SaveRejectionAsync(request, reason, managerUserId);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        private async Task ValidateWarehouseAsync(int warehouseId)
        {
            if (!await _repository.IsActiveWarehouseAsync(warehouseId))
                throw new ArgumentException("Select an active warehouse.");
        }

        private async Task ValidateItemAsync(int variantId, int expectedQty)
        {
            if (expectedQty <= 0) throw new ArgumentException("Expected quantity must be greater than zero.");
            var variant = await _repository.GetProductVariantAsync(variantId);
            // Kiểm tra bằng C# để cả tab/xuống dòng cũng không được coi là SKU hợp lệ.
            if (variant == null || string.IsNullOrWhiteSpace(variant.SKU) || variant.WeightKg <= 0
                || variant.Product == null || variant.Product.Status != "Active")
                throw new ArgumentException("Select an active product variant with a valid SKU and positive weight.");
        }

        private async Task ValidateStaffAsync(int staffId, int warehouseId)
        {
            if (!await _repository.IsActiveAssignedUserAsync(staffId, warehouseId, "Warehouse Staff"))
                throw new ArgumentException("Assigned staff must be active warehouse staff assigned to this warehouse.");
        }

        private async Task ValidateManagerAsync(int managerId, int warehouseId)
        {
            if (!await _repository.IsActiveAssignedUserAsync(managerId, warehouseId, "Manager"))
                throw new ArgumentException("Manager must be active and assigned to this warehouse.");
        }

        private Task<List<InboundRequestItem>> LoadItemsAsync(int requestId, bool tracking)
        {
            return _repository.LoadItemsAsync(requestId, tracking);
        }

        // Revision chỉ là dấu vân tay nội dung để phát hiện màn hình cũ, không phải trạng thái nghiệp vụ.
        private static string CalculateRevision(InboundRequest request, List<InboundRequestItem> items)
        {
            byte[] content = JsonSerializer.SerializeToUtf8Bytes(new
            {
                request.Id, request.WarehouseId, request.AssignedStaffUserId, request.Status,
                request.SupplierId, request.SupplierCommitmentStatus, request.ExpectedDeliveryDate,
                request.ApprovedByUserId, request.DecisionDate,
                Items = items.OrderBy(x => x.Id).Select(x => new { x.Id, x.ProductVariantId, x.ExpectedQty })
            });
            return Convert.ToHexString(SHA256.HashData(content));
        }

        private static void ValidateRevision(InboundRequest request, List<InboundRequestItem> items, string? expectedRevision)
        {
            // Client cũ được phép không gửi; web hiện tại luôn gửi revision đã hiển thị.
            if (expectedRevision != null && expectedRevision != CalculateRevision(request, items))
                throw new GoodsReceiptException(409, "IN_STALE_REVISION", "Inbound request changed. Reload the latest details before continuing.");
        }

        // Task 6: chỉ tính phiếu Goods Receipt đã được Manager duyệt.
        public async Task<InboundReceivingProgressDto?> GetReceivingProgressAsync(int inboundRequestId)
        {
            if (inboundRequestId <= 0)
            {
                throw new ArgumentException("Invalid inbound request ID.");
            }
            var request = await _repository.GetByIdAsync(inboundRequestId);
            if (request == null)
            {
                return null;
            }
            var requestItems = await _repository.GetItemsByRequestIdAsync(inboundRequestId);
            var receipts = await _repository.GetGoodsReceiptsByRequestIdAsync(inboundRequestId);
            var approvedReceiptIds = receipts.Where(x => x.Status == "Approved")
                .Select(x => x.Id).ToList();
            var receivedItems = await _repository.GetGoodsReceiptItemsByReceiptIdsAsync(approvedReceiptIds);
            var variantIds = requestItems.Select(x => x.ProductVariantId).Distinct().ToList();
            if (requestItems.Count == 0 || requestItems.Any(x => x.ExpectedQty <= 0)
                || receivedItems.Any(x => !variantIds.Contains(x.ProductVariantId)))
            {
                throw new InvalidOperationException("Inbound receiving quantities are inconsistent.");
            }
            var result = new InboundReceivingProgressDto
            {
                InboundRequestId = inboundRequestId,
                TotalExpectedQty = 0,
                TotalReceivedQty = 0,
                RemainingQty = 0
            };
            foreach (int variantId in variantIds)
            {
                int expected = requestItems.Where(x => x.ProductVariantId == variantId).Sum(x => x.ExpectedQty);
                int received = receivedItems.Where(x => x.ProductVariantId == variantId).Sum(x => x.ReceivedQty);
                if (received < 0 || received > expected)
                {
                    throw new InvalidOperationException("Received quantity exceeds expected quantity for a variant.");
                }
                result.Items.Add(new InboundReceivingProgressItemDto
                {
                    ProductVariantId = variantId,
                    ExpectedQty = expected,
                    ReceivedQty = received,
                    RemainingQty = expected - received
                });
                result.TotalExpectedQty += expected;
                result.TotalReceivedQty += received;
                result.RemainingQty += expected - received;
            }
            result.ProgressPercentage = Math.Round(100.0 * result.TotalReceivedQty.Value / result.TotalExpectedQty, 2);
            if (result.Items.All(x => x.RemainingQty == 0))
            {
                result.ProgressStatus = "Completed";
            }
            else if (result.TotalReceivedQty > 0)
            {
                result.ProgressStatus = "PartiallyReceived";
            }
            else
            {
                result.ProgressStatus = "Pending";
            }
            return result;
        }
    }
}
