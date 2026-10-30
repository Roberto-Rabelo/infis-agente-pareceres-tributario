using Api.RAG.Application;
using Api.RAG.Domain;
using Api.RAG.Tests.Fixtures;

namespace Api.RAG.Tests.Application;

public class ServicoConsultaTests
{
    private static async Task<Ambiente> ComBase()
    {
        var ambiente = new Ambiente();
        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);
        return ambiente;
    }

    [Fact]
    public async Task PorRegra_RetornaPrimeiroOTrechoQueDefineARegra()
    {
        var ambiente = await ComBase();

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-002"]), CancellationToken.None);

        Assert.Equal(2, resultado.Trechos.Count);
        Assert.Equal("CRZ-NFE-EFD-002", resultado.Trechos[0].Trecho.RegraPrincipal);
        Assert.Equal("CRZ-NFE-EFD-001", resultado.Trechos[1].Trecho.RegraPrincipal);
        Assert.All(resultado.Trechos, t => Assert.Equal(MotivoRecuperacao.Regra, t.Motivo));
        Assert.Empty(resultado.Limitacoes);
        Assert.Null(resultado.Consumo);
    }

    [Fact]
    public async Task Trecho_TemFonteVersaoELocalizacaoRecuperaveis()
    {
        var ambiente = await ComBase();

        var trecho = (await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-ECD-ECF-001"]), CancellationToken.None)).Trechos[0];

        Assert.Equal("docs/03-regras-cruzamento.md", trecho.Documento.Chave);
        Assert.Equal("2026-10-29", trecho.Documento.Versao);
        Assert.Equal(TipoDocumento.BaseRegrasInterna, trecho.Documento.Tipo);
        Assert.Contains("CRZ-ECD-ECF-001", Documentos.Cruzamentos.Split('\n')[trecho.Trecho.LinhaInicio - 1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Semantica_OrdenaPorSimilaridadeERegistraConsumo()
    {
        var ambiente = await ComBase();
        var consumoAntes = ambiente.Memoria.Consumo.Count;

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("evento de cancelamento homologado COD_SIT", limite: 3), CancellationToken.None);

        Assert.Equal("CRZ-NFE-EFD-002", resultado.Trechos[0].Trecho.RegraPrincipal);
        Assert.All(resultado.Trechos, t => Assert.Equal(MotivoRecuperacao.Semantica, t.Motivo));
        Assert.True(resultado.Trechos.Zip(resultado.Trechos.Skip(1)).All(p => p.First.Similaridade >= p.Second.Similaridade));
        Assert.NotNull(resultado.Consumo);
        Assert.Equal("CONSULTA", ambiente.Memoria.Consumo.Last().Operacao);
        Assert.Equal(consumoAntes + 1, ambiente.Memoria.Consumo.Count);
    }

    [Fact]
    public async Task RegraESemantica_NaoRepetemTrecho()
    {
        var ambiente = await ComBase();

        var resultado = await ambiente.Consulta.ConsultarAsync(
            Ambiente.Pergunta("cancelamento homologado", regraIds: ["CRZ-NFE-EFD-002"], limite: 5), CancellationToken.None);

        Assert.Equal(resultado.Trechos.Count, resultado.Trechos.Select(t => t.TrechoId).Distinct().Count());
        Assert.Equal(MotivoRecuperacao.Regra, resultado.Trechos[0].Motivo);
        Assert.Contains(resultado.Trechos, t => t.Motivo == MotivoRecuperacao.Semantica);
    }

    [Fact]
    public async Task FiltroPorObrigacao_ExcluiTrechosDeOutrosPares()
    {
        var ambiente = await ComBase();

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("saldos", obrigacoes: ["ECF"], limite: 10), CancellationToken.None);

        Assert.DoesNotContain(resultado.Trechos, t => t.Trecho.RegraPrincipal?.StartsWith("CRZ-NFE", StringComparison.Ordinal) == true);
        Assert.Contains(resultado.Trechos, t => t.Trecho.RegraPrincipal == "CRZ-ECD-ECF-001");
    }

    [Fact]
    public async Task FiltroPorTipoDocumento_SeparaBaseInternaDeManualOficial()
    {
        var ambiente = await ComBase();

        var resultado = await ambiente.Consulta.ConsultarAsync(
            Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-001"], tipos: [TipoDocumento.ManualOficial]), CancellationToken.None);

        Assert.Empty(resultado.Trechos);
        Assert.Contains(resultado.Limitacoes, l => l.Contains("CRZ-NFE-EFD-001", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DataDeReferencia_SelecionaVersaoVigente()
    {
        var ambiente = new Ambiente();
        await ambiente.Ingestao.IngerirAsync(
            Ambiente.Comando(chave: "manual/guia-efd", tipo: TipoDocumento.ManualOficial, vigenciaInicio: new DateOnly(2026, 1, 1), vigenciaFim: new DateOnly(2026, 12, 31)),
            CancellationToken.None);

        var vigente = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-001"], dataReferencia: new DateOnly(2026, 3, 1)), CancellationToken.None);
        var foraDaVigencia = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-001"], dataReferencia: new DateOnly(2025, 3, 1)), CancellationToken.None);

        Assert.NotEmpty(vigente.Trechos);
        Assert.Empty(foraDaVigencia.Trechos);
    }

    [Fact]
    public async Task SemResultados_DeclaraLimitacao()
    {
        var ambiente = new Ambiente();

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("qualquer coisa"), CancellationToken.None);

        Assert.Empty(resultado.Trechos);
        Assert.Contains("Nenhum trecho encontrado para os filtros informados.", resultado.Limitacoes);
    }

    [Fact]
    public async Task LimiarConfigurado_DescartaTrechosPoucoSimilares()
    {
        var ambiente = new Ambiente(consulta: new OpcoesConsulta { LimiarSimilaridade = 0.99 });
        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("cancelamento"), CancellationToken.None);

        Assert.Empty(resultado.Trechos);
    }

    [Fact]
    public async Task FalhaNaSemantica_ComTrechosPorRegra_RetornaParcialComLimitacao()
    {
        var ambiente = await ComBase();
        ambiente.Gerador.Falhar = true;

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("cancelamento", regraIds: ["CRZ-NFE-EFD-002"]), CancellationToken.None);

        Assert.NotEmpty(resultado.Trechos);
        Assert.Contains(resultado.Limitacoes, l => l.StartsWith("Busca semântica indisponível", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FalhaNaSemantica_SemTrechosPorRegra_EhErroTecnico()
    {
        var ambiente = await ComBase();
        ambiente.Gerador.Falhar = true;

        await Assert.ThrowsAsync<ProvedorEmbeddingsException>(() => ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("cancelamento"), CancellationToken.None));
    }

    [Fact]
    public async Task BaseIndisponivel_PropagaErroTecnico()
    {
        var ambiente = await ComBase();
        ambiente.Memoria.Indisponivel = true;

        await Assert.ThrowsAsync<BaseIndisponivelException>(() =>
            ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-002"]), CancellationToken.None));
    }

    public static TheoryData<ConsultaConhecimento, string> ConsultasInvalidas => new()
    {
        { Ambiente.Pergunta(), "CONSULTA_VAZIA" },
        { Ambiente.Pergunta(new string('a', 2001)), "PERGUNTA_EXCEDE_LIMITE" },
        { Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-001' OR 1=1"]), "REGRA_ID_INVALIDO" },
        { Ambiente.Pergunta("x", obrigacoes: ["SPED"]), "OBRIGACAO_DESCONHECIDA" },
        { Ambiente.Pergunta("x", limite: 0), "LIMITE_INVALIDO" },
        { Ambiente.Pergunta("x", limite: 31), "LIMITE_INVALIDO" },
    };

    [Theory]
    [MemberData(nameof(ConsultasInvalidas))]
    public async Task EntradaInvalida_RejeitadaSemChamarProvedor(ConsultaConhecimento consulta, string codigo)
    {
        var ambiente = new Ambiente();

        var erro = await Assert.ThrowsAsync<ErroValidacaoException>(() => ambiente.Consulta.ConsultarAsync(consulta, CancellationToken.None));

        Assert.Equal(codigo, erro.Codigo);
        Assert.Equal(0, ambiente.Gerador.Chamadas);
    }
}
