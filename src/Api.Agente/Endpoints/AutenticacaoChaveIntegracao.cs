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
/// Chave de integração estática do orquestrador (SOFIA) para o RPC A2A. Vale somente em <c>/a2a</c>; a API REST
/// continua exigindo JWT. Aceita <c>Authorization: Bearer &lt;chave&gt;</c> ou <c>X-API-Key: &lt;chave&gt;</c>.
/// Sem chave configurada (ou curta demais) o esquema nunca autentica: falha fechada.
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
    public const string CabecalhoApiKey = "X-API-Key";
    public const int TamanhoMinimoChave = 16;
    private const string PrefixoBearer = "Bearer ";

    /// <summary>Escolhe o esquema por requisição: chave de integração só no RPC A2A e quando o Bearer não é um JWT.</summary>
    public static string Selecionar(HttpContext http)
    {
        if (!http.Request.Path.StartsWithSegments(ResolvedorUrlA2A.CaminhoRpc, StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        if (http.Request.Headers.ContainsKey(CabecalhoApiKey))
        {
            return Esquema;
        }

        var bearer = ValorBearer(http.Request.Headers.Authorization.ToString());
        return bearer is not null && !bearer.Contains('.', StringComparison.Ordinal)
            ? Esquema
            : JwtBearerDefaults.AuthenticationScheme;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var esperada = opcoesA2A.CurrentValue.ChaveIntegracao?.Trim();
        if (string.IsNullOrEmpty(esperada) || esperada.Length < TamanhoMinimoChave)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apresentada = Request.Headers.TryGetValue(CabecalhoApiKey, out var apiKey)
            ? apiKey.ToString().Trim()
            : ValorBearer(Request.Headers.Authorization.ToString());
        if (string.IsNullOrEmpty(apresentada))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(apresentada), Encoding.UTF8.GetBytes(esperada)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Chave de integração inválida."));
        }

        var identidade = new ClaimsIdentity([new Claim(ClaimTypes.Name, "integracao-a2a")], Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }

    private static string? ValorBearer(string cabecalho) =>
        cabecalho.StartsWith(PrefixoBearer, StringComparison.OrdinalIgnoreCase)
            ? cabecalho[PrefixoBearer.Length..].Trim()
            : null;
}
