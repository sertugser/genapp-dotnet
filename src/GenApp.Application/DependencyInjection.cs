using GenApp.Application.Customers;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();

        return services;
    }
}
