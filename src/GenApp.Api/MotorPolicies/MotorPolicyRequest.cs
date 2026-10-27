using System.ComponentModel.DataAnnotations;
using GenApp.Domain.Policies;

namespace GenApp.Api.MotorPolicies;

// Request body of motor policy add (docs/api-design.md sections 4.2 and 4.3). Every field must be present, but texts
// may be empty (section 1). Maximum lengths and digits are the COMMAREA field sizes; the numeric COMMAREA fields are
// unsigned PIC 9 (legacy-analysis/use-cases/05-motor-inquire.md, note 8). brokerId has 10 digits in the COMMAREA
// but the column is an integer. policyNumber, customerNumber and lastChanged are not part of the request; if sent
// they are ignored.
public class MotorPolicyRequest
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
    public string? Make { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(15)]
    public string? Model { get; init; }

    [Required, Range(0, 999999)]
    public int? Value { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(7)]
    public string? RegistrationNumber { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(8)]
    public string? Colour { get; init; }

    [Required, Range(0, 9999)]
    public int? Cc { get; init; }

    [Required]
    public DateOnly? ManufactureDate { get; init; }

    [Required, Range(0, 999999)]
    public int? Premium { get; init; }

    [Required, Range(0, 999999)]
    public int? Accidents { get; init; }

    public Policy ToPolicy(int customerNumber) => new()
    {
        CustomerNumber = customerNumber,
        IssueDate = IssueDate,
        ExpiryDate = ExpiryDate,
        BrokerId = BrokerId,
        BrokersReference = BrokersReference,
        Payment = Payment,
    };

    public MotorPolicy ToMotorPolicy() => new()
    {
        Make = Make,
        Model = Model,
        Value = Value,
        RegNumber = RegistrationNumber,
        Colour = Colour,
        Cc = (short?)Cc,
        YearOfManufacture = ManufactureDate,
        Premium = Premium,
        Accidents = Accidents,
    };
}

// Request body of motor policy update: the same fields and the lastChanged value returned by the inquiry, sent back
// unchanged (docs/api-design.md section 5).
public sealed class MotorPolicyUpdateRequest : MotorPolicyRequest
{
    [Required]
    public DateTime? LastChanged { get; init; }

    // The column is "timestamp without time zone" and Npgsql rejects DateTimeKind.Utc for it (ADR 0003 decision 3);
    // a value sent with a "Z" keeps its digits and is compared as they are.
    public DateTime LastChangedValue => DateTime.SpecifyKind(LastChanged!.Value, DateTimeKind.Unspecified);
}
