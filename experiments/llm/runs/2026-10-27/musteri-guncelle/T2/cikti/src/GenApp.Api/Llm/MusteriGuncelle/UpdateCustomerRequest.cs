using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// PUT /api/customers/{customerNumber} istek gövdesi. Uzunluklar lgcmarea.cpy'deki alan uzunluklarıdır.
/// Gövdede gönderilen customerNumber yok sayılır (bilinmeyen alan olarak deserileştirilmez).
/// </summary>
public sealed class UpdateCustomerRequest
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? FirstName { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? LastName { get; init; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına gitmeden 400 ile reddedilir
    [Required]
    public DateOnly? DateOfBirth { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? HouseName { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(4)]
    public string? HouseNumber { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Postcode { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneMobile { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneHome { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(100)]
    public string? Email { get; init; }
}
