using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace PortalTarefas.Web.Security;
public static class IdentitySeed
{
    public static async Task ExecutarAsync(IServiceProvider services, IConfiguration config)
    {
        var users=services.GetRequiredService<UserManager<IdentityUser>>();
        var roles=services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach(var role in new[]{"Usuario","Administrador"})
            if(!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        foreach(var (key,email,admin) in new[]{("Aluno","aluno@portaltarefas.test",false),("Admin","admin@portaltarefas.test",true),("Equipe","equipe@portaltarefas.test",false)})
        {
            var user=await users.FindByEmailAsync(email);
            if(user is null)
            {
                var password=config[$"Bootstrap:{key}Password"];
                if(string.IsNullOrWhiteSpace(password))continue;
                user=new IdentityUser{UserName=email,Email=email,EmailConfirmed=true};
                var result=await users.CreateAsync(user,password);
                if(!result.Succeeded)throw new InvalidOperationException("Credencial de bootstrap não atende aos requisitos de senha.");
            }
            if(!await users.IsInRoleAsync(user,"Usuario"))await users.AddToRoleAsync(user,"Usuario");
            if(admin && !await users.IsInRoleAsync(user,"Administrador"))await users.AddToRoleAsync(user,"Administrador");
            if(admin && !(await users.GetClaimsAsync(user)).Any(c=>c.Type=="permissao" && c.Value=="editar-outras-equipes"))
                await users.AddClaimAsync(user,new Claim("permissao","editar-outras-equipes"));
        }
    }
}
