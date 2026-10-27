using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri cevabı (API tasarımı 4.1).
/// </summary>
public sealed class MusteriSorgulaResponse
{
    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    // DateOnly, System.Text.Json ile yyyy-MM-dd biçiminde yazılır.
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; init; }

    [JsonPropertyName("houseName")]
    public string HouseName { get; init; } = string.Empty;

    [JsonPropertyName("houseNumber")]
    public string HouseNumber { get; init; } = string.Empty;

    [JsonPropertyName("postcode")]
    public string Postcode { get; init; } = string.Empty;

    [JsonPropertyName("phoneMobile")]
    public string PhoneMobile { get; init; } = string.Empty;

    [JsonPropertyName("phoneHome")]
    public string PhoneHome { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
