namespace GenApp.Domain.Policies;

public class EndowmentPolicy
{
    public int PolicyNumber { get; set; }
    public string? Equities { get; set; }
    public string? WithProfits { get; set; }
    public string? ManagedFund { get; set; }
    public string? FundName { get; set; }
    public short? Term { get; set; }
    public int? SumAssured { get; set; }
    public string? LifeAssured { get; set; }
    public string? PaddingData { get; set; }
}
