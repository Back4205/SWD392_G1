using EWMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using EWMS.Api.Interfaces;
using EWMS.Api.Repositories;
using EWMS.Api.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<EWMSDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EWMSDatabase")
    ));
// Add services to the container.

builder.Services.AddControllers();
// Inbound Request module
builder.Services.AddScoped<IInboundRequestRepository, InboundRequestRepository>();
builder.Services.AddScoped<IInboundRequestService, InboundRequestService>();
// Goods Receipt: AddScoped tạo service cho mỗi HTTP request.
// DbContext cũng Scoped, nên các service trong request dùng chung transaction.
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<GoodsReceiptService>();
builder.Services.AddScoped<SerialNumberService>();
builder.Services.AddScoped<GoodsReceiptApprovalService>();
builder.Services.AddScoped<IInboundService, InboundReceivingService>();

// Người 2 chưa có StockService: dùng bản thay thế chỉ ở môi trường Development.
// Khi ghép nhóm, thay dòng đăng ký này bằng IStockService, StockService thật.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IStockService, MockStockService>();
}
else
{
    throw new InvalidOperationException("Register the real IStockService before running Goods Receipt outside Development.");
}

// Chặn cấu hình giả lập user/seed khi chạy ngoài môi trường Development.
bool developmentHeader = builder.Configuration.GetValue<bool>("GoodsReceipts:UseDevelopmentUserHeader");
bool developmentSeed = builder.Configuration.GetValue<bool>("GoodsReceipts:SeedDevelopmentData");
if (!builder.Environment.IsDevelopment() && (developmentHeader || developmentSeed))
{
    throw new InvalidOperationException("Goods receipt development settings are forbidden outside Development.");
}
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Tùy chọn tạo dataset demo; mặc định tắt và chỉ dùng database development riêng.
if (developmentSeed)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<EWMSDbContext>();
    await GoodsReceiptDemoSeeder.SeedAsync(context, app.Environment, app.Logger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
