using System.ComponentModel.DataAnnotations;
using GenApp.Domain.Customers;

namespace GenApp.Api.Customers;

// Request body of customer add and update (docs/api-design.md section 4.1). Every field must be present, but texts
// may be empty, as the legacy system stores them unchecked (section 1). Maximum lengths are the COMMAREA lengths;
// a longer or missing value is rejected with 400 by [ApiController]. customerNumber is not part of the request; if
// sent it is ignored.
public sealed class CustomerRequest
{
    [Required(AllowEmptyStrings = true), MaxLength(10)]
    public string? FirstName { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(20)]
    public string? LastName { get; init; }

    [Required]
    public DateOnly? DateOfBirth { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(20)]
    public string? HouseName { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(4)]
    public string? HouseNumber { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(8)]
    public string? Postcode { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(20)]
    public string? PhoneMobile { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(20)]
    public string? PhoneHome { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(100)]
    public string? Email { get; init; }

    public Customer ToCustomer(int customerNumber = 0) => new()
    {
        CustomerNumber = customerNumber,
        FirstName = FirstName,
        LastName = LastName,
        DateOfBirth = DateOfBirth,
        HouseName = HouseName,
        HouseNumber = HouseNumber,
        // The legacy screen upper-cases the postcode before calling the business program (lgtestc1.cbl:126-127 for
        // add, :188-189 for update); the API replaces the screen, so it does the same (docs/api-design.md section 1).
        Postcode = Postcode?.ToUpperInvariant(),
        PhoneMobile = PhoneMobile,
        PhoneHome = PhoneHome,
        EmailAddress = Email,
    };
}
