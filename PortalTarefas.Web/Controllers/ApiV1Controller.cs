using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalTarefas.Web.Data;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace PortalTarefas.Web.Controllers;

[ApiController, Route("api/v1/tarefas"),Authorize]
public class ApiV1Controller(PortalTarefasDbContext db, TarefaRepository repo) : ControllerBase
{
    /// <summary>Lista tarefas com paginação e ordenação limitada a campos conhecidos.</summary>
    [HttpGet(Name="ListarTarefas"), ProducesResponseType<PaginaTarefas>(200), ProducesResponseType<ProblemDetails>(400)]
    public async Task<ActionResult<PaginaTarefas>> Listar(int pagina=1, int tamanho=10, string ordenarPor="id",
        string direcao="asc", bool? concluida=null, string? titulo=null, Prioridade? prioridade=null)
    {
        if (pagina < 1 || pagina > 100000 || tamanho < 1 || tamanho > 50 ||
            !new[] {"id","titulo","prazo"}.Contains(ordenarPor) || !new[]{"asc","desc"}.Contains(direcao) ||
            (prioridade.HasValue && !Enum.IsDefined(prioridade.Value)))
            return Problem(statusCode:400,title:"Paginação, ordenação ou filtro inválido.");
        var q=db.Tarefas.AsNoTracking().AsQueryable();
        var uid=User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(!User.IsInRole("Administrador"))q=q.Where(t=>t.ProprietarioId==null || t.ProprietarioId==uid);
        if(concluida.HasValue) q=q.Where(t=>t.Concluida==concluida);
        if(!string.IsNullOrWhiteSpace(titulo)) q=q.Where(t=>t.Titulo.Contains(titulo));
        if(prioridade.HasValue) q=q.Where(t=>t.Prioridade==prioridade);
        var total=await q.CountAsync();
        var desc=direcao=="desc";
        var sorted=ordenarPor switch {
            "titulo"=>desc?q.OrderByDescending(t=>t.Titulo).ThenBy(t=>t.Id):q.OrderBy(t=>t.Titulo).ThenBy(t=>t.Id),
            "prazo"=>desc?q.OrderByDescending(t=>t.Prazo).ThenBy(t=>t.Id):q.OrderBy(t=>t.Prazo).ThenBy(t=>t.Id),
            _=>desc?q.OrderByDescending(t=>t.Id):q.OrderBy(t=>t.Id)
        };
        var itens=await sorted.Skip((pagina-1)*tamanho).Take(tamanho)
            .Select(t=>new ResumoTarefaDto(t.Id,t.Titulo,t.Concluida,t.Prioridade,t.Prazo)).ToArrayAsync();
        Response.Headers["X-Total-Count"]=total.ToString();
        return Ok(new PaginaTarefas(itens,pagina,tamanho,total,(int)Math.Ceiling(total/(double)tamanho)));
    }
    [HttpGet("{id:int}",Name="ObterTarefa"), ProducesResponseType<DetalheTarefaDto>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<DetalheTarefaDto>> Obter(int id)
    {
        var t=await repo.ObterAsync(id);
        return t is null ? Problem(statusCode:404,title:"Tarefa não encontrada.") : Ok(DetalheTarefaDto.De(t));
    }
    [HttpPost(Name="CriarTarefa"), ProducesResponseType<DetalheTarefaDto>(201), ProducesResponseType<ValidationProblemDetails>(400)]
    [Authorize(Roles="Usuario")]
    public async Task<ActionResult<DetalheTarefaDto>> Criar(CriarTarefaDto entrada)
    {
        var t=await repo.CriarAsync(entrada,entrada.Prioridade,entrada.Prazo,User.FindFirstValue(ClaimTypes.NameIdentifier));
        return CreatedAtAction(nameof(Obter),new{id=t.Id},DetalheTarefaDto.De(t));
    }
    [HttpPut("{id:int}",Name="AtualizarTarefa"), ProducesResponseType<DetalheTarefaDto>(200), ProducesResponseType<ProblemDetails>(409), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(400)]
    public async Task<ActionResult<DetalheTarefaDto>> Atualizar(int id,AtualizarTarefaDto entrada)
    {
        var alvo=await db.Tarefas.AsNoTracking().FirstOrDefaultAsync(t=>t.Id==id);
        if(alvo is null)return Problem(statusCode:404,title:"Tarefa não encontrada.");
        if(alvo.ProprietarioId!=User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.HasClaim("permissao","editar-outras-equipes"))return Forbid();
        byte[] versao;
        try{versao=Convert.FromBase64String(entrada.Versao);}catch(FormatException){return Problem(statusCode:400,title:"Versão inválida.");}
        if(versao.Length!=8)return Problem(statusCode:400,title:"Versão inválida.");
        try {
            if(!await repo.AtualizarAsync(id,entrada,entrada.Prioridade,entrada.Prazo,versao))return Problem(statusCode:404,title:"Tarefa não encontrada.");
        }catch(DbUpdateConcurrencyException){return Problem(statusCode:409,title:"Tarefa alterada em outra sessão. Recarregue antes de editar.");}
        return Ok(DetalheTarefaDto.De((await repo.ObterAsync(id))!));
    }
    [HttpDelete("{id:int}",Name="ExcluirTarefa"), ProducesResponseType(204), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409), ProducesResponseType<ProblemDetails>(400)]
    [Authorize(Roles="Administrador")]
    public async Task<IActionResult> Excluir(int id,[FromQuery]string versao)
    {
        byte[] token;try{token=Convert.FromBase64String(versao);}catch(FormatException){return Problem(statusCode:400,title:"Versão inválida.");}
        if(token.Length!=8)return Problem(statusCode:400,title:"Versão inválida.");
        var t=await db.Tarefas.FindAsync(id);if(t is null)return Problem(statusCode:404,title:"Tarefa não encontrada.");
        db.Entry(t).Property(x=>x.Versao).OriginalValue=token;db.Tarefas.Remove(t);
        try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Problem(statusCode:409,title:"Versão desatualizada.");}
        return NoContent();
    }
}
