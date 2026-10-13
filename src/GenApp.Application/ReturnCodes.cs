namespace GenApp.Application;

// The legacy CA-RETURN-CODE values a service can produce (docs/api-design.md section 3). The Api turns them into
// HTTP status codes; database failures (90, and the 503 case of ADR 0002 decision 8) arrive as exceptions instead,
// see Persistence/DatabaseException.cs.
public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string ConcurrencyConflict = "02";
    public const string CustomerNotFound = "70";
    public const string DatabaseError = "90";
}
