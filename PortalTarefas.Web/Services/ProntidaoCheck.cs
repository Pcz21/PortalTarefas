using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortalTarefas.Web.Data;
namespace PortalTarefas.Web.Services;
public class ProntidaoCheck(IServiceScopeFactory scopes,IHttpClientFactory clients,IConfiguration config):IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default)
    {
        try
        {
            await using var scope=scopes.CreateAsyncScope();
            var db=scope.ServiceProvider.GetRequiredService<PortalTarefasDbContext>();
            if(!await db.Database.CanConnectAsync(cancellationToken))return HealthCheckResult.Unhealthy();
            var url=config["Dependencia:BaseUrl"];
            if(!string.IsNullOrWhiteSpace(url))
            {
                using var response=await clients.CreateClient("health").GetAsync(url.TrimEnd('/')+"/service",cancellationToken);
                if(!response.IsSuccessStatusCode)return HealthCheckResult.Unhealthy();
            }
            return HealthCheckResult.Healthy();
        }
        catch(Exception){return HealthCheckResult.Unhealthy();}
    }
}
