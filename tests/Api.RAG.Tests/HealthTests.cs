using System.Net;
using Api.RAG.Tests.Fixtures;

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
}
