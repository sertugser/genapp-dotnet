namespace GenApp.Domain.Claims;

public class Claim
{
    public int ClaimNumber { get; set; }
    public int PolicyNumber { get; set; }
    public DateOnly? ClaimDate { get; set; }
    public int? Paid { get; set; }
    public int? Value { get; set; }
    public string? Cause { get; set; }
    public string? Observations { get; set; }
}
