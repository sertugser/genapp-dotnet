using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.EndowmentPolicies;

// Endowment policy fields of docs/api-design.md sections 4.2 and 4.5. Texts are trimmed (ADR 0002 decision 6) and an
// empty text is returned as "", never null (section 1). A NULL number is returned as 0, as LGIPDB01 does for the
// broker and payment columns (lgipdb01.cbl:397-402). paddingData is not part of the API (section 4.5). lastChanged is
// returned in the server's format and is sent back unchanged on update (section 5).
public sealed record EndowmentPolicyResponse(
    int PolicyNumber,
    int CustomerNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    DateTime LastChanged,
    int BrokerId,
    string BrokersReference,
    int Payment,
    string WithProfits,
    string Equities,
    string ManagedFund,
    string FundName,
    int Term,
    int SumAssured,
    string LifeAssured)
{
    public static EndowmentPolicyResponse From(PolicyRecord<EndowmentPolicy> record)
    {
        var (policy, endowment) = record;
        return new(
            policy.PolicyNumber,
            policy.CustomerNumber,
            policy.IssueDate,
            policy.ExpiryDate,
            policy.LastChanged,
            policy.BrokerId ?? 0,
            Text(policy.BrokersReference),
            policy.Payment ?? 0,
            Text(endowment.WithProfits),
            Text(endowment.Equities),
            Text(endowment.ManagedFund),
            Text(endowment.FundName),
            endowment.Term ?? 0,
            endowment.SumAssured ?? 0,
            Text(endowment.LifeAssured));
    }

    private static string Text(string? value) => value?.TrimEnd() ?? "";
}
