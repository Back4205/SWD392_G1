-- SQL Script Seed Suppliers, Categories, Products, ProductVariants and StorageBins into EWMS_GoodsReceipt_Development
USE EWMS_GoodsReceipt_Development;
GO

SET NOCOUNT ON;

-- 1. SEED SUPPLIERS (5 total)
IF NOT EXISTS (SELECT 1 FROM Supplier WHERE SupplierCode = 'SUP-APPLE')
BEGIN
    INSERT INTO Supplier (SupplierCode, SupplierName, ContactPerson, Email, PhoneNumber, Address, Status)
    VALUES ('SUP-APPLE', N'Apple Distribution Vietnam', N'Nguyễn Văn Đại Diện', 'supplier-apple@example.com', '0901234567', N'Quận 1, TP Hồ Chí Minh', 'Active');
END;

IF NOT EXISTS (SELECT 1 FROM Supplier WHERE SupplierCode = 'SUP-SAMSUNG')
BEGIN
    INSERT INTO Supplier (SupplierCode, SupplierName, ContactPerson, Email, PhoneNumber, Address, Status)
    VALUES ('SUP-SAMSUNG', N'Samsung Electronics Vietnam', N'Trần Thị Kim Loan', 'contact@samsung-vn.com', '0902345678', N'Khu Công nghệ cao, TP Thủ Đức, TP Hồ Chí Minh', 'Active');
END;

IF NOT EXISTS (SELECT 1 FROM Supplier WHERE SupplierCode = 'SUP-SONY')
BEGIN
    INSERT INTO Supplier (SupplierCode, SupplierName, ContactPerson, Email, PhoneNumber, Address, Status)
    VALUES ('SUP-SONY', N'Sony Electronics Vietnam', N'Lê Hoàng Long', 'b2b@sony.com.vn', '0903456789', N'Tầng 6, President Place, 93 Nguyễn Du, Q1, TP HCM', 'Active');
END;

IF NOT EXISTS (SELECT 1 FROM Supplier WHERE SupplierCode = 'SUP-DELL')
BEGIN
    INSERT INTO Supplier (SupplierCode, SupplierName, ContactPerson, Email, PhoneNumber, Address, Status)
    VALUES ('SUP-DELL', N'Dell Global B.V. Vietnam', N'Phạm Quốc Hưng', 'orders@dell-vietnam.vn', '0904567890', N'Tòa nhà Landmark 81, Bình Thạnh, TP Hồ Chí Minh', 'Active');
END;

IF NOT EXISTS (SELECT 1 FROM Supplier WHERE SupplierCode = 'SUP-ASUS')
BEGIN
    INSERT INTO Supplier (SupplierCode, SupplierName, ContactPerson, Email, PhoneNumber, Address, Status)
    VALUES ('SUP-ASUS', N'Asus Technology Vietnam', N'Vũ Minh Tuấn', 'distributor@asus.com.vn', '0905678901', N'Tầng 8, Tòa nhà Viettel, 285 Cách Mạng Tháng 8, Q10, TP HCM', 'Active');
END;

-- 2. SEED CATEGORIES
IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryName = N'Thiết bị di động')
BEGIN
    INSERT INTO Category (CategoryName) VALUES (N'Thiết bị di động');
END;

IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryName = N'Máy tính xách tay')
BEGIN
    INSERT INTO Category (CategoryName) VALUES (N'Máy tính xách tay');
END;

IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryName = N'Máy tính bảng')
BEGIN
    INSERT INTO Category (CategoryName) VALUES (N'Máy tính bảng');
END;

IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryName = N'Thiết bị âm thanh')
BEGIN
    INSERT INTO Category (CategoryName) VALUES (N'Thiết bị âm thanh');
END;

IF NOT EXISTS (SELECT 1 FROM Category WHERE CategoryName = N'Màn hình & Phụ kiện')
BEGIN
    INSERT INTO Category (CategoryName) VALUES (N'Màn hình & Phụ kiện');
END;

-- Variables for Category IDs
DECLARE @CatMobile INT = (SELECT TOP 1 Id FROM Category WHERE CategoryName = N'Thiết bị di động');
DECLARE @CatLaptop INT = (SELECT TOP 1 Id FROM Category WHERE CategoryName = N'Máy tính xách tay');
DECLARE @CatTablet INT = (SELECT TOP 1 Id FROM Category WHERE CategoryName = N'Máy tính bảng');
DECLARE @CatAudio INT = (SELECT TOP 1 Id FROM Category WHERE CategoryName = N'Thiết bị âm thanh');
DECLARE @CatMonitor INT = (SELECT TOP 1 Id FROM Category WHERE CategoryName = N'Màn hình & Phụ kiện');

-- 3. SEED PRODUCTS & VARIANTS (20 Products)

-- 1. iPhone 15 Pro Max 256GB (already existing or ensure)
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-IP15P')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMobile, 'PRD-IP15P', N'iPhone 15 Pro Max 256GB', 'Apple', 'A3106', 'Active');
END;
DECLARE @P_IP15P INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-IP15P');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'IP15P-256-TI')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_IP15P, 'IP15P-256-TI', N'Titan Tự Nhiên 256GB', 'Natural Titanium', '256GB', 0.22, 28000000, 32990000, 12, N'A17 Pro, 6.7 inch Super Retina XDR');
END;

-- 2. iPhone 15 128GB
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-IP15')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMobile, 'PRD-IP15', N'iPhone 15 128GB', 'Apple', 'A3090', 'Active');
END;
DECLARE @P_IP15 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-IP15');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'IP15-128-BLK')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_IP15, 'IP15-128-BLK', N'Đen 128GB', 'Black', '128GB', 0.17, 17500000, 21490000, 12, N'A16 Bionic, 6.1 inch OLED Super Retina XDR');
END;

-- 3. iPhone 16 Pro 256GB
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-IP16P')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMobile, 'PRD-IP16P', N'iPhone 16 Pro 256GB', 'Apple', 'A3293', 'Active');
END;
DECLARE @P_IP16P INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-IP16P');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'IP16P-256-DES')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_IP16P, 'IP16P-256-DES', N'Sa Mạc Titan 256GB', 'Desert Titanium', '256GB', 0.20, 27000000, 31990000, 12, N'A18 Pro, Camera Control, 6.3 inch 120Hz');
END;

-- 4. Samsung Galaxy S24 Ultra
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-SS24U')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMobile, 'PRD-SS24U', N'Samsung Galaxy S24 Ultra', 'Samsung', 'SM-S928B', 'Active');
END;
DECLARE @P_SS24U INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-SS24U');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'SS24U-512-GRY')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_SS24U, 'SS24U-512-GRY', N'Xám Titan 512GB', 'Titanium Gray', '512GB', 0.23, 26000000, 33990000, 12, N'Snapdragon 8 Gen 3, S-Pen, Camera 200MP');
END;

-- 5. Samsung Galaxy S24 256GB
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-SS24')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMobile, 'PRD-SS24', N'Samsung Galaxy S24 256GB', 'Samsung', 'SM-S921B', 'Active');
END;
DECLARE @P_SS24 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-SS24');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'SS24-256-YEL')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_SS24, 'SS24-256-YEL', N'Vàng Amber 256GB', 'Amber Yellow', '256GB', 0.17, 15500000, 19990000, 12, N'Exynos 2400, 6.2 inch Dynamic AMOLED 2X');
END;

-- 6. Samsung Galaxy Z Fold5
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-SSFOLD5')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMobile, 'PRD-SSFOLD5', N'Samsung Galaxy Z Fold5', 'Samsung', 'SM-F946B', 'Active');
END;
DECLARE @P_SSFOLD5 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-SSFOLD5');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'ZFOLD5-256-BLU')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_SSFOLD5, 'ZFOLD5-256-BLU', N'Xanh Icy 256GB', 'Icy Blue', '256GB', 0.25, 29000000, 36990000, 12, N'Màn gập 7.6 inch 120Hz, Snapdragon 8 Gen 2');
END;

-- 7. MacBook Pro 14 M3 Pro
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-MBP14')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatLaptop, 'PRD-MBP14', N'MacBook Pro 14 M3 Pro', 'Apple', 'MRX33', 'Active');
END;
DECLARE @P_MBP14 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-MBP14');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'MBP14-M3P-18-512')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_MBP14, 'MBP14-M3P-18-512', N'Đen Không Gian 18GB/512GB', 'Space Black', '512GB', 1.61, 42000000, 49990000, 12, N'Apple M3 Pro 11-core CPU, 14-core GPU, Liquid Retina XDR');
END;

-- 8. MacBook Air 13 M3
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-MBA13')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatLaptop, 'PRD-MBA13', N'MacBook Air 13 M3', 'Apple', 'MRXN3', 'Active');
END;
DECLARE @P_MBA13 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-MBA13');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'MBA13-M3-8-256')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_MBA13, 'MBA13-M3-8-256', N'Bạc Ánh Sao 8GB/256GB', 'Starlight', '256GB', 1.24, 23000000, 27990000, 12, N'Apple M3 8-core CPU, 8-core GPU, Retina 13.6 inch');
END;

-- 9. Dell XPS 13 Plus 9320
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-DELLXPS13')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatLaptop, 'PRD-DELLXPS13', N'Dell XPS 13 Plus 9320', 'Dell', 'XPS-9320', 'Active');
END;
DECLARE @P_DELLXPS13 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-DELLXPS13');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'XPS13-I7-16-512')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_DELLXPS13, 'XPS13-I7-16-512', N'Bạch Kim i7/16GB/512GB', 'Platinum', '512GB', 1.26, 35000000, 41500000, 12, N'Core i7-1360P, 16GB LPDDR5, 13.4 inch 3.5K OLED Touch');
END;

-- 10. Dell Gaming G15 5530
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-DELLG15')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatLaptop, 'PRD-DELLG15', N'Dell Gaming G15 5530', 'Dell', 'G15-5530', 'Active');
END;
DECLARE @P_DELLG15 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-DELLG15');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'G15-I7-16-512-RTX4060')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_DELLG15, 'G15-I7-16-512-RTX4060', N'Xám Đậm i7/16GB/RTX4060', 'Dark Shadow Gray', '512GB', 2.81, 24000000, 28990000, 12, N'Core i7-13650HX, RTX 4060 8GB, 15.6 inch FHD 165Hz');
END;

-- 11. Asus ROG Strix G16
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-ASUSROG')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatLaptop, 'PRD-ASUSROG', N'Asus ROG Strix G16', 'Asus', 'G614JVR', 'Active');
END;
DECLARE @P_ASUSROG INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-ASUSROG');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'ROG-G16-I9-16-1TB')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_ASUSROG, 'ROG-G16-I9-16-1TB', N'Eclipse Gray i9/16GB/1TB', 'Eclipse Gray', '1TB', 2.50, 34000000, 39990000, 24, N'Core i9-14900HX, RTX 4060 8GB, 16 inch QHD+ 240Hz');
END;

-- 12. Asus Zenbook 14 OLED
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-ASUSZEN')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatLaptop, 'PRD-ASUSZEN', N'Asus Zenbook 14 OLED', 'Asus', 'UX3405MA', 'Active');
END;
DECLARE @P_ASUSZEN INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-ASUSZEN');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'ZEN14-U7-16-1TB')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_ASUSZEN, 'ZEN14-U7-16-1TB', N'Ponder Blue Ultra 7/16GB', 'Ponder Blue', '1TB', 1.20, 26000000, 30990000, 24, N'Intel Core Ultra 7 155H, 14 inch 3K OLED 120Hz');
END;

-- 13. iPad Air 11 M2 Wi-Fi
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-IPADAIR')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatTablet, 'PRD-IPADAIR', N'iPad Air 11 M2 Wi-Fi', 'Apple', 'MUWC3', 'Active');
END;
DECLARE @P_IPADAIR INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-IPADAIR');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'IPADAIR-M2-128-BLU')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_IPADAIR, 'IPADAIR-M2-128-BLU', N'Xanh Blue 128GB', 'Blue', '128GB', 0.46, 14000000, 16990000, 12, N'Apple M2 chip, Liquid Retina 11 inch, Apple Pencil Pro support');
END;

-- 14. iPad Pro 11 M4 OLED
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-IPADPRO')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatTablet, 'PRD-IPADPRO', N'iPad Pro 11 M4 OLED', 'Apple', 'MVV83', 'Active');
END;
DECLARE @P_IPADPRO INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-IPADPRO');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'IPADPRO-M4-256-SLV')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_IPADPRO, 'IPADPRO-M4-256-SLV', N'Bạc 256GB', 'Silver', '256GB', 0.44, 24500000, 28990000, 12, N'Apple M4 chip, Ultra Retina XDR OLED, siêu mỏng 5.3mm');
END;

-- 15. Samsung Galaxy Tab S9
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-SSTABS9')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatTablet, 'PRD-SSTABS9', N'Samsung Galaxy Tab S9', 'Samsung', 'SM-X710', 'Active');
END;
DECLARE @P_SSTABS9 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-SSTABS9');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'TABS9-128-BEI')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_SSTABS9, 'TABS9-128-BEI', N'Kem Beige 128GB', 'Beige', '128GB', 0.50, 16000000, 19990000, 12, N'Dynamic AMOLED 2X 11 inch 120Hz, S-Pen kèm máy, IP68');
END;

-- 16. Sony WH-1000XM5
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-SONYWH')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatAudio, 'PRD-SONYWH', N'Sony WH-1000XM5', 'Sony', 'WH-1000XM5', 'Active');
END;
DECLARE @P_SONYWH INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-SONYWH');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'SONY-WH1000XM5-BLK')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_SONYWH, 'SONY-WH1000XM5-BLK', N'Đen ANC Cao Cấp', 'Black', 'N/A', 0.25, 6500000, 8490000, 12, N'Chống ồn đỉnh cao Auto NC Optimizer, 30h pin, LDAC');
END;

-- 17. Sony WF-1000XM5
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-SONYWF')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatAudio, 'PRD-SONYWF', N'Sony WF-1000XM5', 'Sony', 'WF-1000XM5', 'Active');
END;
DECLARE @P_SONYWF INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-SONYWF');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'SONY-WF1000XM5-SLV')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_SONYWF, 'SONY-WF1000XM5-SLV', N'Bạc True Wireless ANC', 'Silver', 'N/A', 0.06, 4800000, 6290000, 12, N'2 chip xử lý V2 & QN2e, màng loa Dynamic Driver X, IPX4');
END;

-- 18. AirPods Pro 2 USB-C
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-APPODSPRO')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatAudio, 'PRD-APPODSPRO', N'AirPods Pro 2 USB-C', 'Apple', 'MTJV3', 'Active');
END;
DECLARE @P_APPODSPRO INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-APPODSPRO');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'APPODS-PRO2-USBC')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_APPODSPRO, 'APPODS-PRO2-USBC', N'Trắng MagSafe USB-C', 'White', 'N/A', 0.06, 4700000, 5990000, 12, N'Chip Apple H2, Chủ động khử tiếng ồn 2x, Adaptive Audio');
END;

-- 19. Dell UltraSharp U2724D
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-DELLU27')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMonitor, 'PRD-DELLU27', N'Dell UltraSharp U2724D', 'Dell', 'U2724D', 'Active');
END;
DECLARE @P_DELLU27 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-DELLU27');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'DELL-U2724D-2K120')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_DELLU27, 'DELL-U2724D-2K120', N'Bạc 27 inch 2K 120Hz', 'Platinum Silver', 'N/A', 6.55, 8200000, 10490000, 36, N'27 inch QHD (2560x1440), IPS Black, 120Hz, 100% sRGB & 98% DCI-P3');
END;

-- 20. Asus TUF Gaming VG279QL3A
IF NOT EXISTS (SELECT 1 FROM Product WHERE ProductCode = 'PRD-ASUSVG27')
BEGIN
    INSERT INTO Product (CategoryId, ProductCode, ProductName, Brand, Model, Status)
    VALUES (@CatMonitor, 'PRD-ASUSVG27', N'Asus TUF Gaming VG279QL3A', 'Asus', 'VG279QL3A', 'Active');
END;
DECLARE @P_ASUSVG27 INT = (SELECT Id FROM Product WHERE ProductCode = 'PRD-ASUSVG27');
IF NOT EXISTS (SELECT 1 FROM ProductVariant WHERE SKU = 'ASUS-VG27-FHD180')
BEGIN
    INSERT INTO ProductVariant (ProductId, SKU, VariantName, Color, Capacity, WeightKg, CostPrice, SellingPrice, WarrantyMonths, Specs)
    VALUES (@P_ASUSVG27, 'ASUS-VG27-FHD180', N'Đen 27 inch FHD 180Hz', 'Black', 'N/A', 6.20, 4100000, 5290000, 36, N'27 inch Fast IPS, FHD, 180Hz, 1ms GtG, ELMB Sync, DisplayHDR 400');
END;

-- 4. SEED STORAGE BINS (BIN-A1-02 to BIN-A1-10) in Rack 1
DECLARE @RackId INT = (SELECT TOP 1 Id FROM StorageRack WHERE RackCode = 'RACK-A1');
IF @RackId IS NOT NULL
BEGIN
    DECLARE @i INT = 2;
    WHILE @i <= 10
    BEGIN
        DECLARE @binCode NVARCHAR(50) = 'BIN-A1-' + RIGHT('00' + CAST(@i AS VARCHAR(2)), 2);
        DECLARE @binName NVARCHAR(100) = N'Vị trí lưu kho A1-' + RIGHT('00' + CAST(@i AS VARCHAR(2)), 2);
        IF NOT EXISTS (SELECT 1 FROM StorageBin WHERE BinCode = @binCode)
        BEGIN
            INSERT INTO StorageBin (RackId, BinCode, BinName, MaxWeightKg)
            VALUES (@RackId, @binCode, @binName, 500.00);
        END;
        SET @i = @i + 1;
    END;
END;

-- 5. SEED PRODUCT-SUPPLIER RELATIONS
DECLARE @S_Apple INT = (SELECT Id FROM Supplier WHERE SupplierCode = 'SUP-APPLE');
DECLARE @S_Samsung INT = (SELECT Id FROM Supplier WHERE SupplierCode = 'SUP-SAMSUNG');
DECLARE @S_Sony INT = (SELECT Id FROM Supplier WHERE SupplierCode = 'SUP-SONY');
DECLARE @S_Dell INT = (SELECT Id FROM Supplier WHERE SupplierCode = 'SUP-DELL');
DECLARE @S_Asus INT = (SELECT Id FROM Supplier WHERE SupplierCode = 'SUP-ASUS');

IF @S_Apple IS NOT NULL AND @P_IP15P IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_IP15P AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_IP15P, @S_Apple, 'APL-IP15P', 28000000, 3, 1);
IF @S_Apple IS NOT NULL AND @P_IP15 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_IP15 AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_IP15, @S_Apple, 'APL-IP15', 17500000, 3, 1);
IF @S_Apple IS NOT NULL AND @P_IP16P IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_IP16P AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_IP16P, @S_Apple, 'APL-IP16P', 27000000, 3, 1);
IF @S_Apple IS NOT NULL AND @P_MBP14 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_MBP14 AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_MBP14, @S_Apple, 'APL-MBP14', 42000000, 5, 1);
IF @S_Apple IS NOT NULL AND @P_MBA13 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_MBA13 AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_MBA13, @S_Apple, 'APL-MBA13', 23000000, 3, 1);
IF @S_Apple IS NOT NULL AND @P_IPADAIR IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_IPADAIR AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_IPADAIR, @S_Apple, 'APL-IPADAIR', 14000000, 3, 1);
IF @S_Apple IS NOT NULL AND @P_IPADPRO IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_IPADPRO AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_IPADPRO, @S_Apple, 'APL-IPADPRO', 24500000, 3, 1);
IF @S_Apple IS NOT NULL AND @P_APPODSPRO IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_APPODSPRO AND SupplierId = @S_Apple)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_APPODSPRO, @S_Apple, 'APL-APPODS', 4700000, 2, 1);

IF @S_Samsung IS NOT NULL AND @P_SS24U IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_SS24U AND SupplierId = @S_Samsung)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_SS24U, @S_Samsung, 'SAM-S24U', 26000000, 3, 1);
IF @S_Samsung IS NOT NULL AND @P_SS24 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_SS24 AND SupplierId = @S_Samsung)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_SS24, @S_Samsung, 'SAM-S24', 15500000, 3, 1);
IF @S_Samsung IS NOT NULL AND @P_SSFOLD5 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_SSFOLD5 AND SupplierId = @S_Samsung)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_SSFOLD5, @S_Samsung, 'SAM-FOLD5', 29000000, 4, 1);
IF @S_Samsung IS NOT NULL AND @P_SSTABS9 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_SSTABS9 AND SupplierId = @S_Samsung)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_SSTABS9, @S_Samsung, 'SAM-TABS9', 16000000, 3, 1);

IF @S_Sony IS NOT NULL AND @P_SONYWH IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_SONYWH AND SupplierId = @S_Sony)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_SONYWH, @S_Sony, 'SNY-WH1000', 6500000, 2, 1);
IF @S_Sony IS NOT NULL AND @P_SONYWF IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_SONYWF AND SupplierId = @S_Sony)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_SONYWF, @S_Sony, 'SNY-WF1000', 4800000, 2, 1);

IF @S_Dell IS NOT NULL AND @P_DELLXPS13 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_DELLXPS13 AND SupplierId = @S_Dell)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_DELLXPS13, @S_Dell, 'DEL-XPS13', 35000000, 5, 1);
IF @S_Dell IS NOT NULL AND @P_DELLG15 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_DELLG15 AND SupplierId = @S_Dell)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_DELLG15, @S_Dell, 'DEL-G15', 24000000, 5, 1);
IF @S_Dell IS NOT NULL AND @P_DELLU27 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_DELLU27 AND SupplierId = @S_Dell)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_DELLU27, @S_Dell, 'DEL-U2724D', 8200000, 4, 1);

IF @S_Asus IS NOT NULL AND @P_ASUSROG IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_ASUSROG AND SupplierId = @S_Asus)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_ASUSROG, @S_Asus, 'ASU-G16', 34000000, 4, 1);
IF @S_Asus IS NOT NULL AND @P_ASUSZEN IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_ASUSZEN AND SupplierId = @S_Asus)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_ASUSZEN, @S_Asus, 'ASU-ZEN14', 26000000, 4, 1);
IF @S_Asus IS NOT NULL AND @P_ASUSVG27 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductSupplier WHERE ProductId = @P_ASUSVG27 AND SupplierId = @S_Asus)
    INSERT INTO ProductSupplier (ProductId, SupplierId, SupplierSKU, UnitPrice, LeadTimeDays, IsPreferred) VALUES (@P_ASUSVG27, @S_Asus, 'ASU-VG27', 4100000, 3, 1);

PRINT 'Seed completed successfully!';
