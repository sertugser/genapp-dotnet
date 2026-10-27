using System.Text.Json.Serialization;

namespace GenApp.Llm.MotorGuncelle;

public sealed class MotorPolicyResponse
{
    [JsonPropertyName("policyNumber")]
    public int PolicyNumber { get; init; }

    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("issueDate")]
    public DateOnly? IssueDate { get; init; }

    [JsonPropertyName("expiryDate")]
    public DateOnly? ExpiryDate { get; init; }

    [JsonPropertyName("lastChanged")]
    public string LastChanged { get; init; } = "";

    [JsonPropertyName("brokerId")]
    public int? BrokerId { get; init; }

    [JsonPropertyName("brokersReference")]
    public string BrokersReference { get; init; } = "";

    [JsonPropertyName("payment")]
    public int? Payment { get; init; }

    [JsonPropertyName("make")]
    public string Make { get; init; } = "";

    [JsonPropertyName("model")]
    public string Model { get; init; } = "";

    [JsonPropertyName("value")]
    public int? Value { get; init; }

    [JsonPropertyName("registrationNumber")]
    public string RegistrationNumber { get; init; } = "";

    [JsonPropertyName("colour")]
    public string Colour { get; init; } = "";

    [JsonPropertyName("cc")]
    public int? Cc { get; init; }

    [JsonPropertyName("manufactureDate")]
    public DateOnly? ManufactureDate { get; init; }

    [JsonPropertyName("premium")]
    public int? Premium { get; init; }

    [JsonPropertyName("accidents")]
    public int? Accidents { get; init; }
}
