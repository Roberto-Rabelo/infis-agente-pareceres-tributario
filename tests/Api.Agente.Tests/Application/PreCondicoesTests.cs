using System.Text.Json.Nodes;
using Api.Agente.Domain;
using Api.Agente.Tests.Fixtures;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Application;

public class PreCondicoesTests
{
    [Fact]
    public void ObrigacoesConsistentes_Aptas()
    {
        var resultado = Analisar(Lote("11222333", "2026-01-01", "2026-12-31", EfdIcmsIpi(), Ecd(), NFe()));

        Assert.All(resultado.Aptidao, a => Assert.True(a.Apta, a.ObrigacaoId));
        Assert.DoesNotContain(resultado.Achados, a => a.Natureza == Natureza.PreCondicaoNaoAtendida);
        Assert.Equal(3, resultado.Cobertura.Count(c => c.Apta));
    }

    [Fact]
    public void Pre001_EstruturaInvalida_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd["campoNaoPrevisto"] = "x";

        var resultado = Analisar(Lote(efd, NFe()));

        AssertNaoApta(resultado, "efd-icms-1", "PRE-001");
        Assert.True(Aptidao(resultado, "nfe-1").Apta);
    }

    [Fact]
    public void Pre001_CnpjComDvInvalido_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd["contribuinte"]!["cnpj"] = "11222333000182";

        AssertNaoApta(Analisar(Lote(efd)), "efd-icms-1", "PRE-001");
    }

    [Fact]
    public void Pre001_IdRepetido_SegundaNaoApta()
    {
        var resultado = Analisar(Lote(EfdIcmsIpi(id: "x"), NFe(id: "x")));

        Assert.Equal(2, resultado.Aptidao.Count);
        Assert.True(resultado.Aptidao[0].Apta);
        Assert.False(resultado.Aptidao[1].Apta);
        Assert.Contains("PRE-001", resultado.Aptidao[1].RegrasNaoAtendidas);
    }

    [Fact]
    public void Pre001_DocumentoSemChave_NaoApta()
    {
        var nfe = NFe();
        nfe["identificacao"] = new JsonObject();

        AssertNaoApta(Analisar(Lote(nfe)), "nfe-1", "PRE-001");
    }

    [Fact]
    public void Pre001_CnpjAlfanumericoValido_Apto()
    {
        var resultado = Analisar(Lote("12ABC345", "2026-03-01", "2026-03-31", EfdIcmsIpi(cnpj: CnpjAlfanumerico)));

        Assert.True(Aptidao(resultado, "efd-icms-1").Apta);
    }

    [Fact]
    public void Pre002_SemVersao_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd.Remove("versaoLayout");

        AssertNaoApta(Analisar(Lote(efd)), "efd-icms-1", "PRE-002");
    }

    [Fact]
    public void Pre002_VersaoNaoSuportada_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd["versaoLayout"] = "019";

        var resultado = Analisar(Lote(efd, EfdContribuicoes()));

        AssertNaoApta(resultado, "efd-icms-1", "PRE-002");
        AssertNaoApta(resultado, "efd-contrib-1", "PRE-002");
    }

    [Fact]
    public void Pre002_VersaoForaDeVigencia_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd["periodo"] = new JsonObject { ["inicio"] = "2025-03-01", ["fim"] = "2025-03-31" };

        AssertNaoApta(Analisar(Lote("11222333", "2025-01-01", "2026-12-31", efd)), "efd-icms-1", "PRE-002");
    }

    [Fact]
    public void Pre003_SemRelatorio_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd.Remove("validacaoOrigem");

        AssertNaoApta(Analisar(Lote(efd)), "efd-icms-1", "PRE-003");
    }

    [Fact]
    public void Pre003_Reprovada_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd["validacaoOrigem"] = Validacao("REPROVADO", ("ERRO", "Registro C190 não fecha com C100."));

        AssertNaoApta(Analisar(Lote(efd)), "efd-icms-1", "PRE-003");
    }

    [Fact]
    public void Pre003_AprovadaComErro_NaoApta()
    {
        var efd = EfdIcmsIpi();
        efd["validacaoOrigem"] = Validacao("APROVADO", ("ERRO", "Registro obrigatório ausente."));

        AssertNaoApta(Analisar(Lote(efd)), "efd-icms-1", "PRE-003");
    }

    [Fact]
    public void Pre003_AprovadaComAdvertencia_AptaComAlerta()
    {
        var efd = EfdIcmsIpi();
        efd["validacaoOrigem"] = Validacao("APROVADO", ("ADVERTENCIA", "Participante sem inscrição estadual."));

        var aptidao = Aptidao(Analisar(Lote(efd)), "efd-icms-1");

        Assert.True(aptidao.Apta);
        var advertencia = Assert.Single(aptidao.AdvertenciasOrigem);
        Assert.Equal("Participante sem inscrição estadual.", advertencia.Mensagem);
        Assert.Equal("agente-validador-teste", advertencia.AgenteOrigem);
        Assert.Equal(Severidade.Alerta, advertencia.Severidade);
        Assert.Equal(10, advertencia.Linha);
    }

    [Fact]
    public void Pre004_OriginalESubstituta_UsaSubstituta()
    {
        var original = EfdIcmsIpi(id: "original", recibo: "REC-1");
        var substituta = EfdIcmsIpi(id: "substituta", recibo: "REC-2");
        substituta["finalidade"] = "SUBSTITUTA";
        substituta["identificacao"]!["reciboAnterior"] = "REC-1";

        var resultado = Analisar(Lote(original, substituta));

        Assert.True(Aptidao(resultado, "substituta").Apta);
        var descartada = Aptidao(resultado, "original");
        Assert.False(descartada.Apta);
        Assert.Contains("substituta", descartada.MotivoDescarte, StringComparison.Ordinal);
        Assert.DoesNotContain(resultado.Achados, a => a.RegraId == "PRE-004");
    }

    [Fact]
    public void Pre004_CadeiaAmbigua_NenhumaApta()
    {
        var resultado = Analisar(Lote(EfdIcmsIpi(id: "a", recibo: "REC-1"), EfdIcmsIpi(id: "b", recibo: "REC-2")));

        AssertNaoApta(resultado, "a", "PRE-004");
        AssertNaoApta(resultado, "b", "PRE-004");
    }

    [Fact]
    public void Pre004_DocumentoRepetido_DescartaSegundo()
    {
        var resultado = Analisar(Lote(NFe(id: "nfe-a"), NFe(id: "nfe-b")));

        Assert.True(Aptidao(resultado, "nfe-a").Apta);
        Assert.False(Aptidao(resultado, "nfe-b").Apta);
        Assert.NotNull(Aptidao(resultado, "nfe-b").MotivoDescarte);
    }

    [Fact]
    public void Pre004_EstabelecimentosDiferentes_AmbosAptos()
    {
        var resultado = Analisar(Lote(EfdIcmsIpi(id: "matriz"), EfdIcmsIpi(id: "filial", cnpj: CnpjFilial, recibo: "REC-F")));

        Assert.True(Aptidao(resultado, "matriz").Apta);
        Assert.True(Aptidao(resultado, "filial").Apta);
    }

    [Fact]
    public void Pre005_OutroContribuinte_NaoApta()
    {
        var resultado = Analisar(Lote(EfdIcmsIpi(), EfdIcmsIpi(id: "outro", cnpj: CnpjOutroContribuinte, recibo: "REC-O")));

        Assert.True(Aptidao(resultado, "efd-icms-1").Apta);
        AssertNaoApta(resultado, "outro", "PRE-005");
    }

    [Fact]
    public void Pre005_PeriodoSemSobreposicao_NaoApta()
    {
        var resultado = Analisar(Lote("11222333", "2026-05-01", "2026-05-31", EfdIcmsIpi()));

        AssertNaoApta(resultado, "efd-icms-1", "PRE-005");
    }

    private static void AssertNaoApta(Api.Agente.Application.Analise.ResultadoAnalise resultado, string id, string regra)
    {
        var aptidao = Aptidao(resultado, id);
        Assert.False(aptidao.Apta);
        Assert.Contains(regra, aptidao.RegrasNaoAtendidas);
        Assert.Contains(resultado.Achados, a =>
            a.RegraId == regra
            && a.Natureza == Natureza.PreCondicaoNaoAtendida
            && a.Severidade is null
            && a.Evidencias.Any(e => e.ObrigacaoId == id));
        Assert.DoesNotContain(resultado.Cobertura, c => c.ObrigacaoId == id && c.Apta);
    }
}
