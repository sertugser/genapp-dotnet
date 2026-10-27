namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Müşteri cevabı. Metinlerin sonundaki boşluklar kırpılır, boş metin "" olarak döner.
/// </summary>
public sealed class CustomerResponse
{
    public int CustomerNumber { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public string HouseName { get; init; } = string.Empty;
    public string HouseNumber { get; init; } = string.Empty;
    public string Postcode { get; init; } = string.Empty;
    public string PhoneMobile { get; init; } = string.Empty;
    public string PhoneHome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
