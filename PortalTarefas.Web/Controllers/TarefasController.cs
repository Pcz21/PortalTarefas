using Microsoft.AspNetCore.Mvc;
using PortalTarefas.Web.Filters;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace PortalTarefas.Web.Controllers;

// Escopo do filtro: somente as telas MVC, preservando o contrato das APIs.
[ServiceFilter(typeof(RegistroAcaoFilter)),Microsoft.AspNetCore.Authorization.Authorize]
public class TarefasController(TarefaRepository repository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await repository.ListarAsync());

    [HttpGet]
    public async Task<IActionResult> Details(int id) => await repository.ObterAsync(id) is { } tarefa
        ? View(tarefa) : NotFound("Tarefa não encontrada.");

    [HttpGet]
    public IActionResult Create() => View(new TarefaInputModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TarefaInputModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var tarefa = await repository.CriarAsync(model, model.Prioridade, model.Prazo);
        TempData["Mensagem"] = "Tarefa cadastrada com sucesso.";
        return RedirectToAction(nameof(Details), new { id = tarefa.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tarefa = await repository.ObterAsync(id);
        if (tarefa is null) return NotFound("Tarefa não encontrada.");
        ViewData["Id"] = id;
        return View(TarefaInputModel.DeTarefa(tarefa));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TarefaInputModel model)
    {
        if (await repository.ObterAsync(id) is null) return NotFound("Tarefa não encontrada.");
        ViewData["Id"] = id;
        if (!ModelState.IsValid) return View(model);
        byte[] versao;
        try { versao = Convert.FromBase64String(model.Versao); }
        catch (FormatException) { ModelState.AddModelError("Versao", "Versão inválida. Reabra a edição."); return View(model); }
        if (versao.Length != 8) { ModelState.AddModelError("Versao", "Reabra a edição para obter a versão atual."); return View(model); }
        try
        {
            if (!await repository.AtualizarAsync(id, model, model.Prioridade, model.Prazo, versao)) return NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            Response.StatusCode = 409;
            ModelState.AddModelError("", "Esta tarefa foi alterada em outra sessão. Reabra os detalhes e revise os dados antes de tentar novamente.");
            return View(model);
        }
        TempData["Mensagem"] = "Tarefa atualizada com sucesso.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
