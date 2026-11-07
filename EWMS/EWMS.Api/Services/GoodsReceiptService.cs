using EWMS.Api.Repositories;
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
        private readonly GoodsReceiptRepository _receipts;
        private readonly GoodsReceiptScanRepository _scans;
        private readonly SerialNumberRepository _serials;
        private readonly IInboundRequestRepository _inbounds;
        private readonly GenericRepository<User> _users;
        private readonly GenericRepository<UserWarehouse> _assignments;
        private readonly GenericRepository<Warehouse> _warehouses;
        private readonly GenericRepository<StorageBin> _bins;
        private readonly StockRepository _stocks;
        private readonly IStockService _stockService;
        private readonly IInboundService _inboundService;
        private readonly IConfiguration _configuration;

        public GoodsReceiptService(EWMSDbContext context, IStockService stockService,
            IInboundService inboundService, IConfiguration configuration,
            GoodsReceiptRepository? receipts = null, GoodsReceiptScanRepository? scans = null,
            SerialNumberRepository? serials = null, IInboundRequestRepository? inbounds = null,
            GenericRepository<User>? users = null, GenericRepository<UserWarehouse>? assignments = null,
            GenericRepository<Warehouse>? warehouses = null, GenericRepository<StorageBin>? bins = null,
            StockRepository? stocks = null)
        {
            _context = context;
            _receipts = receipts ?? new GoodsReceiptRepository(context);
            _scans = scans ?? new GoodsReceiptScanRepository(context);
            _serials = serials ?? new SerialNumberRepository(context);
            _inbounds = inbounds ?? new InboundRequestRepository(context);
            _users = users ?? new GenericRepository<User>(context);
            _assignments = assignments ?? new GenericRepository<UserWarehouse>(context);
            _warehouses = warehouses ?? new GenericRepository<Warehouse>(context);
            _bins = bins ?? new GenericRepository<StorageBin>(context);
            _stocks = stocks ?? new StockRepository(context);
            _stockService = stockService;
            _inboundService = inboundService;
            _configuration = configuration;
        }

        // UI lấy role/quyền kho thật từ database, không dựa vào lựa chọn role ở trình duyệt.
        public async Task<GoodsReceiptSessionDto> GetSessionAsync(int userId)
        {
            await ValidateUserAsync(userId, null);
            var user = await _users.Query.AsNoTracking().Include(x => x.Role)
                .SingleAsync(x => x.Id == userId);
            var warehouseIds = await _assignments.Query.Where(x => x.UserId == userId)
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
            var warehouseIds = await _assignments.Query.Where(x => x.UserId == userId)
                .Select(x => x.WarehouseId).ToListAsync();
            var inbounds = await _inbounds.Query.AsNoTracking().Include(x => x.Warehouse)
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
            var warehouse = await _warehouses.Query.AsNoTracking().SingleAsync(x => x.Id == warehouseId);
            if (!string.Equals(warehouse.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new GoodsReceiptException(409, "GR_WAREHOUSE_INACTIVE", "Warehouse is inactive.");
            }
            var bins = await _bins.Query.AsNoTracking().Include(x => x.Rack)
                .Where(x => x.Rack.Section.WarehouseId == warehouseId)
                .OrderBy(x => x.BinCode).ThenBy(x => x.Id).ToListAsync();
            var stockWeights = await _stocks.Query.AsNoTracking()
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
            var warehouseIds = await _assignments.Query.Where(x => x.UserId == userId)
                .Select(x => x.WarehouseId).ToListAsync();
            var query = _receipts.Query.AsNoTracking()
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
                var inbound = await _inbounds.Query.Include(x => x.Warehouse)
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
                _receipts.Add(receipt);
                await _receipts.SaveChangesAsync();
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
                await _receipts.SaveChangesAsync();
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
                receipt.Confirm();
                MarkChanged(receipt);
                await _receipts.SaveChangesAsync();
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
            var receipt = await _receipts.GetByIdAsync(id);
            if (receipt == null)
            {
                throw new GoodsReceiptException(404, "GR_NOT_FOUND", "Goods receipt was not found.");
            }
            return receipt;
        }

        public async Task ValidateUserAsync(int userId, string? requiredRole)
        {
            var user = await _users.Query.AsNoTracking().Include(x => x.Role)
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
            bool assigned = await _assignments.Query.AnyAsync(x => x.UserId == userId && x.WarehouseId == warehouseId);
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
            // MSG93: chưa nhận thiết bị nào thì không gửi duyệt được.
            if (isSubmission && receipt.Items.Sum(x => x.ReceivedQty) == 0)
            {
                throw new GoodsReceiptException(400, "MSG93", "Scan at least one device before submitting.");
            }
            bool duplicate = await _scans.Query.Where(x => x.GoodsReceiptId == receipt.Id)
                .AnyAsync(x => _serials.Query.Any(s => s.SerialCode == x.SerialCode));
            if (duplicate)
            {
                throw new GoodsReceiptException(409, "MSG158", "A serial is already registered in inventory.");
            }
            await _stockService.ValidateCapacityAsync(receipt, isSubmission);
        }

        public void MarkChanged(GoodsReceipt receipt)
        {
            _receipts.MarkChanged(receipt);
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
                DecidedByName = receipt.ApprovedByUserId.HasValue
                    ? await _users.Query.Where(x => x.Id == receipt.ApprovedByUserId.Value).Select(x => x.FullName).SingleOrDefaultAsync()
                    : null,
                DecisionDate = receipt.ApprovalDate,
                RejectionReason = receipt.RejectionReason,
                ApprovalNote = receipt.ApprovalNote
            };
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
                result.Items.Add(itemDto);
            }
            return result;
        }
    }
}
