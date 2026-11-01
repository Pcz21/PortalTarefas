> Estado atualizado após autorização do titular: repositório público em https://github.com/Pcz21/PortalTarefas . O CI já foi executado com sucesso no GitHub; resultados em https://github.com/Pcz21/PortalTarefas/actions/workflows/ci.yml . A implantação externa e a execução do workflow de deploy continuam pendentes. As seções abaixo preservam o histórico de preparação e os procedimentos de operação.

# Operação e implantação

A revisão ao final deste documento registra Docker, staging e rollback locais comprovados. CI/CD remoto aguarda autorização.

## Configurações externas

| Variável | Finalidade | Regra |
|---|---|---|
| ASPNETCORE_ENVIRONMENT | Development ou Production | Production não expõe Scalar, OpenAPI e falha didática |
| ConnectionStrings__PortalTarefas | Conexão SQL Server | Obrigatória; User Secrets local ou secret store do provedor |
| Database__Inicializar | Aplicar migrations e seed | true somente quando a inicialização for deliberadamente autorizada; padrão Production false |
| Dependencia__BaseUrl | Serviço HTTP essencial adotado | No laboratório, http://127.0.0.1:5188; configurável externamente |
| Https__Redirecionar | Redirecionamento HTTPS | true no perfil HTTPS; false somente atrás de terminação TLS ou demonstração HTTP |
| Https__Porta | Porta HTTPS | 7157 no laboratório |
| Bootstrap__AlunoPassword | Criar usuário demonstrativo ausente | Segredo externo forte; nunca copiar para imagem ou ZIP |
| Bootstrap__AdminPassword | Criar administrador ausente | Segredo externo forte |
| Bootstrap__EquipePassword | Criar segunda equipe ausente | Segredo externo forte |

Cookies Secure exigem HTTPS no navegador. Em contêiner, use terminação TLS no proxy do ambiente e configure encaminhamento de cabeçalhos apenas de proxies confiáveis. Não exponha uma demonstração HTTP como implantação segura de produção. Defina armazenamento persistente e proteção das chaves de Data Protection antes de múltiplas réplicas ou reinicializações em produção.

## Pipeline

`.github/workflows/ci.yml` define pull requests e main, SDK e Node explícitos, restore com lock, build, testes .NET, npm ci, testes da camada cliente, build React, publicação e imagem identificada por commit. O upload depende do sucesso das etapas anteriores. `scripts/ci.ps1` executa a mesma sequência localmente e interrompe antes do publish quando um teste falha.

O teste controlado usa a variável PORTAL_FORCE_TEST_FAILURE apenas no processo de testes para tornar uma asserção falsa. O registro mostra o teste falhando e confirma que o diretório de publicação não foi criado. Isso comprova o bloqueio local; não é uma execução do GitHub Actions.

## Imagem e staging

```powershell
$revision = git rev-parse HEAD
docker build --label "org.opencontainers.image.revision=$revision" -t "portaltarefas:$revision" .
docker run --rm --name portaltarefas-staging -p 8080:8080 --env-file CAMINHO_EXTERNO_ENV "portaltarefas:$revision"
Invoke-WebRequest http://localhost:8080/health/live
Invoke-WebRequest http://localhost:8080/health/ready
```

O arquivo externo de ambiente não deve ser salvo no repositório. SQL Server em contêiner ou hospedagem deve usar uma conexão compatível com esse ambiente; autenticação integrada do Windows não pode ser presumida no Linux. Execute migrations por processo controlado antes da promoção.

O Dockerfile usa etapas separadas para React, .NET e runtime e executa o runtime com APP_UID não privilegiado. O build React fica incorporado aos estáticos da API, reduzindo origens, configuração de CORS e artefatos de implantação. A opção de front-end separado daria ciclos independentes de publicação, ao custo de configurar mais um serviço e sua política de origem.

## Promoção e rollback

Guarde a imagem pelo SHA do commit, o inventário de dependências e o resultado dos testes. Promova somente a mesma imagem aprovada em staging. Para rollback, volte à imagem anterior validada, confirme compatibilidade das migrations e repita liveness, readiness, login e consulta. Alterações destrutivas de esquema exigem estratégia de dados própria, não apenas retorno de imagem.

Não há envio automático à faculdade. Não há credenciais de hospedagem nem push remoto configurados nesta cópia. A execução remota do workflow e a implantação externa dependem da escolha e configuração de um repositório e de um ambiente de teste.
## Revisão da Unidade 8 em 1 de novembro de 2026

Docker e WSL2 estavam operacionais no começo desta revisão. O log histórico confirma o erro de acesso/renomeação do socket sailor-ingest.sock; o log posterior registra o engine em execução. Não se comprovou a causa raiz nem qual ação externa recuperou o engine. Nenhum reset, exclusão de socket ou reinício global foi necessário.

Build Linux, UID 1654 sem root, health check Docker, Production, login/CSRF e SQL isolado foram testados. A troca para imagem candidata e o retorno à imagem anterior preservaram tarefa e sessão. As duas imagens do ensaio possuem o mesmo binário e metadados de release diferentes; o teste comprova o mecanismo de retorno, não uma mudança funcional nem rollback de schema.

`scripts/deploy-container.ps1` resolve a imagem pelo ID, preserva o contêiner anterior, executa liveness/readiness e, se o candidato falhar, restaura o anterior e verifica readiness. A execução válida e a falha intencional por conexão vazia foram verificadas. Os registros estão em evidencias/unidade-8/revisao-docker/deploy. Não há remoção de volumes ou contêineres pelo script. A troca tem uma breve interrupção; não foi implementada promoção sem downtime.

### Executar o mecanismo local com configuração externa

```powershell
.\scripts\deploy-container.ps1 -ImageRef IMAGEM_LOCAL_APROVADA -ContainerName portaltarefas-staging -EnvFile CAMINHO_EXTERNO_ENV -Network REDE_DO_PORTAL -CertificateDirectory DIRETORIO_EXTERNO_TLS -KeyVolume VOLUME_CHAVES_DO_PORTAL
```

Use um arquivo externo de ambiente com conexão, URLs HTTP 8080 e HTTPS 8443 e caminho/senha de PFX do próprio ambiente. O diretório de TLS é montado em /certs. Nunca coloque esses valores no repositório. Volumes de chaves precisam permitir escrita pelo UID 1654. Mantenha banco e chaves persistentes com política de backup; use proteção das chaves adequada ao host. Migrations destrutivas não são revertidas automaticamente.

### CI e implantação remota preparados e ainda não executados

CI gera imagem TAR, checksum SHA256, ID imutável e testes como artefato identificado pelo SHA. O workflow manual deploy.yml valida conclusão bem-sucedida do CI no mesmo SHA, branch main e workflow ci.yml. Depois confere o TAR e executa o script local de implantação. A entrada de um CI de pull request não é aceita para deploy.

Antes de usar, após autorização de publicação:

1. Criar ou escolher repositório privado Pcz21/PortalTarefas e publicar a cópia de código revisada. Não publicar o histórico acadêmico existente, que contém relatórios e roteiro oficial.
2. Executar CI em main e conferir resultado, checksum e artefatos. A análise estática local com actionlint passou, mas não substitui essa execução.
3. Preparar host Linux dedicado com Docker, PowerShell 7, gh e jq; cadastrar runner self-hosted com rótulo portaltarefas. Não usar esse runner para jobs de código não revisado.
4. Criar environments staging e production, restringir branches e configurar revisores conforme recursos disponíveis no plano GitHub. A existência dessas regras deve ser conferida no GitHub; não foram criadas nesta revisão.
5. Definir, por environment, as variáveis PORTAL_ENV_FILE, PORTAL_CERT_DIRECTORY, PORTAL_NETWORK, PORTAL_KEY_VOLUME, PORTAL_HTTP_PORT e PORTAL_HTTPS_PORT. Os caminhos apontam para arquivos privados já provisionados no host; valores de senhas não são variables do GitHub.
6. Separar portas, bancos e volumes de staging e production. As portas publicadas pelo script são de loopback; configure proxy/TLS e acesso externo no host escolhido. Não há hospedagem externa configurada.
7. Acionar implantação manual pelo ID do CI e SHA aprovado. Conferir live/ready, login e consulta pelo endereço externo. Para retorno manual, acionar o mesmo workflow com um CI anterior aprovado e compatível com o schema atual. A falha de saúde do candidato aciona o retorno automático.

O conector GitHub está autenticado como Pcz21. A CLI gh não está autenticada. A identidade de commit foi configurada somente neste repositório com nome do aluno e email noreply. Nenhum remoto foi criado, nenhum push/upload ocorreu e nenhum workflow remoto foi acionado.

### Recursos de teste preservados

Rede portaltarefas-u8-revisao, volumes portaltarefas-u8-sql-dados e portaltarefas-u8-keys e contêineres com prefixo portaltarefas-u8 foram criados somente para este ensaio. Segredos e certificado efêmero estão em qa/u8-container, fora do Git e da entrega. O certificado vale três dias e não deve ser reutilizado para hospedagem. Os contêineres Sqlserver e Mysql e seus volumes existentes foram apenas listados, sem iniciar, parar ou excluir.

Referências consultadas: https://docs.docker.com/desktop/features/wsl/ ; https://docs.docker.com/desktop/settings-and-maintenance/backup-and-restore/ ; https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments ; https://github.com/rhysd/actionlint/releases/tag/v1.7.12 .
