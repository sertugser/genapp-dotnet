namespace GenApp.Domain.Policies;

public class Policy
{
    public int PolicyNumber { get; set; }
    public int CustomerNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? PolicyType { get; set; }
    public DateTime LastChanged { get; set; }
    public int? BrokerId { get; set; }
    public string? BrokersReference { get; set; }
    public int? Payment { get; set; }
    public short? Commission { get; set; }
}
