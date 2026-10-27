using GenApp.Application;
using GenApp.Application.Policies;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.HousePolicies;

// House policy operations of docs/api-design.md section 2.3 (legacy screen LGTESTP3).
[ApiController]
[Route("api/customers/{customerNumber:int}/house-policies")]
[Produces("application/json")]
public sealed class HousePoliciesController(HousePolicyService service) : ControllerBase
{
    /// <summary>Konut poliçesi ekle (LGAPOL01, 01AHOU).</summary>
    [HttpPost]
    [ProducesResponseType<HousePolicyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Add(int customerNumber, HousePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddAsync(request.ToPolicy(customerNumber), request.ToHousePolicy(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ReturnCodeProblems.For(result.ReturnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();
        }

        var policy = HousePolicyResponse.From(result.Value!);
        return CreatedAtAction(nameof(Inquire), new { customerNumber, policyNumber = policy.PolicyNumber }, policy);
    }

    /// <summary>Konut poliçesi sorgula (LGIPOL01, 01IHOU).</summary>
    [HttpGet("{policyNumber:int}")]
    [ProducesResponseType<HousePolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Inquire(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(HousePolicyResponse.From(result.Value!))
            : Problem(result.ReturnCode, customerNumber, policyNumber);
    }

    /// <summary>Konut poliçesi güncelle (LGUPOL01, 01UHOU).</summary>
    [HttpPut("{policyNumber:int}")]
    [ProducesResponseType<HousePolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Update(
        int customerNumber, int policyNumber, HousePolicyUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            customerNumber, policyNumber, request.ToPolicy(customerNumber), request.ToHousePolicy(),
            request.LastChangedValue, cancellationToken);

        return result.IsSuccess
            ? Ok(HousePolicyResponse.From(result.Value!))
            : Problem(result.ReturnCode, customerNumber, policyNumber);
    }

    /// <summary>Konut poliçesi sil (LGDPOL01, 01DHOU).</summary>
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
            _ => $"{customerNumber} numaralı müşterinin {policyNumber} numaralı konut poliçesi bulunamadı.",
        }).ToResult();
}
