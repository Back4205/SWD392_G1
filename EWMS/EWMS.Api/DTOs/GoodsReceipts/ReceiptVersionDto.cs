using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EWMS.Api.DTOs.GoodsReceipts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class ReceiptVersionDto
    {
        // Gửi lại Version lấy từ API chi tiết/scan gần nhất.
        [Required]
        public string ExpectedVersion { get; set; } = string.Empty;
    }
}
