using Api.RAG.Application;
using Api.RAG.Domain;
using Api.RAG.Tests.Fixtures;

namespace Api.RAG.Tests.Application;

public class ServicoIngestaoTests
{
    [Fact]
    public async Task Ingestao_GravaDocumentoComMetadadosETrechosVetorizados()
    {
        var ambiente = new Ambiente();

        var resultado = await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        Assert.Equal(SituacaoIngestao.Criado, resultado.Situacao);
        var documento = Assert.Single(ambiente.Memoria.Documentos);
        Assert.Equal(TipoDocumento.BaseRegrasInterna, documento.Tipo);
        Assert.Equal(SegmentadorMarkdown.Hash(Documentos.Cruzamentos), documento.HashConteudo);
        Assert.Equal(Ambiente.Modelo, documento.ModeloEmbedding);
        Assert.Equal(Ambiente.Dimensao, documento.DimensaoEmbedding);
        Assert.Equal(resultado.QuantidadeTrechos, ambiente.Memoria.QuantidadeTrechos);
    }

    [Fact]
    public async Task Reingestao_MesmoConteudoEVersao_EhIdempotenteESemNovaChamadaAoProvedor()
    {
        var ambiente = new Ambiente();
        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);
        var chamadas = ambiente.Gerador.Chamadas;
        var trechos = ambiente.Memoria.QuantidadeTrechos;

        var resultado = await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        Assert.Equal(SituacaoIngestao.JaExistente, resultado.Situacao);
        Assert.Equal(chamadas, ambiente.Gerador.Chamadas);
        Assert.Equal(trechos, ambiente.Memoria.QuantidadeTrechos);
    }

    [Fact]
    public async Task MesmaVersao_ComConteudoDiferente_EhConflito()
    {
        var ambiente = new Ambiente();
        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        await Assert.ThrowsAsync<ConflitoVersaoException>(() =>
            ambiente.Ingestao.IngerirAsync(Ambiente.Comando(conteudo: Documentos.Cruzamentos + "\nNova linha."), CancellationToken.None));
    }

    [Fact]
    public async Task NovaVersao_SubstituiAAnterior_QuePermaneceIdentificavel()
    {
        var ambiente = new Ambiente();
        var v1 = await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        var v2 = await ambiente.Ingestao.IngerirAsync(
            Ambiente.Comando(conteudo: Documentos.Cruzamentos.Replace("COD_SIT = 02", "COD_SIT = 02 ou 03", StringComparison.Ordinal), versao: "2026-11-01"),
            CancellationToken.None);

        Assert.Equal(v1.Documento.Id, v2.SubstituiuDocumentoId);
        Assert.Equal(StatusDocumento.Substituido, ambiente.Memoria.Documentos.Single(d => d.Id == v1.Documento.Id).Status);
        var consulta = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-002"]), CancellationToken.None);
        Assert.All(consulta.Trechos, t => Assert.Equal("2026-11-01", t.Documento.Versao));
        Assert.Contains("ou 03", consulta.Trechos[0].Trecho.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FalhaDoProvedor_NaoPersisteNadaERegistraConsumoComoFalha()
    {
        var ambiente = new Ambiente();
        ambiente.Gerador.Falhar = true;

        var erro = await Assert.ThrowsAsync<ProvedorEmbeddingsException>(() => ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None));

        Assert.Equal("PROVEDOR_EMBEDDINGS_INDISPONIVEL", erro.Codigo);
        Assert.Empty(ambiente.Memoria.Documentos);
        var registro = Assert.Single(ambiente.Memoria.Consumo);
        Assert.Equal("FALHA", registro.Status);
        Assert.Null(registro.TokensEntrada);
    }

    [Fact]
    public async Task DimensaoIncompativel_EhRejeitada()
    {
        var ambiente = new Ambiente();
        ambiente.Gerador.DimensaoRetornada = Ambiente.Dimensao + 1;

        var erro = await Assert.ThrowsAsync<ProvedorEmbeddingsException>(() => ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None));

        Assert.Equal("DIMENSAO_INCOMPATIVEL", erro.Codigo);
        Assert.Empty(ambiente.Memoria.Documentos);
    }

    [Fact]
    public async Task Consumo_UmRegistroPorLote_ComTokensInformados()
    {
        var ambiente = new Ambiente();

        var resultado = await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        var lotes = (int)Math.Ceiling(resultado.QuantidadeTrechos / 4.0);
        Assert.Equal(lotes, ambiente.Memoria.Consumo.Count);
        Assert.All(ambiente.Memoria.Consumo, r =>
        {
            Assert.Equal("SUCESSO", r.Status);
            Assert.Equal("INGESTAO", r.Operacao);
            Assert.Equal("corr-teste", r.CorrelacaoId);
            Assert.NotNull(r.TokensEntrada);
        });
    }

    [Fact]
    public async Task ConsumoDesconhecido_FicaNuloNaoZero()
    {
        var ambiente = new Ambiente();
        ambiente.Gerador.InformarUso = false;

        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(), CancellationToken.None);

        Assert.All(ambiente.Memoria.Consumo, r => Assert.Null(r.TokensEntrada));
    }

    [Fact]
    public async Task TrechoSemObrigacaoIdentificada_HerdaAsDoDocumento()
    {
        var ambiente = new Ambiente();

        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(obrigacoes: ["NFE", "EFD_ICMS_IPI"]), CancellationToken.None);

        var resultado = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta("Cada regra indica origem destino", obrigacoes: ["NFE"], limite: 10), CancellationToken.None);
        var geral = Assert.Single(resultado.Trechos, t => t.Trecho.RegraIds.Count == 0);
        Assert.Equal(["EFD_ICMS_IPI", "NFE"], geral.Trecho.Obrigacoes);
    }

    public static TheoryData<ComandoIngestao, string> ComandosInvalidos => new()
    {
        { Ambiente.Comando(chave: "Docs/Maiuscula.md"), "CHAVE_INVALIDA" },
        { Ambiente.Comando(versao: " "), "VERSAO_INVALIDA" },
        { Ambiente.Comando(origem: ""), "ORIGEM_INVALIDA" },
        { Ambiente.Comando(formato: "pdf"), "FORMATO_NAO_SUPORTADO" },
        { Ambiente.Comando(conteudo: "   "), "DOCUMENTO_SEM_CONTEUDO" },
        { Ambiente.Comando(obrigacoes: ["SPED_FISCAL"]), "OBRIGACAO_DESCONHECIDA" },
        { Ambiente.Comando(vigenciaInicio: new DateOnly(2026, 12, 1), vigenciaFim: new DateOnly(2026, 1, 1)), "VIGENCIA_INVALIDA" },
    };

    [Theory]
    [MemberData(nameof(ComandosInvalidos))]
    public async Task MetadadosObrigatorios_SaoValidados(ComandoIngestao comando, string codigo)
    {
        var ambiente = new Ambiente();

        var erro = await Assert.ThrowsAsync<ErroValidacaoException>(() => ambiente.Ingestao.IngerirAsync(comando, CancellationToken.None));

        Assert.Equal(codigo, erro.Codigo);
        Assert.Equal(0, ambiente.Gerador.Chamadas);
    }
}
