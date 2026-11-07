using EWMS.Api.Data;
using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories;

public class GoodsReceiptScanRepository(EWMSDbContext context) : GenericRepository<GoodsReceiptSerial>(context)
{
    public Task<bool> ExistsAsync(int receiptId, string code) => Query
        .AnyAsync(x => x.GoodsReceiptId == receiptId && x.SerialCode == code);
}
