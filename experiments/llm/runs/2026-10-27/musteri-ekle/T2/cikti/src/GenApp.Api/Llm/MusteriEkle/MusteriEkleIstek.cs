using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MusteriEkle;

// customerNumber istekte yoktur; gönderilirse JSON çözümlemesinde yok sayılır (lgacdb01.cbl:285).
public sealed class MusteriEkleIstek
{
    [StringLength(10)] // CA-FIRST-NAME PIC X(10)
    public string? FirstName { get; set; }

    [StringLength(20)] // CA-LAST-NAME PIC X(20)
    public string? LastName { get; set; }

    // CA-DOB PIC X(10); biçim denetimi controller'da yapılır (yyyy-MM-dd).
    public string? DateOfBirth { get; set; }

    [StringLength(20)] // CA-HOUSE-NAME PIC X(20)
    public string? HouseName { get; set; }

    [StringLength(4)] // CA-HOUSE-NUM PIC X(4)
    public string? HouseNumber { get; set; }

    [StringLength(8)] // CA-POSTCODE PIC X(8)
    public string? Postcode { get; set; }

    [StringLength(20)] // CA-PHONE-MOBILE PIC X(20)
    public string? PhoneMobile { get; set; }

    [StringLength(20)] // CA-PHONE-HOME PIC X(20)
    public string? PhoneHome { get; set; }

    [StringLength(100)] // CA-EMAIL-ADDRESS PIC X(100)
    public string? Email { get; set; }
}
