using Microsoft.AspNetCore.Mvc;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Services;

namespace PortalTarefas.Web.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[Route("mvc-api/tarefas")]
public class TarefasApiController(TarefaRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool? concluida) => Ok(await repository.ListarAsync(concluida));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id) => await repository.ObterAsync(id) is { } tarefa
        ? Ok(tarefa) : NotFound(new { mensagem = "Tarefa não encontrada." });

    // ApiController executa DataAnnotations antes de chamar a ação.
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] TarefaEntrada entrada)
    {
        var tarefa = await repository.CriarAsync(entrada);
        return CreatedAtAction(nameof(Obter), new { id = tarefa.Id }, tarefa);
    }
}
