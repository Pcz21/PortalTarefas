using Microsoft.EntityFrameworkCore;
using PortalTarefas.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace PortalTarefas.Web.Data;
// Sem estado de usuário ou de requisição: seguro para pooling.
public class PortalTarefasDbContext(DbContextOptions<PortalTarefasDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<EventoTarefa> Eventos => Set<EventoTarefa>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        var t = model.Entity<Tarefa>();
        t.Property(x => x.Titulo).HasMaxLength(100).IsRequired();
        t.Property(x => x.Descricao).HasMaxLength(500);
        t.Property(x => x.ProprietarioId).HasMaxLength(450);
        t.Property(x => x.Versao).IsRowVersion();
        t.HasIndex(x => new { x.Concluida, x.Prazo });
        var e = model.Entity<EventoTarefa>();
        e.Property(x => x.Operacao).HasMaxLength(80).IsRequired();
        e.HasOne<Tarefa>().WithMany().HasForeignKey(x => x.TarefaId).OnDelete(DeleteBehavior.Cascade);
    }
}
