namespace GenApp.Domain.Legacy;

// CA-CUSTOMER-REQUEST REDEFINES CA-REQUEST-SPECIFIC (lgcmarea.cpy:15-26).
public sealed class CustomerCommarea : Commarea
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string HouseName { get; set; } = "";
    public string HouseNumber { get; set; } = "";
    public string Postcode { get; set; } = "";
    public int NumberOfPolicies { get; set; }
    public string PhoneMobile { get; set; } = "";
    public string PhoneHome { get; set; } = "";
    public string EmailAddress { get; set; } = "";

    protected override void WriteRequestSpecific(CommareaWriter writer)
    {
        writer.Text(FirstName, 10);       // CA-FIRST-NAME     X(10)
        writer.Text(LastName, 20);        // CA-LAST-NAME      X(20)
        writer.Text(DateOfBirth, 10);     // CA-DOB            X(10)
        writer.Text(HouseName, 20);       // CA-HOUSE-NAME     X(20)
        writer.Text(HouseNumber, 4);      // CA-HOUSE-NUM      X(4)
        writer.Text(Postcode, 8);         // CA-POSTCODE       X(8)
        writer.Number(NumberOfPolicies, 3); // CA-NUM-POLICIES 9(3)
        writer.Text(PhoneMobile, 20);     // CA-PHONE-MOBILE   X(20)
        writer.Text(PhoneHome, 20);       // CA-PHONE-HOME     X(20)
        writer.Text(EmailAddress, 100);   // CA-EMAIL-ADDRESS  X(100)
    }

    protected override void ReadRequestSpecific(CommareaReader reader)
    {
        FirstName = reader.Text(10);
        LastName = reader.Text(20);
        DateOfBirth = reader.Text(10);
        HouseName = reader.Text(20);
        HouseNumber = reader.Text(4);
        Postcode = reader.Text(8);
        NumberOfPolicies = (int)reader.Number(3);
        PhoneMobile = reader.Text(20);
        PhoneHome = reader.Text(20);
        EmailAddress = reader.Text(100);
    }
}
