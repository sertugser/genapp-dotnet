using GenApp.Api.Policies;
using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.CommercialPolicies;

// Commercial policy fields of docs/api-design.md section 4.6, after the common fields of PolicyResponse.
// issueDate, expiryDate and lastChanged come from COMMERCIAL (STARTDATE, RENEWALDATE, REQUESTDATE), as LGIPDB01 reads
// them (17-commercial-inquire.md, note 1); add writes the same values to both tables, so they match POLICY for every
// policy added through the API. The broker fields and payment are the stored values, which the legacy inquiry does not
// read (deliberate deviation, docs/api-design.md section 3).
public sealed record CommercialPolicyResponse : PolicyResponse
{
    private CommercialPolicyResponse(Policy policy, CommercialPolicy commercial)
        : base(policy, commercial.StartDate, commercial.RenewalDate, commercial.RequestDate ?? policy.LastChanged)
    {
    }

    public required string Address { get; init; }
    public required string Postcode { get; init; }
    public required string Latitude { get; init; }
    public required string Longitude { get; init; }
    public required string Customer { get; init; }
    public required string PropertyType { get; init; }
    public required int FirePeril { get; init; }
    public required int FirePremium { get; init; }
    public required int CrimePeril { get; init; }
    public required int CrimePremium { get; init; }
    public required int FloodPeril { get; init; }
    public required int FloodPremium { get; init; }
    public required int WeatherPeril { get; init; }
    public required int WeatherPremium { get; init; }
    public required int Status { get; init; }
    public required string RejectionReason { get; init; }

    public static CommercialPolicyResponse From(PolicyRecord<CommercialPolicy> record)
    {
        var (policy, commercial) = record;
        return new(policy, commercial)
        {
            Address = ResponseText.Of(commercial.Address),
            Postcode = ResponseText.Of(commercial.Zipcode),
            Latitude = ResponseText.Of(commercial.LatitudeN),
            Longitude = ResponseText.Of(commercial.LongitudeW),
            Customer = ResponseText.Of(commercial.Customer),
            PropertyType = ResponseText.Of(commercial.PropertyType),
            FirePeril = commercial.FirePeril ?? 0,
            FirePremium = commercial.FirePremium ?? 0,
            CrimePeril = commercial.CrimePeril ?? 0,
            CrimePremium = commercial.CrimePremium ?? 0,
            FloodPeril = commercial.FloodPeril ?? 0,
            FloodPremium = commercial.FloodPremium ?? 0,
            WeatherPeril = commercial.WeatherPeril ?? 0,
            WeatherPremium = commercial.WeatherPremium ?? 0,
            Status = commercial.Status ?? 0,
            RejectionReason = ResponseText.Of(commercial.RejectionReason),
        };
    }
}
