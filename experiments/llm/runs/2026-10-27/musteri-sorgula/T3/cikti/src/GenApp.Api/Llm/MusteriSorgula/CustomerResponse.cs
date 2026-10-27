using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri cevabı (API tasarımı 4.1).
/// </summary>
public sealed class CustomerResponse
{
    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = "";

    /// <summary>yyyy-MM-dd biçiminde.</summary>
    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("houseName")]
    public string HouseName { get; init; } = "";

    [JsonPropertyName("houseNumber")]
    public string HouseNumber { get; init; } = "";

    [JsonPropertyName("postcode")]
    public string Postcode { get; init; } = "";

    [JsonPropertyName("phoneMobile")]
    public string PhoneMobile { get; init; } = "";

    [JsonPropertyName("phoneHome")]
    public string PhoneHome { get; init; } = "";

    [JsonPropertyName("email")]
    public string Email { get; init; } = "";
}
