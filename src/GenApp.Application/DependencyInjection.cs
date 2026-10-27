using GenApp.Application.Customers;
using GenApp.Application.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();
        services.AddScoped<MotorPolicyService>();
        services.AddScoped<HousePolicyService>();

        return services;
    }
}
