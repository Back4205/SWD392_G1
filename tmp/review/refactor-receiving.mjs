import fs from 'node:fs';
const root = 'SWD392_G1/EWMS/EWMS.Api/Services/';
function edit(name, fields, parameters, init, substitutions) {
  const path = root + name + '.cs';
  let s = fs.readFileSync(path, 'utf8');
  s = 'using EWMS.Api.Repositories;\n' + s;
  s = s.replace('private readonly EWMSDbContext _context;', 'private readonly EWMSDbContext _context;\n' + fields);
  const start = s.indexOf('public ' + name + '(');
  const end = s.indexOf(')\n', start) >= 0 ? s.indexOf(')\n', start) : s.indexOf(')\r\n', start);
  if (start < 0 || end < 0) throw new Error('Constructor not found: ' + name);
  s = s.slice(0, end) + ',\n' + parameters + s.slice(end);
  s = s.replace('_context = context;', '_context = context;\n' + init);
  for (const [a,b] of substitutions) s = s.replaceAll(a,b);
  fs.writeFileSync(path, s);
}
edit('GoodsReceiptService', `        private readonly GoodsReceiptRepository _receipts;
        private readonly GoodsReceiptScanRepository _scans;
        private readonly SerialNumberRepository _serials;
        private readonly GenericRepository<InboundRequest> _inbounds;
        private readonly GenericRepository<User> _users;
        private readonly GenericRepository<UserWarehouse> _assignments;
        private readonly GenericRepository<Warehouse> _warehouses;
        private readonly GenericRepository<StorageBin> _bins;
        private readonly StockRepository _stocks;`,
`            GoodsReceiptRepository? receipts = null, GoodsReceiptScanRepository? scans = null,
            SerialNumberRepository? serials = null, GenericRepository<InboundRequest>? inbounds = null,
            GenericRepository<User>? users = null, GenericRepository<UserWarehouse>? assignments = null,
            GenericRepository<Warehouse>? warehouses = null, GenericRepository<StorageBin>? bins = null,
            StockRepository? stocks = null`,
`            _receipts = receipts ?? new GoodsReceiptRepository(context);
            _scans = scans ?? new GoodsReceiptScanRepository(context);
            _serials = serials ?? new SerialNumberRepository(context);
            _inbounds = inbounds ?? new GenericRepository<InboundRequest>(context);
            _users = users ?? new GenericRepository<User>(context);
            _assignments = assignments ?? new GenericRepository<UserWarehouse>(context);
            _warehouses = warehouses ?? new GenericRepository<Warehouse>(context);
            _bins = bins ?? new GenericRepository<StorageBin>(context);
            _stocks = stocks ?? new StockRepository(context);`,
[[' _context.GoodsReceipts.Add(receipt)', ' _receipts.Add(receipt)'],
 ['_context.GoodsReceipts', '_receipts.Query'], ['_context.GoodsReceiptSerials','_scans.Query'],
 ['_context.SerialNumbers','_serials.Query'], ['_context.InboundRequests','_inbounds.Query'],
 ['_context.Users','_users.Query'], ['_context.UserWarehouses','_assignments.Query'],
 ['_context.Warehouses','_warehouses.Query'], ['_context.StorageBins','_bins.Query'], ['_context.Stocks','_stocks.Query'],
 ['_context.SaveChangesAsync()', '_receipts.SaveChangesAsync()'],
 ['receipt.Status = GoodsReceiptStatus.PendingApproval;', 'receipt.Confirm();']]);
// Load graph and rowversion tracking belong to the receipt repository.
{
 const p=root+'GoodsReceiptService.cs'; let s=fs.readFileSync(p,'utf8');
 const begin=s.indexOf('            var receipt = await _receipts.Query', s.indexOf('public async Task<GoodsReceipt> LoadReceiptAsync'));
 const end=s.indexOf('            if (receipt == null)',begin);
 s=s.slice(0,begin)+'            var receipt = await _receipts.GetByIdAsync(id);\n'+s.slice(end);
 const mb=s.indexOf('            receipt.UpdatedAt = DateTime.UtcNow;',s.indexOf('public void MarkChanged'));
 const me=s.indexOf('\n        }',mb);
 s=s.slice(0,mb)+'            _receipts.MarkChanged(receipt);'+s.slice(me);
 fs.writeFileSync(p,s);
}
edit('SerialNumberService',`        private readonly GoodsReceiptScanRepository _scans;
        private readonly SerialNumberRepository _serials;`,
`            GoodsReceiptScanRepository? scans = null, SerialNumberRepository? serials = null`,
`            _scans = scans ?? new GoodsReceiptScanRepository(context);
            _serials = serials ?? new SerialNumberRepository(context);`,
 [['_context.GoodsReceiptSerials\n                    .AnyAsync(x => x.GoodsReceiptId == id && x.SerialCode == dto.SerialCode)', '_scans.ExistsAsync(id, dto.SerialCode)'],
  ['_context.SerialNumbers.AnyAsync(x => x.SerialCode == dto.SerialCode)', '_serials.ExistsBySerialCodeAsync(dto.SerialCode)'],
  ['item.ReceivedQty = item.Serials.Count;', 'item.UpdateQuantity(item.Serials.Count);'],
  ['_context.SaveChangesAsync()', '_scans.SaveChangesAsync()']]);
edit('GoodsReceiptApprovalService',`        private readonly GoodsReceiptRepository _receipts;
        private readonly SerialNumberRepository _serials;`,
`            GoodsReceiptRepository? receipts = null, SerialNumberRepository? serials = null`,
`            _receipts = receipts ?? new GoodsReceiptRepository(context);
            _serials = serials ?? new SerialNumberRepository(context);`,
 [['_context.SaveChangesAsync()', '_receipts.SaveChangesAsync()'],
  ['receipt.Status = GoodsReceiptStatus.Approved;', 'receipt.UpdateStatus(GoodsReceiptStatus.Approved);'],
  ['receipt.Status = GoodsReceiptStatus.Rejected;', 'receipt.UpdateStatus(GoodsReceiptStatus.Rejected);']]);
{
 const p=root+'GoodsReceiptApprovalService.cs';let s=fs.readFileSync(p,'utf8');
 const b=s.indexOf('                        var serial = new SerialNumber');
 const e=s.indexOf('\n                    }',b);
 s=s.slice(0,b)+'                        _serials.RegisterWithMovement(receipt, item, scan.SerialCode, userId);'+s.slice(e);
 fs.writeFileSync(p,s);
}
edit('StockReceivingService',`        private readonly StockRepository _stocks;
        private readonly GenericRepository<Warehouse> _warehouses;
        private readonly GenericRepository<StorageBin> _bins;`,
`            StockRepository? stocks = null, GenericRepository<Warehouse>? warehouses = null,
            GenericRepository<StorageBin>? bins = null`,
`            _stocks = stocks ?? new StockRepository(context);
            _warehouses = warehouses ?? new GenericRepository<Warehouse>(context);
            _bins = bins ?? new GenericRepository<StorageBin>(context);`,
 [['_context.Stocks.Add(stock)', '_stocks.Add(stock)'],['_context.Stocks', '_stocks.Query'],
  ['_context.Warehouses', '_warehouses.Query'],['_context.StorageBins', '_bins.Query'],
  ['stock.OnHandQty = checked(stock.OnHandQty + item.ReceivedQty);', 'stock.IncreaseOnHand(item.ReceivedQty);'],
  ['_context.SaveChangesAsync()', '_stocks.SaveChangesAsync()']]);
edit('InboundReceivingService',`        private readonly GenericRepository<InboundRequest> _inbounds;
        private readonly GenericRepository<InboundRequestItem> _items;
        private readonly GenericRepository<GoodsReceiptItem> _receivedItems;`,
`            GenericRepository<InboundRequest>? inbounds = null, GenericRepository<InboundRequestItem>? items = null,
            GenericRepository<GoodsReceiptItem>? receivedItems = null`,
`            _inbounds = inbounds ?? new GenericRepository<InboundRequest>(context);
            _items = items ?? new GenericRepository<InboundRequestItem>(context);
            _receivedItems = receivedItems ?? new GenericRepository<GoodsReceiptItem>(context);`,
 [['_context.InboundRequestItems', '_items.Query'],['_context.GoodsReceiptItems','_receivedItems.Query'],
  ['_context.InboundRequests', '_inbounds.Query'],['_context.SaveChangesAsync()', '_inbounds.SaveChangesAsync()']]);
