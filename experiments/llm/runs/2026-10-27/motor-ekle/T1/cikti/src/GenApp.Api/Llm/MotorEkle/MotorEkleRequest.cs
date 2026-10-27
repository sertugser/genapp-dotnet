using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Motor poliçesi ekleme isteği (01AMOT). policyNumber, customerNumber ve lastChanged
/// yalnızca cevapta olduğundan burada yoktur; gönderilirse yok sayılır.
/// </summary>
public sealed class MotorEkleRequest
{
    // Db2 DATE sütunu geçersiz/boş tarihi kabul etmez (90); hatalı girdi veritabanına ulaşmadan reddedilir.
    [Required]
    public DateOnly? IssueDate { get; set; }

    [Required]
    public DateOnly? ExpiryDate { get; set; }

    // CA-BROKERID PIC 9(10); integer sütununa sığmayan değer veritabanı hatası (90) olur.
    [Range(0L, 9_999_999_999L)]
    public long BrokerId { get; set; }

    [MaxLength(10)]
    public string? BrokersReference { get; set; }

    [Range(0, 999_999)]
    public int Payment { get; set; }

    [MaxLength(15)]
    public string? Make { get; set; }

    [MaxLength(15)]
    public string? Model { get; set; }

    [Range(0, 999_999)]
    public int Value { get; set; }

    [MaxLength(7)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(8)]
    public string? Colour { get; set; }

    [Range(0, 9_999)]
    public int Cc { get; set; }

    // CA-M-MANUFACTURED, Db2 DATE sütununa (YEAROFMANUFACTURE) yazılır; geçersiz değer Db2'de 90 olurdu.
    [Required]
    public DateOnly? ManufactureDate { get; set; }

    [Range(0, 999_999)]
    public int Premium { get; set; }

    [Range(0, 999_999)]
    public int Accidents { get; set; }
}
