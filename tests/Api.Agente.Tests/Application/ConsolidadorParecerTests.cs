using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Api.Agente.Domain.Motor;
using Api.Agente.Tests.Fixtures;
using static Api.Agente.Tests.Fixtures.Pareceres;

namespace Api.Agente.Tests.Application;

/// <summary>Hipóteses, escopo e limitações vêm do resultado determinístico, nunca da redação do modelo (P2, P4).</summary>
public class ConsolidadorParecerTests
{
    private static async Task<ParecerLlm> Gerar(ResultadoAnalise analise, Action<JsonObject> alterar)
    {
        var resposta = ParecerValido(analise);
        alterar(resposta);
        var gerado = await Servico(new ChatFalso().Responder(resposta)).GerarAsync(analise, "execucao", "correlacao", CancellationToken.None);
        return gerado.Parecer!;
    }

    [Fact]
    public async Task HipotesesOmitidasPeloModelo_PreenchidasPeloSistema()
    {
        var parecer = await Gerar(Analise(), p => p["achados"]![0]!["hipotesesLegitimas"] = new JsonArray());

        Assert.Equal([Hipotese], Assert.Single(parecer.Achados).HipotesesLegitimas);
    }

    [Fact]
    public async Task HipoteseRedigidaPeloModelo_Descartada()
    {
        var parecer = await Gerar(Analise(), p => p["achados"]![0]!["hipotesesLegitimas"] = new JsonArray("Hipótese inventada."));

        Assert.Equal([Hipotese], Assert.Single(parecer.Achados).HipotesesLegitimas);
    }

    [Fact]
    public async Task EscopoRedigidoPeloModelo_Substituido()
    {
        var analise = Analise() with
        {
            Regras =
            [
                new ResultadoRegra("M1-P01", SituacaoRegra.ComInconsistencia, null, true),
                new ResultadoRegra("M1-I01", SituacaoRegra.NaoConclusiva, "Depende de M1-P01.", true),
            ],
        };

        var parecer = await Gerar(analise, p => p["escopoELimitacoes"] = "Não há limitações.");

        Assert.DoesNotContain("Não há limitações", parecer.EscopoELimitacoes, StringComparison.Ordinal);
        Assert.Contains("executadas: 1 de 2", parecer.EscopoELimitacoes, StringComparison.Ordinal);
        Assert.Contains("M1-I01 (NAO_CONCLUSIVA)", parecer.EscopoELimitacoes, StringComparison.Ordinal);
        Assert.Contains("M1-I01: Depende de M1-P01.", parecer.Limitacoes);
    }

    [Fact]
    public async Task RegraPendenteSemMotivo_ViraLimitacao()
    {
        var analise = Analise() with { Regras = [new ResultadoRegra("M1-R01", SituacaoRegra.AusenciaEvidencias, null, true)] };

        var parecer = await Gerar(analise, _ => { });

        Assert.Contains(parecer.Limitacoes, l => l.StartsWith("M1-R01:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AfirmacaoProibida_ReenviadaAoModelo_DepoisAceita()
    {
        var analise = Analise();
        var invalida = ParecerValido(analise);
        invalida["sintese"] = "A escrituração está em conformidade.";
        var chat = new ChatFalso().Responder(invalida).Responder(ParecerValido(analise));

        var gerado = await Servico(chat).GerarAsync(analise, "execucao", "correlacao", CancellationToken.None);

        Assert.NotNull(gerado.Parecer);
        Assert.Equal(["RESPOSTA_INVALIDA", "RESPOSTA_VALIDA"], gerado.Consumo.Select(c => c.Resultado));
    }
}
