using Api.Agente.Application.Analise;
using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;
using Api.Agente.Domain.Motor;
using Api.Agente.Tests.Fixtures;
using static Api.Agente.Tests.Domain.RegrasDeTeste;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Governanca;

/// <summary>Spec 004, Fase 1: o motor lê o catálogo completo de docs/regras com a governança de aprovação.</summary>
public class CatalogoCompletoTests
{
    private static readonly string[] AprovadasHoje =
        ["M1-I01", "M1-I03", "M1-I05", "M1-I06", "M1-P01", "M1-P02", "M1-R01", "M1-R02", "M1-R03", "M1-R04"];

    private static readonly string[] MigradasParaGenerico = ["M1-I03", "M1-I05", "M1-I06"];

    private static CatalogoRegras Catalogo => CatalogoReal.Value;

    [Fact]
    public void CatalogoReal_158RegrasDeCoesaoE5PreCondicoes()
    {
        Assert.Equal(158, Catalogo.Regras.Count(r => r.Tipo == TipoRegra.Cruzamento));
        Assert.Equal(5, Catalogo.Regras.Count(r => r.Tipo == TipoRegra.PreCondicao && r.Status == StatusRegra.Aprovada));
        Assert.Equal("0.3.0", Catalogo.Versao);
    }

    [Fact]
    public void CatalogoReal_SoAsRegrasDeAprovacoesJsonEstaoAprovadas()
    {
        var aprovadas = Catalogo.Regras.Where(r => r.Tipo == TipoRegra.Cruzamento && r.Status == StatusRegra.Aprovada).Select(r => r.Id).Order(StringComparer.Ordinal);

        Assert.Equal(AprovadasHoje, aprovadas);
        Assert.All(Catalogo.Regras.Where(r => r.Tipo == TipoRegra.Cruzamento && !AprovadasHoje.Contains(r.Id)), r => Assert.Equal(StatusRegra.Proposta, r.Status));
    }

    [Fact]
    public void CatalogoReal_TodaRegraTemPassoDaOrdemDeExecucao()
    {
        Assert.All(Catalogo.Regras.Where(r => r.Tipo == TipoRegra.Cruzamento), r => Assert.InRange(r.Passo, 1, 8));
        Assert.Equal(4, Catalogo.ObterObrigatoria("M1-P01").Passo);
        Assert.Equal(5, Catalogo.ObterObrigatoria("M10-02").Passo);
        Assert.Equal(7, Catalogo.ObterObrigatoria("M12-01").Passo);
        Assert.Equal(1, Catalogo.ObterObrigatoria("PAR-001").Passo);
    }

    [Fact]
    public void CatalogoReal_DependenciaDispensadaFicaRegistradaComMotivo()
    {
        var regra = Catalogo.ObterObrigatoria("M1-P01");

        Assert.Empty(regra.DependeDe);
        Assert.Equal(["ENT-003", "PAR-001"], regra.DependenciasDispensadas.Keys.Order(StringComparer.Ordinal));
        Assert.All(regra.DependenciasDispensadas.Values, motivo => Assert.False(string.IsNullOrWhiteSpace(motivo)));
    }

    [Fact]
    public void CatalogoReal_TodaAprovadaTemExecutor_AsMigradasPeloGenerico()
    {
        var motor = MotorRegras.Montar(ValidadoresNfeEfd.Todos(), Catalogo);
        var executores = motor.Executores(Catalogo).ToDictionary(e => e.RegraId);

        Assert.Empty(motor.RegrasAprovadasSemExecutor(Catalogo));
        Assert.All(MigradasParaGenerico, id => Assert.True(executores[id].Generico, id));
        Assert.All(AprovadasHoje.Except(MigradasParaGenerico), id => Assert.True(executores[id].Dedicado, id));
    }

    [Fact]
    public void CatalogoReal_PropostasComPendenciaOuSemExecutorTemMotivo()
    {
        var executores = MotorRegras.Montar(ValidadoresNfeEfd.Todos(), Catalogo).Executores(Catalogo);

        Assert.All(executores.Where(e => !e.Executavel), e => Assert.False(string.IsNullOrWhiteSpace(e.Motivo), e.RegraId));
    }

    [Fact]
    public void AprovadaSemExecutor_ImpedeInicializacao()
    {
        var catalogo = new CatalogoRegras("t", [Cruzamento("M3-98", TipoRelacao.Agregacao)]);

        var faltantes = MotorRegras.Montar([], catalogo).RegrasAprovadasSemExecutor(catalogo);

        Assert.Contains("M3-98", Assert.Single(faltantes), StringComparison.Ordinal);
    }

    [Fact]
    public void AprovadaComParametroSemValor_Recusada()
    {
        var regra = Cruzamento("M1-I98") with { ParametrosUsados = ["TOL_X"] };
        var parametros = new CatalogoParametros(new Dictionary<string, ParametroRegra> { ["TOL_X"] = new("TOL_X", "decimal", false, null, null) });

        var erros = ValidadorCatalogo.Validar(new CatalogoRegras("t", [regra]) { Parametros = parametros });

        Assert.Contains(erros, e => e.Contains("PARAM:TOL_X", StringComparison.Ordinal));
    }

    [Fact]
    public void PropostaComParametroSemValorOuPendencia_Aceita()
    {
        var regra = Cruzamento("M1-I98", status: StatusRegra.Proposta) with { ParametrosUsados = ["TOL_X"], Pendencias = ["severidade INFO sem correspondência."] };

        Assert.Empty(ValidadorCatalogo.Validar(new CatalogoRegras("t", [regra])));
    }

    [Fact]
    public void AprovadaComPendencia_Recusada()
    {
        var regra = Cruzamento("M1-I98") with { Pendencias = ["severidade INFO sem correspondência."] };

        Assert.Contains(ValidadorCatalogo.Validar(new CatalogoRegras("t", [regra])), e => e.Contains("pendência", StringComparison.Ordinal));
    }

    [Fact]
    public void AprovadaDependendoDeProposta_Recusada()
    {
        var catalogo = new CatalogoRegras("t", [Cruzamento("PAR-901", TipoRelacao.Parametro, status: StatusRegra.Proposta), Cruzamento("M1-I98", dependeDe: ["PAR-901"])]);

        Assert.Contains(ValidadorCatalogo.Validar(catalogo), e => e.Contains("PAR-901, que não está aprovada", StringComparison.Ordinal));
    }

    [Fact]
    public void Proposta_NaoExecuta()
    {
        var resultado = Analisar(Lote(EfdIcmsIpi(), NFe()));

        Assert.DoesNotContain(resultado.Regras, r => Catalogo.ObterObrigatoria(r.RegraId).Status != StatusRegra.Aprovada);
        Assert.DoesNotContain(resultado.Achados, a => a.RegraId.StartsWith("M2-", StringComparison.Ordinal));
    }

    [Fact]
    public void Cobertura_InformadaNasLimitacoes()
    {
        var resultado = Analisar(Lote(EfdIcmsIpi(), NFe()));

        Assert.Contains("10 de 158 regras de cruzamento aprovadas", Assert.Single(resultado.Limitacoes), StringComparison.Ordinal);
    }

    /// <summary>O agente cruza o que vier: todas as obrigações ou só parte delas. Regra sem nenhuma de suas obrigações não se aplica.</summary>
    [Fact]
    public void LoteParcial_SemObrigacoesDasRegrasAprovadas_NaoAplicavelSemFalha()
    {
        var resultado = Analisar(Lote(Ecd(), EfdContribuicoes()));

        Assert.All(resultado.Regras, r => Assert.Equal(SituacaoRegra.NaoAplicavel, r.Situacao));
        Assert.DoesNotContain(resultado.Achados, a => a.Natureza == Natureza.FalhaTecnica);
    }

    /// <summary>
    /// Com só um lado do cruzamento, a regra não é presumida conforme: fica sem evidência, apontando o que faltou (P4/P5),
    /// e quem depende dela fica não conclusiva.
    /// </summary>
    [Fact]
    public void LoteParcial_SoUmLadoDoCruzamento_AusenciaDeEvidenciasApontandoAFaltante()
    {
        var resultado = Analisar(Lote(NFe()));

        Assert.All(resultado.Regras, r => Assert.Contains(r.Situacao, new[] { SituacaoRegra.AusenciaEvidencias, SituacaoRegra.NaoConclusiva }));
        Assert.Contains(resultado.Regras, r => r.Situacao == SituacaoRegra.AusenciaEvidencias);
        Assert.All(
            resultado.Achados.Where(a => a.Natureza == Natureza.AusenciaEvidencias && a.ObrigacoesFaltantes.Count > 0),
            a => Assert.Contains(a.ObrigacoesFaltantes, f => f.Tipo == TipoObrigacao.EfdIcmsIpi));
        Assert.NotEqual(StatusAnalise.Concluida, resultado.Status);
    }
}
