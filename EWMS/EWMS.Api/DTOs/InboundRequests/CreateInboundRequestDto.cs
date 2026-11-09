
using System.ComponentModel.DataAnnotations;

namespace EWMS.Api.DTOs.InboundRequests
{
    public class CreateInboundRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "WarehouseId must be greater than 0.")]
        public int WarehouseId { get; set; }

        // UC11/SD03: Manager chọn nhân viên kho đã được phân công vào kho nhận.
        [Required]
        [Range(1, int.MaxValue)]
        public int? AssignedStaffUserId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one product is required.")]
        public List<CreateInboundRequestItemDto> Items { get; set; } = new();
    }

    public class CreateInboundRequestItemDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int ProductVariantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ExpectedQty must be greater than 0.")]
        public int ExpectedQty { get; set; }
    }
}
