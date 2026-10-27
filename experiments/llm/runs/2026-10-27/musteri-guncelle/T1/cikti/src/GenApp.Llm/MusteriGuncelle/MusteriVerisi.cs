namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Veritabanına yazılacak müşteri alanları (LGUCDB01'deki UPDATE CUSTOMER SET listesi).
/// </summary>
public sealed record MusteriVerisi(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string EmailAddress);
