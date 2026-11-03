
using System.ComponentModel.DataAnnotations;

namespace EWMS.Api.DTOs.InboundRequests
{
    public class ReviewInboundRequestDto
    {
        [Required(ErrorMessage = "Decision is required.")]
        public string Decision { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }
    }
}
