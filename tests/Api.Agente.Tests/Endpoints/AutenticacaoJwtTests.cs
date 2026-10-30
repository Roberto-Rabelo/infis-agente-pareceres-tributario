using Api.Agente.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;

namespace Api.Agente.Tests.Endpoints;

/// <summary>D15: sem provedor, falha fechada; chaves locais do user-jwts só valem em Development.</summary>
public class AutenticacaoJwtTests
{
    private static JwtBearerOptions Configurar(string ambiente, bool chavesLocais, string? authority = null)
    {
        var valores = new Dictionary<string, string?> { ["Autenticacao:Authority"] = authority, ["Autenticacao:Audience"] = "api-agente" };
        if (chavesLocais)
        {
            valores[AutenticacaoJwt.SecaoChavesLocais + ":0:Issuer"] = "dotnet-user-jwts";
            valores[AutenticacaoJwt.SecaoChavesLocais + ":0:Value"] = "ZmFrZQ==";
        }

        var opcoes = new JwtBearerOptions();
        AutenticacaoJwt.Configurar(opcoes, new ConfigurationBuilder().AddInMemoryCollection(valores).Build(), new HostingEnvironment { EnvironmentName = ambiente });
        return opcoes;
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Testing", true)]
    [InlineData("Development", false)]
    public void SemProvedor_FalhaFechada(string ambiente, bool chavesLocais) =>
        Assert.Empty(Configurar(ambiente, chavesLocais).TokenValidationParameters.IssuerSigningKeys!);

    [Fact]
    public void Development_ComChavesDoUserJwts_MantemAConfiguracaoLocal() =>
        Assert.Null(Configurar("Development", chavesLocais: true).TokenValidationParameters.IssuerSigningKeys);

    [Fact]
    public void ComProvedor_UsaAuthorityEAudience()
    {
        var opcoes = Configurar("Production", chavesLocais: true, authority: "https://idp.exemplo");

        Assert.Equal(("https://idp.exemplo", "api-agente"), (opcoes.Authority, opcoes.Audience));
    }
}
