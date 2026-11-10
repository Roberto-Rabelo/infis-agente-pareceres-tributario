using Api.RAG.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.RAG.Tests.Endpoints;

/// <summary>D13/D15: sem provedor, falha fechada; chaves locais do user-jwts só valem em Development.</summary>
public class AutenticacaoJwtTests
{
    private static readonly string ChaveValida = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private static JwtBearerOptions Configurar(
        string ambiente, bool chavesLocais, string? authority = null, string? chaveSimetrica = null, string? emissor = "emissor-teste")
    {
        var valores = new Dictionary<string, string?>
        {
            ["Autenticacao:Authority"] = authority,
            ["Autenticacao:Audience"] = "api-rag",
            ["Autenticacao:ChaveSimetrica"] = chaveSimetrica,
            ["Autenticacao:Emissor"] = emissor,
        };
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

        Assert.Equal(("https://idp.exemplo", "api-rag"), (opcoes.Authority, opcoes.Audience));
    }

    [Fact]
    public void ComChaveSimetrica_ValidaEmissorAudienciaEAssinatura()
    {
        var parametros = Configurar("Production", chavesLocais: false, chaveSimetrica: ChaveValida).TokenValidationParameters;

        Assert.Equal(("emissor-teste", "api-rag"), (parametros.ValidIssuer, parametros.ValidAudience));
        Assert.IsType<SymmetricSecurityKey>(parametros.IssuerSigningKey);
        Assert.Equal([SecurityAlgorithms.HmacSha256], parametros.ValidAlgorithms);
    }

    [Theory]
    [InlineData("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGx0e", "emissor-teste")]
    [InlineData("isto não é base64", "emissor-teste")]
    [InlineData(null, null)]
    public void ChaveSimetricaCurtaInvalidaOuSemEmissor_FalhaFechada(string? chave, string? emissor)
    {
        var parametros = Configurar("Production", chavesLocais: false, chaveSimetrica: chave ?? ChaveValida, emissor: emissor).TokenValidationParameters;

        Assert.Empty(parametros.IssuerSigningKeys!);
        Assert.Null(parametros.IssuerSigningKey);
    }

    [Fact]
    public void ComProvedor_PrevaleceSobreChaveSimetrica() =>
        Assert.Null(Configurar("Production", chavesLocais: false, authority: "https://idp.exemplo", chaveSimetrica: ChaveValida)
            .TokenValidationParameters.IssuerSigningKey);

    [Fact]
    public async Task ComChaveSimetrica_AceitaTokenAssinadoERecusaOutraChave()
    {
        var parametros = Configurar("Production", chavesLocais: false, chaveSimetrica: ChaveValida).TokenValidationParameters;
        var manipulador = new JsonWebTokenHandler();

        string Emitir(byte[] chave) => manipulador.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "emissor-teste",
            Audience = "api-rag",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha256),
        });

        var valido = await manipulador.ValidateTokenAsync(Emitir(Convert.FromBase64String(ChaveValida)), parametros);
        var outraChave = await manipulador.ValidateTokenAsync(Emitir(new byte[32]), parametros);

        Assert.True(valido.IsValid);
        Assert.False(outraChave.IsValid);
    }
}
