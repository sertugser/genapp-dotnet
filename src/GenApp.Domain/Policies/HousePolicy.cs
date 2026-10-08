namespace GenApp.Domain.Policies;

public class HousePolicy
{
    public int PolicyNumber { get; set; }
    public string? PropertyType { get; set; }
    public short? Bedrooms { get; set; }
    public int? Value { get; set; }
    public string? HouseName { get; set; }
    public string? HouseNumber { get; set; }
    public string? Postcode { get; set; }
}
