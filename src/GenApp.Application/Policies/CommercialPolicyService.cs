using GenApp.Application.Persistence;
using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

// Replaces LGAPOL01, LGIPOL01 and LGDPOL01 for commercial policies (request IDs 01ACOM, 01ICOM, 02ICOM, 03ICOM,
// 05ICOM, 01DCOM; legacy-analysis/use-cases/16-18). There is no commercial update in the legacy system, so
// UpdateAsync is not offered (docs/api-design.md section 2.5).
public sealed class CommercialPolicyService(IPolicyRepository policies, IUnitOfWork unitOfWork)
    : PolicyService<CommercialPolicy>(policies, unitOfWork)
{
    protected override string PolicyType => "C";

    protected override void CopyChanges(CommercialPolicy details, CommercialPolicy changes) =>
        throw new NotSupportedException("The legacy system has no commercial policy update.");

    // 02ICOM: 01 when there is no commercial policy with this number (lgipdb01.cbl:784-790).
    public async Task<OperationResult<PolicyRecord<CommercialPolicy>>> InquireByPolicyAsync(
        int policyNumber, CancellationToken cancellationToken = default)
    {
        var record = await Policies.FindCommercialAsync(policyNumber, cancellationToken);
        return record is null || record.Policy.PolicyType != PolicyType
            ? OperationResult<PolicyRecord<CommercialPolicy>>.Failure(ReturnCodes.NotFound)
            : OperationResult<PolicyRecord<CommercialPolicy>>.Success(record);
    }

    // 03ICOM. The legacy cursor keeps only the last row; the new system returns them all, and an empty list when there
    // is none, as the legacy 00 (docs/api-design.md section 3, deviation table).
    public async Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> InquireByCustomerAsync(
        int customerNumber, CancellationToken cancellationToken = default) =>
        OfType(await Policies.FindCommercialByCustomerAsync(customerNumber, cancellationToken));

    // 05ICOM, the same rules as 03ICOM.
    public async Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> InquireByPostcodeAsync(
        string postcode, CancellationToken cancellationToken = default) =>
        OfType(await Policies.FindCommercialByPostcodeAsync(postcode, cancellationToken));

    private IReadOnlyList<PolicyRecord<CommercialPolicy>> OfType(IReadOnlyList<PolicyRecord<CommercialPolicy>> records) =>
        records.Where(r => r.Policy.PolicyType == PolicyType).ToList();
}
