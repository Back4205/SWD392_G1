
using EWMS.Api.Data;
using EWMS.Api.Entities;
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories
{
    public class InboundRequestRepository : GenericRepository<InboundRequest>, IInboundRequestRepository
    {
        private readonly EWMSDbContext _context;

        public InboundRequestRepository(EWMSDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<List<int>> GetAssignedWarehouseIdsAsync(int userId) =>
            _context.UserWarehouses.Where(x => x.UserId == userId).Select(x => x.WarehouseId).ToListAsync();

        public Task<bool> IsAssignedToWarehouseAsync(int userId, int warehouseId) =>
            _context.UserWarehouses.AnyAsync(x => x.UserId == userId && x.WarehouseId == warehouseId);

        public async Task<InboundLookupDto> GetLookupsAsync(int userId)
        {
            var warehouseIds = await GetAssignedWarehouseIdsAsync(userId);
            var products = await _context.ProductVariants.AsNoTracking()
                .Where(x => x.Product != null && x.Product.Status == "Active" && x.WeightKg > 0)
                .OrderBy(x => x.SKU)
                .Select(x => new InboundProductLookupDto
                {
                    Id = x.Id, SKU = x.SKU, ProductName = x.Product!.ProductName
                }).ToListAsync();
            return new InboundLookupDto
            {
                Warehouses = await _context.Warehouses.AsNoTracking()
                    .Where(x => warehouseIds.Contains(x.Id) && x.Status == "Active")
                    .OrderBy(x => x.WarehouseName)
                    .Select(x => new InboundWarehouseLookupDto { Id = x.Id, WarehouseName = x.WarehouseName }).ToListAsync(),
                Suppliers = await _context.Suppliers.AsNoTracking().Where(x => x.Status == "Active")
                    .OrderBy(x => x.SupplierName)
                    .Select(x => new InboundSupplierLookupDto { Id = x.Id, SupplierName = x.SupplierName }).ToListAsync(),
                ProductVariants = products.Where(x => !string.IsNullOrWhiteSpace(x.SKU)).ToList(),
                WarehouseStaff = await _context.UserWarehouses.AsNoTracking()
                    .Where(x => warehouseIds.Contains(x.WarehouseId) && x.Warehouse.Status == "Active"
                        && x.User.Status == "Active" && x.User.Role.RoleName == "Warehouse Staff")
                    .OrderBy(x => x.User.FullName)
                    .Select(x => new InboundStaffLookupDto
                    {
                        UserId = x.UserId, FullName = x.User.FullName, WarehouseId = x.WarehouseId
                    }).ToListAsync()
            };
        }

        // Task 1: Create Inbound Request
        public async Task<InboundRequest> CreateAsync(
            InboundRequest request,
            List<InboundRequestItem> items)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.InboundRequests.Add(request);
                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    item.InboundRequestId = request.Id;
                }

                _context.InboundRequestItems.AddRange(items);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return request;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Task 2: Get Inbound Request List
        public override async Task<List<InboundRequest>> GetAllAsync()
        {
            return await _context.InboundRequests
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        // Task 3: Get Inbound Request by ID
        public override async Task<InboundRequest?> GetByIdAsync(int id)
        {
            return await _context.InboundRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Task 3: Get items of an Inbound Request
        public async Task<List<InboundRequestItem>> GetItemsByRequestIdAsync(
            int inboundRequestId)
        {
            return await _context.InboundRequestItems
                .AsNoTracking()
                .Where(item => item.InboundRequestId == inboundRequestId)
                .OrderBy(item => item.Id)
                .ToListAsync();
        }

        public Task<InboundRequest?> GetForUpdateAsync(int id) =>
            _context.InboundRequests.SingleOrDefaultAsync(x => x.Id == id);

        public Task<List<InboundRequestItem>> LoadItemsAsync(int requestId, bool tracking)
        {
            var query = _context.InboundRequestItems.Where(x => x.InboundRequestId == requestId);
            if (!tracking) query = query.AsNoTracking();
            return query.Include(x => x.ProductVariant).ThenInclude(x => x.Product)
                .OrderBy(x => x.Id).ToListAsync();
        }

        public Task<string?> GetUserNameAsync(int userId) => _context.Users
            .Where(x => x.Id == userId).Select(x => (string?)x.FullName).SingleOrDefaultAsync();

        public Task<bool> HasGoodsReceiptsAsync(int requestId) =>
            _context.GoodsReceipts.AnyAsync(x => x.InboundRequestId == requestId);

        public Task<bool> IsActiveWarehouseAsync(int warehouseId) =>
            _context.Warehouses.AnyAsync(x => x.Id == warehouseId && x.Status == "Active");

        public Task<bool> IsActiveSupplierAsync(int supplierId) =>
            _context.Suppliers.AnyAsync(x => x.Id == supplierId && x.Status == "Active");

        public Task<ProductVariant?> GetProductVariantAsync(int variantId) =>
            _context.ProductVariants.AsNoTracking().Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == variantId);

        public Task<bool> IsActiveAssignedUserAsync(int userId, int warehouseId, string role) =>
            _context.UserWarehouses.AnyAsync(x => x.UserId == userId && x.WarehouseId == warehouseId
                && x.User.Status == "Active" && x.User.Role.RoleName == role);

        // SD04: the repository records the commitment and all confirmed lines together.
        // The service owns the surrounding transaction; these methods never commit it.
        public async Task SaveCommitmentAsync(InboundRequest request, int supplierId, string status,
            DateTime? expectedDate, IReadOnlyList<(InboundRequestItem Item, int VariantId, int Quantity)> items)
        {
            request.SupplierId = supplierId;
            request.RecordSupplierCommitment(status, expectedDate);
            foreach (var confirmed in items)
                confirmed.Item.UpdateConfirmedDetails(confirmed.VariantId, confirmed.Quantity);
            await _context.SaveChangesAsync();
        }

        public async Task SaveApprovalAsync(InboundRequest request, int managerUserId)
        {
            request.RecordApproval(managerUserId);
            await _context.SaveChangesAsync();
        }

        public async Task SaveRejectionAsync(InboundRequest request, string reason, int managerUserId)
        {
            request.RecordRejection(reason, managerUserId);
            await _context.SaveChangesAsync();
        }

        // Task 6: Get Goods Receipts belonging to an Inbound Request
        public async Task<List<GoodsReceipt>> GetGoodsReceiptsByRequestIdAsync(
            int inboundRequestId)
        {
            return await _context.GoodsReceipts
                .AsNoTracking()
                .Where(receipt => receipt.InboundRequestId == inboundRequestId)
                .OrderBy(receipt => receipt.Id)
                .ToListAsync();
        }

        // Task 6: Get items belonging to specified Goods Receipts
        public async Task<List<GoodsReceiptItem>> GetGoodsReceiptItemsByReceiptIdsAsync(
            List<int> goodsReceiptIds)
        {
            if (goodsReceiptIds == null || goodsReceiptIds.Count == 0)
            {
                return new List<GoodsReceiptItem>();
            }

            return await _context.GoodsReceiptItems
                .AsNoTracking()
                .Where(item => goodsReceiptIds.Contains(item.GoodsReceiptId))
                .OrderBy(item => item.Id)
                .ToListAsync();
        }



    }
}
