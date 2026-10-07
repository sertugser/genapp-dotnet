namespace GenApp.Domain.Customers;

public class Customer
{
    public int CustomerNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? HouseName { get; set; }
    public string? HouseNumber { get; set; }
    public string? Postcode { get; set; }
    public string? PhoneHome { get; set; }
    public string? PhoneMobile { get; set; }
    public string? EmailAddress { get; set; }
}
