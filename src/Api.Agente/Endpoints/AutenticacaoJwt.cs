using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Agente.Endpoints;

/// <summary>ADR D15: provedor OIDC configurado ou, só para teste, chave simétrica (feature 003); sem eles, falha fechada, exceto chaves locais em Development.</summary>
public static class AutenticacaoJwt
{
    public const string SecaoChavesLocais = "Authentication:Schemes:Bearer:SigningKeys";
    public const int TamanhoMinimoChaveBytes = 32;

    public static void Configurar(JwtBearerOptions opcoes, IConfiguration configuracao, IHostEnvironment ambiente)
    {
        var authority = configuracao["Autenticacao:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            opcoes.Authority = authority;
            opcoes.Audience = configuracao["Autenticacao:Audience"];
            return;
        }

        // Ambiente de teste sem IdP (feature 003): HS256 com chave compartilhada nas variáveis do host.
        if (ChaveSimetrica(configuracao["Autenticacao:ChaveSimetrica"]) is { } chave)
        {
            var emissor = configuracao["Autenticacao:Emissor"];
            var audiencia = configuracao["Autenticacao:Audience"];
            if (!string.IsNullOrWhiteSpace(emissor) && !string.IsNullOrWhiteSpace(audiencia))
            {
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = emissor,
                    ValidAudience = audiencia,
                    IssuerSigningKey = chave,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
                return;
            }
        }

        // Chaves do `dotnet user-jwts` ficam nos user-secrets e só valem em Development.
        if (ambiente.IsDevelopment() && configuracao.GetSection(SecaoChavesLocais).Exists())
        {
            return;
        }

        opcoes.TokenValidationParameters = new TokenValidationParameters { IssuerSigningKeys = [], ValidateIssuerSigningKey = true };
    }

    /// <summary>Base64 com pelo menos 256 bits; qualquer outro valor é ignorado e mantém a falha fechada.</summary>
    private static SymmetricSecurityKey? ChaveSimetrica(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var bytes = new byte[valor.Length];
        return Convert.TryFromBase64String(valor.Trim(), bytes, out var lidos) && lidos >= TamanhoMinimoChaveBytes
            ? new SymmetricSecurityKey(bytes[..lidos])
            : null;
    }
}
