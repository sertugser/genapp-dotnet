using GenApp.Application.Customers;
using GenApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace GenApp.Infrastructure.Persistence;

// Replaces the Db2 customer programs LGACDB01, LGACDB02, LGICDB01 and LGUCDB01.
internal sealed class CustomerRepository(GenAppDbContext db) : ICustomerRepository
{
    public void Add(Customer customer) => db.Customers.Add(customer);

    public void AddSecurity(CustomerSecure security) => db.CustomerSecures.Add(security);

    // LGICDB01 (lgicdb01.cbl:169-190). Read only, so the row is not tracked.
    public Task<Customer?> FindAsync(int customerNumber, CancellationToken cancellationToken = default) =>
        DatabaseErrors.RunAsync(() => db.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerNumber == customerNumber, cancellationToken));

    // LGUCDB01 (lgucdb01.cbl:152-174): one UPDATE without reading first; no partial update, an empty value overwrites
    // the stored one. ExecuteUpdateAsync runs at once: the update is the operation's only write.
    public Task<int> UpdateAsync(Customer customer, CancellationToken cancellationToken = default) =>
        DatabaseErrors.RunAsync(() => db.Customers
            .Where(c => c.CustomerNumber == customer.CustomerNumber)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.FirstName, customer.FirstName)
                .SetProperty(c => c.LastName, customer.LastName)
                .SetProperty(c => c.DateOfBirth, customer.DateOfBirth)
                .SetProperty(c => c.HouseName, customer.HouseName)
                .SetProperty(c => c.HouseNumber, customer.HouseNumber)
                .SetProperty(c => c.Postcode, customer.Postcode)
                .SetProperty(c => c.PhoneMobile, customer.PhoneMobile)
                .SetProperty(c => c.PhoneHome, customer.PhoneHome)
                .SetProperty(c => c.EmailAddress, customer.EmailAddress),
                cancellationToken));
}
