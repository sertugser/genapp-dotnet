using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Hatalı istekleri API tasarımındaki biçimle (type: validation-error, errors listesi) 400 olarak döndürür.
/// [ApiController]'ın otomatik model doğrulama filtresinden (Order -2000) önce çalışır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidationErrorFilterAttribute : ActionFilterAttribute
{
    public ValidationErrorFilterAttribute()
    {
        Order = -3000;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
            Detail = "İstekte eksik ya da hatalı alanlar var."
        };

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        context.Result = result;
    }
}
