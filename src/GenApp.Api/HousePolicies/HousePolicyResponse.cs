using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.HousePolicies;

// House policy fields of docs/api-design.md sections 4.2 and 4.4. Texts are trimmed (ADR 0002 decision 6) and an
// empty text is returned as "", never null (section 1). A NULL number is returned as 0, as LGIPDB01 does for the
// broker and payment columns (lgipdb01.cbl:460-462, :494-499). lastChanged is returned in the server's format and
// is sent back unchanged on update (section 5).
public sealed record HousePolicyResponse(
    int PolicyNumber,
    int CustomerNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    DateTime LastChanged,
    int BrokerId,
    string BrokersReference,
    int Payment,
    string PropertyType,
    int Bedrooms,
    int Value,
    string HouseName,
    string HouseNumber,
    string Postcode)
{
    public static HousePolicyResponse From(PolicyRecord<HousePolicy> record)
    {
        var (policy, house) = record;
        return new(
            policy.PolicyNumber,
            policy.CustomerNumber,
            policy.IssueDate,
            policy.ExpiryDate,
            policy.LastChanged,
            policy.BrokerId ?? 0,
            Text(policy.BrokersReference),
            policy.Payment ?? 0,
            Text(house.PropertyType),
            house.Bedrooms ?? 0,
            house.Value ?? 0,
            Text(house.HouseName),
            Text(house.HouseNumber),
            Text(house.Postcode));
    }

    private static string Text(string? value) => value?.TrimEnd() ?? "";
}
