using GenApp.Application.Customers;
using GenApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace GenApp.Infrastructure.Persistence;

// Replaces the Db2 customer programs LGACDB01, LGICDB01 and LGUCDB01.
internal sealed class CustomerRepository(GenAppDbContext db) : ICustomerRepository
{
    // LGICDB01 (lgicdb01.cbl:169-190). Read only, so the row is not tracked.
    public async Task<Customer?> FindAsync(int customerNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.CustomerNumber == customerNumber, cancellationToken);
        }
        catch (Exception e) when (DatabaseErrors.IsDatabaseError(e))
        {
            throw DatabaseErrors.Translate(e);
        }
    }
}
