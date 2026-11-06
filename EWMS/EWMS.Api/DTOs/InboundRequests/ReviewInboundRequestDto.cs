
using System.ComponentModel.DataAnnotations;

namespace EWMS.Api.DTOs.InboundRequests
{
    public class ReviewInboundRequestDto
    {
        [Required(ErrorMessage = "Decision is required.")]
        public string Decision { get; set; } = string.Empty;

        [StringLength(255)]
        public string? RejectionReason { get; set; }

        // Dữ liệu kiểm tra kỹ thuật, không tạo thêm trạng thái nghiệp vụ.
        public string? ExpectedRevision { get; set; }
    }
}
