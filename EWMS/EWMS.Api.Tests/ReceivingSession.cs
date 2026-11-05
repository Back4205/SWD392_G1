using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using EWMS.Api.Services;
using Microsoft.Extensions.Configuration;

namespace EWMS.Api.Tests;

// Mỗi lần dùng helper tạo DbContext mới, giống một HTTP request riêng.
internal sealed class ReceivingSession : IDisposable
{
    public EWMSDbContext Context { get; }
    public GoodsReceiptService Receipts { get; }
    public SerialNumberService Serials { get; }
    public GoodsReceiptApprovalService Approval { get; }

    public ReceivingSession(SqlServerFixture fixture, FailurePoint failure = FailurePoint.None,
        IConfiguration? configuration = null)
    {
        Context = fixture.CreateContext();
        IStockService stock = new MockStockService(Context);
        IInboundService inbound = new InboundReceivingService(Context);
        if (failure == FailurePoint.AfterSerial || failure == FailurePoint.AfterStock)
            stock = new FailingStockService(stock, failure);
        if (failure == FailurePoint.AfterApproved)
            inbound = new FailingInboundService(inbound);
        configuration = configuration ?? new ConfigurationBuilder().Build();
        Receipts = new GoodsReceiptService(Context, stock, inbound, configuration);
        Serials = new SerialNumberService(Context, Receipts, configuration);
        Approval = new GoodsReceiptApprovalService(Context, Receipts, stock, inbound);
    }

    public void Dispose() => Context.Dispose();

    private sealed class FailingStockService : IStockService
    {
        private readonly IStockService _real;
        private readonly FailurePoint _failure;
        public FailingStockService(IStockService real, FailurePoint failure)
        {
            _real = real;
            _failure = failure;
        }
        public Task ValidateCapacityAsync(GoodsReceipt receipt, bool requireBins)
            => _real.ValidateCapacityAsync(receipt, requireBins);
        public async Task ApplyApprovedReceiptAsync(GoodsReceipt receipt)
        {
            if (_failure == FailurePoint.AfterStock)
                await _real.ApplyApprovedReceiptAsync(receipt);
            throw new InjectedFailureException();
        }
    }

    private sealed class FailingInboundService : IInboundService
    {
        private readonly IInboundService _real;
        public FailingInboundService(IInboundService real) { _real = real; }
        public Task<List<ReceivingLineDto>> GetLinesAsync(int inboundId) => _real.GetLinesAsync(inboundId);
        public Task RefreshReceivingProgressAsync(int inboundId) => throw new InjectedFailureException();
    }
}

public enum FailurePoint { None, AfterSerial, AfterStock, AfterApproved }
public class InjectedFailureException : Exception { }
