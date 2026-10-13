using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

// The common policy row and the type's own row, as LGIPDB01 returns them together.
public sealed record PolicyRecord<TDetails>(Policy Policy, TDetails Details)
    where TDetails : class, IPolicyDetails;
