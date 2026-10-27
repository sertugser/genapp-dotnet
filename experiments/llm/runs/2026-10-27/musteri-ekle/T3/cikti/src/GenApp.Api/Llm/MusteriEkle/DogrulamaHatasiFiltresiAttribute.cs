using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GenApp.Llm.MusteriEkle;

// API tasarımı 3 ('98' notu): hatalı alanlı istekler 400, type "validation-error" ve "errors" listesiyle döner.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DogrulamaHatasiFiltresiAttribute : Attribute, IActionFilter, IOrderedFilter
{
    // [ApiController]'ın ModelStateInvalidFilter'ından (-2000) önce çalışır.
    public int Order => int.MinValue;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

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

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
