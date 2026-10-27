using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// PUT /api/customers/{customerNumber} istek gövdesi. Uzunluklar lgcmarea.cpy'deki alan uzunluklarıdır.
/// </summary>
public sealed class MusteriGuncelleIstek
{
    // Yalnızca cevapta olan alan: gönderilirse yok sayılır, istek reddedilmez (API tasarımı, bölüm 1).
    [JsonPropertyName("customerNumber")]
    public JsonElement? CustomerNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir.
    [Required]
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("houseName")]
    public string? HouseName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(4)]
    [JsonPropertyName("houseNumber")]
    public string? HouseNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("phoneMobile")]
    public string? PhoneMobile { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("phoneHome")]
    public string? PhoneHome { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(100)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
