using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
namespace PortalTarefas.Web.Controllers;
public class LoginDto
{
    [Required,EmailAddress] public string Email {get;set;}="";
    [Required] public string Senha {get;set;}="";
}
[ApiController,Route("auth")]
public class AuthController(SignInManager<IdentityUser> signIn,UserManager<IdentityUser> users,IAntiforgery antiforgery,ILogger<AuthController> logger):ControllerBase
{
    [HttpGet("csrf"),AllowAnonymous]
    public IActionResult Csrf()=>Ok(new{token=antiforgery.GetAndStoreTokens(HttpContext).RequestToken});
    [HttpPost("login"),AllowAnonymous,ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto entrada)
    {
        var result=await signIn.PasswordSignInAsync(entrada.Email,entrada.Senha,false,true);
        logger.LogInformation("Login Resultado={Resultado} | {Id}",result.Succeeded?"Sucesso":"Negado",HttpContext.TraceIdentifier);
        if(!result.Succeeded)return Problem(statusCode:401,title:"Credenciais inválidas ou acesso temporariamente bloqueado.");
        return Ok(new{autenticado=true});
    }
    [HttpGet("me"),Authorize]
    public async Task<IActionResult> Me()
    {
        var user=await users.GetUserAsync(User);
        if(user is null)return Unauthorized();
        return Ok(new{id=user.Id,email=user.Email,roles=await users.GetRolesAsync(user),podeEditarEquipe=User.HasClaim("permissao","editar-outras-equipes")});
    }
    [HttpPost("logout"),Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return NoContent();}
    [HttpGet("outras-equipes"),Authorize(Policy="EditarOutrasEquipes")]
    public IActionResult Equipes()=>Ok(new{permitido=true});
}
