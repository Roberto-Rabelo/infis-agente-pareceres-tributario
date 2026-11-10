using Api.RAG.Application;
using Api.RAG.Endpoints;
using Api.RAG.Endpoints.Documentacao;
using Api.RAG.Infrastructure;
using Api.RAG.Infrastructure.Postgres;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenAI.Embeddings;
using Pgvector.Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpcoesEmbeddings>(builder.Configuration.GetSection(OpcoesEmbeddings.Secao));
builder.Services.Configure<OpcoesSegmentacao>(builder.Configuration.GetSection(OpcoesSegmentacao.Secao));
builder.Services.Configure<OpcoesConsulta>(builder.Configuration.GetSection(OpcoesConsulta.Secao));
builder.Services.Configure<OpcoesIngestao>(builder.Configuration.GetSection(OpcoesIngestao.Secao));

var tamanhoMaximo = builder.Configuration.GetSection(OpcoesIngestao.Secao).Get<OpcoesIngestao>()?.TamanhoMaximoBytes
    ?? new OpcoesIngestao().TamanhoMaximoBytes;
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = tamanhoMaximo);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();

// Segredos (string de conexão e chave do provedor) vêm de variáveis de ambiente ou secret manager, nunca do appsettings.
var conexao = builder.Configuration.GetConnectionString("Rag");
if (string.IsNullOrWhiteSpace(conexao))
{
    builder.Services.AddSingleton<RepositorioNaoConfigurado>();
    builder.Services.AddSingleton<IRepositorioConhecimento>(sp => sp.GetRequiredService<RepositorioNaoConfigurado>());
    builder.Services.AddSingleton<IRegistroConsumo>(sp => sp.GetRequiredService<RepositorioNaoConfigurado>());
}
else
{
    builder.Services.AddSingleton(_ =>
    {
        var fonte = new NpgsqlDataSourceBuilder(conexao);
        fonte.UseVector();
        return fonte.Build();
    });
    builder.Services.AddSingleton<RepositorioPostgres>();
    builder.Services.AddSingleton<IRepositorioConhecimento>(sp => sp.GetRequiredService<RepositorioPostgres>());
    builder.Services.AddSingleton<IRegistroConsumo>(sp => sp.GetRequiredService<RepositorioPostgres>());
}

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
{
    var chave = builder.Configuration["OpenAI:ApiKey"];
    var opcoes = sp.GetRequiredService<IOptions<OpcoesEmbeddings>>().Value;
    return string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(opcoes.Modelo)
        ? new GeradorNaoConfigurado()
        : new EmbeddingClient(opcoes.Modelo, chave).AsIEmbeddingGenerator(opcoes.Dimensao);
});
builder.Services.AddSingleton<ProvedorEmbeddings>();
builder.Services.AddSingleton<ServicoIngestao>();
builder.Services.AddSingleton<ServicoConsulta>();

builder.Services.AddControllers();
builder.Services.AdicionarDocumentacaoApi();
builder.Services.AddHealthChecks();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<FerramentasMcp>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration, IHostEnvironment>(AutenticacaoJwt.Configurar);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Papeis.PoliticaAdministracao, p => p.RequireAuthenticatedUser().RequireRole(Papeis.Administrador))
    .AddPolicy(Papeis.PoliticaConsulta, p => p.RequireAuthenticatedUser().RequireRole(Papeis.Consulta));

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(conexao) && app.Configuration.GetValue("Postgres:AplicarMigracoes", true))
{
    var dimensao = app.Services.GetRequiredService<IOptions<OpcoesEmbeddings>>().Value.Dimensao;
    await MigradorPostgres.AplicarAsync(conexao, dimensao, CancellationToken.None).ConfigureAwait(false);
}

app.UseExceptionHandler(erro => erro.Run(async contexto =>
{
    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await contexto.Response.WriteAsJsonAsync(new ErroPadronizado("ERRO_INTERNO", "Falha técnica no processamento.")).ConfigureAwait(false);
}));
// Na Vercel (services) a RAG fica atrás de /rag e recebe o caminho original; sem prefixo, segue na raiz.
var caminhoBase = app.Configuration["Hospedagem:PathBase"];
if (!string.IsNullOrWhiteSpace(caminhoBase))
{
    app.UsePathBase(caminhoBase);
}

app.UseRouting();
app.UsarDocumentacaoApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapMcp("/mcp").RequireAuthorization(Papeis.PoliticaConsulta);

await app.RunAsync().ConfigureAwait(false);

#pragma warning disable CA1050, S3903 // Exposto para WebApplicationFactory nos testes.
public partial class Program;
#pragma warning restore CA1050, S3903
