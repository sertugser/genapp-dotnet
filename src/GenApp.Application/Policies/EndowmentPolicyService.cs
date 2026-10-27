using GenApp.Application.Persistence;
using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

// Replaces LGAPOL01, LGIPOL01, LGUPOL01 and LGDPOL01 for endowment policies (request IDs 01AEND, 01IEND, 01UEND,
// 01DEND; legacy-analysis/use-cases/12-15). Inquire returns 01 when there is no endowment row (lgipdb01.cbl:361-367,
// :421-423).
public sealed class EndowmentPolicyService(IPolicyRepository policies, IUnitOfWork unitOfWork)
    : PolicyService<EndowmentPolicy>(policies, unitOfWork)
{
    protected override string PolicyType => "E";

    // lgupdb01.cbl:395-405. PADDINGDATA is not in the SET list, so it keeps the stored value.
    protected override void CopyChanges(EndowmentPolicy endowment, EndowmentPolicy changes)
    {
        endowment.WithProfits = changes.WithProfits;
        endowment.Equities = changes.Equities;
        endowment.ManagedFund = changes.ManagedFund;
        endowment.FundName = changes.FundName;
        endowment.Term = changes.Term;
        endowment.SumAssured = changes.SumAssured;
        endowment.LifeAssured = changes.LifeAssured;
    }
}
