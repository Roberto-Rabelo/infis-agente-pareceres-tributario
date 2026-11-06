using Api.Agente.Application.A2A;

namespace Api.Agente.Tests.Application;

public class ResolvedorUrlA2ATests
{
    [Fact]
    public void Configurada_PrevaleceSobreOHost()
    {
        var url = ResolvedorUrlA2A.Resolver("https://agente.example/a2a/", "http", "localhost");

        Assert.Equal("https://agente.example/a2a", url);
    }

    [Fact]
    public void SemConfiguracao_UsaEsquemaEHost()
    {
        var url = ResolvedorUrlA2A.Resolver(" ", "https", "agente-parecer.vercel.app");

        Assert.Equal("https://agente-parecer.vercel.app/a2a", url);
        Assert.DoesNotContain("agent-card", url, StringComparison.Ordinal);
    }
}
