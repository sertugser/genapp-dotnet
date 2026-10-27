using GenApp.Application.Persistence;
using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

// Replaces LGAPOL01, LGIPOL01, LGUPOL01 and LGDPOL01 for house policies (request IDs 01AHOU, 01IHOU, 01UHOU, 01DHOU;
// legacy-analysis/use-cases/08-11). Inquire returns 01 when there is no house row (lgipdb01.cbl:469-475, :510-514).
// The house fields are not checked, as in the legacy program (lgapdb01.cbl:405-425).
public sealed class HousePolicyService(IPolicyRepository policies, IUnitOfWork unitOfWork)
    : PolicyService<HousePolicy>(policies, unitOfWork)
{
    protected override string PolicyType => "H";

    // lgupdb01.cbl:431-442
    protected override void CopyChanges(HousePolicy house, HousePolicy changes)
    {
        house.PropertyType = changes.PropertyType;
        house.Bedrooms = changes.Bedrooms;
        house.Value = changes.Value;
        house.HouseName = changes.HouseName;
        house.HouseNumber = changes.HouseNumber;
        house.Postcode = changes.Postcode;
    }
}
