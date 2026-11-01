using Microsoft.AspNetCore.Mvc;
using PortalTarefas.Web.Services;

namespace PortalTarefas.Web.ViewComponents;

public class TarefasVencidasViewComponent(TarefaRepository repository) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() => View(await repository.VencidasAsync(DateOnly.FromDateTime(DateTime.Today)));
}
