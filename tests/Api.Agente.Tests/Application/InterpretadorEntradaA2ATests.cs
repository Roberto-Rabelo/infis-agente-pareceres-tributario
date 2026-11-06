using System.Text;
using System.Text.Json.Nodes;
using Api.Agente.Application.A2A;
using Api.Agente.Tests.Fixtures;

namespace Api.Agente.Tests.Application;

public class InterpretadorEntradaA2ATests
{
    [Fact]
    public void LoteDireto_SemEnvelopeSofia()
    {
        var bytes = Lotes.Bytes(Lotes.Lote(Lotes.EfdIcmsIpi()));

        var entrada = InterpretadorEntradaA2A.Interpretar(bytes, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"));

        Assert.Null(entrada.Erro);
        Assert.NotNull(entrada.CorpoLote);
    }

    [Fact]
    public void SofiaSemLoteNoTexto_Erro()
    {
        var json = new JsonObject
        {
            ["versaoContrato"] = "1.0",
            ["idExecucao"] = "8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e",
            ["idSolicitacao"] = "e1b3a4cb-d729-4bd4-b796-b9c94be07b46",
            ["usuario"] = new JsonObject { ["nome"] = "Usuario Teste" },
            ["pergunta"] = "Analisar",
            ["documentos"] = new JsonArray(new JsonObject
            {
                ["idDocumento"] = "doc-1",
                ["nome"] = "nota.xml",
                ["texto"] = "<NFe/>",
            }),
        };

        var entrada = InterpretadorEntradaA2A.Interpretar(Encoding.UTF8.GetBytes(json.ToJsonString()), "x", "y");

        Assert.Equal("LOTE_AUSENTE", entrada.Erro!.Codigo);
        Assert.Equal("8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e", entrada.IdExecucao);
    }
}
