using GenApp.Domain.Customers;

namespace GenApp.Api.Customers;

// Customer fields of docs/api-design.md section 4.1. Texts go through ResponseText.
public sealed record CustomerResponse(
    int CustomerNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string Email)
{
    public static CustomerResponse From(Customer customer) => new(
        customer.CustomerNumber,
        ResponseText.Of(customer.FirstName),
        ResponseText.Of(customer.LastName),
        customer.DateOfBirth,
        ResponseText.Of(customer.HouseName),
        ResponseText.Of(customer.HouseNumber),
        ResponseText.Of(customer.Postcode),
        ResponseText.Of(customer.PhoneMobile),
        ResponseText.Of(customer.PhoneHome),
        ResponseText.Of(customer.EmailAddress));
}
