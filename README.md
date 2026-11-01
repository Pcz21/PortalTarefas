# PortalTarefas

Aplicação acadêmica ASP.NET Core 10, MVC/Razor, EF Core/SQL Server, React/TypeScript, Identity e OpenTelemetry.

## Executar localmente

Requer SDK .NET 10.0.401, Node.js 24, npm e SQL Server.
Configure ConnectionStrings:PortalTarefas e Bootstrap:AlunoPassword, AdminPassword e EquipePassword usando dotnet user-secrets --project PortalTarefas.Web. As senhas exigem 12 caracteres com maiúscula, minúscula, número e símbolo. Não inclua segredos no código.

```powershell
dotnet tool restore
dotnet restore PortalTarefas.slnx --locked-mode
dotnet dev-certs https --trust
dotnet ef database update --project PortalTarefas.Web
./scripts/publicar-frontend.ps1
dotnet run --project PortalTarefas.Web
```

Abra https://localhost:7157/spa/. Consulte documentacao/AUTENTICACAO.md e documentacao/OPERACAO_E_DEPLOY.md.

## Validar

```powershell
./scripts/ci.ps1
docker build -t portaltarefas:local .
```

Docker, staging e rollback foram verificados localmente. Workflows remotos ainda não executados. A configuração de ambientes, runner e hospedagem deve anteceder o workflow manual de implantação.

Esta cópia técnica não contém relatórios acadêmicos, RA, prints, roteiro oficial ou histórico Git do projeto original.
