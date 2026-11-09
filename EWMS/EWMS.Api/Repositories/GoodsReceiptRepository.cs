using EWMS.Api.Data;
using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories;

public class GoodsReceiptRepository(EWMSDbContext context) : GenericRepository<GoodsReceipt>(context)
{
    public override Task<GoodsReceipt?> GetByIdAsync(int id) => Query
        .Include(x => x.InboundRequest).ThenInclude(x => x.Warehouse)
        .Include(x => x.Items).ThenInclude(x => x.ProductVariant).ThenInclude(x => x.Product)
        .Include(x => x.Items).ThenInclude(x => x.Serials)
        .SingleOrDefaultAsync(x => x.Id == id);

    public void MarkChanged(GoodsReceipt receipt)
    {
        receipt.UpdatedAt = DateTime.UtcNow;
        Context.Entry(receipt).Property(x => x.UpdatedAt).IsModified = true;
    }
}
