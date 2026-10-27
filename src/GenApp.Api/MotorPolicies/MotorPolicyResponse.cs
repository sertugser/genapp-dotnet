using GenApp.Api.Policies;
using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.MotorPolicies;

// Motor policy fields of docs/api-design.md section 4.3, after the common fields of PolicyResponse.
public sealed record MotorPolicyResponse : PolicyResponse
{
    private MotorPolicyResponse(Policy policy) : base(policy)
    {
    }

    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int Value { get; init; }
    public required string RegistrationNumber { get; init; }
    public required string Colour { get; init; }
    public required int Cc { get; init; }
    public required DateOnly? ManufactureDate { get; init; }
    public required int Premium { get; init; }
    public required int Accidents { get; init; }

    public static MotorPolicyResponse From(PolicyRecord<MotorPolicy> record)
    {
        var (policy, motor) = record;
        return new(policy)
        {
            Make = ResponseText.Of(motor.Make),
            Model = ResponseText.Of(motor.Model),
            Value = motor.Value ?? 0,
            RegistrationNumber = ResponseText.Of(motor.RegNumber),
            Colour = ResponseText.Of(motor.Colour),
            Cc = motor.Cc ?? 0,
            ManufactureDate = motor.YearOfManufacture,
            Premium = motor.Premium ?? 0,
            Accidents = motor.Accidents ?? 0,
        };
    }
}
