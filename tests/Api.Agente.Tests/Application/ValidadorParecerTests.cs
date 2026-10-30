using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using static Api.Agente.Tests.Fixtures.Pareceres;

namespace Api.Agente.Tests.Application;

/// <summary>T068: validação estrutural e semântica da resposta do LLM (P2, P4, P9, P10, P15, FR-036).</summary>
public class ValidadorParecerTests
{
    private static ResultadoValidacaoParecer Validar(ResultadoAnalise analise, string? resposta) =>
        Validador.Validar(
            resposta,
            analise,
            MontadorDadosParecer.Montar(analise),
            analise.Fundamentacao.SelectMany(f => f.Referencias).Select(r => r.TrechoId).ToHashSet(StringComparer.Ordinal));

    private static List<string> Codigos(Action<JsonObject> alterar, ResultadoAnalise? analise = null)
    {
        var alvo = analise ?? Analise();
        var parecer = ParecerValido(alvo);
        alterar(parecer);
        var resultado = Validar(alvo, parecer.ToJsonString());
        Assert.False(resultado.Valido);
        Assert.Null(resultado.Parecer);
        return [.. resultado.Erros.Select(e => e.Codigo)];
    }

    private static JsonObject PrimeiroAchado(JsonObject parecer) => parecer["achados"]![0]!.AsObject();

    [Fact]
    public void RespostaConforme_Aceita()
    {
        var analise = Analise();

        var resultado = Validar(analise, ParecerValido(analise).ToJsonString());

        Assert.True(resultado.Valido);
        Assert.Equal("CRZ-NFE-EFD-001", Assert.Single(resultado.Parecer!.Achados).RegraId);
    }

    [Theory]
    [InlineData(null, "RESPOSTA_VAZIA")]
    [InlineData("  ", "RESPOSTA_VAZIA")]
    [InlineData("O parecer é: tudo certo.", "JSON_INVALIDO")]
    [InlineData("{}", "SCHEMA_INVALIDO")]
    public void RespostaForaDoFormato_Rejeitada(string? resposta, string codigo) =>
        Assert.Equal(codigo, Assert.Single(Validar(Analise(), resposta).Erros).Codigo);

    /// <summary>P10: campos operacionais pertencem ao envelope, nunca à saída do modelo.</summary>
    [Theory]
    [InlineData("execucaoId")]
    [InlineData("correlacaoId")]
    [InlineData("consumo")]
    [InlineData("modelo")]
    public void CampoOperacionalNaSaida_Rejeitado(string campo) =>
        Assert.Equal(["SCHEMA_INVALIDO"], Codigos(p => p[campo] = "x"));

    [Fact]
    public void StatusDiferenteDoSistema_Rejeitado() =>
        Assert.Contains("STATUS_DIVERGENTE", Codigos(p => p["status"] = "CONCLUIDA_PARCIAL"));

    [Fact]
    public void AchadoReclassificado_Rejeitado() =>
        Assert.Contains("ACHADO_RECLASSIFICADO", Codigos(p => PrimeiroAchado(p)["severidade"] = "ALERTA"));

    [Fact]
    public void AchadoComNaturezaTrocada_Rejeitado() =>
        Assert.Contains("ACHADO_RECLASSIFICADO", Codigos(p => PrimeiroAchado(p)["natureza"] = "AUSENCIA_EVIDENCIAS"));

    [Fact]
    public void AchadoOmitido_Rejeitado() =>
        Assert.Contains("ACHADO_OMITIDO", Codigos(p => p["achados"] = new JsonArray()));

    [Fact]
    public void AchadoDuplicado_Rejeitado() =>
        Assert.Contains("ACHADO_DUPLICADO", Codigos(p => p["achados"]!.AsArray().Add(PrimeiroAchado(p).DeepClone())));

    [Fact]
    public void AchadoCriadoPeloModelo_Rejeitado()
    {
        var codigos = Codigos(p =>
        {
            var novo = PrimeiroAchado(p).DeepClone().AsObject();
            novo["achadoId"] = "A999";
            p["achados"]!.AsArray().Add(novo);
        });

        Assert.Contains("ACHADO_NAO_RECEBIDO", codigos);
    }

    [Fact]
    public void HipoteseLegitimaOmitida_Rejeitada() =>
        Assert.Contains("HIPOTESE_OMITIDA", Codigos(p => PrimeiroAchado(p)["hipotesesLegitimas"] = new JsonArray()));

    /// <summary>FR-036: indício da IA nunca é ERRO_GRAVE.</summary>
    [Fact]
    public void IndicioIaComErroGrave_Rejeitado()
    {
        var codigos = Codigos(p =>
        {
            var indicio = Indicio("trecho-CRZ-NFE-EFD-001");
            indicio["severidade"] = "ERRO_GRAVE";
            p["indiciosIa"] = new JsonArray(indicio);
        });

        Assert.Equal(["SCHEMA_INVALIDO"], codigos);
    }

    [Fact]
    public void IndicioIaComoNaturezaDeAchado_Rejeitado() =>
        Assert.Equal(["SCHEMA_INVALIDO"], Codigos(p => PrimeiroAchado(p)["natureza"] = "INDICIO_IA"));

    [Fact]
    public void IndicioSemTrechoDeApoio_Rejeitado() =>
        Assert.Contains("INDICIO_SEM_REFERENCIA", Codigos(p => p["indiciosIa"] = new JsonArray(Indicio())));

    /// <summary>US7 cenário 3: referência a trecho não recuperado nesta execução.</summary>
    [Fact]
    public void ReferenciaInexistente_Rejeitada()
    {
        var codigos = Codigos(p => PrimeiroAchado(p)["referencias"] = new JsonArray("trecho-inventado"));

        Assert.Equal(["REFERENCIA_INEXISTENTE"], codigos);
    }

    [Fact]
    public void FonteCitadaInexistente_Rejeitada() =>
        Assert.Contains("REFERENCIA_INEXISTENTE", Codigos(p => p["fontesCitadas"]!.AsArray().Add("Manual EFD 3.2")));

    [Fact]
    public void RevisaoHumanaDispensadaQuandoObrigatoria_Rejeitada() =>
        Assert.Contains("REVISAO_HUMANA_OMITIDA", Codigos(p => p["revisaoHumana"]!["requerida"] = false));

    [Fact]
    public void IndicioIa_ExigeRevisaoHumanaMesmoSemAchadoQueAExija()
    {
        var analise = Analise(revisao: new RevisaoHumana(false, []));

        var codigos = Codigos(
            p =>
            {
                p["indiciosIa"] = new JsonArray(Indicio("trecho-CRZ-NFE-EFD-001"));
                p["revisaoHumana"]!["requerida"] = false;
            },
            analise);

        Assert.Equal(["REVISAO_HUMANA_OMITIDA"], codigos);
    }

    [Fact]
    public void AchadoSemSeveridade_AceitoComNulo()
    {
        var analise = Analise(achados: Cruzamento(severidade: null));

        Assert.True(Validar(analise, ParecerValido(analise).ToJsonString()).Valido);
    }

    [Fact]
    public void TodosOsStatusDoSistema_SaoRepresentaveisNoContrato()
    {
        foreach (var status in Enum.GetValues<StatusAnalise>())
        {
            var analise = Analise(status);

            Assert.True(Validar(analise, ParecerValido(analise).ToJsonString()).Valido, status.ToString());
        }
    }
}
