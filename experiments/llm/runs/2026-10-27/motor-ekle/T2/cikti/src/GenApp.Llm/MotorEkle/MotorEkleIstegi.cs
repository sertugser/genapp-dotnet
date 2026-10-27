using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Motor poliçesi ekleme isteği. Uzunluk ve hane sınırları lgcmarea.cpy'deki alanlardır.
/// "Yalnızca cevapta" olan alanlar (policyNumber, customerNumber) ve lastChanged tanımlı değildir;
/// gönderilirse yok sayılır (LGAPDB01 LASTCHANGED için CURRENT TIMESTAMP kullanır).
/// </summary>
public sealed class MotorEkleIstegi
{
    // CA-ISSUE-DATE PIC X(10)
    [Required]
    public DateOnly? IssueDate { get; set; }

    // CA-EXPIRY-DATE PIC X(10)
    [Required]
    public DateOnly? ExpiryDate { get; set; }

    // CA-BROKERID PIC 9(10)
    [Required]
    [Range(0L, 9_999_999_999L)]
    public long? BrokerId { get; set; }

    // CA-BROKERSREF PIC X(10)
    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? BrokersReference { get; set; }

    // CA-PAYMENT PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Payment { get; set; }

    // CA-M-MAKE PIC X(15)
    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Make { get; set; }

    // CA-M-MODEL PIC X(15)
    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Model { get; set; }

    // CA-M-VALUE PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Value { get; set; }

    // CA-M-REGNUMBER PIC X(7)
    [Required(AllowEmptyStrings = true)]
    [StringLength(7)]
    public string? RegistrationNumber { get; set; }

    // CA-M-COLOUR PIC X(8)
    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Colour { get; set; }

    // CA-M-CC PIC 9(4)
    [Required]
    [Range(0, 9_999)]
    public int? Cc { get; set; }

    // CA-M-MANUFACTURED PIC X(10)
    [Required]
    public DateOnly? ManufactureDate { get; set; }

    // CA-M-PREMIUM PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Premium { get; set; }

    // CA-M-ACCIDENTS PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Accidents { get; set; }
}
