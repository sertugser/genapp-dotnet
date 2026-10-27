using GenApp.Application.Persistence;
using GenApp.Domain.Policies;

namespace GenApp.Application.Policies;

// Replaces LGAPOL01, LGIPOL01, LGUPOL01 and LGDPOL01 for motor policies (request IDs 01AMOT, 01IMOT, 01UMOT, 01DMOT;
// legacy-analysis/use-cases/04-07). Inquire returns 01 when there is no motor row (lgipdb01.cbl:564-569, :608-612).
public sealed class MotorPolicyService(IPolicyRepository policies, IUnitOfWork unitOfWork)
    : PolicyService<MotorPolicy>(policies, unitOfWork)
{
    protected override string PolicyType => "M";

    // lgupdb01.cbl:469-483
    protected override void CopyChanges(MotorPolicy motor, MotorPolicy changes)
    {
        motor.Make = changes.Make;
        motor.Model = changes.Model;
        motor.Value = changes.Value;
        motor.RegNumber = changes.RegNumber;
        motor.Colour = changes.Colour;
        motor.Cc = changes.Cc;
        motor.YearOfManufacture = changes.YearOfManufacture;
        motor.Premium = changes.Premium;
        motor.Accidents = changes.Accidents;
    }
}
