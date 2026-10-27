namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// Motor poliçesi sorgulama cevabı (API tasarımı 4.2 ve 4.3).
/// </summary>
public sealed record MotorPolicyResponse
{
    public int PolicyNumber { get; init; }
    public int CustomerNumber { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string LastChanged { get; init; } = "";
    public int BrokerId { get; init; }
    public string BrokersReference { get; init; } = "";
    public int Payment { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int Value { get; init; }
    public string RegistrationNumber { get; init; } = "";
    public string Colour { get; init; } = "";
    public int Cc { get; init; }
    public DateOnly? ManufactureDate { get; init; }
    public int Premium { get; init; }
    public int Accidents { get; init; }
}
