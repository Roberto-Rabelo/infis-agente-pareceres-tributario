using System.Net;
using System.Text.Json;
using Api.Agente.Endpoints.Documentacao;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Api.Agente.Tests.Endpoints;

/// <summary>Constitution 5.1: o documento OpenAPI é anônimo, por isso só existe quando habilitado e fora de Production.</summary>
public class DocumentacaoApiTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private const string Rota = "/swagger/v1/swagger.json";

    [Fact]
    public async Task Swagger_DesabilitadoPorPadrao_404()
    {
        using var cliente = fabrica.CreateClient();

        using var resposta = await cliente.GetAsync(Rota);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Swagger_HabilitadoEmProduction_404()
    {
        using var producao = fabrica.WithWebHostBuilder(b => b
            .UseEnvironment(Environments.Production)
            .UseSetting(DocumentacaoApi.ChaveHabilitado, "true"));
        using var cliente = producao.CreateClient();

        using var resposta = await cliente.GetAsync(Rota);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Swagger_Habilitado_DocumentaAnaliseComContratoESeguranca()
    {
        using var habilitada = Habilitada();
        using var cliente = habilitada.CreateClient();

        using var resposta = await cliente.GetAsync(Rota);
        using var documento = await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync());
        var raiz = documento.RootElement;
        var operacao = raiz.GetProperty("paths").GetProperty("/api/v1/analises").GetProperty("post");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = operacao.GetProperty("requestBody");
        Assert.True(corpo.GetProperty("required").GetBoolean());
        var midia = corpo.GetProperty("content").GetProperty("application/json");
        Assert.Equal("#/components/schemas/LoteEntradaDto", midia.GetProperty("schema").GetProperty("$ref").GetString());
        Assert.Equal("1.0.0", midia.GetProperty("example").GetProperty("versaoContrato").GetString());

        var respostas = operacao.GetProperty("responses").EnumerateObject().Select(p => p.Name).Order().ToList();
        Assert.Equal(["200", "400", "401", "413", "500"], respostas);
        Assert.True(operacao.GetProperty("security")[0].TryGetProperty(DocumentacaoApi.EsquemaSeguranca, out _));
        Assert.Contains(operacao.GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "X-Correlation-Id");

        var componentes = raiz.GetProperty("components");
        Assert.Equal("bearer", componentes.GetProperty("securitySchemes").GetProperty(DocumentacaoApi.EsquemaSeguranca).GetProperty("scheme").GetString());
        var obrigacoes = componentes.GetProperty("schemas").GetProperty("LoteEntradaDto").GetProperty("properties").GetProperty("obrigacoes");
        Assert.Equal("#/components/schemas/ObrigacaoDto", obrigacoes.GetProperty("items").GetProperty("$ref").GetString());
        var tipos = componentes.GetProperty("schemas").GetProperty("ObrigacaoDto").GetProperty("properties").GetProperty("tipo").GetProperty("enum");
        Assert.Contains(tipos.EnumerateArray(), t => t.GetString() == "EFD_ICMS_IPI");
    }

    [Fact]
    public async Task Swagger_Habilitado_InterfaceDisponivel()
    {
        using var habilitada = Habilitada();
        using var cliente = habilitada.CreateClient();

        using var resposta = await cliente.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private WebApplicationFactory<Program> Habilitada() =>
        fabrica.WithWebHostBuilder(b => b.UseSetting(DocumentacaoApi.ChaveHabilitado, "true"));
}
