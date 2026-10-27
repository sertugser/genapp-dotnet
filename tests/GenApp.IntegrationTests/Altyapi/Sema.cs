namespace GenApp.IntegrationTests.Altyapi;

/// <summary>Bir sütunun adının ne kadar kesin bilindiği ve testlerde nasıl kullanıldığı.</summary>
internal enum SutunDurumu
{
    /// <summary>
    /// Adı docs/database-schema.md'de geçiyor ya da Db2 adından belgedeki kuralla (DS:21) türetiliyor; testler bu sütunu
    /// doğrular. Smoke testinde bulunamazsa smoke testi başarısız olur.
    /// </summary>
    Kesin,

    /// <summary>
    /// Ad ya da API alanıyla eşlemesi belgede belirsiz (Aşama 1'de "(s)"). Smoke testinde bulunamazsa BELGE BELİRSİZ
    /// olarak raporlanır ve ilgili alan veritabanında doğrulanmaz, yalnızca API cevabında doğrulanır.
    /// </summary>
    SmokeaBagli,

    /// <summary>Değeri belgede tanımsız; testler doğrulamaz. Smoke testi yalnızca varlığını raporlar.</summary>
    Dogrulanmaz,
}

internal sealed record BeklenenSutun(string Tablo, string Ad, SutunDurumu Durum, string Kaynak);

/// <summary>
/// Veritabanı doğrulamasında kullanılan tablo ve sütun adları. Tek kaynak docs/database-schema.md (DS): tablo adları
/// DS:10-19, kolon adları "Db2'deki adın snake_case'i" kuralı (DS:21; örnekler <c>firstName → first_name</c>,
/// <c>phonehome → phone_home</c>). DS sütunları tek tek listelemediği için Db2 adları DS'nin atıf yaptığı
/// <c>base/cntl/db2cre.jcl</c> satırlarından yalnızca AD olarak alınmıştır (değer ya da davranış için kullanılmaz).
/// Uygulamanın kendi sınıfları (DbContext, entity) kullanılmaz.
/// </summary>
/// <remarks>
/// API alanı ↔ sütun eşlemesi belgede yoktur (Aşama 0, G2). Bu dosya yalnızca adları verir; hangi API alanının hangi
/// sütuna yazıldığı testlerde, belge satırıyla birlikte yazılır.
/// </remarks>
internal static class Sema
{
    public static class Customer
    {
        public const string Tablo = "customer";                // DS:12
        public const string CustomerNumber = "customer_number"; // DS:49 (adıyla)
        public const string FirstName = "first_name";           // db2cre:113 firstName; DS:21 örneği
        public const string LastName = "last_name";             // db2cre:114
        public const string DateOfBirth = "date_of_birth";      // db2cre:115
        public const string HouseName = "house_name";           // db2cre:116
        public const string HouseNumber = "house_number";       // db2cre:117
        public const string Postcode = "postcode";              // db2cre:118
        public const string PhoneHome = "phone_home";           // db2cre:119 phonehome; DS:21 örneği
        public const string PhoneMobile = "phone_mobile";       // db2cre:120 phonemobile; DS:21 örneğine benzetilerek
        public const string EmailAddress = "email_address";     // db2cre:121 emailaddress; (s) ad ve email ↔ sütun eşlemesi
    }

    public static class CustomerSecure
    {
        public const string Tablo = "customer_secure";          // DS:13
        public const string CustomerNumber = "customer_number"; // db2cre:131; SRS FR-01: yalnızca satırın varlığı
        public const string CustomerPass = "customer_pass";     // db2cre:132; değeri tanımsız
        public const string StateIndicator = "state_indicator"; // db2cre:133; değeri tanımsız
        public const string PassChanges = "pass_changes";       // db2cre:134; değeri tanımsız
    }

    public static class Policy
    {
        public const string Tablo = "policy";                     // DS:14
        public const string PolicyNumber = "policy_number";       // DS:49 (adıyla)
        public const string CustomerNumber = "customer_number";   // DS:63-64 (policy.customer_number)
        public const string IssueDate = "issue_date";             // db2cre:165
        public const string ExpiryDate = "expiry_date";           // db2cre:166
        public const string PolicyType = "policy_type";           // db2cre:167; değerler AD:136 (H, M, E, C)
        public const string LastChanged = "last_changed";         // DS:58 (adıyla)
        public const string BrokerId = "broker_id";               // db2cre:169
        public const string BrokersReference = "brokers_reference"; // db2cre:170
        public const string Payment = "payment";                  // db2cre:171
        public const string Commission = "commission";            // db2cre:172; API'de yok, değeri tanımsız
    }

    public static class Motor
    {
        public const string Tablo = "motor";                        // DS:17
        public const string PolicyNumber = "policy_number";         // db2cre:276
        public const string Make = "make";                          // db2cre:277
        public const string Model = "model";                        // db2cre:278
        public const string Value = "value";                        // db2cre:279
        public const string RegNumber = "reg_number";               // db2cre:280 regNumber; (s) registrationNumber ↔ sütun
        public const string Colour = "colour";                      // db2cre:281
        public const string Cc = "cc";                              // db2cre:282
        public const string YearOfManufacture = "year_of_manufacture"; // db2cre:283; (s) manufactureDate ↔ sütun
        public const string Premium = "premium";                    // db2cre:284
        public const string Accidents = "accidents";                // db2cre:285
    }

    public static class House
    {
        public const string Tablo = "house";                  // DS:16
        public const string PolicyNumber = "policy_number";   // db2cre:241
        public const string PropertyType = "property_type";   // db2cre:242
        public const string Bedrooms = "bedrooms";            // db2cre:243
        public const string Value = "value";                  // db2cre:244
        public const string HouseName = "house_name";         // db2cre:245
        public const string HouseNumber = "house_number";     // db2cre:246
        public const string Postcode = "postcode";            // db2cre:247
    }

    public static class Endowment
    {
        public const string Tablo = "endowment";              // DS:15
        public const string PolicyNumber = "policy_number";   // db2cre:204
        public const string Equities = "equities";            // db2cre:205
        public const string WithProfits = "with_profits";     // db2cre:206
        public const string ManagedFund = "managed_fund";     // db2cre:207
        public const string FundName = "fund_name";           // db2cre:208
        public const string Term = "term";                    // db2cre:209
        public const string SumAssured = "sum_assured";       // db2cre:210
        public const string LifeAssured = "life_assured";     // db2cre:211
        public const string PaddingData = "padding_data";     // db2cre:212; DS:28 "paddingData" yazıyor; değeri tanımsız
    }

    public static class Commercial
    {
        public const string Tablo = "commercial";             // DS:18
        public const string PolicyNumber = "policy_number";   // db2cre:314
        public const string RequestDate = "request_date";     // DS:44 (adıyla); değeri tanımsız
        public const string StartDate = "start_date";         // db2cre:316; issueDate ile eşlemesi tanımsız
        public const string RenewalDate = "renewal_date";     // db2cre:317; expiryDate ile eşlemesi tanımsız
        public const string Address = "address";             // db2cre:318
        public const string Zipcode = "zipcode";             // db2cre:319; (s) postcode ↔ sütun
        public const string LatitudeN = "latitude_n";         // db2cre:320; (s) latitude ↔ sütun
        public const string LongitudeW = "longitude_w";       // db2cre:321; (s) longitude ↔ sütun
        public const string Customer = "customer";           // db2cre:322
        public const string PropertyType = "property_type";   // db2cre:323
        public const string FirePeril = "fire_peril";         // db2cre:324
        public const string FirePremium = "fire_premium";     // db2cre:325
        public const string CrimePeril = "crime_peril";       // db2cre:326
        public const string CrimePremium = "crime_premium";   // db2cre:327
        public const string FloodPeril = "flood_peril";       // db2cre:328
        public const string FloodPremium = "flood_premium";   // db2cre:329
        public const string WeatherPeril = "weather_peril";   // db2cre:330
        public const string WeatherPremium = "weather_premium"; // db2cre:331
        public const string Status = "status";               // db2cre:332
        public const string RejectionReason = "rejection_reason"; // db2cre:333
    }

    /// <summary><c>claim</c> tablosu (DS:19) hiçbir operasyonda kullanılmaz (DS:91-96); yalnızca varlığı kontrol edilir.</summary>
    public const string ClaimTablosu = "claim";

    /// <summary>DS:10-19'daki sekiz tablo.</summary>
    public static readonly IReadOnlyList<string> BeklenenTablolar =
    [
        Customer.Tablo, CustomerSecure.Tablo, Policy.Tablo, Endowment.Tablo, House.Tablo, Motor.Tablo,
        Commercial.Tablo, ClaimTablosu,
    ];

    /// <summary>Smoke testinin information_schema'da aradığı sütunlar.</summary>
    public static readonly IReadOnlyList<BeklenenSutun> BeklenenSutunlar =
    [
        K(Customer.Tablo, Customer.CustomerNumber, "DS:49"),
        K(Customer.Tablo, Customer.FirstName, "db2cre:113, DS:21"),
        K(Customer.Tablo, Customer.LastName, "db2cre:114"),
        K(Customer.Tablo, Customer.DateOfBirth, "db2cre:115"),
        K(Customer.Tablo, Customer.HouseName, "db2cre:116"),
        K(Customer.Tablo, Customer.HouseNumber, "db2cre:117"),
        K(Customer.Tablo, Customer.Postcode, "db2cre:118"),
        K(Customer.Tablo, Customer.PhoneHome, "db2cre:119, DS:21"),
        K(Customer.Tablo, Customer.PhoneMobile, "db2cre:120"),
        S(Customer.Tablo, Customer.EmailAddress, "db2cre:121 (emailaddress)"),

        K(CustomerSecure.Tablo, CustomerSecure.CustomerNumber, "db2cre:131"),
        D(CustomerSecure.Tablo, CustomerSecure.CustomerPass, "db2cre:132"),
        D(CustomerSecure.Tablo, CustomerSecure.StateIndicator, "db2cre:133"),
        D(CustomerSecure.Tablo, CustomerSecure.PassChanges, "db2cre:134"),

        K(Policy.Tablo, Policy.PolicyNumber, "DS:49"),
        K(Policy.Tablo, Policy.CustomerNumber, "DS:63-64"),
        K(Policy.Tablo, Policy.IssueDate, "db2cre:165"),
        K(Policy.Tablo, Policy.ExpiryDate, "db2cre:166"),
        K(Policy.Tablo, Policy.PolicyType, "db2cre:167"),
        K(Policy.Tablo, Policy.LastChanged, "DS:58"),
        K(Policy.Tablo, Policy.BrokerId, "db2cre:169"),
        K(Policy.Tablo, Policy.BrokersReference, "db2cre:170"),
        K(Policy.Tablo, Policy.Payment, "db2cre:171"),
        D(Policy.Tablo, Policy.Commission, "db2cre:172"),

        K(Motor.Tablo, Motor.PolicyNumber, "db2cre:276"),
        K(Motor.Tablo, Motor.Make, "db2cre:277"),
        K(Motor.Tablo, Motor.Model, "db2cre:278"),
        K(Motor.Tablo, Motor.Value, "db2cre:279"),
        S(Motor.Tablo, Motor.RegNumber, "db2cre:280 (regNumber)"),
        K(Motor.Tablo, Motor.Colour, "db2cre:281"),
        K(Motor.Tablo, Motor.Cc, "db2cre:282"),
        S(Motor.Tablo, Motor.YearOfManufacture, "db2cre:283 (yearOfManufacture)"),
        K(Motor.Tablo, Motor.Premium, "db2cre:284"),
        K(Motor.Tablo, Motor.Accidents, "db2cre:285"),

        K(House.Tablo, House.PolicyNumber, "db2cre:241"),
        K(House.Tablo, House.PropertyType, "db2cre:242"),
        K(House.Tablo, House.Bedrooms, "db2cre:243"),
        K(House.Tablo, House.Value, "db2cre:244"),
        K(House.Tablo, House.HouseName, "db2cre:245"),
        K(House.Tablo, House.HouseNumber, "db2cre:246"),
        K(House.Tablo, House.Postcode, "db2cre:247"),

        K(Endowment.Tablo, Endowment.PolicyNumber, "db2cre:204"),
        K(Endowment.Tablo, Endowment.Equities, "db2cre:205"),
        K(Endowment.Tablo, Endowment.WithProfits, "db2cre:206"),
        K(Endowment.Tablo, Endowment.ManagedFund, "db2cre:207"),
        K(Endowment.Tablo, Endowment.FundName, "db2cre:208"),
        K(Endowment.Tablo, Endowment.Term, "db2cre:209"),
        K(Endowment.Tablo, Endowment.SumAssured, "db2cre:210"),
        K(Endowment.Tablo, Endowment.LifeAssured, "db2cre:211"),
        D(Endowment.Tablo, Endowment.PaddingData, "db2cre:212, DS:28 (paddingData)"),

        K(Commercial.Tablo, Commercial.PolicyNumber, "db2cre:314"),
        D(Commercial.Tablo, Commercial.RequestDate, "DS:44"),
        D(Commercial.Tablo, Commercial.StartDate, "db2cre:316"),
        D(Commercial.Tablo, Commercial.RenewalDate, "db2cre:317"),
        K(Commercial.Tablo, Commercial.Address, "db2cre:318"),
        S(Commercial.Tablo, Commercial.Zipcode, "db2cre:319 (Zipcode)"),
        S(Commercial.Tablo, Commercial.LatitudeN, "db2cre:320 (LatitudeN)"),
        S(Commercial.Tablo, Commercial.LongitudeW, "db2cre:321 (LongitudeW)"),
        K(Commercial.Tablo, Commercial.Customer, "db2cre:322"),
        K(Commercial.Tablo, Commercial.PropertyType, "db2cre:323"),
        K(Commercial.Tablo, Commercial.FirePeril, "db2cre:324"),
        K(Commercial.Tablo, Commercial.FirePremium, "db2cre:325"),
        K(Commercial.Tablo, Commercial.CrimePeril, "db2cre:326"),
        K(Commercial.Tablo, Commercial.CrimePremium, "db2cre:327"),
        K(Commercial.Tablo, Commercial.FloodPeril, "db2cre:328"),
        K(Commercial.Tablo, Commercial.FloodPremium, "db2cre:329"),
        K(Commercial.Tablo, Commercial.WeatherPeril, "db2cre:330"),
        K(Commercial.Tablo, Commercial.WeatherPremium, "db2cre:331"),
        K(Commercial.Tablo, Commercial.Status, "db2cre:332"),
        K(Commercial.Tablo, Commercial.RejectionReason, "db2cre:333"),
    ];

    private static BeklenenSutun K(string tablo, string ad, string kaynak) => new(tablo, ad, SutunDurumu.Kesin, kaynak);

    private static BeklenenSutun S(string tablo, string ad, string kaynak) => new(tablo, ad, SutunDurumu.SmokeaBagli, kaynak);

    private static BeklenenSutun D(string tablo, string ad, string kaynak) => new(tablo, ad, SutunDurumu.Dogrulanmaz, kaynak);
}
