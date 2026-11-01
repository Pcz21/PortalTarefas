# Autenticação e autorização

O navegador usa Identity com cookie protegido porque SPA e API são publicados na mesma origem. O cookie é HttpOnly, Secure e SameSite Strict, dura 30 minutos e tem renovação deslizante. Não há token em localStorage. Tokens bearer proprietários do Identity ou JWT seriam apropriados a clientes com outras necessidades; não foram adicionados sem um emissor OAuth/OIDC e sem necessidade de cliente externo.

CSRF protege login, logout e operações que alteram dados. A SPA busca `/auth/csrf` e envia X-CSRF-TOKEN. A sessão é restaurada por `/auth/me`. Senhas exigem 12 caracteres, maiúscula, minúscula, número e símbolo; cinco falhas bloqueiam o usuário por cinco minutos.

| Operação | Anônimo | Usuario | Administrador |
|---|---|---|---|
| Listar tarefas visíveis | 401 | 200 | 200 |
| Criar tarefa própria | 401 | 201 | 201 |
| Ler tarefa de outro usuário | 401 | 404 | 200 |
| Editar tarefa de outra equipe | 401 | 403 | 200 com claim |
| Excluir tarefa | 401 | 403 | 204 |
| Política outras-equipes | 401 | 403 | 200 |

As tarefas seed sem proprietário podem ser lidas por usuários autenticados e editadas pelo administrador. Os endpoints legados e as telas MVC também são protegidos; os diagnósticos de desenvolvimento são restritos ao administrador. A autorização do servidor permanece efetiva independentemente dos links exibidos na SPA.

## Preparar um acesso local

Configure a conexão e três senhas próprias em User Secrets. Os usuários de demonstração são aluno@portaltarefas.test, admin@portaltarefas.test e equipe@portaltarefas.test. Nenhuma senha acompanha o repositório.

```powershell
dotnet user-secrets set "Bootstrap:AlunoPassword" "SUA_SENHA_FORTE" --project PortalTarefas.Web
dotnet user-secrets set "Bootstrap:AdminPassword" "OUTRA_SENHA_FORTE" --project PortalTarefas.Web
dotnet user-secrets set "Bootstrap:EquipePassword" "TERCEIRA_SENHA_FORTE" --project PortalTarefas.Web
```

Substitua os marcadores por valores próprios. Os marcadores acima não são senhas válidas do projeto. O seed só cria usuários ausentes, sem redefinir senhas já existentes. Para recuperação de acesso existente, use o procedimento local com UserManager e token de redefinição; não altere hashes manualmente.
