namespace GenApp.Domain.Legacy;

// CA-ENDOWMENT REDEFINES CA-POLICY-SPECIFIC (lgcmarea.cpy:46-54).
// CA-E-PADDING-DATA is not filler: it carries data (lgapdb01.cbl:344).
public sealed class EndowmentPolicyCommarea : PolicyCommarea
{
    public string WithProfits { get; set; } = "";
    public string Equities { get; set; } = "";
    public string ManagedFund { get; set; } = "";
    public string FundName { get; set; } = "";
    public int Term { get; set; }
    public int SumAssured { get; set; }
    public string LifeAssured { get; set; } = "";
    public string PaddingData { get; set; } = "";

    protected override void WritePolicySpecific(CommareaWriter writer)
    {
        writer.Text(WithProfits, 1);      // CA-E-WITH-PROFITS X
        writer.Text(Equities, 1);         // CA-E-EQUITIES     X
        writer.Text(ManagedFund, 1);      // CA-E-MANAGED-FUND X
        writer.Text(FundName, 10);        // CA-E-FUND-NAME    X(10)
        writer.Number(Term, 2);           // CA-E-TERM         99
        writer.Number(SumAssured, 6);     // CA-E-SUM-ASSURED  9(6)
        writer.Text(LifeAssured, 31);     // CA-E-LIFE-ASSURED X(31)
        writer.Text(PaddingData, 32348); // CA-E-PADDING-DATA X(32348)
    }

    protected override void ReadPolicySpecific(CommareaReader reader)
    {
        WithProfits = reader.Text(1);
        Equities = reader.Text(1);
        ManagedFund = reader.Text(1);
        FundName = reader.Text(10);
        Term = (int)reader.Number(2);
        SumAssured = (int)reader.Number(6);
        LifeAssured = reader.Text(31);
        PaddingData = reader.Text(32348);
    }
}
