using System.Diagnostics;
using System.Globalization;

namespace PortalTarefas.Web.Middleware;

public class RegistroRequisicaoMiddleware(RequestDelegate next, ILogger<RegistroRequisicaoMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var cronometro = Stopwatch.StartNew();
        using var escopo=logger.BeginScope(new Dictionary<string,object?>{["TraceId"]=System.Diagnostics.Activity.Current?.TraceId.ToString(),["RequisicaoId"]=context.TraceIdentifier});
        logger.LogInformation("ENTRADA {Metodo} {Caminho} | {Id}", context.Request.Method, context.Request.Path, context.TraceIdentifier);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Requisicao-Id"] = context.TraceIdentifier;
            context.Response.Headers["X-Tempo-Ms"] = cronometro.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });
        try { await next(context); }
        finally
        {
            logger.LogInformation("SAIDA {Metodo} {Caminho} => {Status} em {Tempo} ms | {Id}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                cronometro.ElapsedMilliseconds, context.TraceIdentifier);
        }
    }
}

public static class RegistroRequisicaoExtensions
{
    public static IApplicationBuilder UseRegistroRequisicao(this IApplicationBuilder app)
        => app.UseMiddleware<RegistroRequisicaoMiddleware>();
}
