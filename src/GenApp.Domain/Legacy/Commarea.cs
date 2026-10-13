namespace GenApp.Domain.Legacy;

// lgcmarea.cpy: one 32,500-byte block shared by every request. The 18-byte header is common;
// subclasses write CA-REQUEST-SPECIFIC, which each request type REDEFINES differently.
public abstract class Commarea
{
    public const int Length = 32500;

    public string RequestId { get; set; } = "";
    public int ReturnCode { get; set; }
    public long CustomerNumber { get; set; }

    public string ToFixedWidth()
    {
        var writer = new CommareaWriter();
        writer.Text(RequestId, 6);        // CA-REQUEST-ID    X(6)
        writer.Number(ReturnCode, 2);     // CA-RETURN-CODE   9(2)
        writer.Number(CustomerNumber, 10); // CA-CUSTOMER-NUM 9(10)
        WriteRequestSpecific(writer);
        return writer.ToString().PadRight(Length);
    }

    protected abstract void WriteRequestSpecific(CommareaWriter writer);

    // The caller chooses T, just as each COBOL program chooses which REDEFINES to read.
    public static T Parse<T>(string raw) where T : Commarea, new()
    {
        if (raw.Length != Length)
        {
            throw new ArgumentException($"A COMMAREA is {Length} characters long, but {raw.Length} were given.", nameof(raw));
        }

        var reader = new CommareaReader(raw);
        var commarea = new T
        {
            RequestId = reader.Text(6),
            ReturnCode = (int)reader.Number(2),
            CustomerNumber = reader.Number(10),
        };
        commarea.ReadRequestSpecific(reader);
        return commarea;
    }

    protected abstract void ReadRequestSpecific(CommareaReader reader);
}
