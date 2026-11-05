
using System.Data;
using EWMS.Api.Common;
using EWMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;

namespace EWMS.Api.Services
{
    public class InboundRequestService : IInboundRequestService
    {
        private readonly IInboundRequestRepository _repository;
        private readonly EWMSDbContext _context;
        private readonly IConfiguration _configuration;

        public InboundRequestService(IInboundRequestRepository repository,
            EWMSDbContext context, IConfiguration configuration)
        {
            _repository = repository;
            _context = context;
            _configuration = configuration;
        }

        // Task 1: Create Inbound Request
        public async Task<int> CreateAsync(
            CreateInboundRequestDto dto,
            int requestedByUserId)
        {
            // 1. Validate Warehouse
            if (dto.WarehouseId <= 0 ||
                !await _repository.WarehouseExistsAsync(dto.WarehouseId))
            {
                throw new ArgumentException(
                    "Warehouse does not exist.");
            }

            // 2. Validate User
            if (requestedByUserId <= 0 ||
                !await _repository.UserExistsAsync(requestedByUserId))
            {
                throw new ArgumentException(
                    "Requested user does not exist.");
            }

            // 3. Validate Items
            if (dto.Items == null || dto.Items.Count == 0)
            {
                throw new ArgumentException(
                    "At least one product is required.");
            }

            // 4. Check duplicate ProductVariant
            if (dto.Items.Select(x => x.ProductVariantId)
                .Distinct().Count() != dto.Items.Count)
            {
                throw new ArgumentException(
                    "Duplicate ProductVariant is not allowed.");
            }

            // 5. Validate ProductVariant and quantity
            foreach (var item in dto.Items)
            {
                if (item.ExpectedQty <= 0)
                {
                    throw new ArgumentException(
                        "Expected quantity must be greater than zero.");
                }

                if (item.ProductVariantId <= 0 ||
                    !await _repository.ProductVariantExistsAsync(
                        item.ProductVariantId))
                {
                    throw new ArgumentException(
                        $"ProductVariant {item.ProductVariantId} does not exist.");
                }
            }

            // 6. Create InboundRequest
            var request = new InboundRequest
            {
                WarehouseId = dto.WarehouseId,
                RequestedByUserId = requestedByUserId,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow,
                SupplierId = null,
                ApprovedByUserId = null,
                SupplierCommitmentStatus = null,
                ExpectedDeliveryDate = null,
                RejectionReason = null
            };

            // 7. Create InboundRequestItems
            var items = dto.Items.Select(item =>
                new InboundRequestItem
                {
                    ProductVariantId = item.ProductVariantId,
                    ExpectedQty = item.ExpectedQty
                }).ToList();

            // 8. Save request and items
            var createdRequest =
                await _repository.CreateAsync(request, items);

            return createdRequest.Id;
        }

        // Task 2: Get Inbound Request List

        // Task 2: Get Inbound Request List
        public async Task<List<InboundRequestListDto>> GetAllAsync()
        {
            var requests = await _repository.GetAllAsync();

            return requests.Select(request => new InboundRequestListDto
            {
                Id = request.Id,
                WarehouseId = request.WarehouseId,
                RequestedByUserId = request.RequestedByUserId,
                SupplierId = request.SupplierId,
                Status = request.Status,
                CreatedDate = request.CreatedDate,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate
            }).ToList();
        }

        // Map thủ công để nhìn rõ kế hoạch SKU và serial nhà cung cấp đã khai báo.
        public async Task<InboundRequestDetailDto?> GetByIdAsync(int id)
        {
            var request = await _context.InboundRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (request == null) return null;
            var items = await _context.InboundRequestItems.AsNoTracking()
                .Where(x => x.InboundRequestId == id).Include(x => x.ProductVariant).ThenInclude(x => x.Product)
                .OrderBy(x => x.Id).ToListAsync();
            var expectedSerials = await _context.InboundExpectedSerials.AsNoTracking()
                .Where(x => x.InboundRequestId == id).OrderBy(x => x.Id).ToListAsync();
            var result = new InboundRequestDetailDto
            {
                Id = request.Id, WarehouseId = request.WarehouseId, SupplierId = request.SupplierId,
                RequestedByUserId = request.RequestedByUserId, ApprovedByUserId = request.ApprovedByUserId,
                SupplierCommitmentStatus = request.SupplierCommitmentStatus,
                HasSupplierManifest = request.HasSupplierManifest,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate, RejectionReason = request.RejectionReason,
                Status = request.Status, CreatedDate = request.CreatedDate
            };
            foreach (var item in items)
            {
                result.Items.Add(new InboundRequestDetailItemDto
                {
                    Id = item.Id, ProductVariantId = item.ProductVariantId, ExpectedQty = item.ExpectedQty,
                    SKU = item.ProductVariant.SKU, ProductName = item.ProductVariant.Product?.ProductName ?? "",
                    ExpectedSerials = expectedSerials.Where(x => x.InboundRequestItemId == item.Id)
                        .Select(x => x.SerialCode).ToList()
                });
            }
            return result;
        }

        public async Task UpdateSupplierCommitmentAsync(int requestId, UpdateSupplierCommitmentDto dto)
        {
            // Commitment và bảng kê phải cùng thành công hoặc cùng rollback.
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                var request = await _context.InboundRequests.SingleOrDefaultAsync(x => x.Id == requestId);
                if (request == null) throw new KeyNotFoundException("Inbound request was not found.");
                if (request.Status != "Pending")
                    throw new InvalidOperationException("Only pending inbound requests can be updated.");
                if (!dto.SupplierId.HasValue || !await _context.Suppliers.AnyAsync(x => x.Id == dto.SupplierId.Value && x.Status == "Active"))
                    throw new GoodsReceiptException(400, "IN_SUPPLIER_INACTIVE", "Select an active supplier.");
                string status = dto.CommitmentStatus?.Trim() ?? "";
                if (status != "Confirmed" && status != "Rejected")
                    throw new ArgumentException("Commitment status must be Confirmed or Rejected.");

                var newSerials = new List<InboundExpectedSerial>();
                if (status == "Confirmed")
                {
                    if (!dto.ExpectedDeliveryDate.HasValue)
                        throw new ArgumentException("Expected delivery date is required when confirmed.");
                    if (dto.ExpectedDeliveryDate.Value.Date < DateTime.Today)
                        throw new ArgumentException("Expected delivery date cannot be in the past.");
                    var items = await LoadManifestItemsAsync(requestId);
                    // Phải khai báo đúng tất cả các dòng Manager đã yêu cầu; không tự đổi SKU/số lượng.
                    if (dto.Items == null || dto.Items.Any(x => x == null) || dto.Items.Count != items.Count || items.Count == 0
                        || dto.Items.Select(x => x.ItemId).Distinct().Count() != items.Count)
                        throw new GoodsReceiptException(400, "IN_MANIFEST_ITEMS", "Declare every requested item exactly once.");
                    foreach (var item in items)
                    {
                        var declared = dto.Items.SingleOrDefault(x => x.ItemId == item.Id);
                        if (declared == null)
                            throw new GoodsReceiptException(400, "IN_MANIFEST_ITEMS", "Declared item does not belong to this request.");
                        if (declared.SKU != item.ProductVariant.SKU)
                            throw new GoodsReceiptException(400, "IN_MANIFEST_SKU", "Declared SKU must match the requested item.");
                        if (declared.SerialCodes == null || declared.SerialCodes.Count != item.ExpectedQty)
                            throw new GoodsReceiptException(400, "IN_MANIFEST_COUNT", "Serial count must equal expected quantity for every SKU.");
                        foreach (string serial in declared.SerialCodes)
                        {
                            newSerials.Add(new InboundExpectedSerial
                            {
                                InboundRequestId = requestId, InboundRequestItemId = item.Id, SerialCode = serial
                            });
                        }
                    }
                    await ValidateManifestAsync(requestId, items, newSerials);
                }

                var oldSerials = await _context.InboundExpectedSerials.Where(x => x.InboundRequestId == requestId).ToListAsync();
                _context.InboundExpectedSerials.RemoveRange(oldSerials);
                // Flush DELETE trước INSERT để sửa bảng kê không vướng unique index của chính bảng kê cũ.
                await _context.SaveChangesAsync();
                _context.InboundExpectedSerials.AddRange(newSerials);
                request.SupplierId = dto.SupplierId;
                request.SupplierCommitmentStatus = status;
                request.HasSupplierManifest = status == "Confirmed";
                request.ExpectedDeliveryDate = status == "Confirmed" ? dto.ExpectedDeliveryDate : null;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task ReviewInboundRequestAsync(int requestId, ReviewInboundRequestDto dto, int managerUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Dùng cùng khóa với Purchasing và Goods Receipt để không duyệt dữ liệu đang bị sửa.
                await _context.LockGoodsReceiptAsync();
                var request = await _context.InboundRequests.SingleOrDefaultAsync(x => x.Id == requestId);
                if (request == null) throw new KeyNotFoundException("Inbound request was not found.");
                if (request.Status != "Pending")
                    throw new InvalidOperationException("Only pending inbound requests can be reviewed.");
                if (managerUserId <= 0 || !await _repository.UserExistsAsync(managerUserId))
                    throw new ArgumentException("Manager user does not exist.");
                string decision = dto.Decision?.Trim() ?? "";
                if (decision != "Approved" && decision != "Rejected")
                    throw new ArgumentException("Decision must be Approved or Rejected.");
                if (decision == "Rejected" && string.IsNullOrWhiteSpace(dto.RejectionReason))
                    throw new ArgumentException("Rejection reason is required.");
                if (decision == "Approved")
                {
                    if (request.SupplierCommitmentStatus != "Confirmed" || !request.HasSupplierManifest)
                        throw new GoodsReceiptException(409, "IN_MANIFEST_REQUIRED", "Supplier confirmation and a complete serial manifest are required before approval.");
                    var items = await LoadManifestItemsAsync(requestId);
                    var serials = await _context.InboundExpectedSerials.Where(x => x.InboundRequestId == requestId).ToListAsync();
                    // Kiểm tra lại vì có thể dữ liệu/serial chính thức đã đổi từ lúc Purchasing xác nhận.
                    await ValidateManifestAsync(requestId, items, serials);
                }
                request.Status = decision;
                request.ApprovedByUserId = managerUserId;
                request.RejectionReason = decision == "Rejected" ? dto.RejectionReason!.Trim() : null;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        private Task<List<InboundRequestItem>> LoadManifestItemsAsync(int requestId)
        {
            return _context.InboundRequestItems.Where(x => x.InboundRequestId == requestId)
                .Include(x => x.ProductVariant).ThenInclude(x => x.Product).OrderBy(x => x.Id).ToListAsync();
        }

        private async Task ValidateManifestAsync(int requestId, List<InboundRequestItem> items,
            List<InboundExpectedSerial> serials)
        {
            if (items.Count == 0 || serials.Any(x => !items.Any(item => item.Id == x.InboundRequestItemId)))
                throw new GoodsReceiptException(400, "IN_MANIFEST_ITEMS", "Manifest must contain the requested items only.");
            foreach (var item in items)
            {
                if (item.ExpectedQty <= 0 || serials.Count(x => x.InboundRequestItemId == item.Id) != item.ExpectedQty)
                    throw new GoodsReceiptException(400, "IN_MANIFEST_COUNT", "Serial count must equal expected quantity for every SKU.");
                if (item.ProductVariant.Product == null || item.ProductVariant.Product.Status != "Active")
                    throw new GoodsReceiptException(400, "IN_MANIFEST_SKU", "Requested product must remain active.");
            }
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var serial in serials)
            {
                // Giữ chữ hoa/thường và số 0 đầu; không đổi serial thành số.
                GoodsReceiptSerialRules.Validate(serial.SerialCode, _configuration["GoodsReceipts:SerialPattern"]);
                if (!codes.Add(serial.SerialCode))
                    throw new GoodsReceiptException(400, "IN_MANIFEST_DUPLICATE", "A serial cannot appear more than once in the manifest.");
            }
            var serialCodes = codes.ToList();
            bool registered = await _context.SerialNumbers.AnyAsync(x => serialCodes.Contains(x.SerialCode));
            bool promisedElsewhere = await _context.InboundExpectedSerials.AnyAsync(x => x.InboundRequestId != requestId
                && serialCodes.Contains(x.SerialCode) && x.InboundRequestItem.InboundRequest.HasSupplierManifest
                && (x.InboundRequestItem.InboundRequest.Status == "Pending"
                    || x.InboundRequestItem.InboundRequest.Status == "Approved"
                    || x.InboundRequestItem.InboundRequest.Status == "PartiallyReceived"
                    || x.InboundRequestItem.InboundRequest.Status == "Completed"));
            if (registered || promisedElsewhere)
                throw new GoodsReceiptException(409, "IN_MANIFEST_SERIAL_IN_USE", "A declared serial is already registered or promised in another active inbound request.");
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
