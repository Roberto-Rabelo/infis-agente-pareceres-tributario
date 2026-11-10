using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Api.Agente.Application.A2A;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Api.Agente.Endpoints;

/// <summary>
/// Chave Bearer compartilhada da SOFIA (<c>SOFIA_A2A_TOKEN</c>), conforme a skill sofia-a2a-autenticacao.
/// Vale somente no Agent Card e no RPC A2A; a API REST continua exigindo JWT. Bearer opaco: não exige formato JWT,
/// compara o valor exato em tempo constante e não normaliza maiúsculas/minúsculas.
/// </summary>
public sealed class AutenticacaoChaveIntegracao(
    IOptionsMonitor<AuthenticationSchemeOptions> opcoes,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<OpcoesA2A> opcoesA2A)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, logger, encoder)
{
    public const string Esquema = "ChaveIntegracaoA2A";
    public const string EsquemaPadrao = "Padrao";
    public const string CaminhoCard = "/.well-known/agent-card.json";
    private const string PrefixoBearer = "Bearer ";

    public static bool EhRotaA2A(PathString caminho) =>
        caminho.StartsWithSegments(ResolvedorUrlA2A.CaminhoRpc, StringComparison.OrdinalIgnoreCase)
        || caminho.Equals(CaminhoCard, StringComparison.OrdinalIgnoreCase);

    /// <summary>Escolhe o esquema por requisição: chave compartilhada só nas rotas A2A e quando o Bearer não é um JWT.</summary>
    public static string Selecionar(HttpContext http)
    {
        if (!EhRotaA2A(http.Request.Path))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        var bearer = ValorBearer(http.Request.Headers.Authorization.ToString());
        return bearer is not null && !bearer.Contains('.', StringComparison.Ordinal)
            ? Esquema
            : JwtBearerDefaults.AuthenticationScheme;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var a2a = opcoesA2A.CurrentValue;
        if (!a2a.IntegracaoDisponivel)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apresentada = ValorBearer(Request.Headers.Authorization.ToString());
        if (string.IsNullOrEmpty(apresentada))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var esperada = a2a.ChaveIntegracao!.Trim();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(apresentada), Encoding.UTF8.GetBytes(esperada)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Chave de integração inválida."));
        }

        var identidade = new ClaimsIdentity([new Claim(ClaimTypes.Name, "sofia-a2a")], Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }

    private static string? ValorBearer(string cabecalho) =>
        cabecalho.StartsWith(PrefixoBearer, StringComparison.OrdinalIgnoreCase)
            ? cabecalho[PrefixoBearer.Length..].Trim()
            : null;
}
