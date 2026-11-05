using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EWMS.Api.DTOs.GoodsReceipts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class RejectGoodsReceiptDto : ReceiptVersionDto
    {
        [Required, StringLength(255)]
        public string Reason { get; set; } = string.Empty;
    }
}
