using GenApp.Domain.Legacy;

namespace GenApp.UnitTests.Legacy;

/// <summary>
/// Yaz → oku → yeniden yaz: okuma, yazmayla aynı alanları aynı sırayla kullanmalıdır. Okumada iki alanın yeri
/// karışırsa ya da bir alan atlanırsa yeniden yazılan metin ilkinden farklı olur. Ayrıca her türde birkaç
/// özellik elle yazılmış değerle karşılaştırılır.
/// </summary>
public sealed class CommareaRoundTripTests
{
    private static T GidisDonus<T>(T commarea) where T : Commarea, new()
    {
        var metin = commarea.ToFixedWidth();

        var okunan = Commarea.Parse<T>(metin);

        Assert.Equal(metin, okunan.ToFixedWidth());
        return okunan;
    }

    [Fact]
    public void Musteri_gidis_donus()
    {
        var okunan = GidisDonus(new CustomerCommarea
        {
            RequestId = "01ICUS", ReturnCode = 0, CustomerNumber = 2,
            FirstName = "Scott", LastName = "Tracey", DateOfBirth = "1965-09-30", HouseName = "Tracey Island",
            HouseNumber = "1", Postcode = "TB14TV", NumberOfPolicies = 3, PhoneMobile = "07700 900123",
            PhoneHome = "001 911911", EmailAddress = "REFROOM@TBHOLDINGS.COM",
        });

        Assert.Equal(2, okunan.CustomerNumber);
        Assert.Equal("Scott", okunan.FirstName);
        Assert.Equal(3, okunan.NumberOfPolicies);
        Assert.Equal("REFROOM@TBHOLDINGS.COM", okunan.EmailAddress);
    }

    [Fact]
    public void Musteri_guvenlik_gidis_donus()
    {
        var okunan = GidisDonus(new CustomerSecurityCommarea
        {
            RequestId = "02ACUS", CustomerNumber = 1000001,
            Password = "5f4dcc3b5aa765d61d8327deb882cf99", Count = "12", State = "N",
        });

        Assert.Equal("5f4dcc3b5aa765d61d8327deb882cf99", okunan.Password);
        Assert.Equal("12", okunan.Count);
        Assert.Equal("N", okunan.State);
    }

    [Fact]
    public void Motor_gidis_donus()
    {
        var okunan = GidisDonus(new MotorPolicyCommarea
        {
            RequestId = "01IMOT", ReturnCode = 1, CustomerNumber = 1, PolicyNumber = 1,
            IssueDate = "2026-01-01", ExpiryDate = "2027-01-01", LastChanged = "2026-10-15 10:22:31.123456",
            BrokerId = 42, BrokersReference = "BR1", Payment = 100,
            Make = "FORD", Model = "KA", Value = 8500, RegistrationNumber = "LL60LOO", Colour = "ORANGE",
            Cc = 1000, ManufactureDate = "1999-07-19", Premium = 450, Accidents = 2,
        });

        Assert.Equal(1, okunan.ReturnCode);
        Assert.Equal(1, okunan.PolicyNumber);
        Assert.Equal("2026-10-15 10:22:31.123456", okunan.LastChanged);
        Assert.Equal(42, okunan.BrokerId);
        Assert.Equal("FORD", okunan.Make);
        Assert.Equal("KA", okunan.Model);
        Assert.Equal(8500, okunan.Value);
        Assert.Equal(2, okunan.Accidents);
    }

    [Fact]
    public void Hayat_gidis_donus()
    {
        var okunan = GidisDonus(new EndowmentPolicyCommarea
        {
            RequestId = "01IEND", CustomerNumber = 3, PolicyNumber = 5,
            WithProfits = "Y", Equities = "N", ManagedFund = "N", FundName = "SHEPPA", Term = 10,
            SumAssured = 50000, LifeAssured = "Shep", PaddingData = "EXTRA",
        });

        Assert.Equal("Y", okunan.WithProfits);
        Assert.Equal(10, okunan.Term);
        Assert.Equal(50000, okunan.SumAssured);
        Assert.Equal("EXTRA", okunan.PaddingData);
    }

    [Fact]
    public void Konut_gidis_donus()
    {
        var okunan = GidisDonus(new HousePolicyCommarea
        {
            RequestId = "01IHOU", CustomerNumber = 4, PolicyNumber = 6,
            PropertyType = "HOUSE", Bedrooms = 5, Value = 1500000, HouseName = "", HouseNumber = "   5",
            Postcode = "SO211UP",
        });

        Assert.Equal(5, okunan.Bedrooms);
        Assert.Equal(1500000, okunan.Value);
        Assert.Equal("", okunan.HouseName);
        Assert.Equal("   5", okunan.HouseNumber);
    }

    [Fact]
    public void Ticari_gidis_donus()
    {
        var okunan = GidisDonus(new CommercialPolicyCommarea
        {
            RequestId = "01ICOM", CustomerNumber = 1, PolicyNumber = 10,
            Address = "HURSLEY PARK", Postcode = "SO212JN", Latitude = "51.026701", Longitude = "-1.398903",
            Customer = "IBM", PropertyType = "OFFICES", FirePeril = 3, FirePremium = 8000, CrimePeril = 3,
            CrimePremium = 25000, FloodPeril = 5, FloodPremium = 2000, WeatherPeril = 4, WeatherPremium = 5000,
            Status = 1, RejectionReason = "ACTIVE",
        });

        Assert.Equal("-1.398903", okunan.Longitude);
        Assert.Equal(25000, okunan.CrimePremium);
        Assert.Equal(4, okunan.WeatherPeril);
        Assert.Equal("ACTIVE", okunan.RejectionReason);
    }

    [Fact]
    public void Hasar_gidis_donus()
    {
        var okunan = GidisDonus(new ClaimCommarea
        {
            RequestId = "01ICLM", PolicyNumber = 1, ClaimNumber = 7, ClaimDate = "2026-03-01", Paid = 1500,
            Value = 2000, Cause = "FLOOD", Observations = "KITCHEN",
        });

        Assert.Equal(7, okunan.ClaimNumber);
        Assert.Equal(1500, okunan.Paid);
        Assert.Equal("KITCHEN", okunan.Observations);
    }
}
