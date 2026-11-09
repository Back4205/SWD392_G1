using EWMS.Api.Interfaces;

namespace EWMS.Api.Repositories;

public static class ReceivingRepositoryRegistration
{
    public static IServiceCollection AddReceivingRepositories(this IServiceCollection services)
    {
        services.AddScoped<IInboundRequestRepository, InboundRequestRepository>();
        services.AddScoped(typeof(GenericRepository<>));
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<GoodsReceiptRepository>();
        services.AddScoped<GoodsReceiptScanRepository>();
        services.AddScoped<SerialNumberRepository>();
        services.AddScoped<StockRepository>();
        return services;
    }
}
