namespace GenApp.Domain.Policies;

public class EndowmentPolicy : IPolicyDetails
{
    public int PolicyNumber { get; set; }
    public Policy? Policy { get; set; }
    public string? Equities { get; set; }
    public string? WithProfits { get; set; }
    public string? ManagedFund { get; set; }
    public string? FundName { get; set; }
    public short? Term { get; set; }
    public int? SumAssured { get; set; }
    public string? LifeAssured { get; set; }
    public string? PaddingData { get; set; }
}
