namespace GenApp.Llm.MotorSorgula;

public sealed class MotorPolicyRow
{
    public string? PolicyType { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public DateTime LastChanged { get; init; }
    public int? BrokerId { get; init; }
    public string? BrokersReference { get; init; }
    public int? Payment { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Value { get; init; }
    public string? RegNumber { get; init; }
    public string? Colour { get; init; }
    public int? Cc { get; init; }
    public DateOnly? YearOfManufacture { get; init; }
    public int? Premium { get; init; }
    public int? Accidents { get; init; }
}
