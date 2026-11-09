using EWMS.Api.Common;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Services;
using EWMS.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class GoodsReceiptFlowTests
{
    private readonly SqlServerFixture _fixture;
    public GoodsReceiptFlowTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task GR01_GR02_EmptyListAndMissingDetail()
    {
        var data = await _fixture.SeedAsync();
        using var session = new ReceivingSession(_fixture);
        var list = await session.Receipts.GetAllAsync(new GoodsReceiptFilterDto(), data.StaffId);
        Assert.Empty(list.Items);
        await Error(() => session.Receipts.GetByIdAsync(int.MaxValue, data.StaffId), 404);
    }

    [Fact]
    public async Task GR03_GR07_GR13_GR18_GR22_GR27_GR35_GR37_ReceiveEightThenTwo()
    {
        var data = await _fixture.SeedAsync();
        var first = await Create(data);
        Assert.Equal("Draft", first.Status);
        Assert.Equal(data.StaffId, first.ReceivedByUserId);
        Assert.Null(first.Items.Single().BinId);
        await NoInventory(data, first.Id);
        var originalVersion = first.Version;
        first = await Scan(data, first, 8);
        Assert.NotEqual(originalVersion, first.Version);
        Assert.Equal(8, first.Items.Single().ReceivedQty);
        Assert.Equal(8, first.Items.Single().Serials.Count);
        Assert.Null(first.Items.Single().BinId);
        await NoInventory(data, first.Id);
        first = await Place(data, first);
        first = await Submit(data, first);
        Assert.Equal("PendingApproval", first.Status);
        Assert.Equal(0, first.Items.Single().ApprovedReceivedQty);
        await NoInventory(data, first.Id);
        using (var manager = new ReceivingSession(_fixture))
        {
            var list = await manager.Receipts.GetAllAsync(new GoodsReceiptFilterDto { Status = "PendingApproval" }, data.ManagerId);
            Assert.Contains(list.Items, x => x.Id == first.Id);
        }
        first = await Approve(data, first);
        Assert.Equal("Approved", first.Status);
        Assert.Equal(2, first.Items.Single().RemainingQty);
        await Inventory(data, 8, "PartiallyReceived");
        using (var manager = new ReceivingSession(_fixture))
            await Error(() => manager.Approval.ApproveAsync(first.Id, Version(first), data.ManagerId), 409);
        var second = await Pending(data, 2);
        await Approve(data, second);
        await Inventory(data, 10, "Completed");
        using (var staff = new ReceivingSession(_fixture))
            await Error(() => staff.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId), 409, "MSG92");
        using var context = _fixture.CreateContext();
        var movements = await context.SerialMovements.Where(x => x.RefId == first.Id || x.RefId == second.Id).ToListAsync();
        Assert.Equal(10, movements.Count);
        Assert.All(movements, x => { Assert.Null(x.FromBinId); Assert.Equal(data.BinId, x.ToBinId); Assert.Equal(data.ManagerId, x.PerformedByUserId); });
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    public async Task GR04_UnapprovedInboundCannotCreateReceipt(string state)
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            (await context.InboundRequests.FindAsync(data.InboundId))!.Status = state;
            await context.SaveChangesAsync();
        }
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId), 409, "MSG91");
        using var check = _fixture.CreateContext();
        Assert.False(await check.GoodsReceipts.AnyAsync(x => x.InboundRequestId == data.InboundId));
    }

    [Fact]
    public async Task GR05_CompletedInboundCannotCreateReceipt()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            (await context.InboundRequests.FindAsync(data.InboundId))!.Status = "Completed";
            await context.SaveChangesAsync();
        }
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId), 409, "MSG92");
    }

    [Fact]
    public async Task GR06_WarehouseAndActiveUserGuards()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Create(data);
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.GetByIdAsync(receipt.Id, data.OtherUserId), 403);
        Assert.Empty((await session.Receipts.GetAllAsync(new GoodsReceiptFilterDto(), data.OtherUserId)).Items);
        await Error(() => session.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.OtherUserId), 403);
        await Error(() => session.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.InactiveUserId), 403);
    }

    [Theory]
    [InlineData("bad serial", false)]
    [InlineData("", false)]
    [InlineData("VALID-SERIAL", true)]
    public async Task GR08_InvalidFormatOrWrongSkuDoesNotPersist(string code, bool wrongSku)
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Create(data);
        using (var session = new ReceivingSession(_fixture))
            await Error(() => session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = code, SKU = wrongSku ? "WRONG" : data.SKU }, data.StaffId), 400);
        var after = await Detail(data, receipt.Id);
        Assert.Equal(0, after.Items.Single().ReceivedQty);
        Assert.Empty(after.Items.Single().Serials);
        Assert.Equal(receipt.Version, after.Version);
    }

    [Fact]
    public async Task GR09_GR10_DuplicateScanAndOfficialSerialDoNotIncrement()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Scan(data, await Create(data), 1);
        using (var session = new ReceivingSession(_fixture))
            await Error(() => session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = receipt.Items.Single().Serials.Single().SerialCode, SKU = data.SKU }, data.StaffId), 409, "MSG158");
        using (var context = _fixture.CreateContext())
        {
            context.SerialNumbers.Add(new SerialNumber { ProductVariantId = data.VariantId, CurrentBinId = data.BinId, SerialCode = "OFFICIAL-" + data.Suffix, CurrentStatus = "IN_STOCK" });
            await context.SaveChangesAsync();
        }
        using (var session = new ReceivingSession(_fixture))
            await Error(() => session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "OFFICIAL-" + data.Suffix, SKU = data.SKU }, data.StaffId), 409, "MSG158");
        Assert.Equal(1, (await Detail(data, receipt.Id)).Items.Single().ReceivedQty);
    }

    [Fact]
    public async Task GR11_ConcurrentSameScanHasOneWinner()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Create(data);
        async Task<bool> Attempt()
        {
            using var session = new ReceivingSession(_fixture);
            try
            {
                await session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
                    new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "RACE-" + data.Suffix, SKU = data.SKU }, data.StaffId);
                return true;
            }
            catch (GoodsReceiptException ex) when (ex.StatusCode == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Attempt(), Attempt()), x => x);
        Assert.Equal(1, (await Detail(data, receipt.Id)).Items.Single().ReceivedQty);
    }

    [Fact]
    public async Task GR12_ItemFromAnotherReceiptCannotBeUsed()
    {
        var data = await _fixture.SeedAsync();
        var first = await Create(data);
        var second = await Create(data);
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Serials.AddAsync(first.Id, second.Items.Single().Id,
            new AddReceiptSerialDto { ExpectedVersion = first.Version, SerialCode = "ITEM-" + data.Suffix, SKU = data.SKU }, data.StaffId), 400);
        await Error(() => session.Receipts.UpdateDraftAsync(first.Id, new UpdateGoodsReceiptDto
        {
            ExpectedVersion = first.Version, Items = new List<UpdateReceiptBinDto> { new() { ItemId = second.Items.Single().Id, BinId = data.BinId } }
        }, data.StaffId), 400);
    }

    [Fact]
    public async Task GR13_GR14_GR17_SubmitRequiresScanAndCorrectBin()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Create(data);
        using (var session = new ReceivingSession(_fixture))
            await Error(() => session.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId), 400, "MSG93");
        receipt = await Scan(data, receipt, 1);
        using (var session = new ReceivingSession(_fixture))
        {
            await Error(() => session.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId), 400, "MSG94");
            await Error(() => session.Receipts.UpdateDraftAsync(receipt.Id, Placement(receipt, data.OtherBinId), data.StaffId), 400);
        }
        await NoInventory(data, receipt.Id);
        Assert.Equal("Draft", (await Detail(data, receipt.Id)).Status);
    }

    [Fact]
    public async Task GR15_CapacityAggregatesMultipleLinesOnSameRack()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            (await context.StorageRacks.FindAsync(data.RackId))!.MaxWeightKg = 4;
            context.InboundRequestItems.Add(new InboundRequestItem { InboundRequestId = data.InboundId, ProductVariantId = data.SecondVariantId, ExpectedQty = 1 });
            await context.SaveChangesAsync();
        }
        var receipt = await Create(data);
        foreach (var item in receipt.Items.ToList())
        {
            using var session = new ReceivingSession(_fixture);
            await session.Serials.AddAsync(receipt.Id, item.Id,
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "RACK-" + item.Id, SKU = item.SKU }, data.StaffId);
            receipt = await Detail(data, receipt.Id);
        }
        using var edit = new ReceivingSession(_fixture);
        await Error(() => edit.Receipts.UpdateDraftAsync(receipt.Id, new UpdateGoodsReceiptDto
        {
            ExpectedVersion = receipt.Version,
            Items = new List<UpdateReceiptBinDto>
            {
                new() { ItemId = receipt.Items[0].Id, BinId = data.BinId },
                new() { ItemId = receipt.Items[1].Id, BinId = data.SecondBinId }
            }
        }, data.StaffId), 409, "MSG96");
    }

    [Theory]
    [InlineData("PendingApproval")]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    public async Task GR19_PendingAndTerminalReceiptsCannotBeEdited(string state)
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 1);
        if (state == "Approved") receipt = await Approve(data, receipt);
        if (state == "Rejected")
        {
            using var manager = new ReceivingSession(_fixture);
            receipt = await manager.Approval.RejectAsync(receipt.Id,
                new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = "Damaged" }, data.ManagerId);
        }
        using var staff = new ReceivingSession(_fixture);
        await Error(() => staff.Receipts.UpdateDraftAsync(receipt.Id, Placement(receipt, data.BinId), data.StaffId), 409);
        await Error(() => staff.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "LOCKED-" + data.Suffix, SKU = data.SKU }, data.StaffId), 409);
    }

    [Fact]
    public async Task GR20_StaleWriteCannotOverwriteScan()
    {
        var data = await _fixture.SeedAsync();
        var before = await Create(data);
        var after = await Scan(data, before, 1);
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.UpdateDraftAsync(before.Id, Placement(before, data.BinId), data.StaffId), 409);
        await Error(() => session.Receipts.SubmitAsync(before.Id, Version(before), data.StaffId), 409);
        Assert.Equal(after.Version, (await Detail(data, after.Id)).Version);
    }

    [Fact]
    public async Task GR21_OnlyManagerMayDecide()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 1);
        foreach (int actor in new[] { data.StaffId, data.PurchasingUserId })
        {
            using var session = new ReceivingSession(_fixture);
            await Error(() => session.Approval.ApproveAsync(receipt.Id, Version(receipt), actor), 403);
            await Error(() => session.Approval.RejectAsync(receipt.Id,
                new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = "No" }, actor), 403);
        }
        await NoInventory(data, receipt.Id);
    }

    [Fact]
    public async Task GR23_GR24_RejectPersistsUnicodeAndNeverAddsInventory()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 2);
        using (var manager = new ReceivingSession(_fixture))
            await Error(() => manager.Approval.RejectAsync(receipt.Id,
                new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = " " }, data.ManagerId), 400);
        using (var manager = new ReceivingSession(_fixture))
            await manager.Approval.RejectAsync(receipt.Id,
                new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = " Hàng vỡ, thiếu phụ kiện " }, data.ManagerId);
        var after = await Detail(data, receipt.Id);
        Assert.Equal("Rejected", after.Status);
        Assert.Equal("Hàng vỡ, thiếu phụ kiện", after.RejectionReason);
        Assert.Equal(data.ManagerId, after.DecidedByUserId);
        Assert.NotNull(after.DecisionDate);
        Assert.Equal(2, after.Items.Single().Serials.Count);
        await NoInventory(data, receipt.Id);
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Approval.ApproveAsync(receipt.Id, Version(after), data.ManagerId), 409);
    }

    [Fact]
    public async Task GR25_ApprovalRechecksRemainingConsumedSinceSubmit()
    {
        var data = await _fixture.SeedAsync(expected: 3);
        var first = await Pending(data, 2);
        var second = await Pending(data, 2);
        await Approve(data, first);
        using var manager = new ReceivingSession(_fixture);
        await Error(() => manager.Approval.ApproveAsync(second.Id, Version(second), data.ManagerId), 409, "MSG95");
        await Inventory(data, 2, "PartiallyReceived");
    }

    [Fact]
    public async Task GR26_ApprovalRechecksCapacityConsumedSinceSubmit()
    {
        var data = await _fixture.SeedAsync(capacity: 10);
        var receipt = await Pending(data, 3);
        using (var context = _fixture.CreateContext())
        {
            context.Stocks.Add(new Stock { ProductVariantId = data.SecondVariantId, BinId = data.BinId, OnHandQty = 2 });
            await context.SaveChangesAsync();
        }
        using var manager = new ReceivingSession(_fixture);
        await Error(() => manager.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId), 409, "MSG96");
        await NoInventory(data, receipt.Id);
    }

    [Fact]
    public async Task GR28_ConcurrentApproveAndRejectHaveOneWinner()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 2);
        async Task<bool> Decide(bool approve)
        {
            using var session = new ReceivingSession(_fixture);
            try
            {
                if (approve) await session.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId);
                else await session.Approval.RejectAsync(receipt.Id,
                    new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = "Damaged" }, data.ManagerId);
                return true;
            }
            catch (GoodsReceiptException ex) when (ex.StatusCode == 409) { return false; }
        }
        Assert.Single(await Task.WhenAll(Decide(true), Decide(false)), x => x);
        var after = await Detail(data, receipt.Id);
        if (after.Status == "Approved") await Inventory(data, 2, "PartiallyReceived");
        else { Assert.Equal("Rejected", after.Status); await NoInventory(data, receipt.Id); }
    }

    [Fact]
    public async Task GR29_ConcurrentApprovalsCannotConsumeSameRemainingTwice()
    {
        var data = await _fixture.SeedAsync(expected: 3);
        var first = await Pending(data, 2);
        var second = await Pending(data, 2);
        Assert.Single(await Task.WhenAll(TryApprove(data, first), TryApprove(data, second)), x => x);
        await Inventory(data, 2, "PartiallyReceived");
    }

    [Fact]
    public async Task GR30_ConcurrentDifferentInboundsCannotOverfillRack()
    {
        var data = await _fixture.SeedAsync(capacity: 6);
        int otherId = await AddInbound(data, 10);
        var first = await Pending(data, 2);
        int originalId = data.InboundId;
        data.InboundId = otherId;
        var second = await Pending(data, 2);
        data.InboundId = originalId;
        Assert.Single(await Task.WhenAll(TryApprove(data, first), TryApprove(data, second)), x => x);
        using var context = _fixture.CreateContext();
        Assert.Equal(2, await context.Stocks.Where(x => x.BinId == data.BinId).SumAsync(x => x.OnHandQty));
    }

    [Fact]
    public async Task GR31_ConcurrentApprovalsCreateOneStockKeyAndAddBothCounts()
    {
        var data = await _fixture.SeedAsync();
        var first = await Pending(data, 2);
        data.InboundId = await AddInbound(data, 10);
        var second = await Pending(data, 3);
        Assert.All(await Task.WhenAll(TryApprove(data, first), TryApprove(data, second)), Assert.True);
        using var context = _fixture.CreateContext();
        var stocks = await context.Stocks.Where(x => x.ProductVariantId == data.VariantId && x.BinId == data.BinId).ToListAsync();
        Assert.Single(stocks);
        Assert.Equal(5, stocks[0].OnHandQty);
        Assert.Equal(0, stocks[0].ReservedQty);
    }

    [Fact]
    public async Task GR32_ReceiptPreservesReservedStock()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            context.Stocks.Add(new Stock { ProductVariantId = data.VariantId, BinId = data.BinId, OnHandQty = 5, ReservedQty = 3 });
            await context.SaveChangesAsync();
        }
        await Approve(data, await Pending(data, 2));
        using var check = _fixture.CreateContext();
        var stock = await check.Stocks.SingleAsync(x => x.ProductVariantId == data.VariantId && x.BinId == data.BinId);
        Assert.Equal(7, stock.OnHandQty);
        Assert.Equal(3, stock.ReservedQty);
    }

    [Theory]
    [InlineData(FailurePoint.AfterSerial)]
    [InlineData(FailurePoint.AfterStock)]
    [InlineData(FailurePoint.AfterApproved)]
    public async Task GR33_GR34_FailureAtEachWriteBoundaryRollsBackEverything(FailurePoint failure)
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 2);
        using (var manager = new ReceivingSession(_fixture, failure))
            await Assert.ThrowsAsync<InjectedFailureException>(() => manager.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId));
        var after = await Detail(data, receipt.Id);
        Assert.Equal("PendingApproval", after.Status);
        Assert.Equal(receipt.Version, after.Version);
        Assert.Null(after.DecidedByUserId);
        await NoInventory(data, receipt.Id);
        await Approve(data, after);
        await Inventory(data, 2, "PartiallyReceived");
    }

    [Fact]
    public async Task SameWarehouseUserCanReadButOnlyCreatorCanEdit()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            context.UserWarehouses.Add(new UserWarehouse { UserId = data.OtherUserId, WarehouseId = data.WarehouseId });
            await context.SaveChangesAsync();
        }
        var receipt = await Create(data);
        using var session = new ReceivingSession(_fixture);
        Assert.Equal(receipt.Id, (await session.Receipts.GetByIdAsync(receipt.Id, data.OtherUserId)).Id);
        await Error(() => session.Receipts.UpdateDraftAsync(receipt.Id, Placement(receipt, data.BinId), data.OtherUserId), 403);
        await Error(() => session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "OWNER-" + data.Suffix, SKU = data.SKU }, data.OtherUserId), 403);
    }

    [Fact]
    public async Task ConcurrentDoubleApprovalDoesNotDuplicateInventory()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 2);
        Assert.Single(await Task.WhenAll(TryApprove(data, receipt), TryApprove(data, receipt)), x => x);
        await Inventory(data, 2, "PartiallyReceived");
        using var context = _fixture.CreateContext();
        Assert.Equal(2, await context.SerialMovements.CountAsync(x => x.RefId == receipt.Id));
    }

    [Fact]
    public async Task ConcurrentCrossReceiptSerialCanOnlyBecomeOneOfficialDevice()
    {
        var data = await _fixture.SeedAsync();
        async Task<GoodsReceiptDetailDto> SharedPending()
        {
            var receipt = await Create(data);
            using var session = new ReceivingSession(_fixture);
            await session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "SHARED-" + data.Suffix, SKU = data.SKU }, data.StaffId);
            return await Submit(data, await Place(data, await Detail(data, receipt.Id)));
        }
        var first = await SharedPending();
        data.InboundId = await AddInbound(data, 10);
        var second = await SharedPending();
        Assert.Single(await Task.WhenAll(TryApprove(data, first), TryApprove(data, second)), x => x);
        using var context = _fixture.CreateContext();
        Assert.Equal(1, await context.SerialNumbers.CountAsync(x => x.ProductVariantId == data.VariantId));
        Assert.Equal(1, (await context.Stocks.SingleAsync(x => x.ProductVariantId == data.VariantId)).OnHandQty);
    }

    [Fact]
    public async Task WarehouseCapacityIncludesReservedStockInOtherBins()
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Pending(data, 2);
        using (var context = _fixture.CreateContext())
        {
            (await context.Warehouses.FindAsync(data.WarehouseId))!.MaxWeightKg = 6;
            context.Stocks.Add(new Stock { ProductVariantId = data.SecondVariantId, BinId = data.SecondBinId, OnHandQty = 1, ReservedQty = 1 });
            await context.SaveChangesAsync();
        }
        using var manager = new ReceivingSession(_fixture);
        await Error(() => manager.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId), 409, "MSG96");
        await NoInventory(data, receipt.Id);
    }

    [Fact]
    public async Task StaleScanAndExcessScanLeaveVersionAndCountUnchanged()
    {
        var data = await _fixture.SeedAsync(expected: 1);
        var before = await Create(data);
        var receipt = await Scan(data, before, 1);
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
            new AddReceiptSerialDto { ExpectedVersion = before.Version, SerialCode = "STALE-" + data.Suffix, SKU = data.SKU }, data.StaffId), 409, "GR_STALE_VERSION");
        await Error(() => session.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "EXCESS-" + data.Suffix, SKU = data.SKU }, data.StaffId), 409, "MSG95");
        var after = await Detail(data, receipt.Id);
        Assert.Equal(receipt.Version, after.Version);
        Assert.Equal(1, after.Items.Single().ReceivedQty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedSerialPolicyIsRecheckedAtSubmitAndApprove(bool approval)
    {
        var data = await _fixture.SeedAsync();
        var receipt = await Place(data, await Scan(data, await Create(data), 1));
        if (approval) receipt = await Submit(data, receipt);
        var strictConfiguration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["GoodsReceipts:SerialPattern"] = @"\AONLY-ALLOWED\z" }).Build();
        using var session = new ReceivingSession(_fixture, configuration: strictConfiguration);
        if (approval)
            await Error(() => session.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId), 400, "GR_SERIAL_FORMAT");
        else
            await Error(() => session.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId), 400, "GR_SERIAL_FORMAT");
        Assert.Equal(approval ? "PendingApproval" : "Draft", (await Detail(data, receipt.Id)).Status);
        await NoInventory(data, receipt.Id);
    }

    private async Task<GoodsReceiptDetailDto> Create(TestData data)
    {
        using var session = new ReceivingSession(_fixture);
        return await session.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> Detail(TestData data, int id)
    {
        using var session = new ReceivingSession(_fixture);
        return await session.Receipts.GetByIdAsync(id, data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> Scan(TestData data, GoodsReceiptDetailDto receipt, int quantity)
    {
        var item = receipt.Items.Single(x => x.ProductVariantId == data.VariantId);
        string currentVersion = receipt.Version;
        for (int i = 0; i < quantity; i++)
        {
            using var session = new ReceivingSession(_fixture);
            var scan = await session.Serials.AddAsync(receipt.Id, item.Id,
                new AddReceiptSerialDto { ExpectedVersion = currentVersion, SerialCode = $"S-{receipt.Id}-{i}-{data.Suffix}", SKU = item.SKU }, data.StaffId);
            currentVersion = scan.Version;
        }
        return await Detail(data, receipt.Id);
    }
    private static ReceiptVersionDto Version(GoodsReceiptDetailDto receipt)
        => new ReceiptVersionDto { ExpectedVersion = receipt.Version };
    private static UpdateGoodsReceiptDto Placement(GoodsReceiptDetailDto receipt, int binId)
        => new UpdateGoodsReceiptDto
        {
            ExpectedVersion = receipt.Version,
            Items = receipt.Items.Select(x => new UpdateReceiptBinDto { ItemId = x.Id, BinId = binId }).ToList()
        };
    private async Task<GoodsReceiptDetailDto> Place(TestData data, GoodsReceiptDetailDto receipt)
    {
        using var session = new ReceivingSession(_fixture);
        return await session.Receipts.UpdateDraftAsync(receipt.Id, Placement(receipt, data.BinId), data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> Submit(TestData data, GoodsReceiptDetailDto receipt)
    {
        using var session = new ReceivingSession(_fixture);
        return await session.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> Pending(TestData data, int count)
        => await Submit(data, await Place(data, await Scan(data, await Create(data), count)));
    private async Task<GoodsReceiptDetailDto> Approve(TestData data, GoodsReceiptDetailDto receipt)
    {
        using var session = new ReceivingSession(_fixture);
        return await session.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId);
    }
    private async Task<bool> TryApprove(TestData data, GoodsReceiptDetailDto receipt)
    {
        try { await Approve(data, receipt); return true; }
        catch (GoodsReceiptException ex) when (ex.StatusCode == 409) { return false; }
    }
    private async Task NoInventory(TestData data, int receiptId)
    {
        using var context = _fixture.CreateContext();
        Assert.False(await context.Stocks.AnyAsync(x => x.ProductVariantId == data.VariantId));
        Assert.False(await context.SerialNumbers.AnyAsync(x => x.OriginGoodsReceiptItem != null && x.OriginGoodsReceiptItem.GoodsReceiptId == receiptId));
        Assert.False(await context.SerialMovements.AnyAsync(x => x.RefId == receiptId));
        Assert.Equal("Approved", (await context.InboundRequests.FindAsync(data.InboundId))!.Status);
    }
    private async Task Inventory(TestData data, int quantity, string inboundStatus)
    {
        using var context = _fixture.CreateContext();
        var stock = await context.Stocks.SingleAsync(x => x.ProductVariantId == data.VariantId && x.BinId == data.BinId);
        Assert.Equal(quantity, stock.OnHandQty);
        Assert.Equal(quantity, await context.SerialNumbers.CountAsync(x => x.ProductVariantId == data.VariantId && x.CurrentBinId == data.BinId));
        Assert.Equal(inboundStatus, (await context.InboundRequests.FindAsync(data.InboundId))!.Status);
        // Kiểm tra contract tiến độ của module Inbound bạn cùng nhóm đã làm.
        var inboundService = new InboundRequestService(new InboundRequestRepository(context), context, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var progress = await inboundService.GetReceivingProgressAsync(data.InboundId);
        Assert.NotNull(progress);
        Assert.Equal(quantity, progress.TotalReceivedQty);
        Assert.Equal(progress.TotalExpectedQty - quantity, progress.RemainingQty);
    }
    private async Task<int> AddInbound(TestData data, int expected)
    {
        using var context = _fixture.CreateContext();
        var inbound = new InboundRequest { WarehouseId = data.WarehouseId, RequestedByUserId = data.PurchasingUserId, ApprovedByUserId = data.ManagerId, Status = "Approved", CreatedDate = DateTime.UtcNow };
        context.InboundRequests.Add(inbound);
        await context.SaveChangesAsync();
        context.InboundRequestItems.Add(new InboundRequestItem { InboundRequestId = inbound.Id, ProductVariantId = data.VariantId, ExpectedQty = expected });
        await context.SaveChangesAsync();
        return inbound.Id;
    }
    private static async Task Error(Func<Task> operation, int status, string? code = null)
    {
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(operation);
        Assert.Equal(status, error.StatusCode);
        if (code != null) Assert.Equal(code, error.Code);
    }
}
