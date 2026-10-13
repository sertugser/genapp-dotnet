namespace GenApp.Domain.Legacy;

// CA-HOUSE REDEFINES CA-POLICY-SPECIFIC (lgcmarea.cpy:56-63).
public sealed class HousePolicyCommarea : PolicyCommarea
{
    public string PropertyType { get; set; } = "";
    public int Bedrooms { get; set; }
    public int Value { get; set; }
    public string HouseName { get; set; } = "";
    public string HouseNumber { get; set; } = "";
    public string Postcode { get; set; } = "";

    protected override void WritePolicySpecific(CommareaWriter writer)
    {
        writer.Text(PropertyType, 15);    // CA-H-PROPERTY-TYPE X(15)
        writer.Number(Bedrooms, 3);       // CA-H-BEDROOMS      9(3)
        writer.Number(Value, 8);          // CA-H-VALUE         9(8)
        writer.Text(HouseName, 20);       // CA-H-HOUSE-NAME    X(20)
        writer.Text(HouseNumber, 4);      // CA-H-HOUSE-NUMBER  X(4)
        writer.Text(Postcode, 8);         // CA-H-POSTCODE      X(8)
    }

    protected override void ReadPolicySpecific(CommareaReader reader)
    {
        PropertyType = reader.Text(15);
        Bedrooms = (int)reader.Number(3);
        Value = (int)reader.Number(8);
        HouseName = reader.Text(20);
        HouseNumber = reader.Text(4);
        Postcode = reader.Text(8);
    }
}
