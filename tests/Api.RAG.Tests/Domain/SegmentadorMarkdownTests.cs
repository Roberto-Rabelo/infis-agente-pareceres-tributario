using Api.RAG.Domain;
using Api.RAG.Tests.Fixtures;

namespace Api.RAG.Tests.Domain;

public class SegmentadorMarkdownTests
{
    [Fact]
    public void LinhaDeTabela_ViraTrechoComCabecalhoSecaoELinha()
    {
        var trechos = SegmentadorMarkdown.Segmentar(Documentos.Cruzamentos, 600);

        var regra = Assert.Single(trechos, t => t.RegraPrincipal == "CRZ-NFE-EFD-002");
        Assert.StartsWith("| ID | Condição |", regra.Texto, StringComparison.Ordinal);
        Assert.Contains("COD_SIT = 02", regra.Texto, StringComparison.Ordinal);
        Assert.Equal("03 – Regras de Cruzamento entre Obrigações > 2. NF-e/NFC-e → EFD ICMS/IPI", regra.Secao);
        Assert.Equal(regra.LinhaInicio, regra.LinhaFim);
        Assert.Contains("CRZ-NFE-EFD-002", Documentos.Cruzamentos.Split('\n')[regra.LinhaInicio - 1], StringComparison.Ordinal);
    }

    [Fact]
    public void RegraCitada_EntraNosIds_MasNaoEhPrincipal()
    {
        var trechos = SegmentadorMarkdown.Segmentar(Documentos.Cruzamentos, 600);

        var regra = Assert.Single(trechos, t => t.RegraPrincipal == "CRZ-NFE-EFD-001");
        Assert.Equal(["CRZ-NFE-EFD-001", "CRZ-NFE-EFD-002"], regra.RegraIds);
    }

    [Fact]
    public void Obrigacoes_DerivadasDoIdEDoTituloDaSecao()
    {
        var trechos = SegmentadorMarkdown.Segmentar(Documentos.Cruzamentos, 600);

        Assert.Equal(["EFD_ICMS_IPI", "NFCE", "NFE"], trechos.Single(t => t.RegraPrincipal == "CRZ-NFE-EFD-001").Obrigacoes);
        Assert.Equal(["ECD", "ECF"], trechos.Single(t => t.RegraPrincipal == "CRZ-ECD-ECF-001").Obrigacoes);
        Assert.Empty(trechos[0].Obrigacoes);
    }

    [Fact]
    public void Paragrafos_AgrupadosAteOLimite_SemPerderConteudo()
    {
        var paragrafos = Enumerable.Range(1, 12).Select(i => $"Parágrafo {i} " + new string('x', 80)).ToList();
        var conteudo = "# Título\n\n" + string.Join("\n\n", paragrafos);

        var trechos = SegmentadorMarkdown.Segmentar(conteudo, 300);

        Assert.True(trechos.Count > 1);
        Assert.All(trechos, t => Assert.True(t.Texto.Length <= 300));
        Assert.All(paragrafos, p => Assert.Contains(trechos, t => t.Texto.Contains(p, StringComparison.Ordinal)));
        Assert.Equal(Enumerable.Range(0, trechos.Count), trechos.Select(t => t.Ordem));
    }

    [Fact]
    public void BlocoDeCodigo_ComLinhaEmBranco_PermaneceNoMesmoTrecho()
    {
        const string Conteudo = "# Modelo\n\n```json\n{\n\n  \"id\": \"CRZ-NFE-EFD-001\"\n}\n```\n";

        var trecho = Assert.Single(SegmentadorMarkdown.Segmentar(Conteudo, 600));

        Assert.StartsWith("```json", trecho.Texto, StringComparison.Ordinal);
        Assert.EndsWith("```", trecho.Texto, StringComparison.Ordinal);
        Assert.Equal(["CRZ-NFE-EFD-001"], trecho.RegraIds);
    }

    [Fact]
    public void Segmentacao_EhDeterministica()
    {
        var a = SegmentadorMarkdown.Segmentar(Documentos.Cruzamentos, 600);
        var b = SegmentadorMarkdown.Segmentar(Documentos.Cruzamentos.Replace("\n", "\r\n", StringComparison.Ordinal), 600);

        Assert.Equal(a.Select(t => t.HashTexto), b.Select(t => t.HashTexto));
    }

    [Theory]
    [InlineData("03-regras-cruzamento.md", "CRZ-")]
    [InlineData("NUCLEO-1-principios-entidades-matriz.md", "ENT-")]
    [InlineData("NUCLEO-2-mapas-bidirecionais.md", "M1-")]
    public void BaseReal_TodaRegraDefinidaEmTabela_TemTrechoProprio(string arquivo, string prefixo)
    {
        var conteudo = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "docs", arquivo));
        var trechos = SegmentadorMarkdown.Segmentar(conteudo, 1500);

        var definidas = conteudo.Split('\n')
            .Where(l => l.TrimStart().StartsWith('|'))
            .Select(l => ReferenciasRegra.Extrair(l.Trim().TrimStart('|').Split('|')[0]))
            .Where(ids => ids.Count > 0 && ids[0].StartsWith(prefixo, StringComparison.Ordinal))
            .Select(ids => ids[0])
            .Distinct()
            .ToList();

        Assert.NotEmpty(definidas);
        Assert.All(definidas, id => Assert.Contains(trechos, t => t.RegraPrincipal == id));
    }

    [Theory]
    [InlineData("CRZ-NFE-EFD-001", true)]
    [InlineData("ENT-003", true)]
    [InlineData("M1-P05", true)]
    [InlineData("M10-01", true)]
    [InlineData("CRZ-NFE-EFD-1", false)]
    [InlineData("crz-nfe-efd-001", false)]
    [InlineData("CRZ-NFE-EFD-001; DROP TABLE", false)]
    public void IdDeRegra_Validado(string id, bool valido) => Assert.Equal(valido, ReferenciasRegra.EhIdValido(id));
}
