using GenApp.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.Customers;

// Customer operations of docs/api-design.md section 2.1 (legacy screen LGTESTC1).
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public sealed class CustomersController(CustomerService service) : ControllerBase
{
    /// <summary>Müşteri ekle (LGACUS01).</summary>
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Add(CustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddAsync(request.ToCustomer(), cancellationToken);
        var customer = CustomerResponse.From(result.Value!);

        return CreatedAtAction(nameof(Inquire), new { customerNumber = customer.CustomerNumber }, customer);
    }

    /// <summary>Müşteri sorgula (LGICUS01).</summary>
    [HttpGet("{customerNumber:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Inquire(int customerNumber, CancellationToken cancellationToken)
    {
        var result = await service.InquireAsync(customerNumber, cancellationToken);

        return result.IsSuccess
            ? Ok(CustomerResponse.From(result.Value!))
            : NotFoundProblem(result.ReturnCode, customerNumber);
    }

    /// <summary>Müşteri güncelle (LGUCUS01).</summary>
    [HttpPut("{customerNumber:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Update(int customerNumber, CustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(request.ToCustomer(customerNumber), cancellationToken);

        return result.IsSuccess
            ? Ok(CustomerResponse.From(result.Value!))
            : NotFoundProblem(result.ReturnCode, customerNumber);
    }

    private static IActionResult NotFoundProblem(string returnCode, int customerNumber) =>
        ReturnCodeProblems.For(returnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();
}
