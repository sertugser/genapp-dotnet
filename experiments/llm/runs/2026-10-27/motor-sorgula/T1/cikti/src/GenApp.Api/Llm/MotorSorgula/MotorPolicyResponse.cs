namespace GenApp.Llm.MotorSorgula;

public sealed class MotorPolicyResponse
{
    private const string LastChangedFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    public int PolicyNumber { get; init; }
    public int CustomerNumber { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public string LastChanged { get; init; } = "";
    public long BrokerId { get; init; }
    public string BrokersReference { get; init; } = "";
    public int Payment { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int Value { get; init; }
    public string RegistrationNumber { get; init; } = "";
    public string Colour { get; init; } = "";
    public int Cc { get; init; }
    public DateOnly ManufactureDate { get; init; }
    public int Premium { get; init; }
    public int Accidents { get; init; }

    public static MotorPolicyResponse From(MotorPolicyRow row) => new()
    {
        PolicyNumber = row.PolicyNumber,
        CustomerNumber = row.CustomerNumber,
        IssueDate = row.IssueDate,
        ExpiryDate = row.ExpiryDate,
        LastChanged = row.LastChanged.ToString(LastChangedFormat, System.Globalization.CultureInfo.InvariantCulture),
        BrokerId = row.BrokerId,
        BrokersReference = Trim(row.BrokersReference),
        Payment = row.Payment,
        Make = Trim(row.Make),
        Model = Trim(row.Model),
        Value = row.Value,
        RegistrationNumber = Trim(row.RegistrationNumber),
        Colour = Trim(row.Colour),
        Cc = row.Cc,
        ManufactureDate = row.ManufactureDate,
        Premium = row.Premium,
        Accidents = row.Accidents
    };

    private static string Trim(string? value) => (value ?? "").TrimEnd();
}
