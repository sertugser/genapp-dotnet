using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MotorGuncelle;

/// <summary>
/// Motor poliçesi güncelleme isteği. customerNumber ve policyNumber yalnızca cevaptadır;
/// gövdede gelirse bilinmeyen alan olarak yok sayılır.
/// </summary>
public sealed class MotorPolicyUpdateRequest
{
    [Required]
    [JsonPropertyName("issueDate")]
    public DateOnly? IssueDate { get; set; }

    [Required]
    [JsonPropertyName("expiryDate")]
    public DateOnly? ExpiryDate { get; set; }

    // CA-LASTCHANGED: sorgulamada dönen değer aynen geri gönderilir
    [Required(AllowEmptyStrings = true)]
    [JsonPropertyName("lastChanged")]
    public string? LastChanged { get; set; }

    [Required]
    [Range(typeof(long), "0", "9999999999")]
    [JsonPropertyName("brokerId")]
    public long? BrokerId { get; set; }

    [MaxLength(10)]
    [JsonPropertyName("brokersReference")]
    public string? BrokersReference { get; set; }

    // Korunan davranış: POLICY güncellemesinin SET listesinde PAYMENT yok (lgupdb01.cbl:319-325); değer yok sayılır
    [Range(0, 999999)]
    [JsonPropertyName("payment")]
    public int? Payment { get; set; }

    [MaxLength(15)]
    [JsonPropertyName("make")]
    public string? Make { get; set; }

    [MaxLength(15)]
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [Required]
    [Range(0, 999999)]
    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [MaxLength(7)]
    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }

    [MaxLength(8)]
    [JsonPropertyName("colour")]
    public string? Colour { get; set; }

    [Required]
    [Range(0, 9999)]
    [JsonPropertyName("cc")]
    public int? Cc { get; set; }

    [Required]
    [JsonPropertyName("manufactureDate")]
    public DateOnly? ManufactureDate { get; set; }

    [Required]
    [Range(0, 999999)]
    [JsonPropertyName("premium")]
    public int? Premium { get; set; }

    [Required]
    [Range(0, 999999)]
    [JsonPropertyName("accidents")]
    public int? Accidents { get; set; }
}
