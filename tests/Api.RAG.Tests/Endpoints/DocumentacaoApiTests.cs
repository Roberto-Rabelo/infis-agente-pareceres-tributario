using System.Net;
using System.Text.Json;
using Api.RAG.Endpoints.Documentacao;
using Api.RAG.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Api.RAG.Tests.Endpoints;

/// <summary>Constitution 5.1: o documento OpenAPI é anônimo, por isso só existe quando habilitado e fora de Production.</summary>
public class DocumentacaoApiTests(FabricaRag fabrica) : IClassFixture<FabricaRag>
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
    public async Task Swagger_Habilitado_DocumentaAdministracaoSemMcp()
    {
        using var habilitada = fabrica.WithWebHostBuilder(b => b.UseSetting(DocumentacaoApi.ChaveHabilitado, "true"));
        using var cliente = habilitada.CreateClient();

        using var resposta = await cliente.GetAsync(Rota);
        using var documento = await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync());
        var caminhos = documento.RootElement.GetProperty("paths");
        var documentos = caminhos.GetProperty("/api/v1/documentos");
        var ingestao = documentos.GetProperty("post");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(
            "#/components/schemas/DocumentoEntradaDto",
            ingestao.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString());
        var respostas = ingestao.GetProperty("responses").EnumerateObject().Select(p => p.Name).Order().ToList();
        Assert.Equal(["200", "201", "400", "401", "403", "409", "413", "415", "500", "503"], respostas);
        Assert.True(ingestao.GetProperty("security")[0].TryGetProperty(DocumentacaoApi.EsquemaSeguranca, out _));
        Assert.True(documentos.TryGetProperty("get", out _));
        Assert.False(caminhos.TryGetProperty("/mcp", out _));
    }

    [Fact]
    public async Task Swagger_ComPathBase_ServidorERotaComPrefixo()
    {
        using var habilitada = fabrica.WithWebHostBuilder(b => b
            .UseSetting(DocumentacaoApi.ChaveHabilitado, "true")
            .UseSetting("Hospedagem:PathBase", "/rag"));
        using var cliente = habilitada.CreateClient();

        using var resposta = await cliente.GetAsync("/rag" + Rota);
        using var documento = await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("/rag", documento.RootElement.GetProperty("servers")[0].GetProperty("url").GetString());
    }
}
