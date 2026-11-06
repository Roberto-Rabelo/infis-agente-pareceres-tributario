using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;
using Api.Agente.Domain.Motor;
using static Api.Agente.Tests.Domain.RegrasDeTeste;

namespace Api.Agente.Tests.Domain;

public class MotorRegrasTests
{
    private static readonly ObrigacaoRecebida Nfe = Obrigacao("nfe", TipoObrigacao.NFe);
    private static readonly ObrigacaoRecebida Efd = Obrigacao("efd", TipoObrigacao.EfdIcmsIpi);

    [Fact]
    public void Ordem_ParAntesDeExcAntesDePresencaAntesDeIdentidadeAntesDeAgregacaoAntesDeContinuidade()
    {
        var ordem = new List<string>();
        var regras = new[]
        {
            Cruzamento("CRZ-TST-006", TipoRelacao.Continuidade),
            Cruzamento("CRZ-TST-005", TipoRelacao.Agregacao),
            Cruzamento("CRZ-TST-004", TipoRelacao.Identidade),
            Cruzamento("CRZ-TST-003", TipoRelacao.PresencaReversa),
            Cruzamento("CRZ-TST-002", TipoRelacao.Exclusividade),
            Cruzamento("CRZ-TST-001", TipoRelacao.Parametro),
        };
        var validadores = regras.Select(r => new ValidadorFalso(r.Id, (_, regra) => { ordem.Add(regra.Id); return []; }));

        new MotorRegras(validadores).Executar(new CatalogoRegras("t", regras), Contexto(Nfe, Efd));

        Assert.Equal(["CRZ-TST-001", "CRZ-TST-002", "CRZ-TST-003", "CRZ-TST-004", "CRZ-TST-005", "CRZ-TST-006"], ordem);
    }

    [Fact]
    public void Exclusividade_RegistradaAntesDaPresenca_ConsultadaPelaRegraDePresenca()
    {
        var exc = new ValidadorFalso("EXC-T-001", (ctx, regra) => { ctx.RegistrarExclusao(regra.Id, "CHAVE", "Cancelada"); return []; });
        var excluido = false;
        var pre = new ValidadorFalso("CRZ-TST-001", (ctx, _) => { excluido = ctx.EstaExcluido("CHAVE"); return []; });

        new MotorRegras([pre, exc]).Executar(
            new CatalogoRegras("t", [Cruzamento("CRZ-TST-001"), Cruzamento("EXC-T-001", TipoRelacao.Exclusividade)]),
            Contexto(Nfe, Efd));

        Assert.True(excluido);
    }

    [Fact]
    public void Dependencia_ComInconsistencia_TornaDependenteNaoConclusiva()
    {
        var par = Cruzamento("CRZ-TST-001", TipoRelacao.Parametro);
        var valor = Cruzamento("CRZ-TST-002", TipoRelacao.Identidade, dependeDe: ["CRZ-TST-001"]);
        var validadorValor = ValidadorFalso.SemAchados("CRZ-TST-002");

        var resultado = new MotorRegras([ValidadorFalso.ComInconsistencia("CRZ-TST-001"), validadorValor])
            .Executar(new CatalogoRegras("t", [valor, par]), Contexto(Nfe, Efd));

        Assert.Equal(SituacaoRegra.ComInconsistencia, Situacao(resultado, "CRZ-TST-001"));
        Assert.Equal(SituacaoRegra.NaoConclusiva, Situacao(resultado, "CRZ-TST-002"));
        Assert.Empty(validadorValor.Chamadas);
    }

    [Fact]
    public void Dependencia_NaoAprovada_TornaDependenteNaoConclusiva()
    {
        var proposta = Cruzamento("CRZ-TST-001", TipoRelacao.Parametro, StatusRegra.Proposta);
        var valor = Cruzamento("CRZ-TST-002", TipoRelacao.Identidade, dependeDe: ["CRZ-TST-001"]);

        var resultado = new MotorRegras([ValidadorFalso.SemAchados("CRZ-TST-002")])
            .Executar(new CatalogoRegras("t", [proposta, valor]), Contexto(Nfe, Efd));

        Assert.Equal(SituacaoRegra.NaoConclusiva, Situacao(resultado, "CRZ-TST-002"));
        Assert.DoesNotContain(resultado.Regras, r => r.RegraId == "CRZ-TST-001");
    }

    [Fact]
    public void ObrigacaoNaoRecebida_AusenciaDeEvidencias_NuncaInconsistencia()
    {
        var validador = ValidadorFalso.ComInconsistencia("CRZ-TST-001");

        var resultado = new MotorRegras([validador]).Executar(new CatalogoRegras("t", [Cruzamento("CRZ-TST-001")]), Contexto(Nfe));

        var achado = Assert.Single(resultado.Achados);
        Assert.Equal(Natureza.AusenciaEvidencias, achado.Natureza);
        Assert.Null(achado.Severidade);
        var faltante = Assert.Single(achado.ObrigacoesFaltantes);
        Assert.Equal(TipoObrigacao.EfdIcmsIpi, faltante.Tipo);
        Assert.Equal("Não recebida no lote.", faltante.Motivo);
        Assert.Empty(validador.Chamadas);
        Assert.True(resultado.Regras.Single().Obrigatoria);
    }

    [Fact]
    public void ObrigacaoRecebidaNaoApta_AusenciaDeEvidenciasComMotivoDistinto()
    {
        var resultado = new MotorRegras([ValidadorFalso.SemAchados("CRZ-TST-001")]).Executar(
            new CatalogoRegras("t", [Cruzamento("CRZ-TST-001")]),
            Contexto([TipoObrigacao.NFe, TipoObrigacao.EfdIcmsIpi], Nfe));

        Assert.StartsWith("Recebida, mas não apta", Assert.Single(resultado.Achados).ObrigacoesFaltantes.Single().Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void VersaoNaoCobertaPelaRegra_AusenciaDeEvidencias()
    {
        var regra = Cruzamento("CRZ-TST-001", versoes: new()
        {
            [TipoObrigacao.NFe] = [],
            [TipoObrigacao.EfdIcmsIpi] = ["020"],
        });

        var resultado = new MotorRegras([ValidadorFalso.SemAchados("CRZ-TST-001")]).Executar(new CatalogoRegras("t", [regra]), Contexto(Nfe, Efd));

        Assert.Equal(SituacaoRegra.AusenciaEvidencias, Situacao(resultado, "CRZ-TST-001"));
    }

    [Fact]
    public void NenhumaObrigacaoDaRegraNoLote_NaoAplicavelENaoObrigatoria()
    {
        var regra = Cruzamento("CRZ-TST-001", obrigacoes: [TipoObrigacao.Ecd, TipoObrigacao.Ecf]);

        var resultado = new MotorRegras([]).Executar(new CatalogoRegras("t", [regra]), Contexto(Nfe, Efd));

        var r = Assert.Single(resultado.Regras);
        Assert.Equal(SituacaoRegra.NaoAplicavel, r.Situacao);
        Assert.False(r.Obrigatoria);
        Assert.Empty(resultado.Achados);
    }

    [Fact]
    public void ForaDeVigencia_NaoAplicavel()
    {
        var regra = Cruzamento("CRZ-TST-001", vigenciaInicio: new DateOnly(2025, 1, 1), vigenciaFim: new DateOnly(2025, 12, 31));

        var resultado = new MotorRegras([ValidadorFalso.ComInconsistencia("CRZ-TST-001")]).Executar(new CatalogoRegras("t", [regra]), Contexto(Nfe, Efd));

        Assert.Equal(SituacaoRegra.NaoAplicavel, Situacao(resultado, "CRZ-TST-001"));
        Assert.Empty(resultado.Achados);
    }

    [Fact]
    public void RegraProposta_NaoExecutada()
    {
        var validador = ValidadorFalso.ComInconsistencia("CRZ-TST-001");

        var resultado = new MotorRegras([validador]).Executar(
            new CatalogoRegras("t", [Cruzamento("CRZ-TST-001", status: StatusRegra.Proposta)]), Contexto(Nfe, Efd));

        Assert.Empty(resultado.Regras);
        Assert.Empty(validador.Chamadas);
    }

    [Fact]
    public void RegraAprovadaSemValidador_FalhaTecnica()
    {
        var resultado = new MotorRegras([]).Executar(new CatalogoRegras("t", [Cruzamento("CRZ-TST-001")]), Contexto(Nfe, Efd));

        Assert.Equal(SituacaoRegra.FalhaTecnica, Situacao(resultado, "CRZ-TST-001"));
        Assert.Equal(Natureza.FalhaTecnica, Assert.Single(resultado.Achados).Natureza);
    }

    [Fact]
    public void ValidadorComExcecao_FalhaTecnicaSemResultadoPresumido()
    {
        var validador = new ValidadorFalso("CRZ-TST-001", (_, _) => throw new InvalidOperationException("falha"));

        var resultado = new MotorRegras([validador]).Executar(new CatalogoRegras("t", [Cruzamento("CRZ-TST-001")]), Contexto(Nfe, Efd));

        Assert.Equal(SituacaoRegra.FalhaTecnica, Situacao(resultado, "CRZ-TST-001"));
        Assert.DoesNotContain(resultado.Achados, a => a.Natureza == Natureza.InconsistenciaCruzamento);
    }

    [Fact]
    public void Inconsistencia_SeveridadeVemDoCatalogo()
    {
        var regra = Cruzamento("CRZ-TST-001");

        var padrao = regra.CriarInconsistencia("x", []);
        var temporal = regra.CriarInconsistencia("x", [], "temporal", 10m, 7m);

        Assert.Equal(Severidade.ErroGrave, padrao.Severidade);
        Assert.Equal(Severidade.Alerta, temporal.Severidade);
        Assert.Equal(3m, temporal.Diferenca);
        Assert.Equal(["Hipótese de teste."], temporal.HipotesesLegitimas);
        Assert.Contains("CRZ-TST-001", padrao.Descricao, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => regra.CriarInconsistencia("x", [], "inexistente"));
    }

    [Fact]
    public void Inconsistencia_NaoPropagaHipoteseGenericaDeLayout()
    {
        var regra = Cruzamento("CRZ-TST-001") with
        {
            HipotesesLegitimas =
            [
                "Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte",
                "Devolução com CFOP 5.202",
            ],
        };

        var achado = regra.CriarInconsistencia("NF-e X: VL_DOC escriturado R$ 900 x vNF R$ 1000.", []);

        Assert.Equal(["Devolução com CFOP 5.202"], achado.HipotesesLegitimas);
        Assert.Contains("VL_DOC", achado.Descricao, StringComparison.Ordinal);
        Assert.Contains("CRZ-TST-001", achado.Descricao, StringComparison.Ordinal);
    }

    [Fact]
    public void MesmaEntrada_MesmosAchados()
    {
        var regras = new[] { Cruzamento("CRZ-TST-002", TipoRelacao.Identidade), Cruzamento("CRZ-TST-001", TipoRelacao.Parametro) };
        var motor = new MotorRegras([ValidadorFalso.ComInconsistencia("CRZ-TST-001"), ValidadorFalso.ComInconsistencia("CRZ-TST-002")]);

        var primeiro = motor.Executar(new CatalogoRegras("t", regras), Contexto(Nfe, Efd));
        var segundo = motor.Executar(new CatalogoRegras("t", regras), Contexto(Nfe, Efd));

        Assert.Equal(primeiro.Achados.Select(a => (a.RegraId, a.Natureza, a.Severidade, a.Descricao)), segundo.Achados.Select(a => (a.RegraId, a.Natureza, a.Severidade, a.Descricao)));
        Assert.Equal(primeiro.Regras, segundo.Regras);
    }

    private static SituacaoRegra Situacao(ResultadoMotor resultado, string id) => resultado.Regras.Single(r => r.RegraId == id).Situacao;
}
