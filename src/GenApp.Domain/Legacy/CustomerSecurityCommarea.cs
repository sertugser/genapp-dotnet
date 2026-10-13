namespace GenApp.Domain.Legacy;

// CA-CUSTSECR-REQUEST REDEFINES CA-REQUEST-SPECIFIC (lgcmarea.cpy:28-32).
public sealed class CustomerSecurityCommarea : Commarea
{
    public string Password { get; set; } = "";
    public string Count { get; set; } = "";
    public string State { get; set; } = "";

    protected override void WriteRequestSpecific(CommareaWriter writer)
    {
        writer.Text(Password, 32);        // CA-CUSTSECR-PASS  X(32)
        writer.Text(Count, 4);            // CA-CUSTSECR-COUNT X(4)
        writer.Text(State, 1);            // CA-CUSTSECR-STATE X(1)
    }

    protected override void ReadRequestSpecific(CommareaReader reader)
    {
        Password = reader.Text(32);
        Count = reader.Text(4);
        State = reader.Text(1);
    }
}
