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
            return ReturnCodeProblems.For(result.ReturnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();
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
            : Problem(result.ReturnCode, customerNumber, policyNumber);
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
            : Problem(result.ReturnCode, customerNumber, policyNumber);
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
            : Problem(returnCode, customerNumber, policyNumber);
    }

    private static IActionResult Problem(string returnCode, int customerNumber, int policyNumber) =>
        ReturnCodeProblems.For(returnCode, returnCode switch
        {
            ReturnCodes.ConcurrencyConflict =>
                $"{policyNumber} numaralı poliçe siz okuduktan sonra değiştirilmiş. Poliçeyi yeniden sorgulayıp tekrar deneyin.",
            ReturnCodes.DatabaseError =>
                $"{customerNumber} numaralı müşterinin {policyNumber} numaralı poliçesi silinemedi.",
            _ => $"{customerNumber} numaralı müşterinin {policyNumber} numaralı motor poliçesi bulunamadı.",
        }).ToResult();
}
