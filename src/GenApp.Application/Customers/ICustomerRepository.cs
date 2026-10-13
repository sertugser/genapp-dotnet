using GenApp.Domain.Customers;

namespace GenApp.Application.Customers;

public interface ICustomerRepository
{
    // LGICDB01: SELECT ... FROM CUSTOMER WHERE CUSTOMERNUMBER = ... (lgicdb01.cbl:169-190). Null when there is no row.
    Task<Customer?> FindAsync(int customerNumber, CancellationToken cancellationToken = default);
}
