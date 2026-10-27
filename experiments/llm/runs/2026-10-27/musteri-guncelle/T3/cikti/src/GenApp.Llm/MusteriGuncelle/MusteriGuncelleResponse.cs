namespace GenApp.Llm.MusteriGuncelle;

public sealed class MusteriGuncelleResponse
{
    public int CustomerNumber { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public DateOnly? DateOfBirth { get; init; }
    public string HouseName { get; init; } = "";
    public string HouseNumber { get; init; } = "";
    public string Postcode { get; init; } = "";
    public string PhoneMobile { get; init; } = "";
    public string PhoneHome { get; init; } = "";
    public string Email { get; init; } = "";
}
