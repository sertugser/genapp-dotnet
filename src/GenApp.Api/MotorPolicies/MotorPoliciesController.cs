using GenApp.Api.Policies;
using GenApp.Application;
using GenApp.Application.Policies;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.MotorPolicies;

// Motor policy operations of docs/api-design.md section 2.2 (legacy screen LGTESTP1).
[ApiController]
[Route("api/customers/{customerNumber:int}/motor-policies")]
[Produces("application/json")]
public sealed class MotorPoliciesController(MotorPolicyService service) : ControllerBase
{
    private const string TypeName = "motor poliçesi";

    /// <summary>Motor poliçesi ekle (LGAPOL01, 01AMOT).</summary>
    [HttpPost]
    [ProducesResponseType<MotorPolicyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Add(int customerNumber, MotorPolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddAsync(request.ToPolicy(customerNumber), request.ToMotorPolicy(), cancellationToken);
        if (!result.IsSuccess)
        {
            return PolicyProblems.CustomerNotFound(result.ReturnCode, customerNumber);
        }

        var policy = MotorPolicyResponse.From(result.Value!);
        return CreatedAtAction(nameof(Inquire), new { customerNumber, policyNumber = policy.PolicyNumber }, policy);
    }

    /// <summary>Motor poliçesi sorgula (LGIPOL01, 01IMOT).</summary>
    [HttpGet("{policyNumber:int}")]
    [ProducesResponseType<MotorPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Inquire(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(MotorPolicyResponse.From(result.Value!))
            : PolicyProblems.For(result.ReturnCode, customerNumber, policyNumber, TypeName);
    }

    /// <summary>Motor poliçesi güncelle (LGUPOL01, 01UMOT).</summary>
    [HttpPut("{policyNumber:int}")]
    [ProducesResponseType<MotorPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Update(
        int customerNumber, int policyNumber, MotorPolicyUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            customerNumber, policyNumber, request.ToPolicy(customerNumber), request.ToMotorPolicy(),
            request.LastChangedValue, cancellationToken);

        return result.IsSuccess
            ? Ok(MotorPolicyResponse.From(result.Value!))
            : PolicyProblems.For(result.ReturnCode, customerNumber, policyNumber, TypeName);
    }

    /// <summary>Motor poliçesi sil (LGDPOL01, 01DMOT).</summary>
    [HttpDelete("{policyNumber:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> Delete(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var returnCode = await service.DeleteAsync(customerNumber, policyNumber, cancellationToken);

        return returnCode == ReturnCodes.Success
            ? NoContent()
            : PolicyProblems.For(returnCode, customerNumber, policyNumber, TypeName);
    }
}
