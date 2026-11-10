using System.Net;
using Api.RAG.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;

namespace Api.RAG.Tests;

public class HealthTests(FabricaRag fabrica) : IClassFixture<FabricaRag>
{
    [Fact]
    public async Task Health_200()
    {
        using var cliente = fabrica.CreateClient();

        using var resposta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/rag/health")]
    [InlineData("/health")]
    public async Task ComPathBase_RespondeComESemPrefixo(string caminho)
    {
        using var comPrefixo = fabrica.WithWebHostBuilder(b => b.UseSetting("Hospedagem:PathBase", "/rag"));
        using var cliente = comPrefixo.CreateClient();

        using var resposta = await cliente.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
