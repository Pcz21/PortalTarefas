using Microsoft.EntityFrameworkCore;
using PortalTarefas.Web.Models;
namespace PortalTarefas.Web.Data;
public static class Seed
{
    public static async Task ExecutarAsync(PortalTarefasDbContext db)
    {
        foreach (var titulo in new[] { "[Seed] Estudar pipeline HTTP", "[Seed] Revisar formulários MVC", "[Seed] Demonstrar persistência SQL" })
            if (!await db.Tarefas.AnyAsync(t => t.Titulo == titulo))
                db.Tarefas.Add(new Tarefa { Titulo = titulo, Descricao = "Dado didático inicial" });
        await db.SaveChangesAsync();
    }
}
