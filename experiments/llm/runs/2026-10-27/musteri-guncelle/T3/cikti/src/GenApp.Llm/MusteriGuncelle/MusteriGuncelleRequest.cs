using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MusteriGuncelle;

// customerNumber istek gövdesinde yoktur; gönderilirse JSON çözümlemesinde yok sayılır (API tasarımı 1).
public sealed class MusteriGuncelleRequest
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir.
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? HouseName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(4)]
    public string? HouseNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Postcode { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneMobile { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneHome { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(100)]
    public string? Email { get; set; }
}
