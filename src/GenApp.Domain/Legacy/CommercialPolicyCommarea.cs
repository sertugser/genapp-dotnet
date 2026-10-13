namespace GenApp.Domain.Legacy;

// CA-COMMERCIAL REDEFINES CA-POLICY-SPECIFIC (lgcmarea.cpy:77-94).
public sealed class CommercialPolicyCommarea : PolicyCommarea
{
    public string Address { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Latitude { get; set; } = "";
    public string Longitude { get; set; } = "";
    public string Customer { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public int FirePeril { get; set; }
    public int FirePremium { get; set; }
    public int CrimePeril { get; set; }
    public int CrimePremium { get; set; }
    public int FloodPeril { get; set; }
    public int FloodPremium { get; set; }
    public int WeatherPeril { get; set; }
    public int WeatherPremium { get; set; }
    public int Status { get; set; }
    public string RejectionReason { get; set; } = "";

    protected override void WritePolicySpecific(CommareaWriter writer)
    {
        writer.Text(Address, 255);        // CA-B-Address        X(255)
        writer.Text(Postcode, 8);         // CA-B-Postcode       X(8)
        writer.Text(Latitude, 11);        // CA-B-Latitude       X(11)
        writer.Text(Longitude, 11);       // CA-B-Longitude      X(11)
        writer.Text(Customer, 255);       // CA-B-Customer       X(255)
        writer.Text(PropertyType, 255);   // CA-B-PropType       X(255)
        writer.Number(FirePeril, 4);      // CA-B-FirePeril      9(4)
        writer.Number(FirePremium, 8);    // CA-B-FirePremium    9(8)
        writer.Number(CrimePeril, 4);     // CA-B-CrimePeril     9(4)
        writer.Number(CrimePremium, 8);   // CA-B-CrimePremium   9(8)
        writer.Number(FloodPeril, 4);     // CA-B-FloodPeril     9(4)
        writer.Number(FloodPremium, 8);   // CA-B-FloodPremium   9(8)
        writer.Number(WeatherPeril, 4);   // CA-B-WeatherPeril   9(4)
        writer.Number(WeatherPremium, 8); // CA-B-WeatherPremium 9(8)
        writer.Number(Status, 4);         // CA-B-Status         9(4)
        writer.Text(RejectionReason, 255); // CA-B-RejectReason  X(255)
    }

    protected override void ReadPolicySpecific(CommareaReader reader)
    {
        Address = reader.Text(255);
        Postcode = reader.Text(8);
        Latitude = reader.Text(11);
        Longitude = reader.Text(11);
        Customer = reader.Text(255);
        PropertyType = reader.Text(255);
        FirePeril = (int)reader.Number(4);
        FirePremium = (int)reader.Number(8);
        CrimePeril = (int)reader.Number(4);
        CrimePremium = (int)reader.Number(8);
        FloodPeril = (int)reader.Number(4);
        FloodPremium = (int)reader.Number(8);
        WeatherPeril = (int)reader.Number(4);
        WeatherPremium = (int)reader.Number(8);
        Status = (int)reader.Number(4);
        RejectionReason = reader.Text(255);
    }
}
