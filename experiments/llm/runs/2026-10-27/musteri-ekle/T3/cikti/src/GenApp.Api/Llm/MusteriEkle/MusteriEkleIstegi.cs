using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriEkle;

// customerNumber bilinçli olarak yoktur: gönderilirse yok sayılır (lgacdb01.cbl:285, yeni numara üretilir).
public sealed class MusteriEkleIstegi
{
    [JsonPropertyName("firstName")]
    [MaxLength(10)]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    [MaxLength(20)]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir.
    [JsonPropertyName("dateOfBirth")]
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    [JsonPropertyName("houseName")]
    [MaxLength(20)]
    public string? HouseName { get; set; }

    [JsonPropertyName("houseNumber")]
    [MaxLength(4)]
    public string? HouseNumber { get; set; }

    [JsonPropertyName("postcode")]
    [MaxLength(8)]
    public string? Postcode { get; set; }

    [JsonPropertyName("phoneMobile")]
    [MaxLength(20)]
    public string? PhoneMobile { get; set; }

    [JsonPropertyName("phoneHome")]
    [MaxLength(20)]
    public string? PhoneHome { get; set; }

    [JsonPropertyName("email")]
    [MaxLength(100)]
    public string? Email { get; set; }
}
