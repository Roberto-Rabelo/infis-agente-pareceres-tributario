using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.RAG.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.RAG.Tests.Fixtures;

public sealed class FabricaRag : WebApplicationFactory<Program>
{
    public RepositorioEmMemoria Repositorio { get; } = new();

    public GeradorFalso Gerador { get; } = new(Ambiente.Dimensao);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Embeddings:Modelo", Ambiente.Modelo);
        builder.UseSetting("Embeddings:Dimensao", Ambiente.Dimensao.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Ingestao:TamanhoMaximoBytes", "20000");
        builder.ConfigureServicesParaTeste();
        builder.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<IRepositorioConhecimento>();
            servicos.RemoveAll<IRegistroConsumo>();
            servicos.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            servicos.AddSingleton<IRepositorioConhecimento>(Repositorio);
            servicos.AddSingleton<IRegistroConsumo>(Repositorio);
            servicos.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(Gerador);
        });
    }
}

public static class ConfiguracaoTeste
{
    /// <summary>Troca apenas a autenticação JWT pelo esquema de teste; o restante da aplicação é o real.</summary>
    /// <summary>Fora de Development os user-secrets não são carregados: a CI nunca usa a chave real (P17).</summary>
    public const string Ambiente = "Testing";

    public static IWebHostBuilder ConfigureServicesParaTeste(this IWebHostBuilder builder) =>
        builder.UseEnvironment(Ambiente).ConfigureTestServices(servicos => servicos
            .AddAuthentication(o =>
            {
                o.DefaultScheme = AutenticacaoTeste.Esquema;
                o.DefaultAuthenticateScheme = AutenticacaoTeste.Esquema;
                o.DefaultChallengeScheme = AutenticacaoTeste.Esquema;
            })
            .AddScheme<AuthenticationSchemeOptions, AutenticacaoTeste>(AutenticacaoTeste.Esquema, _ => { }));
}

/// <summary>Cabeçalho <c>Authorization: Teste papel1,papel2</c>; sem cabeçalho, não autentica.</summary>
public sealed class AutenticacaoTeste(IOptionsMonitor<AuthenticationSchemeOptions> opcoes, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, logger, encoder)
{
    public const string Esquema = "Teste";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var valor = Request.Headers.Authorization.ToString();
        if (!valor.StartsWith(Esquema + " ", StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = valor[(Esquema.Length + 1)..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => new Claim(ClaimTypes.Role, p))
            .Append(new Claim(ClaimTypes.Name, "teste"));
        var identidade = new ClaimsIdentity(claims, Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }
}
