using EWMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EWMS.Api.Tests;

public class FixVietnameseFontEncodingTest
{
    private const string ConnectionString = "Server=.;Database=EWMS_GoodsReceipt_Development;Integrated Security=true;TrustServerCertificate=true;Encrypt=false";

    [Fact]
    public async Task FixAllVietnameseStringsInDevelopmentDatabase()
    {
        var options = new DbContextOptionsBuilder<EWMSDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new EWMSDbContext(options);

        // 1. Categories
        var catMobile = await context.Categories.FindAsync(1);
        if (catMobile != null)
        {
            catMobile.CategoryName = "Thiết bị di động";
        }
        var catMobileDup = await context.Categories.FindAsync(2);
        if (catMobileDup != null)
        {
            var prods = await context.Products.Where(p => p.CategoryId == 2).ToListAsync();
            foreach (var p in prods) p.CategoryId = 1;
            context.Categories.Remove(catMobileDup);
        }
        var catLaptop = await context.Categories.FindAsync(3);
        if (catLaptop != null) catLaptop.CategoryName = "Máy tính xách tay";
        var catTablet = await context.Categories.FindAsync(4);
        if (catTablet != null) catTablet.CategoryName = "Máy tính bảng";
        var catAudio = await context.Categories.FindAsync(5);
        if (catAudio != null) catAudio.CategoryName = "Thiết bị âm thanh";
        var catMonitor = await context.Categories.FindAsync(6);
        if (catMonitor != null) catMonitor.CategoryName = "Màn hình & Phụ kiện";

        // 2. Warehouse
        var wh = await context.Warehouses.SingleOrDefaultAsync(w => w.WarehouseCode == "WH-HN-01");
        if (wh != null)
        {
            wh.WarehouseName = "Kho Tổng Hà Nội";
            wh.Address = "Số 1 Trịnh Văn Bô, Nam Từ Liêm, Hà Nội";
        }

        // 3. StorageSectionType & StorageSection
        var secType = await context.StorageSectionTypes.FindAsync(1);
        if (secType != null) secType.Description = "Khu vực lưu kho tiêu chuẩn";
        var sec = await context.StorageSections.FindAsync(1);
        if (sec != null) sec.SectionName = "Khu Nhập & Lưu kho A";

        // 4. StorageBins
        var bins = await context.StorageBins.ToListAsync();
        foreach (var bin in bins)
        {
            if (bin.BinCode.StartsWith("BIN-A1-"))
            {
                var suffix = bin.BinCode.Replace("BIN-A1-", "");
                bin.BinName = $"Vị trí lưu kho A1-{suffix}";
            }
        }

        // 5. Suppliers
        var supApple = await context.Suppliers.SingleOrDefaultAsync(s => s.SupplierCode == "SUP-APPLE");
        if (supApple != null)
        {
            supApple.SupplierName = "Apple Distribution Vietnam";
            supApple.ContactPerson = "Nguyễn Văn Đại Diện";
            supApple.Address = "Quận 1, TP Hồ Chí Minh";
        }
        var supSamsung = await context.Suppliers.SingleOrDefaultAsync(s => s.SupplierCode == "SUP-SAMSUNG");
        if (supSamsung != null)
        {
            supSamsung.SupplierName = "Samsung Electronics Vietnam";
            supSamsung.ContactPerson = "Trần Thị Kim Loan";
            supSamsung.Address = "Khu Công nghệ cao, TP Thủ Đức, TP Hồ Chí Minh";
        }
        var supSony = await context.Suppliers.SingleOrDefaultAsync(s => s.SupplierCode == "SUP-SONY");
        if (supSony != null)
        {
            supSony.SupplierName = "Sony Electronics Vietnam";
            supSony.ContactPerson = "Lê Hoàng Long";
            supSony.Address = "Tầng 6, President Place, 93 Nguyễn Du, Q1, TP HCM";
        }
        var supDell = await context.Suppliers.SingleOrDefaultAsync(s => s.SupplierCode == "SUP-DELL");
        if (supDell != null)
        {
            supDell.SupplierName = "Dell Global B.V. Vietnam";
            supDell.ContactPerson = "Phạm Quốc Hưng";
            supDell.Address = "Tòa nhà Landmark 81, Bình Thạnh, TP Hồ Chí Minh";
        }
        var supAsus = await context.Suppliers.SingleOrDefaultAsync(s => s.SupplierCode == "SUP-ASUS");
        if (supAsus != null)
        {
            supAsus.SupplierName = "Asus Technology Vietnam";
            supAsus.ContactPerson = "Vũ Minh Tuấn";
            supAsus.Address = "Tầng 8, Tòa nhà Viettel, 285 Cách Mạng Tháng 8, Q10, TP HCM";
        }

        // 6. Users
        var admin = await context.Users.SingleOrDefaultAsync(u => u.UserName == "admin");
        if (admin != null) admin.FullName = "Quản trị viên hệ thống";
        var manager = await context.Users.SingleOrDefaultAsync(u => u.UserName == "manager");
        if (manager != null) manager.FullName = "Quản lý kho (Nguyễn Văn Quản)";
        var purchasing = await context.Users.SingleOrDefaultAsync(u => u.UserName == "purchasing");
        if (purchasing != null) purchasing.FullName = "Trưởng phòng Mua hàng (Trần Thị Mua)";
        var staff = await context.Users.SingleOrDefaultAsync(u => u.UserName == "staff");
        if (staff != null) staff.FullName = "Nhân viên kho (Lê Văn Kho)";
        var sales = await context.Users.SingleOrDefaultAsync(u => u.UserName == "sales");
        if (sales != null) sales.FullName = "Nhân viên bán hàng (Phạm Văn Bán)";

        // 7. Product Variants
        var variants = await context.ProductVariants.ToListAsync();
        var variantDict = new Dictionary<string, (string name, string specs)>
        {
            ["IP15P-256-TI"] = ("Titan Tự Nhiên 256GB", "A17 Pro, 6.7 inch Super Retina XDR"),
            ["IP15-128-BLK"] = ("Đen 128GB", "A16 Bionic, 6.1 inch OLED Super Retina XDR"),
            ["IP16P-256-DES"] = ("Sa Mạc Titan 256GB", "A18 Pro, Camera Control, 6.3 inch 120Hz"),
            ["SS24U-512-GRY"] = ("Xám Titan 512GB", "Snapdragon 8 Gen 3, S-Pen, Camera 200MP"),
            ["SS24-256-YEL"] = ("Vàng Amber 256GB", "Exynos 2400, 6.2 inch Dynamic AMOLED 2X"),
            ["ZFOLD5-256-BLU"] = ("Xanh Icy 256GB", "Màn gập 7.6 inch 120Hz, Snapdragon 8 Gen 2"),
            ["MBP14-M3P-18-512"] = ("Đen Không Gian 18GB/512GB", "Apple M3 Pro 11-core CPU, 14-core GPU, Liquid Retina XDR"),
            ["MBA13-M3-8-256"] = ("Bạc Ánh Sao 8GB/256GB", "Apple M3 8-core CPU, 8-core GPU, Retina 13.6 inch"),
            ["XPS13-I7-16-512"] = ("Bạch Kim i7/16GB/512GB", "Core i7-1360P, 16GB LPDDR5, 13.4 inch 3.5K OLED Touch"),
            ["G15-I7-16-512-RTX4060"] = ("Xám Đậm i7/16GB/RTX4060", "Core i7-13650HX, RTX 4060 8GB, 15.6 inch FHD 165Hz"),
            ["ROG-G16-I9-16-1TB"] = ("Eclipse Gray i9/16GB/1TB", "Core i9-14900HX, RTX 4060 8GB, 16 inch QHD+ 240Hz"),
            ["ZEN14-U7-16-1TB"] = ("Ponder Blue Ultra 7/16GB", "Intel Core Ultra 7 155H, 14 inch 3K OLED 120Hz"),
            ["IPADAIR-M2-128-BLU"] = ("Xanh Blue 128GB", "Apple M2 chip, Liquid Retina 11 inch, Apple Pencil Pro support"),
            ["IPADPRO-M4-256-SLV"] = ("Bạc 256GB", "Apple M4 chip, Ultra Retina XDR OLED, siêu mỏng 5.3mm"),
            ["TABS9-128-BEI"] = ("Kem Beige 128GB", "Dynamic AMOLED 2X 11 inch 120Hz, S-Pen kèm máy, IP68"),
            ["SONY-WH1000XM5-BLK"] = ("Đen ANC Cao Cấp", "Chống ồn đỉnh cao Auto NC Optimizer, 30h pin, LDAC"),
            ["SONY-WF1000XM5-SLV"] = ("Bạc True Wireless ANC", "2 chip xử lý V2 & QN2e, màng loa Dynamic Driver X, IPX4"),
            ["APPODS-PRO2-USBC"] = ("Trắng MagSafe USB-C", "Chip Apple H2, Chủ động khử tiếng ồn 2x, Adaptive Audio"),
            ["DELL-U2724D-2K120"] = ("Bạc 27 inch 2K 120Hz", "27 inch QHD (2560x1440), IPS Black, 120Hz, 100% sRGB & 98% DCI-P3"),
            ["ASUS-VG27-FHD180"] = ("Đen 27 inch FHD 180Hz", "27 inch Fast IPS, FHD, 180Hz, 1ms GtG, ELMB Sync, DisplayHDR 400")
        };

        foreach (var v in variants)
        {
            if (variantDict.TryGetValue(v.SKU, out var data))
            {
                v.VariantName = data.name;
                v.Specs = data.specs;
            }
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public void UpdateDbContextMappingsToNvarchar()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../EWMS/EWMS.Api/Data/EWMSDbContext.cs"));
        if (!File.Exists(path)) return;
        var content = File.ReadAllText(path);

        content = content.Replace(".Property(x => x.Address)\r\n                    .HasColumnType(\"varchar(255)\")",
                                  ".Property(x => x.Address)\r\n                    .HasColumnType(\"nvarchar(255)\")")
                         .Replace(".Property(x => x.Address)\n                    .HasColumnType(\"varchar(255)\")",
                                  ".Property(x => x.Address)\n                    .HasColumnType(\"nvarchar(255)\")")
                         .Replace("e.Property(x => x.Description)\r\n                    .HasColumnType(\"varchar(255)\")",
                                  "e.Property(x => x.Description)\r\n                    .HasColumnType(\"nvarchar(255)\")")
                         .Replace("e.Property(x => x.Description)\n                    .HasColumnType(\"varchar(255)\")",
                                  "e.Property(x => x.Description)\n                    .HasColumnType(\"nvarchar(255)\")");

        File.WriteAllText(path, content);
    }
}
