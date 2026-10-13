namespace GenApp.Domain.Legacy;

// CA-MOTOR REDEFINES CA-POLICY-SPECIFIC (lgcmarea.cpy:65-75).
public sealed class MotorPolicyCommarea : PolicyCommarea
{
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int Value { get; set; }
    public string RegistrationNumber { get; set; } = "";
    public string Colour { get; set; } = "";
    public int Cc { get; set; }
    public string ManufactureDate { get; set; } = "";
    public int Premium { get; set; }
    public int Accidents { get; set; }

    protected override void WritePolicySpecific(CommareaWriter writer)
    {
        writer.Text(Make, 15);            // CA-M-MAKE         X(15)
        writer.Text(Model, 15);           // CA-M-MODEL        X(15)
        writer.Number(Value, 6);          // CA-M-VALUE        9(6)
        writer.Text(RegistrationNumber, 7); // CA-M-REGNUMBER  X(7)
        writer.Text(Colour, 8);           // CA-M-COLOUR       X(8)
        writer.Number(Cc, 4);             // CA-M-CC           9(4)
        writer.Text(ManufactureDate, 10); // CA-M-MANUFACTURED X(10)
        writer.Number(Premium, 6);        // CA-M-PREMIUM      9(6)
        writer.Number(Accidents, 6);      // CA-M-ACCIDENTS    9(6)
    }

    protected override void ReadPolicySpecific(CommareaReader reader)
    {
        Make = reader.Text(15);
        Model = reader.Text(15);
        Value = (int)reader.Number(6);
        RegistrationNumber = reader.Text(7);
        Colour = reader.Text(8);
        Cc = (int)reader.Number(4);
        ManufactureDate = reader.Text(10);
        Premium = (int)reader.Number(6);
        Accidents = (int)reader.Number(6);
    }
}
