using System.Text.Json.Serialization;
using GenApp.Domain.Policies;

namespace GenApp.Api.Policies;

// Fields every policy response has (docs/api-design.md section 4.2); each type adds its own after them. A NULL number
// is returned as 0, as LGIPDB01 does for the broker and payment columns (motor lgipdb01.cbl:551-553, :588-593; house
// :460-462, :494-499; endowment :397-402). lastChanged is returned in the server's format and is sent back unchanged
// on update (section 5).
public abstract record PolicyResponse
{
    protected PolicyResponse(Policy policy)
        : this(policy, policy.IssueDate, policy.ExpiryDate, policy.LastChanged)
    {
    }

    // For a type whose inquiry reads the dates from its own table instead of POLICY (commercial: STARTDATE,
    // RENEWALDATE and REQUESTDATE, 17-commercial-inquire.md note 1).
    protected PolicyResponse(Policy policy, DateOnly? issueDate, DateOnly? expiryDate, DateTime lastChanged)
    {
        PolicyNumber = policy.PolicyNumber;
        CustomerNumber = policy.CustomerNumber;
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        LastChanged = lastChanged;
        BrokerId = policy.BrokerId ?? 0;
        BrokersReference = ResponseText.Of(policy.BrokersReference);
        Payment = policy.Payment ?? 0;
    }

    // Common fields first in the JSON, in this order, then the type's fields.
    [JsonPropertyOrder(-8)]
    public int PolicyNumber { get; }

    [JsonPropertyOrder(-7)]
    public int CustomerNumber { get; }

    [JsonPropertyOrder(-6)]
    public DateOnly? IssueDate { get; }

    [JsonPropertyOrder(-5)]
    public DateOnly? ExpiryDate { get; }

    [JsonPropertyOrder(-4)]
    public DateTime LastChanged { get; }

    [JsonPropertyOrder(-3)]
    public int BrokerId { get; }

    [JsonPropertyOrder(-2)]
    public string BrokersReference { get; }

    [JsonPropertyOrder(-1)]
    public int Payment { get; }
}
