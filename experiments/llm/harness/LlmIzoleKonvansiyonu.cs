using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace GenApp.Api;

/// <summary>
/// LLM deneyi (experiments/llm/README.md, "A. İzole" düzeni). Yalnızca deney çalışma kopyasına eklenir, repodaki
/// src/'de yoktur. <c>LLM_DISLANAN_ACTIONLAR</c> ortam değişkeninde "Controller.Action" çiftleri (noktalı virgülle)
/// verilir; o action'ların adresi ölü bir ön eke taşınır, böylece gerçek adreste yalnızca LLM'in controller'ı yanıt
/// verir. Action'ın kodu silinmez: <c>CreatedAtAction(nameof(Inquire))</c> gibi bağlantılar çalışmaya devam eder.
/// </summary>
public sealed class LlmIzoleKonvansiyonu : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var dislanan = (Environment.GetEnvironmentVariable("LLM_DISLANAN_ACTIONLAR") ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        foreach (var controller in application.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                if (!dislanan.Contains($"{controller.ControllerType.Name}.{action.ActionMethod.Name}"))
                {
                    continue;
                }

                foreach (var selector in action.Selectors)
                {
                    var sablon = selector.AttributeRouteModel?.Template ?? "";
                    var onEk = controller.Selectors.FirstOrDefault()?.AttributeRouteModel?.Template ?? "";
                    var tam = string.IsNullOrEmpty(sablon) ? onEk : $"{onEk}/{sablon}";
                    selector.AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = $"/llm-dislanan/{tam}",
                    };
                }
            }
        }
    }
}
