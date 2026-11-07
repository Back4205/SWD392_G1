using EWMS.Api.Repositories;
﻿using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Services
{
    // Nối nhận hàng với Inbound đã merge. Không thay đổi contract cũ của Người 4.
    public class InboundReceivingService : IInboundService
    {
        private readonly IInboundRequestRepository _inbounds;
        private readonly GenericRepository<InboundRequestItem> _items;
        private readonly GenericRepository<GoodsReceiptItem> _receivedItems;

        public InboundReceivingService(EWMSDbContext context,
            IInboundRequestRepository? inbounds = null, GenericRepository<InboundRequestItem>? items = null,
            GenericRepository<GoodsReceiptItem>? receivedItems = null)
        {
            _inbounds = inbounds ?? new InboundRequestRepository(context);
            _items = items ?? new GenericRepository<InboundRequestItem>(context);
            _receivedItems = receivedItems ?? new GenericRepository<GoodsReceiptItem>(context);
        }

        public async Task<List<ReceivingLineDto>> GetLinesAsync(int inboundId)
        {
            var expectedItems = await _items.Query.Include(x => x.ProductVariant)
                .Where(x => x.InboundRequestId == inboundId).ToListAsync();
            // Chỉ phiếu Approved mới được tính là đã nhận, không tính Draft/Pending/Rejected.
            var approvedItems = await _receivedItems.Query
                .Where(x => x.GoodsReceipt.InboundRequestId == inboundId
                    && x.GoodsReceipt.Status == GoodsReceiptStatus.Approved).ToListAsync();
            var result = new List<ReceivingLineDto>();
            foreach (var item in expectedItems)
            {
                var line = result.SingleOrDefault(x => x.ProductVariantId == item.ProductVariantId);
                if (line == null)
                {
                    line = new ReceivingLineDto
                    {
                        ProductVariantId = item.ProductVariantId,
                        SKU = item.ProductVariant.SKU
                    };
                    result.Add(line);
                }
                line.ExpectedQty += item.ExpectedQty;
            }
            foreach (var item in approvedItems)
            {
                var line = result.SingleOrDefault(x => x.ProductVariantId == item.ProductVariantId);
                if (line == null)
                {
                    throw new GoodsReceiptException(409, "GR_INTEGRITY", "Approved receipt contains an unrequested variant.");
                }
                line.ReceivedQty += item.ReceivedQty;
            }
            if (result.Count == 0)
            {
                throw new GoodsReceiptException(409, "GR_INTEGRITY", "Inbound has no product lines.");
            }
            foreach (var line in result)
            {
                line.RemainingQty = line.ExpectedQty - line.ReceivedQty;
                if (line.ExpectedQty <= 0 || line.ReceivedQty < 0 || line.RemainingQty < 0)
                {
                    throw new GoodsReceiptException(409, "GR_INTEGRITY", "Inbound quantities are inconsistent.");
                }
            }
            return result;
        }

        public async Task RefreshReceivingProgressAsync(int inboundId)
        {
            var inbound = await _inbounds.Query.SingleAsync(x => x.Id == inboundId);
            var lines = await GetLinesAsync(inboundId);
            // Hoàn thành chỉ khi TẤT CẢ SKU đã nhận đủ.
            if (lines.All(x => x.RemainingQty == 0))
            {
                inbound.UpdateStatus("Completed");
            }
            else if (lines.Any(x => x.ReceivedQty > 0))
            {
                inbound.UpdateStatus("PartiallyReceived");
            }
            else
            {
                inbound.UpdateStatus("Approved");
            }
            // Cùng DbContext/transaction của Approval; không tự Commit.
            await _inbounds.SaveChangesAsync();
        }
    }
}
