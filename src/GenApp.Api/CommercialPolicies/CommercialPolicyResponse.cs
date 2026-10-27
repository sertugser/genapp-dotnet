using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.CommercialPolicies;

// Commercial policy fields of docs/api-design.md sections 4.2 and 4.6. Texts are trimmed (ADR 0002 decision 6) and an
// empty text is returned as "", never null (section 1); a NULL number is returned as 0.
// issueDate, expiryDate and lastChanged come from COMMERCIAL (STARTDATE, RENEWALDATE, REQUESTDATE), as LGIPDB01 reads
// them (17-commercial-inquire.md, note 1); add writes the same values to both tables, so they match POLICY for every
// policy added through the API. The broker fields and payment are the stored values, which the legacy inquiry does not
// read (deliberate deviation, docs/api-design.md section 3).
public sealed record CommercialPolicyResponse(
    int PolicyNumber,
    int CustomerNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    DateTime LastChanged,
    int BrokerId,
    string BrokersReference,
    int Payment,
    string Address,
    string Postcode,
    string Latitude,
    string Longitude,
    string Customer,
    string PropertyType,
    int FirePeril,
    int FirePremium,
    int CrimePeril,
    int CrimePremium,
    int FloodPeril,
    int FloodPremium,
    int WeatherPeril,
    int WeatherPremium,
    int Status,
    string RejectionReason)
{
    public static CommercialPolicyResponse From(PolicyRecord<CommercialPolicy> record)
    {
        var (policy, commercial) = record;
        return new(
            policy.PolicyNumber,
            policy.CustomerNumber,
            commercial.StartDate,
            commercial.RenewalDate,
            commercial.RequestDate ?? policy.LastChanged,
            policy.BrokerId ?? 0,
            Text(policy.BrokersReference),
            policy.Payment ?? 0,
            Text(commercial.Address),
            Text(commercial.Zipcode),
            Text(commercial.LatitudeN),
            Text(commercial.LongitudeW),
            Text(commercial.Customer),
            Text(commercial.PropertyType),
            commercial.FirePeril ?? 0,
            commercial.FirePremium ?? 0,
            commercial.CrimePeril ?? 0,
            commercial.CrimePremium ?? 0,
            commercial.FloodPeril ?? 0,
            commercial.FloodPremium ?? 0,
            commercial.WeatherPeril ?? 0,
            commercial.WeatherPremium ?? 0,
            commercial.Status ?? 0,
            Text(commercial.RejectionReason));
    }

    private static string Text(string? value) => value?.TrimEnd() ?? "";
}
