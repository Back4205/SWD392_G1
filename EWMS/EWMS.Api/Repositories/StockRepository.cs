using EWMS.Api.Data;
using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories;

public class StockRepository(EWMSDbContext context) : GenericRepository<Stock>(context)
{
    public Task<Stock?> GetByProductVariantAndBinAsync(int variantId, int binId) => Query
        .SingleOrDefaultAsync(x => x.ProductVariantId == variantId && x.BinId == binId);
}
