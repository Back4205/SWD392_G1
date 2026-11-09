using EWMS.Api.Data;
using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Repositories;

public class SerialNumberRepository(EWMSDbContext context) : GenericRepository<SerialNumber>(context)
{
    public Task<bool> ExistsBySerialCodeAsync(string code) => Query.AnyAsync(x => x.SerialCode == code);
    public Task<SerialNumber?> GetBySerialCodeAsync(string code) => Query.SingleOrDefaultAsync(x => x.SerialCode == code);

    // UC56: serials enter inventory only after Manager approval, in its transaction.
    public void RegisterWithMovement(GoodsReceipt receipt, GoodsReceiptItem item, string code, int userId)
    {
        var serial = new SerialNumber
        {
            ProductVariantId = item.ProductVariantId,
            OriginGoodsReceiptItemId = item.Id,
            CurrentBinId = item.BinId,
            SerialCode = code
        };
        serial.UpdateStatus("IN_STOCK");
        Add(serial);
        Context.SerialMovements.Add(new SerialMovement
        {
            SerialNumber = serial, FromBinId = null, ToBinId = item.BinId,
            PerformedByUserId = userId, MovementType = "RECEIPT", RefType = "GOODS_RECEIPT",
            RefId = receipt.Id, MovementDate = DateTime.UtcNow
        });
    }
}
