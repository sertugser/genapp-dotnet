using GenApp.Application.Persistence;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Poliçe ekle (eski LGAPDB01). Sonuç repository'den değil, ayrı bir kapsamda veritabanından doğrudan okunur.
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryAddTests(PostgresFixture veritabani)
{
    // Poliçe numarasını veritabanı üretir (lgapdb01.cbl:279 DEFAULT, :309 IDENTITY_VAL_LOCAL); tür satırı aynı numarayı alır.
    [Fact]
    public async Task Motor_eklenince_numara_uretilir_ve_motor_satiri_ayni_numarayla_yazilir()
    {
        var musteri = await veritabani.MusteriEkleAsync();

        var police = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy { Make = "FORD", Model = "KA", Value = 8500 });

        Assert.True(police.PolicyNumber >= 1000001);
        var motor = await veritabani.OkuAsync(db => db.MotorPolicies.AsNoTracking().SingleAsync(m => m.PolicyNumber == police.PolicyNumber));
        Assert.Equal("FORD", motor.Make);
        Assert.Equal(8500, motor.Value);
        var kayit = await veritabani.OkuAsync(db => db.Policies.AsNoTracking().SingleAsync(p => p.PolicyNumber == police.PolicyNumber));
        Assert.Equal(musteri, kayit.CustomerNumber);
        Assert.Equal("BR1", kayit.BrokersReference);
    }

    // ADR 0003 Karar 3: saat uygulamadan gelir, mikrosaniyeye kırpılır, Kind Unspecified olur. Saat 14:05:09.1234567.
    [Fact]
    public async Task Last_changed_saatin_mikrosaniyeye_kirpilmis_halidir()
    {
        var musteri = await veritabani.MusteriEkleAsync();

        var police = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy());

        var beklenen = new DateTime(2026, 10, 13, 14, 5, 9).AddTicks(1234560);
        Assert.Equal(beklenen, police.LastChanged);
        Assert.Equal(DateTimeKind.Unspecified, police.LastChanged.Kind);
        var kayit = await veritabani.OkuAsync(db => db.Policies.AsNoTracking().SingleAsync(p => p.PolicyNumber == police.PolicyNumber));
        Assert.Equal(beklenen, kayit.LastChanged);
    }

    // Tür harfini veri katmanı koyar (lgapdb01.cbl:188-200).
    public static TheoryData<IPolicyDetails, string> Turler => new()
    {
        { new EndowmentPolicy(), "E" },
        { new HousePolicy(), "H" },
        { new MotorPolicy(), "M" },
        { new CommercialPolicy(), "C" },
    };

    [Theory]
    [MemberData(nameof(Turler))]
    public async Task Tur_harfi_police_turune_gore_yazilir(IPolicyDetails tur, string beklenenHarf)
    {
        var musteri = await veritabani.MusteriEkleAsync();

        var police = await veritabani.PoliceEkleAsync(musteri, tur);

        var kayit = await veritabani.OkuAsync(db => db.Policies.AsNoTracking().SingleAsync(p => p.PolicyNumber == police.PolicyNumber));
        Assert.Equal(beklenenHarf, kayit.PolicyType);
        Assert.Equal(police.PolicyNumber, tur.PolicyNumber);
    }

    // Olmayan müşteri: Db2 yabancı anahtar hatası -530 → '70' (lgapdb01.cbl:295-296).
    // 999999999: pratikte atanmayan numara (ADR 0005'teki 02-02 testiyle aynı seçim).
    [Fact]
    public async Task Olmayan_musteriye_ekleme_ReferencedRecordNotFoundException_firlatir()
    {
        await Assert.ThrowsAsync<ReferencedRecordNotFoundException>(
            () => veritabani.PoliceEkleAsync(999999999, new MotorPolicy()));
    }
}
