using Api.Agente.Application.Analise;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Parecer;
using Api.Agente.Infrastructure.Conhecimento;
using Api.Agente.Infrastructure.Consumo;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Npgsql;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Application.PreCondicoes;
using Api.Agente.Domain.Catalogo;
using Api.Agente.Domain.Motor;
using Api.Agente.Endpoints;
using Api.Agente.Endpoints.Documentacao;
using Api.Agente.Infrastructure.Catalogo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpcoesEntrada>(builder.Configuration.GetSection(OpcoesEntrada.Secao));
builder.Services.Configure<OpcoesCruzamento>(builder.Configuration.GetSection(OpcoesCruzamento.Secao));
builder.Services.Configure<OpcoesCatalogo>(builder.Configuration.GetSection(OpcoesCatalogo.Secao));

var tamanhoMaximo = builder.Configuration.GetSection(OpcoesEntrada.Secao).Get<OpcoesEntrada>()?.TamanhoMaximoBytes
    ?? new OpcoesEntrada().TamanhoMaximoBytes;
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = tamanhoMaximo);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new LeitorLote(sp.GetRequiredService<IOptions<OpcoesEntrada>>().Value));
builder.Services.AddSingleton(sp => CarregadorCatalogo.CarregarRegras(
    CarregadorCatalogo.ResolverDiretorio(sp.GetRequiredService<IOptions<OpcoesCatalogo>>().Value.Caminho)));
builder.Services.AddSingleton(sp => CarregadorCatalogo.CarregarVersoes(
    CarregadorCatalogo.ResolverDiretorio(sp.GetRequiredService<IOptions<OpcoesCatalogo>>().Value.Caminho)));
builder.Services.AddSingleton(sp =>
{
    var opcoes = sp.GetRequiredService<IOptions<OpcoesCruzamento>>().Value;
    return new ParametrosComparacao(opcoes.Tolerancia, opcoes.Materialidade);
});
builder.Services.AddSingleton<VerificadorPreCondicoes>();
foreach (var validador in ValidadoresNfeEfd.Todos())
{
    builder.Services.AddSingleton(validador);
}

builder.Services.AddSingleton<MotorRegras>();
builder.Services.AddSingleton<ServicoAnalise>();

builder.Services.Configure<OpcoesBaseConhecimento>(builder.Configuration.GetSection(OpcoesBaseConhecimento.Secao));
builder.Services.AddHttpClient(ClienteMcpBaseConhecimento.NomeHttp);
builder.Services.AddSingleton<IBaseConhecimento, ClienteMcpBaseConhecimento>();
builder.Services.AddSingleton<ServicoFundamentacao>();

builder.Services.Configure<OpcoesParecer>(builder.Configuration.GetSection(OpcoesParecer.Secao));
builder.Services.Configure<OpcoesCustos>(builder.Configuration.GetSection(OpcoesCustos.Secao));
builder.Services.AddSingleton(_ => ValidadorParecer.DoArquivo(SchemaSaidaParecer.Caminho(AppContext.BaseDirectory)));
builder.Services.AddSingleton(_ => SchemaSaidaParecer.DoArquivo(AppContext.BaseDirectory));
builder.Services.AddSingleton(sp => PromptParecer.DoArquivo(AppContext.BaseDirectory, sp.GetRequiredService<IOptions<OpcoesParecer>>().Value.VersaoPrompt));

// Segredos via ambiente/secret manager: OpenAI__ApiKey e ConnectionStrings__Agente.
var parecer = builder.Configuration.GetSection(OpcoesParecer.Secao).Get<OpcoesParecer>() ?? new OpcoesParecer();
var conexaoAgente = builder.Configuration.GetConnectionString("Agente");
if (parecer.Habilitado)
{
    var chaveOpenAi = builder.Configuration["OpenAI:ApiKey"];
    if (string.IsNullOrWhiteSpace(chaveOpenAi) || string.IsNullOrWhiteSpace(parecer.Modelo) || string.IsNullOrWhiteSpace(conexaoAgente))
    {
        throw new InvalidOperationException("Parecer habilitado exige OpenAI:ApiKey, Parecer:Modelo e ConnectionStrings:Agente (registro de consumo).");
    }

    builder.Services.AddSingleton<IChatCompletionService>(new OpenAIChatCompletionService(parecer.Modelo, chaveOpenAi));
}

if (string.IsNullOrWhiteSpace(conexaoAgente))
{
    builder.Services.AddSingleton<IRegistroConsumoLlm, RegistroConsumoNaoConfigurado>();
}
else
{
    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(conexaoAgente));
    builder.Services.AddSingleton<IRegistroConsumoLlm, RepositorioConsumoPostgres>();
}

builder.Services.AddSingleton<ServicoParecer>();

builder.Services.AddControllers().AddJsonOptions(o => OpcoesJsonSaida.Configurar(o.JsonSerializerOptions));
builder.Services.AdicionarDocumentacaoApi();
builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration, IHostEnvironment>(AutenticacaoJwt.Configurar);
builder.Services.AddAuthorization();

var app = builder.Build();

_ = app.Services.GetRequiredService<CatalogoRegras>();
_ = app.Services.GetRequiredService<CatalogoVersoesLayout>();
_ = app.Services.GetRequiredService<ServicoParecer>();
if (!string.IsNullOrWhiteSpace(conexaoAgente) && app.Configuration.GetValue("Postgres:AplicarMigracoes", true))
{
    await RepositorioConsumoPostgres.AplicarMigracaoAsync(app.Services.GetRequiredService<NpgsqlDataSource>(), CancellationToken.None).ConfigureAwait(false);
}

app.UseExceptionHandler(erro => erro.Run(async contexto =>
{
    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await contexto.Response.WriteAsJsonAsync(new ErroPadronizado("ERRO_INTERNO", "Falha técnica no processamento.")).ConfigureAwait(false);
}));
app.UsarCorrelacao();
app.UsarDocumentacaoApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

#pragma warning disable CA1050, S3903 // Exposto para WebApplicationFactory nos testes.
public partial class Program;
#pragma warning restore CA1050, S3903
