using GenApp.Domain.Customers;

namespace GenApp.Application.Customers;

// Replaces the customer business programs LGACUS01, LGICUS01 and LGUCUS01.
public sealed class CustomerService(ICustomerRepository customers)
{
    // LGICUS01 → LGICDB01 (legacy-analysis/use-cases/02-customer-inquire.md). LGICUS01 has no business rule of its
    // own; LGICDB01 returns 00 when the row is found and 01 when it is not (lgicdb01.cbl:192-198). The 98 length
    // checks have no counterpart: there is no COMMAREA (docs/api-design.md section 3).
    public async Task<OperationResult<Customer>> InquireAsync(int customerNumber, CancellationToken cancellationToken = default)
    {
        var customer = await customers.FindAsync(customerNumber, cancellationToken);
        return customer is null
            ? OperationResult<Customer>.Failure(ReturnCodes.NotFound)
            : OperationResult<Customer>.Success(customer);
    }
}
