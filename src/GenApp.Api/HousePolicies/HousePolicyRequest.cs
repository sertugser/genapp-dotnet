using System.ComponentModel.DataAnnotations;
using GenApp.Domain.Policies;

namespace GenApp.Api.HousePolicies;

// Request body of house policy add (docs/api-design.md sections 4.2 and 4.4). Every field must be present, but texts
// may be empty (section 1). Maximum lengths and digits are the COMMAREA field sizes; the numeric COMMAREA fields are
// unsigned PIC 9 (legacy-analysis/use-cases/08-house-add.md). An empty property type, 0 bedrooms and 0 value are
// accepted, as in the legacy program (docs/api-design.md section 3). brokerId has 10 digits in the COMMAREA but the
// column is an integer. policyNumber, customerNumber and lastChanged are not part of the request; if sent they are
// ignored.
public class HousePolicyRequest
{
    [Required]
    public DateOnly? IssueDate { get; init; }

    [Required]
    public DateOnly? ExpiryDate { get; init; }

    [Required, Range(0, int.MaxValue)]
    public int? BrokerId { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(10)]
    public string? BrokersReference { get; init; }

    [Required, Range(0, 999999)]
    public int? Payment { get; init; }

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

    public Policy ToPolicy(int customerNumber) => new()
    {
        CustomerNumber = customerNumber,
        IssueDate = IssueDate,
        ExpiryDate = ExpiryDate,
        BrokerId = BrokerId,
        BrokersReference = BrokersReference,
        Payment = Payment,
    };

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

// Request body of house policy update: the same fields and the lastChanged value returned by the inquiry, sent back
// unchanged (docs/api-design.md section 5).
public sealed class HousePolicyUpdateRequest : HousePolicyRequest
{
    [Required]
    public DateTime? LastChanged { get; init; }

    // The column is "timestamp without time zone" and Npgsql rejects DateTimeKind.Utc for it (ADR 0003 decision 3);
    // a value sent with a "Z" keeps its digits and is compared as they are.
    public DateTime LastChangedValue => DateTime.SpecifyKind(LastChanged!.Value, DateTimeKind.Unspecified);
}
