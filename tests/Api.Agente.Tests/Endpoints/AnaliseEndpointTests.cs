using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Api.Agente.Tests.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Endpoints;

public sealed class FabricaApi : WebApplicationFactory<Program>
{
    /// <summary>Fora de Development os user-secrets não são carregados: a CI nunca usa a chave real (P17).</summary>
    public const string Ambiente = "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Ambiente);
        builder.UseSetting("Entrada:TamanhoMaximoBytes", "200000");
        builder.ConfigureTestServices(servicos => servicos
            .AddAuthentication(o =>
            {
                o.DefaultScheme = AutenticacaoTeste.Esquema;
                o.DefaultAuthenticateScheme = AutenticacaoTeste.Esquema;
                o.DefaultChallengeScheme = AutenticacaoTeste.Esquema;
            })
            .AddScheme<AuthenticationSchemeOptions, AutenticacaoTeste>(AutenticacaoTeste.Esquema, _ => { }));
    }
}

public sealed class AutenticacaoTeste(IOptionsMonitor<AuthenticationSchemeOptions> opcoes, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, logger, encoder)
{
    public const string Esquema = "Teste";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.Authorization.ToString() != Esquema + " agente-orquestrador")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identidade = new ClaimsIdentity([new Claim(ClaimTypes.Name, "agente-orquestrador")], Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }
}

public class AnaliseEndpointTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private const string Rota = "/api/v1/analises";

    [Fact]
    public async Task SemAutenticacao_401()
    {
        using var cliente = fabrica.CreateClient();

        using var resposta = await cliente.PostAsync(Rota, Conteudo(Bytes(Lote(EfdIcmsIpi()))));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task SemProvedorDeIdentidadeConfigurado_RecusaQualquerToken()
    {
        using var fabricaReal = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment(FabricaApi.Ambiente));
        using var cliente = fabricaReal.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJub25lIn0.eyJzdWIiOiJ4In0.");

        using var resposta = await cliente.PostAsync(Rota, Conteudo(Bytes(Lote(EfdIcmsIpi()))));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Health_Anonimo_200()
    {
        using var cliente = fabrica.CreateClient();

        using var resposta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task LoteValido_200_EnvelopeMontadoPeloSistema()
    {
        using var cliente = Autenticado();
        cliente.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-123");

        using var resposta = await cliente.PostAsync(Rota, Conteudo(Bytes(Lote(EfdIcmsIpi(), NFe()))));
        using var json = await Json(resposta);
        var raiz = json.RootElement;

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("corr-123", resposta.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("corr-123", raiz.GetProperty("correlacaoId").GetString());
        Assert.Equal(32, raiz.GetProperty("execucaoId").GetString()!.Length);
        Assert.Equal("1.1.0", raiz.GetProperty("versaoEnvelope").GetString());
        Assert.Equal("1.0.0", raiz.GetProperty("versaoContratoEntrada").GetString());
        Assert.Equal("0.1.0", raiz.GetProperty("versaoCatalogo").GetString());
        Assert.Equal("PROCESSADO", raiz.GetProperty("statusTecnico").GetString());
        Assert.Equal(JsonValueKind.Null, raiz.GetProperty("parecer").ValueKind);
        Assert.Equal(0, raiz.GetProperty("consumo").GetArrayLength());

        var resultado = raiz.GetProperty("resultado");
        Assert.Equal("INCONCLUSIVA", resultado.GetProperty("status").GetString());
        Assert.True(resultado.GetProperty("revisaoHumana").GetProperty("requerida").GetBoolean());
        Assert.Equal("EFD_ICMS_IPI", resultado.GetProperty("aptidao")[0].GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task CorrelacaoInvalida_SubstituidaPorNova()
    {
        using var cliente = Autenticado();
        cliente.DefaultRequestHeaders.Add("X-Correlation-Id", "<script>");

        using var resposta = await cliente.PostAsync(Rota, Conteudo(Bytes(Lote(EfdIcmsIpi()))));

        Assert.NotEqual("<script>", resposta.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task ObrigacaoInvalida_200_ComPreCondicaoNaoAtendida()
    {
        using var cliente = Autenticado();
        var efd = EfdIcmsIpi();
        efd.Remove("versaoLayout");

        using var resposta = await cliente.PostAsync(Rota, Conteudo(Bytes(Lote(efd))));
        using var json = await Json(resposta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var achado = json.RootElement.GetProperty("resultado").GetProperty("achados")[0];
        Assert.Equal("PRE-002", achado.GetProperty("regraId").GetString());
        Assert.Equal("PRE_CONDICAO_NAO_ATENDIDA", achado.GetProperty("natureza").GetString());
    }

    [Fact]
    public async Task LoteInvalido_400_SemConformidade()
    {
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync(Rota, Conteudo("{\"versaoContrato\":\"1.0.0\"}"u8.ToArray()));
        using var json = await Json(resposta);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("REJEITADO", json.RootElement.GetProperty("statusTecnico").GetString());
        Assert.Equal("CONTRATO_LOTE_INVALIDO", json.RootElement.GetProperty("erro").GetProperty("codigo").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("resultado").ValueKind);
    }

    [Fact]
    public async Task LoteAcimaDoTamanho_413()
    {
        using var cliente = Autenticado();
        var efd = EfdIcmsIpi();
        efd["registros"]!["0000"]![0]!["campos"]!["PREENCHIMENTO"] = new string('x', 250_000);

        using var resposta = await cliente.PostAsync(Rota, Conteudo(Bytes(Lote(efd))));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, resposta.StatusCode);
    }

    private HttpClient Autenticado()
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AutenticacaoTeste.Esquema, "agente-orquestrador");
        return cliente;
    }

    private static ByteArrayContent Conteudo(byte[] corpo)
    {
        var conteudo = new ByteArrayContent(corpo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return conteudo;
    }

    private static async Task<JsonDocument> Json(HttpResponseMessage resposta) =>
        await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync());
}
