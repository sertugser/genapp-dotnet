namespace GenApp.Domain.Policies;

public class CommercialPolicy
{
    public int PolicyNumber { get; set; }
    public DateTime? RequestDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public string? Address { get; set; }
    public string? Zipcode { get; set; }
    public string? LatitudeN { get; set; }
    public string? LongitudeW { get; set; }
    public string? Customer { get; set; }
    public string? PropertyType { get; set; }
    public short? FirePeril { get; set; }
    public int? FirePremium { get; set; }
    public short? CrimePeril { get; set; }
    public int? CrimePremium { get; set; }
    public short? FloodPeril { get; set; }
    public int? FloodPremium { get; set; }
    public short? WeatherPeril { get; set; }
    public int? WeatherPremium { get; set; }
    public short? Status { get; set; }
    public string? RejectionReason { get; set; }
}
