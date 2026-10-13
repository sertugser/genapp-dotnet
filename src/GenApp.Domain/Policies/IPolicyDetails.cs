namespace GenApp.Domain.Policies;

// A policy type's own row (motor, house, endowment, commercial); it shares its key with the policy row.
public interface IPolicyDetails
{
    int PolicyNumber { get; set; }
    Policy? Policy { get; set; }
}
