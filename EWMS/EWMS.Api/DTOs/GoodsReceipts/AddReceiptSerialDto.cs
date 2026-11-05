using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EWMS.Api.DTOs.GoodsReceipts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class AddReceiptSerialDto : ReceiptVersionDto
    {
        [Required, StringLength(100)]
        public string SerialCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string SKU { get; set; } = string.Empty;
    }
}
