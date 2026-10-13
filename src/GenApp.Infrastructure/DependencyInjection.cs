using GenApp.Application.Persistence;
using GenApp.Application.Policies;
using GenApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<GenAppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();

        return services;
    }
}
