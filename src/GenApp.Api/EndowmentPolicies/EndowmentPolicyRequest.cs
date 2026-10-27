using System.ComponentModel.DataAnnotations;
using GenApp.Domain.Policies;

namespace GenApp.Api.EndowmentPolicies;

// Request body of endowment policy add (docs/api-design.md sections 4.2 and 4.5). Every field must be present, but
// texts may be empty (section 1). Maximum lengths and digits are the COMMAREA field sizes (lgcmarea.cpy:47-53); the
// numeric COMMAREA fields are unsigned PIC 9. The endowment fields are not checked further, as in the legacy program
// (lgapdb01.cbl:342-387); the three flags are carried as one-character texts (section 4.5). brokerId has 10 digits in
// the COMMAREA but the column is an integer. policyNumber, customerNumber, lastChanged and paddingData are not part of
// the request; if sent they are ignored.
public class EndowmentPolicyRequest
{
    [Required]
    public DateOnly? IssueDate { get; init; }

    [Required]
    public DateOnly? ExpiryDate { get; init; }

    [Required, Range(0, int.MaxValue)]
    public int? BrokerId { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(10)]
    public string? BrokersReference { get; init; }

    [Required, Range(0, 999999)]
    public int? Payment { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(1)]
    public string? WithProfits { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(1)]
    public string? Equities { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(1)]
    public string? ManagedFund { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(10)]
    public string? FundName { get; init; }

    [Required, Range(0, 99)]
    public int? Term { get; init; }

    [Required, Range(0, 999999)]
    public int? SumAssured { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(31)]
    public string? LifeAssured { get; init; }

    public Policy ToPolicy(int customerNumber) => new()
    {
        CustomerNumber = customerNumber,
        IssueDate = IssueDate,
        ExpiryDate = ExpiryDate,
        BrokerId = BrokerId,
        BrokersReference = BrokersReference,
        Payment = Payment,
    };

    public EndowmentPolicy ToEndowmentPolicy() => new()
    {
        WithProfits = WithProfits,
        Equities = Equities,
        ManagedFund = ManagedFund,
        FundName = FundName,
        Term = (short?)Term,
        SumAssured = SumAssured,
        LifeAssured = LifeAssured,
    };
}

// Request body of endowment policy update: the same fields and the lastChanged value returned by the inquiry, sent
// back unchanged (docs/api-design.md section 5).
public sealed class EndowmentPolicyUpdateRequest : EndowmentPolicyRequest
{
    [Required]
    public DateTime? LastChanged { get; init; }

    // The column is "timestamp without time zone" and Npgsql rejects DateTimeKind.Utc for it (ADR 0003 decision 3);
    // a value sent with a "Z" keeps its digits and is compared as they are.
    public DateTime LastChangedValue => DateTime.SpecifyKind(LastChanged!.Value, DateTimeKind.Unspecified);
}
