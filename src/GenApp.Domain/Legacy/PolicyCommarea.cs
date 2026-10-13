namespace GenApp.Domain.Legacy;

// CA-POLICY-REQUEST REDEFINES CA-REQUEST-SPECIFIC (lgcmarea.cpy:34-44). The common part is written here;
// each policy type writes CA-POLICY-SPECIFIC, which it REDEFINES again (:46-103).
public abstract class PolicyCommarea : Commarea
{
    public long PolicyNumber { get; set; }
    public string IssueDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
    public string LastChanged { get; set; } = "";
    public long BrokerId { get; set; }
    public string BrokersReference { get; set; } = "";
    public int Payment { get; set; }

    protected sealed override void WriteRequestSpecific(CommareaWriter writer)
    {
        writer.Number(PolicyNumber, 10);  // CA-POLICY-NUM     9(10)
        writer.Text(IssueDate, 10);       // CA-ISSUE-DATE     X(10)
        writer.Text(ExpiryDate, 10);      // CA-EXPIRY-DATE    X(10)
        writer.Text(LastChanged, 26);     // CA-LASTCHANGED    X(26)
        writer.Number(BrokerId, 10);      // CA-BROKERID       9(10)
        writer.Text(BrokersReference, 10); // CA-BROKERSREF    X(10)
        writer.Number(Payment, 6);        // CA-PAYMENT        9(6)
        WritePolicySpecific(writer);
    }

    protected abstract void WritePolicySpecific(CommareaWriter writer);

    protected sealed override void ReadRequestSpecific(CommareaReader reader)
    {
        PolicyNumber = reader.Number(10);
        IssueDate = reader.Text(10);
        ExpiryDate = reader.Text(10);
        LastChanged = reader.Text(26);
        BrokerId = reader.Number(10);
        BrokersReference = reader.Text(10);
        Payment = (int)reader.Number(6);
        ReadPolicySpecific(reader);
    }

    protected abstract void ReadPolicySpecific(CommareaReader reader);
}
