namespace GenApp.Llm.MusteriEkle;

public enum MusteriEkleOutcome
{
    /// <summary>CA-RETURN-CODE = '00'</summary>
    Success,

    /// <summary>CA-RETURN-CODE = '90' (lgacdb01.cbl INSERT-CUSTOMER, SQLCODE NOT EQUAL 0)</summary>
    DatabaseError,

    /// <summary>Kilitlenme / zaman aşımı: 503, returnCode "90" (bilinçli sapma, Karar 8)</summary>
    DatabaseBusy,
}

public sealed record MusteriEkleResult(MusteriEkleOutcome Outcome, CustomerResponse? Customer)
{
    public string ReturnCode => Outcome == MusteriEkleOutcome.Success ? "00" : "90";
}
