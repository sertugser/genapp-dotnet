using GenApp.Application.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.MotorPolicies;

// Motor policy fields of docs/api-design.md sections 4.2 and 4.3. Texts are trimmed (ADR 0002 decision 6) and an
// empty text is returned as "", never null (section 1). A NULL number is returned as 0, as LGIPDB01 does for the
// broker and payment columns (lgipdb01.cbl:551-553, :588-593). lastChanged is returned in the server's format and
// is sent back unchanged on update (section 5).
public sealed record MotorPolicyResponse(
    int PolicyNumber,
    int CustomerNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    DateTime LastChanged,
    int BrokerId,
    string BrokersReference,
    int Payment,
    string Make,
    string Model,
    int Value,
    string RegistrationNumber,
    string Colour,
    int Cc,
    DateOnly? ManufactureDate,
    int Premium,
    int Accidents)
{
    public static MotorPolicyResponse From(PolicyRecord<MotorPolicy> record)
    {
        var (policy, motor) = record;
        return new(
            policy.PolicyNumber,
            policy.CustomerNumber,
            policy.IssueDate,
            policy.ExpiryDate,
            policy.LastChanged,
            policy.BrokerId ?? 0,
            Text(policy.BrokersReference),
            policy.Payment ?? 0,
            Text(motor.Make),
            Text(motor.Model),
            motor.Value ?? 0,
            Text(motor.RegNumber),
            Text(motor.Colour),
            motor.Cc ?? 0,
            motor.YearOfManufacture,
            motor.Premium ?? 0,
            motor.Accidents ?? 0);
    }

    private static string Text(string? value) => value?.TrimEnd() ?? "";
}
