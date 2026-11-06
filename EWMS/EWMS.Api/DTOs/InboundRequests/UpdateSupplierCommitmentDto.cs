
using System;
using System.ComponentModel.DataAnnotations;

namespace EWMS.Api.DTOs.InboundRequests
{
    public class UpdateSupplierCommitmentDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Supplier ID must be greater than 0.")]
        public int? SupplierId { get; set; }

        [Required(ErrorMessage = "Commitment status is required.")]
        public string CommitmentStatus { get; set; } = string.Empty;

        public DateTime? ExpectedDeliveryDate { get; set; }

        public List<ConfirmedInboundItemDto> Items { get; set; } = new();

        public string? ExpectedRevision { get; set; }
    }

    public class ConfirmedInboundItemDto
    {
        public int ItemId { get; set; }
        public int ProductVariantId { get; set; }
        public int ExpectedQty { get; set; }
    }
}
