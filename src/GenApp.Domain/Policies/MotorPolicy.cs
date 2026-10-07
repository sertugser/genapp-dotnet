namespace GenApp.Domain.Policies;

public class MotorPolicy
{
    public int PolicyNumber { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Value { get; set; }
    public string? RegNumber { get; set; }
    public string? Colour { get; set; }
    public short? Cc { get; set; }
    public DateOnly? YearOfManufacture { get; set; }
    public int? Premium { get; set; }
    public int? Accidents { get; set; }
}
