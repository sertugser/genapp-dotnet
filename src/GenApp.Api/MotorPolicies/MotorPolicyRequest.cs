using System.ComponentModel.DataAnnotations;
using GenApp.Api.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.MotorPolicies;

// Request body of motor policy add (docs/api-design.md section 4.3; common fields in PolicyRequest). Maximum lengths
// and digits are the COMMAREA field sizes (legacy-analysis/use-cases/05-motor-inquire.md, note 8).
public class MotorPolicyRequest : PolicyRequest
{
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

// Request body of motor policy update: the same fields and lastChanged (see PolicyRequest).
public sealed class MotorPolicyUpdateRequest : MotorPolicyRequest
{
    [Required]
    public DateTime? LastChanged { get; init; }

    public DateTime LastChangedValue => ColumnValue(LastChanged);
}
