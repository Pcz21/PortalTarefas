using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PortalTarefas.Web.Data;
using PortalTarefas.Web.Models;
using System.Security.Claims;
namespace PortalTarefas.Web.Services;
public class TarefaRepository(PortalTarefasDbContext db, IHttpContextAccessor accessor)
{
    private IQueryable<Tarefa> Visiveis()
    {
        var user=accessor.HttpContext?.User;var uid=user?.FindFirstValue(ClaimTypes.NameIdentifier);
        return user?.IsInRole("Administrador")==true ? db.Tarefas : db.Tarefas.Where(t=>t.ProprietarioId==null || t.ProprietarioId==uid);
    }
    public async Task<IReadOnlyList<Tarefa>> ListarAsync(bool? concluida = null) => await Visiveis().AsNoTracking()
        .Where(t => concluida == null || t.Concluida == concluida).OrderBy(t => t.Id).ToArrayAsync();
    public Task<Tarefa?> ObterAsync(int id) => Visiveis().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    public async Task<Tarefa> CriarAsync(TarefaEntrada entrada, Prioridade prioridade = Prioridade.Normal, DateOnly? prazo = null, string? proprietarioId = null)
    {
        using var span=Observabilidade.Traces.StartActivity("tarefa.criar");
        var inicio=System.Diagnostics.Stopwatch.StartNew();
        var tarefa = new Tarefa { Titulo = entrada.Titulo, Descricao = entrada.Descricao?.Trim(),
            Concluida = entrada.Concluida, Prioridade = prioridade, Prazo = prazo, ProprietarioId = proprietarioId ?? accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) };
        db.Tarefas.Add(tarefa); await db.SaveChangesAsync();
        Observabilidade.RegistrarCriacao();Observabilidade.Duracao.Record(inicio.Elapsed.TotalMilliseconds,new KeyValuePair<string,object?>("operacao","criar"));return tarefa;
    }
    public async Task<bool> AtualizarAsync(int id, TarefaEntrada entrada, Prioridade prioridade, DateOnly? prazo, byte[] versao, bool provocarFalha = false)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        var tarefa = await db.Tarefas.FirstOrDefaultAsync(t => t.Id == id);
        if (tarefa is null) return false;
        var user=accessor.HttpContext?.User;
        if(tarefa.ProprietarioId!=user?.FindFirstValue(ClaimTypes.NameIdentifier) && user?.HasClaim("permissao","editar-outras-equipes")!=true)
            throw new UnauthorizedAccessException("Sem permissão para editar esta tarefa.");
        // Mesmo um PUT idempotente deve rejeitar token obsoleto; sem mudanças,
        // o EF não emitiria UPDATE e portanto não avaliaria o rowversion no SQL.
        if (!tarefa.Versao.SequenceEqual(versao)) throw new DbUpdateConcurrencyException("Versão desatualizada.");
        db.Entry(tarefa).Property(t => t.Versao).OriginalValue = versao;
        tarefa.Titulo = entrada.Titulo; tarefa.Descricao = entrada.Descricao?.Trim();
        tarefa.Concluida = entrada.Concluida; tarefa.Prioridade = prioridade; tarefa.Prazo = prazo;
        await db.SaveChangesAsync();
        if (provocarFalha) throw new InvalidOperationException("Falha didática antes da auditoria.");
        db.Eventos.Add(new EventoTarefa { TarefaId = id, Operacao = "Edição", InstanteUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(); await transacao.CommitAsync(); return true;
    }
    public async Task<IReadOnlyList<Tarefa>> VencidasAsync(DateOnly hoje) => await Visiveis().AsNoTracking()
        .Where(t => !t.Concluida && t.Prazo < hoje).OrderBy(t => t.Prazo).ToArrayAsync();
    public Task<int> PainelEfAsync(bool concluida) => db.Tarefas.AsNoTracking().Where(t => t.Concluida == concluida).Select(t => t.Id).CountAsync();
    public async Task<int> PainelAdoAsync(bool concluida)
    {
        await using var connection = new SqlConnection(db.Database.GetConnectionString()); await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM Tarefas WHERE Concluida = @concluida", connection);
        command.Parameters.Add("@concluida", System.Data.SqlDbType.Bit).Value = concluida;
        return (int)(await command.ExecuteScalarAsync())!;
    }
    public async Task<object[]> AuditoriaAsync(int id) => await db.Eventos.AsNoTracking().Where(e => e.TarefaId == id)
        .Select(e => (object)new { e.Id, e.TarefaId, e.Operacao, e.InstanteUtc }).ToArrayAsync();
}
