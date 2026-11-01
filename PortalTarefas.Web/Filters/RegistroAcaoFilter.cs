using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PortalTarefas.Web.Filters;

public class RegistroAcaoFilter(ILogger<RegistroAcaoFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tempo = Stopwatch.StartNew();
        logger.LogInformation("FILTRO INICIO {Acao} ModeloValido={Valido} | {Id}",
            context.ActionDescriptor.DisplayName, context.ModelState.IsValid, context.HttpContext.TraceIdentifier);
        var executado = await next();
        logger.LogInformation("FILTRO FIM {Acao} Resultado={Resultado} DuracaoMs={Duracao} | {Id}",
            context.ActionDescriptor.DisplayName, executado.Result?.GetType().Name ?? "Excecao",
            tempo.Elapsed.TotalMilliseconds, context.HttpContext.TraceIdentifier);
    }
}
