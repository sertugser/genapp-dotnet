using GenApp.Domain.Claims;
using GenApp.Domain.Customers;
using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;

namespace GenApp.Infrastructure.Persistence;

public class GenAppDbContext(DbContextOptions<GenAppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerSecure> CustomerSecures => Set<CustomerSecure>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<MotorPolicy> MotorPolicies => Set<MotorPolicy>();
    public DbSet<HousePolicy> HousePolicies => Set<HousePolicy>();
    public DbSet<EndowmentPolicy> EndowmentPolicies => Set<EndowmentPolicy>();
    public DbSet<CommercialPolicy> CommercialPolicies => Set<CommercialPolicy>();
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GenAppDbContext).Assembly);
}
