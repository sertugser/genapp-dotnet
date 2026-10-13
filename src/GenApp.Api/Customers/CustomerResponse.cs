using GenApp.Domain.Customers;

namespace GenApp.Api.Customers;

// Customer fields of docs/api-design.md section 4.1. Texts are trimmed (ADR 0002 decision 6) and an empty text is
// returned as "", never null (section 1).
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
        Text(customer.FirstName),
        Text(customer.LastName),
        customer.DateOfBirth,
        Text(customer.HouseName),
        Text(customer.HouseNumber),
        Text(customer.Postcode),
        Text(customer.PhoneMobile),
        Text(customer.PhoneHome),
        Text(customer.EmailAddress));

    private static string Text(string? value) => value?.TrimEnd() ?? "";
}
