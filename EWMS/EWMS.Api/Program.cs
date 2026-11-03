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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
