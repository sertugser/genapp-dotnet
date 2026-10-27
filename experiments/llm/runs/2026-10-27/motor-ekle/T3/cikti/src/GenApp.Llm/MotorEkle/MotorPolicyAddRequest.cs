using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MotorEkle;

// policyNumber, customerNumber ve lastChanged istekte gelirse yok sayılır (yalnızca cevapta / sunucu üretir).
public sealed class MotorPolicyAddRequest
{
    [Required]
    public DateOnly? IssueDate { get; set; }

    [Required]
    public DateOnly? ExpiryDate { get; set; }

    // Eklemede CA-LASTCHANGED kullanılmaz; değer veritabanında CURRENT TIMESTAMP ile üretilir (lgapdb01 INSERT-POLICY).
    public string? LastChanged { get; set; }

    [Required]
    [Range(typeof(long), "0", "9999999999")]
    public long? BrokerId { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? BrokersReference { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Payment { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Make { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Model { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Value { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(7)]
    public string? RegistrationNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Colour { get; set; }

    [Required]
    [Range(0, 9999)]
    public int? Cc { get; set; }

    [Required]
    public DateOnly? ManufactureDate { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Premium { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Accidents { get; set; }
}
