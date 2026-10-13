namespace GenApp.Domain.Legacy;

// CA-CLAIM REDEFINES CA-POLICY-SPECIFIC (lgcmarea.cpy:96-103). No in-scope operation uses it.
public sealed class ClaimCommarea : PolicyCommarea
{
    public long ClaimNumber { get; set; }
    public string ClaimDate { get; set; } = "";
    public int Paid { get; set; }
    public int Value { get; set; }
    public string Cause { get; set; } = "";
    public string Observations { get; set; } = "";

    protected override void WritePolicySpecific(CommareaWriter writer)
    {
        writer.Number(ClaimNumber, 10);   // CA-C-Num          9(10)
        writer.Text(ClaimDate, 10);       // CA-C-Date         X(10)
        writer.Number(Paid, 8);           // CA-C-Paid         9(8)
        writer.Number(Value, 8);          // CA-C-Value        9(8)
        writer.Text(Cause, 255);          // CA-C-Cause        X(255)
        writer.Text(Observations, 255);   // CA-C-Observations X(255)
    }

    protected override void ReadPolicySpecific(CommareaReader reader)
    {
        ClaimNumber = reader.Number(10);
        ClaimDate = reader.Text(10);
        Paid = (int)reader.Number(8);
        Value = (int)reader.Number(8);
        Cause = reader.Text(255);
        Observations = reader.Text(255);
    }
}
