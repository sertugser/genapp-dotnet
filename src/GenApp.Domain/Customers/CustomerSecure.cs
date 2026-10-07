namespace GenApp.Domain.Customers;

public class CustomerSecure
{
    public int CustomerNumber { get; set; }
    public string? CustomerPass { get; set; }
    public string? StateIndicator { get; set; }
    public int? PassChanges { get; set; }
}
