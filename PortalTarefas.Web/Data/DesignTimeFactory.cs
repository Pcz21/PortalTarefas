using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace PortalTarefas.Web.Data;
public class DesignTimeFactory : IDesignTimeDbContextFactory<PortalTarefasDbContext>
{
    public PortalTarefasDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().AddUserSecrets<PortalTarefasDbContext>(optional: true).AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("PortalTarefas")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:PortalTarefas fora do código.");
        return new(new DbContextOptionsBuilder<PortalTarefasDbContext>().UseSqlServer(connection).Options);
    }
}
