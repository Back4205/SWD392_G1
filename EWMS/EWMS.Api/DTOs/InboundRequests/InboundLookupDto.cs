namespace EWMS.Api.DTOs.InboundRequests;

public class InboundLookupDto
{
    public List<InboundWarehouseLookupDto> Warehouses { get; set; } = new();
    public List<InboundSupplierLookupDto> Suppliers { get; set; } = new();
    public List<InboundProductLookupDto> ProductVariants { get; set; } = new();
    public List<InboundStaffLookupDto> WarehouseStaff { get; set; } = new();
}

public class InboundWarehouseLookupDto
{
    public int Id { get; set; }
    public string WarehouseName { get; set; } = "";
}

public class InboundSupplierLookupDto
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = "";
}

public class InboundProductLookupDto
{
    public int Id { get; set; }
    public string SKU { get; set; } = "";
    public string ProductName { get; set; } = "";
}

public class InboundStaffLookupDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public int WarehouseId { get; set; }
}
