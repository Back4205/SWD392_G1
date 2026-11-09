using EWMS.Api.Entities;
using Xunit;

namespace EWMS.Api.Tests;

public class ReceivingEntityBehaviorTests
{
    [Fact]
    public void StaffConfirmationOnlySubmitsAndManagerDecisionIsTerminal()
    {
        var receipt = new GoodsReceipt { Status = "Draft" };
        Assert.Throws<InvalidOperationException>(() => receipt.UpdateStatus("Approved"));
        Assert.Equal("Draft", receipt.Status);
        receipt.Confirm();
        Assert.Equal("PendingApproval", receipt.Status);
        receipt.UpdateStatus("Approved");
        Assert.Throws<InvalidOperationException>(() => receipt.Confirm());
        Assert.Throws<InvalidOperationException>(() => receipt.UpdateStatus("Rejected"));
        Assert.Equal("Approved", receipt.Status);
    }

    [Fact]
    public void RejectedReceiptCannotBeReopenedBySubmittingAgain()
    {
        var receipt = new GoodsReceipt { Status = "PendingApproval" };
        receipt.UpdateStatus("Rejected");
        Assert.Throws<InvalidOperationException>(() => receipt.UpdateStatus("Draft"));
        Assert.Equal("Rejected", receipt.Status);
    }

    [Fact]
    public void ReceiptQuantityCannotBeChangedIndependentlyOfScans()
    {
        var item = new GoodsReceiptItem();
        Assert.Throws<ArgumentException>(() => item.UpdateQuantity(1));
        Assert.Equal(0, item.ReceivedQty);
        item.Serials.Add(new GoodsReceiptSerial { SerialCode = "SERIAL-001" });
        item.UpdateQuantity(1);
        Assert.Equal(1, item.ReceivedQty);
        Assert.Throws<ArgumentException>(() => item.UpdateQuantity(-1));
        Assert.Equal(1, item.ReceivedQty);
    }

    [Fact]
    public void ReceivingStockPreservesReservationsAndRejectsInvalidOrOverflowingQuantity()
    {
        var stock = new Stock { OnHandQty = 5, ReservedQty = 3 };
        stock.IncreaseOnHand(2);
        Assert.Equal(7, stock.OnHandQty);
        Assert.Equal(3, stock.ReservedQty);
        Assert.Throws<ArgumentOutOfRangeException>(() => stock.IncreaseOnHand(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stock.IncreaseOnHand(-1));
        stock.OnHandQty = int.MaxValue;
        Assert.Throws<OverflowException>(() => stock.IncreaseOnHand(1));
        Assert.Equal(int.MaxValue, stock.OnHandQty);
    }

    [Theory]
    [InlineData("Rejected")]
    [InlineData("Canceled")]
    [InlineData("Completed")]
    public void TerminalInboundStatesCannotBeResubmitted(string status)
    {
        var inbound = new InboundRequest { Status = status };
        Assert.Throws<InvalidOperationException>(() => inbound.UpdateStatus("Pending"));
        Assert.Equal(status, inbound.Status);
    }

    [Fact]
    public void InboundCanCompleteInOneDeliveryOrSeveral()
    {
        var partial = new InboundRequest { Status = "Approved" };
        partial.UpdateStatus("PartiallyReceived");
        Assert.Throws<InvalidOperationException>(() => partial.UpdateStatus("Approved"));
        partial.UpdateStatus("Completed");
        Assert.Equal("Completed", partial.Status);
        var full = new InboundRequest { Status = "Approved" };
        full.UpdateStatus("Completed");
        Assert.Equal("Completed", full.Status);
    }
}
