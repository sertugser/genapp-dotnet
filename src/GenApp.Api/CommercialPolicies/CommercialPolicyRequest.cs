using System.ComponentModel.DataAnnotations;
using GenApp.Domain.Policies;

namespace GenApp.Api.CommercialPolicies;

// Request body of commercial policy add (docs/api-design.md sections 4.2 and 4.6). Every field must be present, but
// texts may be empty (section 1). Maximum lengths and digits are the COMMAREA field sizes (lgcmarea.cpy:73-92); the
// numeric COMMAREA fields are unsigned PIC 9. The commercial fields are not checked further, as in the legacy program
// (16-commercial-add.md, note 7): latitude and longitude are texts. brokerId has 10 digits in the COMMAREA but the
// column is an integer. policyNumber, customerNumber and lastChanged are not part of the request; if sent they are
// ignored. There is no update request: the legacy system has no commercial update (section 2.5).
public sealed class CommercialPolicyRequest
{
    [Required]
    public DateOnly? IssueDate { get; init; }

    [Required]
    public DateOnly? ExpiryDate { get; init; }

    [Required, Range(0, int.MaxValue)]
    public int? BrokerId { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(10)]
    public string? BrokersReference { get; init; }

    [Required, Range(0, 999999)]
    public int? Payment { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(255)]
    public string? Address { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(8)]
    public string? Postcode { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(11)]
    public string? Latitude { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(11)]
    public string? Longitude { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(255)]
    public string? Customer { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(255)]
    public string? PropertyType { get; init; }

    [Required, Range(0, 9999)]
    public int? FirePeril { get; init; }

    [Required, Range(0, 99999999)]
    public int? FirePremium { get; init; }

    [Required, Range(0, 9999)]
    public int? CrimePeril { get; init; }

    [Required, Range(0, 99999999)]
    public int? CrimePremium { get; init; }

    [Required, Range(0, 9999)]
    public int? FloodPeril { get; init; }

    [Required, Range(0, 99999999)]
    public int? FloodPremium { get; init; }

    [Required, Range(0, 9999)]
    public int? WeatherPeril { get; init; }

    [Required, Range(0, 99999999)]
    public int? WeatherPremium { get; init; }

    [Required, Range(0, 9999)]
    public int? Status { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(255)]
    public string? RejectionReason { get; init; }

    public Policy ToPolicy(int customerNumber) => new()
    {
        CustomerNumber = customerNumber,
        IssueDate = IssueDate,
        ExpiryDate = ExpiryDate,
        BrokerId = BrokerId,
        BrokersReference = BrokersReference,
        Payment = Payment,
    };

    public CommercialPolicy ToCommercialPolicy() => new()
    {
        Address = Address,
        Zipcode = Postcode,
        LatitudeN = Latitude,
        LongitudeW = Longitude,
        Customer = Customer,
        PropertyType = PropertyType,
        FirePeril = (short?)FirePeril,
        FirePremium = FirePremium,
        CrimePeril = (short?)CrimePeril,
        CrimePremium = CrimePremium,
        FloodPeril = (short?)FloodPeril,
        FloodPremium = FloodPremium,
        WeatherPeril = (short?)WeatherPeril,
        WeatherPremium = WeatherPremium,
        Status = (short?)Status,
        RejectionReason = RejectionReason,
    };
}
