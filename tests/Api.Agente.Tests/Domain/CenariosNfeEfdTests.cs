using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Domain;
using Api.Agente.Domain.Motor;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Governanca;
using Json.Schema;

namespace Api.Agente.Tests.Domain;

/// <summary>Os lotes de exemplos/cenarios são os mesmos enviados por analise-obrigacoes.http (regras M1 do NUCLEO-2).</summary>
public class CenariosNfeEfdTests
{
    private const int TotalRegras = 10;

    [Fact]
    public void Cenarios_AtendemAoContratoDeEntrada()
    {
        var arquivos = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "exemplos", "cenarios"), "*.json");

        Assert.Equal(TotalRegras, arquivos.Length);
        foreach (var arquivo in arquivos)
        {
            using var documento = JsonDocument.Parse(File.ReadAllBytes(arquivo));
            var avaliacao = GovernancaTests.SchemaEntrada.Value.Evaluate(documento.RootElement, new EvaluationOptions { RequireFormatValidation = true });
            Assert.True(avaliacao.IsValid, arquivo);
        }
    }

    [Fact]
    public void Consistente_SemAchados()
    {
        var resultado = Analisar("01-consistente");

        Assert.Equal(StatusAnalise.Concluida, resultado.Status);
        Assert.Empty(resultado.Achados);
        Assert.False(resultado.RevisaoHumana.Requerida);
        Assert.Equal(TotalRegras, resultado.Regras.Count(r => r.Situacao == SituacaoRegra.Executada));
    }

    [Fact]
    public void Omissao_M1P01()
    {
        var resultado = Analisar("02-omissao-saida");

        var achado = Assert.Single(resultado.Achados);
        Assert.Equal("M1-P01", achado.RegraId);
        Assert.Equal(Severidade.ErroGrave, achado.Severidade);
        Assert.Contains(Lotes.ChaveNfe, achado.Descricao, StringComparison.Ordinal);
        Assert.NotEmpty(achado.HipotesesLegitimas);
        Assert.Equal(SituacaoRegra.NaoConclusiva, Regra(resultado, "M1-I01").Situacao);
        Assert.Equal(StatusAnalise.ConcluidaParcial, resultado.Status);
        Assert.Contains("Há erro grave.", resultado.RevisaoHumana.Motivos);
    }

    [Fact]
    public void ValoresDivergentes_M1I03_M1I05()
    {
        var resultado = Analisar("03-valores-divergentes");

        var total = Assert.Single(resultado.Achados, a => a.RegraId == "M1-I03");
        Assert.Equal(900m, total.ValorOrigem);
        Assert.Equal(1000m, total.ValorDestino);
        Assert.Equal(-100m, total.Diferenca);
        Assert.Equal(0m, total.ToleranciaAplicada);

        var icms = resultado.Achados.Where(a => a.RegraId == "M1-I05").ToList();
        Assert.Equal(2, icms.Count);
        Assert.Contains(icms, a => a.ValorOrigem == 90m && a.ValorDestino == 180m);
        Assert.Equal(StatusAnalise.Concluida, resultado.Status);
        Assert.True(resultado.RevisaoHumana.Requerida);
    }

    [Fact]
    public void CreditoAcimaDoDestacado_M1I06()
    {
        var achado = Assert.Single(Analisar("04-credito-acima-do-destacado").Achados);

        Assert.Equal("M1-I06", achado.RegraId);
        Assert.Equal(60m, achado.ValorOrigem);
        Assert.Equal(250m, achado.ValorDestino);
    }

    [Fact]
    public void CreditoMenorQueODestacado_SemAchado()
    {
        var lote = Lote("04-credito-acima-do-destacado");
        lote["obrigacoes"]![0]!["registros"]!["C100"]![1]!["campos"]!["VL_ICMS"] = "0.00";

        Assert.Empty(Lotes.Analisar(lote).Achados);
    }

    [Fact]
    public void CanceladaEscrituradaRegular_M1P02_M1R01()
    {
        var resultado = Analisar("05-cancelada-escriturada-regular");

        Assert.Equal(Severidade.Alerta, Assert.Single(resultado.Achados, a => a.RegraId == "M1-P02").Severidade);
        Assert.Equal(Severidade.ErroGrave, Assert.Single(resultado.Achados, a => a.RegraId == "M1-R01").Severidade);
        Assert.Equal(2, resultado.Achados.Count);
    }

    [Fact]
    public void CanceladoSemEvento_M1R02()
    {
        var resultado = Analisar("06-cancelado-sem-evento");

        Assert.Single(resultado.Achados, a => a.RegraId == "M1-R02");
        Assert.Single(resultado.Achados, a => a.RegraId == "M1-P01");
    }

    [Fact]
    public void DocumentoDeOutroEstabelecimento_M1R03_M1R04()
    {
        var resultado = Analisar("07-documento-de-outro-estabelecimento");

        Assert.Single(resultado.Achados, a => a.RegraId == "M1-R03");
        Assert.Single(resultado.Achados, a => a.RegraId == "M1-R04");
        Assert.Equal(Natureza.AusenciaEvidencias, Assert.Single(resultado.Achados, a => a.RegraId == "M1-P01").Natureza);
        Assert.Equal(StatusAnalise.ConcluidaParcial, resultado.Status);
    }

    [Fact]
    public void IdentificacaoDivergente_M1I01()
    {
        var resultado = Analisar("08-identificacao-divergente");

        var achados = resultado.Achados.Where(a => a.RegraId == "M1-I01").ToList();
        Assert.Equal(2, achados.Count);
        Assert.Contains(achados, a => a.Severidade == Severidade.ErroGrave && a.Descricao.Contains("NUM_DOC", StringComparison.Ordinal));
        Assert.Contains(achados, a => a.Severidade == Severidade.Alerta && a.Descricao.Contains("DT_DOC", StringComparison.Ordinal));
    }

    [Fact]
    public void C100SemXml_AusenciaDeEvidencias_NuncaErro()
    {
        var resultado = Analisar("09-c100-sem-xml");

        var achado = Assert.Single(resultado.Achados);
        Assert.Equal("M1-R01", achado.RegraId);
        Assert.Equal(Natureza.AusenciaEvidencias, achado.Natureza);
        Assert.Null(achado.Severidade);
        Assert.Equal(StatusAnalise.ConcluidaParcial, resultado.Status);
    }

    [Fact]
    public void Incompleto_NfeReprovada_AusenciaDeEvidencias()
    {
        var resultado = Analisar("10-nfe-reprovada-na-origem");

        Assert.Equal(StatusAnalise.Inconclusiva, resultado.Status);
        Assert.Single(resultado.Achados, a => a.RegraId == "PRE-003");
        Assert.Equal(TotalRegras - 1, resultado.Achados.Count(a => a.Natureza == Natureza.AusenciaEvidencias));
        Assert.Equal(SituacaoRegra.NaoConclusiva, Regra(resultado, "M1-I01").Situacao);
    }

    [Fact]
    public void RegraComInconsistenciaEEvidenciaFaltante_FicaPendente()
    {
        var lote = Lote("05-cancelada-escriturada-regular");
        ((JsonArray)lote["obrigacoes"]!).RemoveAt(2);

        var resultado = Lotes.Analisar(lote);

        Assert.Equal(SituacaoRegra.AusenciaEvidencias, Regra(resultado, "M1-R01").Situacao);
        Assert.Equal(StatusAnalise.ConcluidaParcial, resultado.Status);
    }

    private static ResultadoAnalise Analisar(string cenario) => Lotes.Analisar(Lote(cenario));

    private static JsonObject Lote(string cenario) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "exemplos", "cenarios", cenario + ".json")))!.AsObject();

    private static ResultadoRegra Regra(ResultadoAnalise resultado, string id) => resultado.Regras.Single(r => r.RegraId == id);
}
