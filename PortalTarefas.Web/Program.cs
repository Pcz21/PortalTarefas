using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PortalTarefas.Web.Middleware;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Services;
using PortalTarefas.Web.Filters;
using PortalTarefas.Web.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using PortalTarefas.Web.Security;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;

if(args.Contains("--health-check"))
{
    try
    {
        using var probe=new HttpClient{Timeout=TimeSpan.FromSeconds(3)};
        var port=Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';')[0]??"8080";
        using var response=await probe.GetAsync($"http://127.0.0.1:{port}/health/live");
        Environment.ExitCode=response.IsSuccessStatusCode?0:1;
    }
    catch{Environment.ExitCode=1;}
    return;
}
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options=>options.IncludeScopes=true);
builder.Services.AddOpenTelemetry().ConfigureResource(r=>r.AddService("PortalTarefas"))
    .WithTracing(t=>t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource(Observabilidade.Nome).AddConsoleExporter())
    .WithMetrics(m=>m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddMeter(Observabilidade.Nome)
        .AddConsoleExporter((_,reader)=>reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds=5000));
builder.Services.AddHealthChecks().AddCheck<ProntidaoCheck>("prontidao",tags:["ready"]);
builder.Services.AddHttpClient("health",c=>c.Timeout=TimeSpan.FromSeconds(2));
builder.Services.AddHttpClient("dependencia",c=>c.BaseAddress=new Uri(builder.Configuration["Dependencia:BaseUrl"]??"http://localhost:5188"))
    .AddStandardResilienceHandler(options=>
    {
        options.Retry.MaxRetryAttempts=2;options.Retry.Delay=TimeSpan.FromMilliseconds(100);options.Retry.UseJitter=false;
        options.Retry.DisableForUnsafeHttpMethods();
        options.CircuitBreaker.MinimumThroughput=4;options.CircuitBreaker.FailureRatio=0.75;
        options.CircuitBreaker.SamplingDuration=TimeSpan.FromSeconds(10);options.CircuitBreaker.BreakDuration=TimeSpan.FromSeconds(3);
        options.AttemptTimeout.Timeout=TimeSpan.FromSeconds(1);options.TotalRequestTimeout.Timeout=TimeSpan.FromSeconds(4);
    });
builder.Services.AddRateLimiter(options=>
{
    options.RejectionStatusCode=429;
    options.AddPolicy("demonstracao",context=>RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value??context.Connection.RemoteIpAddress?.ToString()??"local",
        _=>new FixedWindowRateLimiterOptions{PermitLimit=3,Window=TimeSpan.FromSeconds(5),QueueLimit=0}));
});
builder.Services.AddOutputCache();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (_, campo) => $"Informe um valor válido para {campo}.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        campo => $"Informe um valor válido para {campo}.");
});
builder.Services.AddScoped<RegistroAcaoFilter>();
var connection = builder.Configuration.GetConnectionString("PortalTarefas")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:PortalTarefas em User Secrets ou variável de ambiente.");
builder.Services.AddOptions<BancoOptions>().Configure(o=>o.Conexao=connection)
    .Validate(o=>!string.IsNullOrWhiteSpace(o.Conexao),"Configure a conexão externa do PortalTarefas.").ValidateOnStart();
builder.Services.AddDbContextPool<PortalTarefasDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<TarefaRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentity<IdentityUser,IdentityRole>(options =>
{
    options.Password.RequiredLength=12;
    options.Password.RequireNonAlphanumeric=true;
    options.Lockout.MaxFailedAccessAttempts=5;
    options.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(5);
    options.User.RequireUniqueEmail=true;
}).AddEntityFrameworkStores<PortalTarefasDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options=>
{
    options.Cookie.Name=".PortalTarefas.Sessao"; options.Cookie.HttpOnly=true;
    options.Cookie.SecurePolicy=CookieSecurePolicy.Always; options.Cookie.SameSite=SameSiteMode.Strict;
    options.ExpireTimeSpan=TimeSpan.FromMinutes(30); options.SlidingExpiration=true;
    options.Events.OnRedirectToLogin=context=>{context.Response.StatusCode=401;return Task.CompletedTask;};
    options.Events.OnRedirectToAccessDenied=context=>{context.Response.StatusCode=403;return Task.CompletedTask;};
});
builder.Services.AddAntiforgery(options=>{options.HeaderName="X-CSRF-TOKEN";options.Cookie.SecurePolicy=CookieSecurePolicy.Always;});
builder.Services.AddAuthorization(options=>options.AddPolicy("EditarOutrasEquipes",p=>p.RequireClaim("permissao","editar-outras-equipes")));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    options.AddSchemaTransformer((schema, context, _) =>
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (type == typeof(int) || type == typeof(long))
        { schema.Type = JsonSchemaType.Integer; schema.AnyOf = null; schema.Pattern = null; }
        return Task.CompletedTask;
    });
});
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddHttpsRedirection(options => options.HttpsPort = builder.Configuration.GetValue("Https:Porta", 7157));
builder.Services.AddCors(options => options.AddPolicy("Laboratorio", policy => policy
    .WithOrigins("http://localhost:5500", "https://localhost:5501")
    .WithMethods("GET", "POST").WithHeaders("Content-Type", "X-Aluno")
    .WithExposedHeaders("X-Requisicao-Id", "X-Tempo-Ms", "Location")));
var app = builder.Build();
if (app.Configuration.GetValue<bool>("Database:Inicializar"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<PortalTarefasDbContext>();
    await db.Database.MigrateAsync();
    await Seed.ExecutarAsync(db);
    await IdentitySeed.ExecutarAsync(scope.ServiceProvider,app.Configuration);
}

// A ordem determina como a requisição entra e a resposta retorna no pipeline.
app.UseRegistroRequisicao();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = exception is BadHttpRequestException bad ? bad.StatusCode : exception is UnauthorizedAccessException ? 403 : 500;
    await Results.Problem(statusCode: status,
        title: status == 500 ? "Erro interno tratado" : "Requisição inválida",
        detail: status == 500 ? "Não foi possível concluir a operação." : "Verifique os parâmetros, o JSON e o Content-Type.",
        extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
        .ExecuteAsync(context);
}));
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment()) app.UseHsts();
if (builder.Configuration.GetValue("Https:Redirecionar", true)) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Laboratorio");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.Use(async (context,next)=>
{
    if(app.Environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/diagnostico") && !context.User.IsInRole("Administrador"))
    { await Results.Problem(statusCode:context.User.Identity?.IsAuthenticated==true?403:401,title:"Diagnóstico restrito ao administrador.").ExecuteAsync(context);return; }
    if (context.User.Identity?.IsAuthenticated==true &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
    {
        try { await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context); }
        catch(Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
        { await Results.Problem(statusCode:400,title:"Token antifalsificação ausente ou inválido.").ExecuteAsync(context);return; }
    }
    await next(context);
});
app.Use(async (context, next) =>
{
    app.Logger.LogInformation("ENDPOINT ENTRADA {Endpoint} | {Id}", context.GetEndpoint()?.DisplayName, context.TraceIdentifier);
    await next(context);
    app.Logger.LogInformation("ENDPOINT SAIDA {Status} | {Id}", context.Response.StatusCode, context.TraceIdentifier);
});

app.MapGet("/", () => "Hello World!");

app.MapGet("/status", () => new
{
    sistema = "PortalTarefas",
    funcionando = true
});

app.MapGet("/Curso", () => "Estou fazendo o curso de .NET 10.0");

app.MapGet("/api/saudacao", ([FromQuery] string nome, [FromHeader(Name = "X-Aluno")] string? aluno) =>
    string.IsNullOrWhiteSpace(nome)
        ? Results.ValidationProblem(new Dictionary<string, string[]> { ["nome"] = ["Informe o nome."] })
        : Results.Ok(new { mensagem = $"Olá, {nome.Trim()}!", aluno = aluno ?? "Não informado" }));

var tarefas = app.MapGroup("/api/tarefas").RequireAuthorization();
tarefas.MapGet("", async (TarefaRepository repository, bool? concluida) => Results.Ok(await repository.ListarAsync(concluida)));
tarefas.MapGet("/{id:int}", async (int id, TarefaRepository repository) =>
    await repository.ObterAsync(id) is { } tarefa ? Results.Ok(tarefa) : Results.NotFound(new { mensagem = "Tarefa não encontrada." }));
tarefas.MapPost("", async (TarefaEntrada entrada, TarefaRepository repository) =>
{
    var erros = entrada.Validar();
    if (erros.Count > 0) return Results.ValidationProblem(erros);
    var tarefa = await repository.CriarAsync(entrada);
    return Results.Created($"/api/tarefas/{tarefa.Id}", tarefa);
});

// Falha proposital, disponível apenas no ambiente de desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/diagnostico/erro", (Func<IResult>)(() => throw new InvalidOperationException("Falha didática controlada.")));
    app.MapGet("/diagnostico/dados", async (TarefaRepository repo, bool concluida) => Results.Ok(new { efCore = await repo.PainelEfAsync(concluida), adoNet = await repo.PainelAdoAsync(concluida) }));
    app.MapPost("/diagnostico/seed", async (PortalTarefasDbContext db) => { await Seed.ExecutarAsync(db); return Results.Ok(new { seed = await db.Tarefas.CountAsync(t => t.Titulo.StartsWith("[Seed]")) }); });
    app.MapGet("/diagnostico/auditoria/{id:int}", async (int id, TarefaRepository repo) => Results.Ok(await repo.AuditoriaAsync(id)));
    app.MapPost("/diagnostico/rollback/{id:int}", async (int id, TarefaRepository repo) =>
    {
        var atual = await repo.ObterAsync(id);
        if (atual is null) return Results.NotFound();
        await repo.AtualizarAsync(id, new TarefaEntrada { Titulo = "Alteração que deve ser revertida" }, atual.Prioridade, atual.Prazo, atual.Versao, true);
        return Results.Ok();
    });
}

app.MapControllers();
app.MapHealthChecks("/health/live",new HealthCheckOptions{Predicate=_=>false});
app.MapHealthChecks("/health/ready",new HealthCheckOptions{Predicate=r=>r.Tags.Contains("ready")});
app.MapGet("/publico/status",()=>Results.Ok(new{sistema="PortalTarefas",api="v1",geradoUtc=DateTime.UtcNow}))
    .CacheOutput(policy=>policy.Expire(TimeSpan.FromSeconds(5)));
var observacao=app.MapGroup("/observabilidade").RequireAuthorization(p=>p.RequireRole("Administrador"));
observacao.MapGet("/painel",()=>Results.Ok(new{tarefasCriadasNoProcesso=Observabilidade.Total,exportador="OpenTelemetry Console",limite="3 requisições a cada 5 segundos",cacheSegundos=5}));
observacao.MapGet("/resiliencia",async(IHttpClientFactory clients)=>
{
    try { using var r=await clients.CreateClient("dependencia").GetAsync("/service");return Results.Json(new{statusDependencia=(int)r.StatusCode},statusCode:r.IsSuccessStatusCode?200:503); }
    catch(Polly.CircuitBreaker.BrokenCircuitException){return Results.Problem(statusCode:503,title:"Circuito aberto.");}
    catch(Polly.Timeout.TimeoutRejectedException){return Results.Problem(statusCode:504,title:"Tempo limite da dependência.");}
    catch(HttpRequestException){return Results.Problem(statusCode:503,title:"Dependência indisponível.");}
});
var capacidade=app.MapGroup("/capacidade").RequireAuthorization().RequireRateLimiting("demonstracao");
capacidade.MapGet("/consulta",()=>Results.Ok(new{aceita=true}));
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapControllerRoute("default", "{controller=Tarefas}/{action=Index}/{id?}");
app.MapFallbackToFile("/spa/{*path:nonfile}", "spa/index.html");

app.Run();
 
