
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

        public List<SupplierManifestItemDto> Items { get; set; } = new();
    }

    public class SupplierManifestItemDto
    {
        public int ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public List<string> SerialCodes { get; set; } = new();
    }
}
