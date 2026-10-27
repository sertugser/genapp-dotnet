using System.ComponentModel.DataAnnotations;
using GenApp.Api.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.HousePolicies;

// Request body of house policy add (docs/api-design.md section 4.4; common fields in PolicyRequest). Maximum lengths
// and digits are the COMMAREA field sizes (legacy-analysis/use-cases/08-house-add.md). An empty property type,
// 0 bedrooms and 0 value are accepted, as in the legacy program (docs/api-design.md section 3).
public class HousePolicyRequest : PolicyRequest
{
    [Required(AllowEmptyStrings = true), MaxLength(15)]
    public string? PropertyType { get; init; }

    [Required, Range(0, 999)]
    public int? Bedrooms { get; init; }

    [Required, Range(0, 99999999)]
    public int? Value { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(20)]
    public string? HouseName { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(4)]
    public string? HouseNumber { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(8)]
    public string? Postcode { get; init; }

    public HousePolicy ToHousePolicy() => new()
    {
        PropertyType = PropertyType,
        Bedrooms = (short?)Bedrooms,
        Value = Value,
        HouseName = HouseName,
        HouseNumber = HouseNumber,
        Postcode = Postcode,
    };
}

// Request body of house policy update: the same fields and lastChanged (see PolicyRequest).
public sealed class HousePolicyUpdateRequest : HousePolicyRequest
{
    [Required]
    public DateTime? LastChanged { get; init; }

    public DateTime LastChangedValue => ColumnValue(LastChanged);
}
