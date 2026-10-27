using GenApp.Api.Policies;
using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.EndowmentPolicies;

// Endowment policy fields of docs/api-design.md section 4.5, after the common fields of PolicyResponse. paddingData is
// not part of the API (section 4.5).
public sealed record EndowmentPolicyResponse : PolicyResponse
{
    private EndowmentPolicyResponse(Policy policy) : base(policy)
    {
    }

    public required string WithProfits { get; init; }
    public required string Equities { get; init; }
    public required string ManagedFund { get; init; }
    public required string FundName { get; init; }
    public required int Term { get; init; }
    public required int SumAssured { get; init; }
    public required string LifeAssured { get; init; }

    public static EndowmentPolicyResponse From(PolicyRecord<EndowmentPolicy> record)
    {
        var (policy, endowment) = record;
        return new(policy)
        {
            WithProfits = ResponseText.Of(endowment.WithProfits),
            Equities = ResponseText.Of(endowment.Equities),
            ManagedFund = ResponseText.Of(endowment.ManagedFund),
            FundName = ResponseText.Of(endowment.FundName),
            Term = endowment.Term ?? 0,
            SumAssured = endowment.SumAssured ?? 0,
            LifeAssured = ResponseText.Of(endowment.LifeAssured),
        };
    }
}
