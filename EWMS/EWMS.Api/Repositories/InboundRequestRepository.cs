
using EWMS.Api.Data;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EWMS.Api.Repositories
{
    public class InboundRequestRepository : IInboundRequestRepository
    {
        private readonly EWMSDbContext _context;

        public InboundRequestRepository(EWMSDbContext context)
        {
            _context = context;
        }

        // Task 1: Check Warehouse
        public async Task<bool> WarehouseExistsAsync(int warehouseId)
        {
            return await _context.Warehouses
                .AnyAsync(w => w.Id == warehouseId);
        }

        // Task 1: Check Product Variant
        public async Task<bool> ProductVariantExistsAsync(int productVariantId)
        {
            return await _context.ProductVariants
                .AnyAsync(p => p.Id == productVariantId);
        }

        // Task 1: Check User
        public async Task<bool> UserExistsAsync(int userId)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId);
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
        public async Task<List<InboundRequest>> GetAllAsync()
        {
            return await _context.InboundRequests
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        // Task 3: Get Inbound Request by ID
        public async Task<InboundRequest?> GetByIdAsync(int id)
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

        // Task 4: Update Inbound Request
        public async Task UpdateAsync(InboundRequest request)
        {
            _context.InboundRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        // Task 4: Check if Supplier exists
        public async Task<bool> SupplierExistsAsync(int supplierId)
        {
            return await _context.Suppliers
                .AnyAsync(s => s.Id == supplierId);
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
