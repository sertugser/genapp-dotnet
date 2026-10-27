using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

public interface IPolicyRepository
{
    void Add(Policy policy, IPolicyDetails details);

    Task<PolicyRecord<TDetails>?> FindAsync<TDetails>(int customerNumber, int policyNumber, CancellationToken cancellationToken = default)
        where TDetails : class, IPolicyDetails;

    // The policy's type letter (M, H, E, C), or null when this customer has no such policy. A delete checks it first:
    // the legacy DELETE ignores the type (lgdpdb01.cbl:189-194), the new system deletes nothing and returns 01 when
    // the type does not match the address (docs/api-design.md section 3).
    Task<string?> FindTypeAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default);

    void MarkUpdated(Policy policy, DateTime lastChangedFromClient);

    // Returns the number of policy rows deleted. 0 means no such policy for this customer; the service turns
    // that into return code 90, as LGDPDB01 does (lgdpdb01.cbl:198-199, docs/api-design.md section 3).
    Task<int> DeleteAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default);
}
