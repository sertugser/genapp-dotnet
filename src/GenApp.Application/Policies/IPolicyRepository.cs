using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

public interface IPolicyRepository
{
    void Add(Policy policy, IPolicyDetails details);

    Task<PolicyRecord<TDetails>?> FindAsync<TDetails>(int customerNumber, int policyNumber, CancellationToken cancellationToken = default)
        where TDetails : class, IPolicyDetails;

    // LGIPDB01 02ICOM: the commercial policy with this number, whoever its customer is (lgipdb01.cbl:778-781).
    Task<PolicyRecord<CommercialPolicy>?> FindCommercialAsync(int policyNumber, CancellationToken cancellationToken = default);

    // LGIPDB01 03ICOM: every commercial policy of this customer (Cust_Cursor, lgipdb01.cbl:89-118). Empty when none.
    Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> FindCommercialByCustomerAsync(
        int customerNumber, CancellationToken cancellationToken = default);

    // LGIPDB01 05ICOM: every commercial policy with exactly this postcode (Zip_Cursor, lgipdb01.cbl:120-149).
    Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> FindCommercialByPostcodeAsync(
        string postcode, CancellationToken cancellationToken = default);

    // The policy's type letter (M, H, E, C), or null when this customer has no such policy. A delete checks it first:
    // the legacy DELETE ignores the type (lgdpdb01.cbl:189-194), the new system deletes nothing and returns 01 when
    // the type does not match the address (docs/api-design.md section 3).
    Task<string?> FindTypeAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default);

    void MarkUpdated(Policy policy, DateTime lastChangedFromClient);

    // Returns the number of policy rows deleted. 0 means no such policy for this customer; the service turns
    // that into return code 90, as LGDPDB01 does (lgdpdb01.cbl:198-199, docs/api-design.md section 3).
    Task<int> DeleteAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default);
}
