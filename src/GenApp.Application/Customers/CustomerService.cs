using GenApp.Application.Persistence;
using GenApp.Domain.Customers;

namespace GenApp.Application.Customers;

// Replaces the customer business programs LGACUS01, LGICUS01 and LGUCUS01. None of them has a business rule of its
// own; the return codes come from the data programs. The 98 length checks have no counterpart: there is no
// COMMAREA (docs/api-design.md section 3).
public sealed class CustomerService(ICustomerRepository customers, IUnitOfWork unitOfWork)
{
    // Every new customer gets the same password hash, change count 0 and state 'N' (lgacdb01.cbl:181-184).
    private const string DefaultPasswordHash = "5732fec825535eeafb8fac50fee3a8aa";

    // LGACUS01 → LGACDB01 → LGACDB02 (legacy-analysis/use-cases/01-customer-add.md). The number given by the caller
    // is ignored and a new one is generated (lgacdb01.cbl:285). The legacy program ignores a failed CUSTOMER_SECURE
    // insert and still returns 00 (lgacdb02.cbl:178-182); here both rows are written in one transaction or neither
    // is (ADR 0003 decision 2, NFR-04).
    public async Task<OperationResult<Customer>> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        customer.CustomerNumber = 0;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);   // customer_number is generated here

        customers.AddSecurity(new CustomerSecure
        {
            CustomerNumber = customer.CustomerNumber,
            CustomerPass = DefaultPasswordHash,
            StateIndicator = "N",
            PassChanges = 0,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Customer>.Success(customer);
    }

    // LGICUS01 → LGICDB01 (legacy-analysis/use-cases/02-customer-inquire.md): 00 when the row is found, 01 when it
    // is not (lgicdb01.cbl:192-198).
    public async Task<OperationResult<Customer>> InquireAsync(int customerNumber, CancellationToken cancellationToken = default)
    {
        var customer = await customers.FindAsync(customerNumber, cancellationToken);
        return customer is null
            ? OperationResult<Customer>.Failure(ReturnCodes.NotFound)
            : OperationResult<Customer>.Success(customer);
    }

    // LGUCUS01 → LGUCDB01 (legacy-analysis/use-cases/03-customer-update.md): all nine columns are overwritten, no
    // optimistic locking. 01 when there is no such customer (lgucdb01.cbl:173-174); in the legacy system the VSAM
    // step then abends instead, the new system returns 01 on purpose (docs/api-design.md section 3).
    public async Task<OperationResult<Customer>> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var updated = await customers.UpdateAsync(customer, cancellationToken);
        return updated == 0
            ? OperationResult<Customer>.Failure(ReturnCodes.NotFound)
            : OperationResult<Customer>.Success(customer);
    }
}
