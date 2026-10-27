namespace GenApp.Llm.MotorEkle;

public sealed record MotorPolicyResponse(
    int PolicyNumber,
    int CustomerNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string LastChanged,
    long BrokerId,
    string BrokersReference,
    int Payment,
    string Make,
    string Model,
    int Value,
    string RegistrationNumber,
    string Colour,
    int Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);
