using GenApp.Application;
using GenApp.Application.Policies;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.EndowmentPolicies;

// Endowment policy operations of docs/api-design.md section 2.4 (legacy screen LGTESTP2).
[ApiController]
[Route("api/customers/{customerNumber:int}/endowment-policies")]
[Produces("application/json")]
public sealed class EndowmentPoliciesController(EndowmentPolicyService service) : ControllerBase
{
    /// <summary>Hayat sigortası poliçesi ekle (LGAPOL01, 01AEND).</summary>
    [HttpPost]
    [ProducesResponseType<EndowmentPolicyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Add(int customerNumber, EndowmentPolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddAsync(request.ToPolicy(customerNumber), request.ToEndowmentPolicy(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ReturnCodeProblems.For(result.ReturnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();
        }

        var policy = EndowmentPolicyResponse.From(result.Value!);
        return CreatedAtAction(nameof(Inquire), new { customerNumber, policyNumber = policy.PolicyNumber }, policy);
    }

    /// <summary>Hayat sigortası poliçesi sorgula (LGIPOL01, 01IEND).</summary>
    [HttpGet("{policyNumber:int}")]
    [ProducesResponseType<EndowmentPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Inquire(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(EndowmentPolicyResponse.From(result.Value!))
            : Problem(result.ReturnCode, customerNumber, policyNumber);
    }

    /// <summary>Hayat sigortası poliçesi güncelle (LGUPOL01, 01UEND).</summary>
    [HttpPut("{policyNumber:int}")]
    [ProducesResponseType<EndowmentPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Update(
        int customerNumber, int policyNumber, EndowmentPolicyUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            customerNumber, policyNumber, request.ToPolicy(customerNumber), request.ToEndowmentPolicy(),
            request.LastChangedValue, cancellationToken);

        return result.IsSuccess
            ? Ok(EndowmentPolicyResponse.From(result.Value!))
            : Problem(result.ReturnCode, customerNumber, policyNumber);
    }

    /// <summary>Hayat sigortası poliçesi sil (LGDPOL01, 01DEND).</summary>
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
            _ => $"{customerNumber} numaralı müşterinin {policyNumber} numaralı hayat sigortası poliçesi bulunamadı.",
        }).ToResult();
}
