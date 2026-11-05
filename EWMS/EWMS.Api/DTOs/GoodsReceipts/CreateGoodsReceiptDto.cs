using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EWMS.Api.DTOs.GoodsReceipts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateGoodsReceiptDto
    {
        [Range(1, int.MaxValue)]
        public int InboundRequestId { get; set; }
    }
}
