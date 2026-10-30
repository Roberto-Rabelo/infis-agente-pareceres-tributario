using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Agente.Endpoints;

/// <summary>ADR D15: provedor OIDC configurado; sem ele, falha fechada, exceto chaves locais em Development.</summary>
public static class AutenticacaoJwt
{
    public const string SecaoChavesLocais = "Authentication:Schemes:Bearer:SigningKeys";

    public static void Configurar(JwtBearerOptions opcoes, IConfiguration configuracao, IHostEnvironment ambiente)
    {
        var authority = configuracao["Autenticacao:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            opcoes.Authority = authority;
            opcoes.Audience = configuracao["Autenticacao:Audience"];
            return;
        }

        // Chaves do `dotnet user-jwts` ficam nos user-secrets e só valem em Development.
        if (ambiente.IsDevelopment() && configuracao.GetSection(SecaoChavesLocais).Exists())
        {
            return;
        }

        opcoes.TokenValidationParameters = new TokenValidationParameters { IssuerSigningKeys = [], ValidateIssuerSigningKey = true };
    }
}
