using GenApp.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.Customers;

// Customer operations of docs/api-design.md section 2.1 (legacy screen LGTESTC1).
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public sealed class CustomersController(CustomerService service) : ControllerBase
{
    /// <summary>Müşteri sorgula (LGICUS01).</summary>
    [HttpGet("{customerNumber:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Inquire(int customerNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireAsync(customerNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(CustomerResponse.From(result.Value!))
            : ReturnCodeProblems.For(result.ReturnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();
    }
}
