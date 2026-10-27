using System.ComponentModel.DataAnnotations;
using GenApp.Domain.Policies;

namespace GenApp.Api.Policies;

// Fields every policy request has (docs/api-design.md section 4.2); each type adds its own. Every field must be
// present, but texts may be empty (section 1). Maximum lengths and digits are the COMMAREA field sizes; the numeric
// COMMAREA fields are unsigned PIC 9. brokerId has 10 digits in the COMMAREA but the column is an integer.
// policyNumber, customerNumber and lastChanged are not part of an add request; if sent they are ignored.
public abstract class PolicyRequest
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

    public Policy ToPolicy(int customerNumber) => new()
    {
        CustomerNumber = customerNumber,
        IssueDate = IssueDate,
        ExpiryDate = ExpiryDate,
        BrokerId = BrokerId,
        BrokersReference = BrokersReference,
        Payment = Payment,
    };

    // An update request also carries the lastChanged value returned by the inquiry, sent back unchanged
    // (docs/api-design.md section 5). The column is "timestamp without time zone" and Npgsql rejects DateTimeKind.Utc
    // for it (ADR 0003 decision 3); a value sent with a "Z" keeps its digits and is compared as they are.
    protected static DateTime ColumnValue(DateTime? lastChanged) =>
        DateTime.SpecifyKind(lastChanged!.Value, DateTimeKind.Unspecified);
}
