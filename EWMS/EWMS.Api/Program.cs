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

// Cộng tồn bằng SQL thật, dùng chung DbContext và transaction duyệt phiếu.
builder.Services.AddScoped<IStockService, StockReceivingService>();
builder.Services.AddEwmsAuthentication(builder.Configuration, builder.Environment);
bool developmentSeed = builder.Configuration.GetValue<bool>("GoodsReceipts:SeedDevelopmentData");
if (!builder.Environment.IsDevelopment() && developmentSeed)
    throw new InvalidOperationException("Development seeding is forbidden outside Development.");
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

// Có thể đặt bản React build vào wwwroot để chạy cùng API.
bool hasFrontend = File.Exists(Path.Combine(app.Environment.WebRootPath
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html"));
if (hasFrontend)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
// API lạ phải trả 404; chỉ URL trang React mới được trả về index.html.
app.MapFallback("/api/{**path}", () => Results.NotFound(new { message = "API endpoint not found." }));
if (hasFrontend) app.MapFallbackToFile("index.html");

app.Run();
