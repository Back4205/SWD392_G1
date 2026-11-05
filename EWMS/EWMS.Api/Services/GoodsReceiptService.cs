using System.Data;
using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    public class GoodsReceiptService : IGoodsReceiptService
    {
        private readonly EWMSDbContext _context;
        private readonly IStockService _stockService;
        private readonly IInboundService _inboundService;
        private readonly IConfiguration _configuration;

        public GoodsReceiptService(EWMSDbContext context, IStockService stockService,
            IInboundService inboundService, IConfiguration configuration)
        {
            _context = context;
            _stockService = stockService;
            _inboundService = inboundService;
            _configuration = configuration;
        }

        // UI lấy role/quyền kho thật từ database, không dựa vào lựa chọn role ở trình duyệt.
        public async Task<GoodsReceiptSessionDto> GetSessionAsync(int userId)
        {
            await ValidateUserAsync(userId, null);
            var user = await _context.Users.AsNoTracking().Include(x => x.Role)
                .SingleAsync(x => x.Id == userId);
            var warehouseIds = await _context.UserWarehouses.Where(x => x.UserId == userId)
                .Select(x => x.WarehouseId).OrderBy(x => x).ToListAsync();
            return new GoodsReceiptSessionDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role.RoleName,
                WarehouseIds = warehouseIds
            };
        }

        // Query phục vụ màn hình tạo phiếu. Chỉ Staff và Inbound trong kho được phân công.
        public async Task<List<ReceivableInboundDto>> GetReceivableInboundsAsync(int userId)
        {
            await ValidateUserAsync(userId, "Warehouse Staff");
            var warehouseIds = await _context.UserWarehouses.Where(x => x.UserId == userId)
                .Select(x => x.WarehouseId).ToListAsync();
            var inbounds = await _context.InboundRequests.AsNoTracking().Include(x => x.Warehouse)
                .Where(x => warehouseIds.Contains(x.WarehouseId)
                    && (x.Status == "Approved" || x.Status == "PartiallyReceived"))
                .OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id).ToListAsync();
            var result = new List<ReceivableInboundDto>();
            foreach (var inbound in inbounds)
            {
                if (!string.Equals(inbound.Warehouse.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var lines = await _inboundService.GetLinesAsync(inbound.Id);
                // Approved nhưng thực tế đã nhận đủ cũng không được đưa vào danh sách chọn.
                if (!lines.Any(x => x.RemainingQty > 0))
                {
                    continue;
                }
                result.Add(new ReceivableInboundDto
                {
                    Id = inbound.Id,
                    WarehouseId = inbound.WarehouseId,
                    WarehouseName = inbound.Warehouse.WarehouseName,
                    Status = inbound.Status,
                    Lines = lines
                });
            }
            return result;
        }

        // Bin lookup chỉ đọc vị trí trong đúng kho. Hàng Reserved vẫn chiếm tải trọng.
        public async Task<List<ReceiptBinLookupDto>> GetBinsAsync(int warehouseId, int userId)
        {
            await ValidateUserAsync(userId, null);
            await ValidateWarehouseAsync(userId, warehouseId);
            var warehouse = await _context.Warehouses.AsNoTracking().SingleAsync(x => x.Id == warehouseId);
            if (!string.Equals(warehouse.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new GoodsReceiptException(409, "GR_WAREHOUSE_INACTIVE", "Warehouse is inactive.");
            }
            var bins = await _context.StorageBins.AsNoTracking().Include(x => x.Rack)
                .Where(x => x.Rack.Section.WarehouseId == warehouseId)
                .OrderBy(x => x.BinCode).ThenBy(x => x.Id).ToListAsync();
            var stockWeights = await _context.Stocks.AsNoTracking()
                .Where(x => x.Bin.Rack.Section.WarehouseId == warehouseId)
                .Select(x => new { x.BinId, WeightKg = x.OnHandQty * x.ProductVariant.WeightKg })
                .ToListAsync();
            var result = new List<ReceiptBinLookupDto>();
            foreach (var bin in bins)
            {
                decimal currentWeight = stockWeights.Where(x => x.BinId == bin.Id).Sum(x => x.WeightKg);
                result.Add(new ReceiptBinLookupDto
                {
                    Id = bin.Id,
                    BinCode = bin.BinCode,
                    BinName = bin.BinName,
                    RackCode = bin.Rack.RackCode,
                    MaxWeightKg = bin.MaxWeightKg,
                    AvailableWeightKg = bin.MaxWeightKg - currentWeight
                });
            }
            return result;
        }

        // UC-15: lọc quyền kho trước khi phân trang, không trả toàn bộ rồi ẩn trên UI.
        public async Task<GoodsReceiptListDto> GetAllAsync(GoodsReceiptFilterDto filter, int userId)
        {
            await ValidateUserAsync(userId, null);
            if (filter.Page < 1 || filter.PageSize < 1 || filter.PageSize > 100 || filter.FromDate > filter.ToDate)
            {
                throw new GoodsReceiptException(400, "GR_FILTER", "Invalid paging or date range.");
            }
            if (filter.Status != null && filter.Status != GoodsReceiptStatus.Draft
                && filter.Status != GoodsReceiptStatus.PendingApproval && filter.Status != GoodsReceiptStatus.Approved
                && filter.Status != GoodsReceiptStatus.Rejected)
            {
                throw new GoodsReceiptException(400, "GR_FILTER", "Unknown receipt status.");
            }
            var warehouseIds = await _context.UserWarehouses.Where(x => x.UserId == userId)
                .Select(x => x.WarehouseId).ToListAsync();
            var query = _context.GoodsReceipts.AsNoTracking()
                .Where(x => warehouseIds.Contains(x.InboundRequest.WarehouseId));
            if (filter.WarehouseId.HasValue)
            {
                await ValidateWarehouseAsync(userId, filter.WarehouseId.Value);
                query = query.Where(x => x.InboundRequest.WarehouseId == filter.WarehouseId.Value);
            }
            if (filter.Status != null)
            {
                query = query.Where(x => x.Status == filter.Status);
            }
            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.ReceivedDate >= filter.FromDate.Value);
            }
            if (filter.ToDate.HasValue)
            {
                query = query.Where(x => x.ReceivedDate <= filter.ToDate.Value);
            }
            var response = new GoodsReceiptListDto
            {
                TotalCount = await query.CountAsync(),
                Page = filter.Page,
                PageSize = filter.PageSize
            };
            long skip = (long)(filter.Page - 1) * filter.PageSize;
            if (skip > int.MaxValue)
            {
                return response;
            }
            var receipts = await query.Include(x => x.InboundRequest)
                .OrderByDescending(x => x.ReceivedDate).ThenByDescending(x => x.Id)
                .Skip((int)skip).Take(filter.PageSize).ToListAsync();
            foreach (var receipt in receipts)
            {
                response.Items.Add(new GoodsReceiptSummaryDto
                {
                    Id = receipt.Id,
                    InboundRequestId = receipt.InboundRequestId,
                    WarehouseId = receipt.InboundRequest.WarehouseId,
                    ReceivedByUserId = receipt.ReceivedByUserId,
                    ReceivedDate = receipt.ReceivedDate,
                    Status = receipt.Status,
                    Version = Convert.ToBase64String(receipt.Version)
                });
            }
            return response; // Không có dữ liệu vẫn là danh sách rỗng hợp lệ.
        }

        // UC-16: xem chi tiết, sai ID trả 404, sai kho trả 403.
        public async Task<GoodsReceiptDetailDto> GetByIdAsync(int id, int userId)
        {
            await ValidateUserAsync(userId, null);
            var receipt = await LoadReceiptAsync(id);
            await ValidateWarehouseAsync(userId, receipt.InboundRequest.WarehouseId);
            return await MapToDtoAsync(receipt);
        }

        // UC-17: tạo nháp từ Inbound được duyệt và còn thiếu hàng.
        public async Task<GoodsReceiptDetailDto> CreateAsync(CreateGoodsReceiptDto dto, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                await ValidateUserAsync(userId, "Warehouse Staff");
                var inbound = await _context.InboundRequests.Include(x => x.Warehouse)
                    .SingleOrDefaultAsync(x => x.Id == dto.InboundRequestId);
                // MSG91: không có Inbound hoặc Inbound chưa được duyệt thì dừng.
                if (inbound == null)
                {
                    throw new GoodsReceiptException(400, "MSG91", "Inbound request is invalid or not approved.");
                }
                await ValidateWarehouseAsync(userId, inbound.WarehouseId);
                ValidateInbound(inbound);
                var lines = await _inboundService.GetLinesAsync(inbound.Id);
                // MSG92: không còn SKU nào thiếu hàng thì không tạo thêm phiếu.
                if (lines.All(x => x.RemainingQty == 0))
                {
                    throw new GoodsReceiptException(409, "MSG92", "Inbound request has been fully received.");
                }
                var receipt = new GoodsReceipt
                {
                    InboundRequestId = inbound.Id,
                    ReceivedByUserId = userId,
                    ReceivedDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = GoodsReceiptStatus.Draft
                };
                foreach (var line in lines)
                {
                    receipt.Items.Add(new GoodsReceiptItem
                    {
                        ProductVariantId = line.ProductVariantId,
                        ReceivedQty = 0,
                        BinId = null // Swimlane: quét trước, chọn bin sau.
                    });
                }
                _context.GoodsReceipts.Add(receipt);
                await _context.SaveChangesAsync();
                receipt = await LoadReceiptAsync(receipt.Id);
                var result = await MapToDtoAsync(receipt);
                await transaction.CommitAsync();
                return result; // Tạo DRAFT, chưa tăng tồn.
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        // UC-18 SAVE_DRAFT: chỉ lưu bin, frontend không được quyết định ReceivedQty.
        public async Task<GoodsReceiptDetailDto> UpdateDraftAsync(int id, UpdateGoodsReceiptDto dto, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                await ValidateUserAsync(userId, "Warehouse Staff");
                var receipt = await LoadReceiptAsync(id);
                await ValidateEditAsync(receipt, userId, dto.ExpectedVersion);
                if (dto.Items == null || dto.Items.Any(x => x == null)
                    || dto.Items.Select(x => x.ItemId).Distinct().Count() != dto.Items.Count)
                {
                    throw new GoodsReceiptException(400, "GR_ITEMS", "Item assignments must be present and unique.");
                }
                foreach (var assignment in dto.Items)
                {
                    var item = receipt.Items.SingleOrDefault(x => x.Id == assignment.ItemId);
                    if (item == null)
                    {
                        throw new GoodsReceiptException(400, "GR_ITEM", "Item does not belong to this receipt.");
                    }
                    item.BinId = assignment.BinId;
                }
                await ValidateReceiptAsync(receipt, false);
                MarkChanged(receipt);
                await _context.SaveChangesAsync();
                var result = await MapToDtoAsync(receipt);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        // UC-18 SUBMIT: kiểm tra dữ liệu đã lưu rồi chuyển PendingApproval.
        public async Task<GoodsReceiptDetailDto> SubmitAsync(int id, ReceiptVersionDto dto, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _context.LockGoodsReceiptAsync();
                await ValidateUserAsync(userId, "Warehouse Staff");
                var receipt = await LoadReceiptAsync(id);
                await ValidateEditAsync(receipt, userId, dto.ExpectedVersion);
                await ValidateReceiptAsync(receipt, true);
                receipt.Status = GoodsReceiptStatus.PendingApproval;
                MarkChanged(receipt);
                await _context.SaveChangesAsync();
                var result = await MapToDtoAsync(receipt);
                await transaction.CommitAsync();
                return result; // Manager thấy ngay trong list PendingApproval; chưa nhập tồn.
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        // Các hàm bên dưới dùng chung cho Create/Scan/Submit/Approve để không lặp quy tắc.
        public async Task<GoodsReceipt> LoadReceiptAsync(int id)
        {
            var receipt = await _context.GoodsReceipts
                .Include(x => x.InboundRequest).ThenInclude(x => x.Warehouse)
                .Include(x => x.Items).ThenInclude(x => x.ProductVariant).ThenInclude(x => x.Product)
                .Include(x => x.Items).ThenInclude(x => x.Serials)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (receipt == null)
            {
                throw new GoodsReceiptException(404, "GR_NOT_FOUND", "Goods receipt was not found.");
            }
            return receipt;
        }

        public async Task ValidateUserAsync(int userId, string? requiredRole)
        {
            var user = await _context.Users.AsNoTracking().Include(x => x.Role)
                .SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new GoodsReceiptException(403, "GR_USER_INACTIVE", "User is missing or inactive.");
            }
            bool roleAllowed;
            if (requiredRole == null)
            {
                roleAllowed = user.Role.RoleName == "Warehouse Staff" || user.Role.RoleName == "Manager";
            }
            else
            {
                roleAllowed = user.Role.RoleName == requiredRole;
            }
            if (!roleAllowed)
            {
                throw new GoodsReceiptException(403, "GR_ROLE", "This role cannot perform this operation.");
            }
        }

        public async Task ValidateWarehouseAsync(int userId, int warehouseId)
        {
            bool assigned = await _context.UserWarehouses.AnyAsync(x => x.UserId == userId && x.WarehouseId == warehouseId);
            if (!assigned)
            {
                throw new GoodsReceiptException(403, "MSG56", "You do not have access to this warehouse.");
            }
        }

        public async Task ValidateEditAsync(GoodsReceipt receipt, int userId, string version)
        {
            await ValidateWarehouseAsync(userId, receipt.InboundRequest.WarehouseId);
            // Chính sách mặc định: chỉ người tạo được sửa nháp của mình.
            if (receipt.ReceivedByUserId != userId)
            {
                throw new GoodsReceiptException(403, "GR_OWNER", "Only the creator may edit this draft.");
            }
            ValidateStateAndVersion(receipt, GoodsReceiptStatus.Draft, version);
        }

        public void ValidateStateAndVersion(GoodsReceipt receipt, string expectedStatus, string version)
        {
            if (receipt.Status != expectedStatus)
            {
                throw new GoodsReceiptException(409, "GR_STATE", "Receipt is not in the required state: " + expectedStatus);
            }
            byte[] expectedVersion;
            try
            {
                expectedVersion = Convert.FromBase64String(version ?? "");
            }
            catch (FormatException)
            {
                throw new GoodsReceiptException(400, "GR_VERSION_FORMAT", "Version must be Base64.");
            }
            if (expectedVersion.Length != 8)
            {
                throw new GoodsReceiptException(400, "GR_VERSION_FORMAT", "Version must contain 8 bytes.");
            }
            // Hai người mở cùng phiếu: chỉ người gửi đúng Version mới nhất được lưu.
            if (!receipt.Version.SequenceEqual(expectedVersion))
            {
                throw new GoodsReceiptException(409, "GR_STALE_VERSION", "Receipt changed. Reload before trying again.");
            }
        }

        public void ValidateInbound(InboundRequest inbound)
        {
            if (inbound.Status == "Completed")
            {
                throw new GoodsReceiptException(409, "MSG92", "Inbound request has been fully received.");
            }
            if (inbound.Status != "Approved" && inbound.Status != "PartiallyReceived")
            {
                throw new GoodsReceiptException(409, "MSG91", "Inbound request is not approved for receiving.");
            }
            if (!string.Equals(inbound.Warehouse.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new GoodsReceiptException(409, "GR_WAREHOUSE_INACTIVE", "Warehouse is inactive.");
            }
        }

        public void ValidateProduct(GoodsReceiptItem item)
        {
            if (item.ProductVariant.WeightKg <= 0 || string.IsNullOrWhiteSpace(item.ProductVariant.SKU)
                || item.ProductVariant.Product == null
                || !string.Equals(item.ProductVariant.Product.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new GoodsReceiptException(400, "GR_SKU", "Product must be active with a valid SKU and positive weight.");
            }
        }

        public async Task ValidateReceiptAsync(GoodsReceipt receipt, bool isSubmission)
        {
            ValidateInbound(receipt.InboundRequest);
            var lines = await _inboundService.GetLinesAsync(receipt.InboundRequestId);
            // Module hiện có một dòng cho mỗi variant, chưa hỗ trợ chia SKU ra nhiều bin.
            if (receipt.Items.Select(x => x.ProductVariantId).Distinct().Count() != receipt.Items.Count)
            {
                throw new GoodsReceiptException(409, "GR_INTEGRITY", "Duplicate product lines are not supported.");
            }
            foreach (var item in receipt.Items)
            {
                ValidateProduct(item);
                if (!lines.Any(x => x.ProductVariantId == item.ProductVariantId))
                {
                    throw new GoodsReceiptException(400, "GR_SKU", "Receipt variant is not requested by inbound.");
                }
                // MSG157: số lượng phải khớp số serial đã lưu, không tin số client gửi lên.
                if (item.ReceivedQty < 0 || item.ReceivedQty != item.Serials.Count)
                {
                    throw new GoodsReceiptException(409, "MSG157", "Quantity must match persisted scan count.");
                }
                // Format có thể được cấu hình chặt hơn sau lúc Scan, nên kiểm tra lại.
                foreach (var scan in item.Serials)
                {
                    GoodsReceiptSerialRules.Validate(scan.SerialCode, _configuration["GoodsReceipts:SerialPattern"]);
                }
            }
            foreach (var line in lines)
            {
                int receiptQty = receipt.Items.Where(x => x.ProductVariantId == line.ProductVariantId).Sum(x => x.ReceivedQty);
                // MSG95: kiểm tra từng SKU, không lấy SKU thừa bù SKU thiếu.
                if (receiptQty > line.RemainingQty)
                {
                    throw new GoodsReceiptException(409, "MSG95", "Received quantity exceeds remaining quantity.");
                }
            }
            // Đối chiếu cả lúc scan, lưu nháp, gửi duyệt và duyệt; không chỉ kiểm tra ở giao diện.
            await ValidateSupplierManifestAsync(receipt);
            // MSG93: chưa nhận thiết bị nào thì không gửi duyệt được.
            if (isSubmission && receipt.Items.Sum(x => x.ReceivedQty) == 0)
            {
                throw new GoodsReceiptException(400, "MSG93", "Scan at least one device before submitting.");
            }
            bool duplicate = await _context.GoodsReceiptSerials.Where(x => x.GoodsReceiptId == receipt.Id)
                .AnyAsync(x => _context.SerialNumbers.Any(s => s.SerialCode == x.SerialCode));
            if (duplicate)
            {
                throw new GoodsReceiptException(409, "MSG158", "A serial is already registered in inventory.");
            }
            await _stockService.ValidateCapacityAsync(receipt, isSubmission);
        }

        public async Task ValidateSupplierManifestAsync(GoodsReceipt receipt)
        {
            // Inbound đã duyệt từ trước khi có bảng kê vẫn được xem/nhận tiếp.
            // Inbound mới chỉ được Manager duyệt khi Purchasing đã nhập đủ bảng kê.
            if (!receipt.InboundRequest.HasSupplierManifest) return;
            var expected = await _context.InboundExpectedSerials.AsNoTracking()
                .Where(x => x.InboundRequestId == receipt.InboundRequestId)
                .Include(x => x.InboundRequestItem).ToListAsync();
            var lines = await _inboundService.GetLinesAsync(receipt.InboundRequestId);
            if (receipt.InboundRequest.SupplierCommitmentStatus != "Confirmed"
                || expected.Count != lines.Sum(x => x.ExpectedQty)
                || expected.Select(x => x.SerialCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != expected.Count
                || expected.Any(x => !lines.Any(line => line.ProductVariantId == x.InboundRequestItem.ProductVariantId))
                || lines.Any(line => expected.Count(x => x.InboundRequestItem.ProductVariantId == line.ProductVariantId) != line.ExpectedQty))
            {
                throw new GoodsReceiptException(409, "IN_MANIFEST_REQUIRED", "Supplier serial declaration is incomplete or inconsistent.");
            }
            foreach (var item in receipt.Items)
            {
                foreach (var scan in item.Serials)
                {
                    var declared = expected.SingleOrDefault(x => string.Equals(x.SerialCode, scan.SerialCode, StringComparison.OrdinalIgnoreCase));
                    if (declared == null)
                        throw new GoodsReceiptException(400, "GR_SERIAL_NOT_EXPECTED", "Serial is not declared for this inbound request.");
                    if (declared.InboundRequestItem.ProductVariantId != item.ProductVariantId)
                        throw new GoodsReceiptException(400, "GR_SERIAL_WRONG_SKU", "Serial is declared for a different SKU.");
                }
            }
        }

        public void MarkChanged(GoodsReceipt receipt)
        {
            receipt.UpdatedAt = DateTime.UtcNow;
            // Kể cả khi chỉ thêm scan, phải UPDATE header để SQL tạo Version mới.
            _context.Entry(receipt).Property(x => x.UpdatedAt).IsModified = true;
        }

        public async Task<GoodsReceiptDetailDto> MapToDtoAsync(GoodsReceipt receipt)
        {
            var lines = await _inboundService.GetLinesAsync(receipt.InboundRequestId);
            var result = new GoodsReceiptDetailDto
            {
                Id = receipt.Id,
                InboundRequestId = receipt.InboundRequestId,
                WarehouseId = receipt.InboundRequest.WarehouseId,
                ReceivedByUserId = receipt.ReceivedByUserId,
                ReceivedDate = receipt.ReceivedDate,
                Status = receipt.Status,
                Version = Convert.ToBase64String(receipt.Version),
                DecidedByUserId = receipt.ApprovedByUserId,
                DecisionDate = receipt.ApprovalDate,
                RejectionReason = receipt.RejectionReason,
                HasSupplierManifest = receipt.InboundRequest.HasSupplierManifest
            };
            var expectedSerials = await _context.InboundExpectedSerials.AsNoTracking()
                .Where(x => x.InboundRequestId == receipt.InboundRequestId)
                .Include(x => x.InboundRequestItem).OrderBy(x => x.Id).ToListAsync();
            // Đọc trạng thái nhận qua các đợt; phiếu từ chối không giữ serial đã khai báo.
            var activeScans = await _context.GoodsReceiptSerials.AsNoTracking()
                .Where(x => x.GoodsReceiptItem.GoodsReceipt.InboundRequestId == receipt.InboundRequestId
                    && x.GoodsReceiptItem.GoodsReceipt.Status != "Rejected")
                .Select(x => new { x.SerialCode, x.GoodsReceiptId, x.GoodsReceiptItem.ProductVariantId,
                    Status = x.GoodsReceiptItem.GoodsReceipt.Status }).ToListAsync();
            foreach (var item in receipt.Items.OrderBy(x => x.Id))
            {
                var line = lines.Single(x => x.ProductVariantId == item.ProductVariantId);
                var itemDto = new GoodsReceiptItemDto
                {
                    Id = item.Id,
                    ProductVariantId = item.ProductVariantId,
                    SKU = item.ProductVariant.SKU,
                    BinId = item.BinId,
                    ReceivedQty = item.ReceivedQty,
                    ExpectedQty = line.ExpectedQty,
                    ApprovedReceivedQty = line.ReceivedQty,
                    RemainingQty = line.RemainingQty
                };
                foreach (var scan in item.Serials.OrderBy(x => x.Id))
                {
                    itemDto.Serials.Add(new ReceiptSerialDto
                    {
                        Id = scan.Id,
                        ItemId = item.Id,
                        SerialCode = scan.SerialCode,
                        ScannedAt = scan.ScannedAt,
                        ReceivedQty = item.ReceivedQty,
                        Version = result.Version
                    });
                }
                foreach (var declared in expectedSerials.Where(x => x.InboundRequestItem.ProductVariantId == item.ProductVariantId))
                {
                    // Ưu tiên đã duyệt > quét phiếu này > quét phiếu khác > chưa nhận.
                    var matches = activeScans.Where(x => x.ProductVariantId == item.ProductVariantId
                        && string.Equals(x.SerialCode, declared.SerialCode, StringComparison.OrdinalIgnoreCase)).ToList();
                    var approved = matches.FirstOrDefault(x => x.Status == "Approved");
                    var here = matches.FirstOrDefault(x => x.GoodsReceiptId == receipt.Id);
                    var other = matches.FirstOrDefault();
                    var expectedDto = new ExpectedReceiptSerialDto { SerialCode = declared.SerialCode };
                    if (approved != null) { expectedDto.Status = "Received"; expectedDto.ReceiptId = approved.GoodsReceiptId; expectedDto.ScannedSerialCode = approved.SerialCode; }
                    else if (here != null) { expectedDto.Status = "ScannedThisReceipt"; expectedDto.ReceiptId = receipt.Id; expectedDto.ScannedSerialCode = here.SerialCode; }
                    else if (other != null) { expectedDto.Status = "ScannedOtherReceipt"; expectedDto.ReceiptId = other.GoodsReceiptId; expectedDto.ScannedSerialCode = other.SerialCode; }
                    itemDto.ExpectedSerials.Add(expectedDto);
                }
                result.Items.Add(itemDto);
            }
            return result;
        }
    }
}
