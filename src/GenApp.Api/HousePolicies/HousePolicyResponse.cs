using GenApp.Api.Policies;
using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.HousePolicies;

// House policy fields of docs/api-design.md section 4.4, after the common fields of PolicyResponse.
public sealed record HousePolicyResponse : PolicyResponse
{
    private HousePolicyResponse(Policy policy) : base(policy)
    {
    }

    public required string PropertyType { get; init; }
    public required int Bedrooms { get; init; }
    public required int Value { get; init; }
    public required string HouseName { get; init; }
    public required string HouseNumber { get; init; }
    public required string Postcode { get; init; }

    public static HousePolicyResponse From(PolicyRecord<HousePolicy> record)
    {
        var (policy, house) = record;
        return new(policy)
        {
            PropertyType = ResponseText.Of(house.PropertyType),
            Bedrooms = house.Bedrooms ?? 0,
            Value = house.Value ?? 0,
            HouseName = ResponseText.Of(house.HouseName),
            HouseNumber = ResponseText.Of(house.HouseNumber),
            Postcode = ResponseText.Of(house.Postcode),
        };
    }
}
