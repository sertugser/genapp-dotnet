using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

public interface IPolicyRepository
{
    void Add(Policy policy, IPolicyDetails details);

    Task<PolicyRecord<TDetails>?> FindAsync<TDetails>(int customerNumber, int policyNumber, CancellationToken cancellationToken = default)
        where TDetails : class, IPolicyDetails;

    void MarkUpdated(Policy policy, DateTime lastChangedFromClient);

    Task DeleteAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default);
}
