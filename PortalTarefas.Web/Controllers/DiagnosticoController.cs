using Microsoft.AspNetCore.Mvc;

namespace PortalTarefas.Web.Controllers;

[ApiController]
[Route("mvc-api/diagnostico")]
public class DiagnosticoController : ControllerBase
{
    // Mesmo contrato do GET /status, implementado com controller MVC.
    [HttpGet]
    public IActionResult Status() => Ok(new { sistema = "PortalTarefas", funcionando = true });
}
