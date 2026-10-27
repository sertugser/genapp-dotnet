using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// Müşteri ekle isteği. Uzunluklar lgcmarea.cpy'deki CA-CUSTOMER-REQUEST alanlarındandır.
/// customerNumber gönderilirse yok sayılır (lgacdb01.cbl: numara her zaman yeniden üretilir).
/// </summary>
public sealed class CustomerCreateRequest
{
    [Required(AllowEmptyStrings = true)]
    [MaxLength(10)]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir
    [Required]
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("houseName")]
    public string? HouseName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(4)]
    [JsonPropertyName("houseNumber")]
    public string? HouseNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(8)]
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("phoneMobile")]
    public string? PhoneMobile { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("phoneHome")]
    public string? PhoneHome { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(100)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
