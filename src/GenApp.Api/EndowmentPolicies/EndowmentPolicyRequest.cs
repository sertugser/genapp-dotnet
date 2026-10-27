using System.ComponentModel.DataAnnotations;
using GenApp.Api.Policies;
using GenApp.Domain.Policies;

namespace GenApp.Api.EndowmentPolicies;

// Request body of endowment policy add (docs/api-design.md section 4.5; common fields in PolicyRequest). Maximum
// lengths and digits are the COMMAREA field sizes (lgcmarea.cpy:47-53). The endowment fields are not checked further,
// as in the legacy program (lgapdb01.cbl:342-387); the three flags are carried as one-character texts (section 4.5).
// paddingData is not part of the request; if sent it is ignored.
public class EndowmentPolicyRequest : PolicyRequest
{
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

// Request body of endowment policy update: the same fields and lastChanged (see PolicyRequest).
public sealed class EndowmentPolicyUpdateRequest : EndowmentPolicyRequest
{
    [Required]
    public DateTime? LastChanged { get; init; }

    public DateTime LastChangedValue => ColumnValue(LastChanged);
}
