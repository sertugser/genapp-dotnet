using System.ComponentModel.DataAnnotations;
using GenApp.Application;
using GenApp.Application.Policies;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.CommercialPolicies;

// Commercial policy operations of docs/api-design.md section 2.5 (legacy screen LGTESTP4). No update: the legacy
// system has none. 17b and 17d have no customer in the address, so their routes start at /api.
[ApiController]
[Route("api/customers/{customerNumber:int}/commercial-policies")]
[Produces("application/json")]
public sealed class CommercialPoliciesController(CommercialPolicyService service) : ControllerBase
{
    /// <summary>Ticari poliçe ekle (LGAPOL01, 01ACOM).</summary>
    [HttpPost]
    [ProducesResponseType<CommercialPolicyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Add(int customerNumber, CommercialPolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddAsync(request.ToPolicy(customerNumber), request.ToCommercialPolicy(), cancellationToken);
        if (!result.IsSuccess)
        {
            return ReturnCodeProblems.For(result.ReturnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();
        }

        var policy = CommercialPolicyResponse.From(result.Value!);
        return CreatedAtAction(nameof(Inquire), new { customerNumber, policyNumber = policy.PolicyNumber }, policy);
    }

    /// <summary>Ticari poliçe sorgula: müşteri ve poliçe numarasıyla (LGIPOL01, 01ICOM).</summary>
    [HttpGet("{policyNumber:int}")]
    [ProducesResponseType<CommercialPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Inquire(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(CommercialPolicyResponse.From(result.Value!))
            : Problem(result.ReturnCode, customerNumber, policyNumber);
    }

    /// <summary>Ticari poliçe sorgula: yalnızca poliçe numarasıyla (LGIPOL01, 02ICOM).</summary>
    [HttpGet("/api/commercial-policies/{policyNumber:int}")]
    [ProducesResponseType<CommercialPolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> InquireByPolicy(int policyNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireByPolicyAsync(policyNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(CommercialPolicyResponse.From(result.Value!))
            : ReturnCodeProblems.For(result.ReturnCode, $"{policyNumber} numaralı ticari poliçe bulunamadı.").ToResult();
    }

    /// <summary>Ticari poliçe sorgula: müşteri numarasıyla, liste (LGIPOL01, 03ICOM).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CommercialPolicyResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> InquireByCustomer(int customerNumber, CancellationToken cancellationToken)
    {
        var records = await service.InquireByCustomerAsync(customerNumber, cancellationToken);
        return Ok(records.Select(CommercialPolicyResponse.From).ToList());
    }

    /// <summary>Ticari poliçe sorgula: posta koduyla, liste (LGIPOL01, 05ICOM).</summary>
    [HttpGet("/api/commercial-policies")]
    [ProducesResponseType<IReadOnlyList<CommercialPolicyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> InquireByPostcode(
        [FromQuery, Required(AllowEmptyStrings = true), MaxLength(8)] string postcode, CancellationToken cancellationToken)
    {
        var records = await service.InquireByPostcodeAsync(postcode, cancellationToken);
        return Ok(records.Select(CommercialPolicyResponse.From).ToList());
    }

    /// <summary>Ticari poliçe sil (LGDPOL01, 01DCOM).</summary>
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
        ReturnCodeProblems.For(returnCode, returnCode == ReturnCodes.DatabaseError
            ? $"{customerNumber} numaralı müşterinin {policyNumber} numaralı poliçesi silinemedi."
            : $"{customerNumber} numaralı müşterinin {policyNumber} numaralı ticari poliçesi bulunamadı.").ToResult();
}
