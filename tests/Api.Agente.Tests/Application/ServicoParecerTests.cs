using System.Net;
using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Api.Agente.Tests.Fixtures;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using static Api.Agente.Tests.Fixtures.Pareceres;

namespace Api.Agente.Tests.Application;

/// <summary>T069 e ferramentas controladas (P9, P11, P13, P17): o provedor é sempre um duplo roteirizado.</summary>
public class ServicoParecerTests
{
    private const string Execucao = "exec-1";
    private const string Correlacao = "corr-1";

    private static Task<ResultadoParecer> Gerar(ServicoParecer servico) =>
        servico.GerarAsync(Analise(), Execucao, Correlacao, CancellationToken.None);

    private static string ResultadoFerramenta(ChatFalso chat, int chamada) =>
        chat.Historicos[chamada].SelectMany(m => m.Items).OfType<FunctionResultContent>().Last().Result?.ToString() ?? string.Empty;

    [Fact]
    public async Task Desabilitado_RetornaSomenteDeterministicoComLimitacao()
    {
        var chat = new ChatFalso();

        var resultado = await Gerar(Servico(chat, configurar: o => o.Habilitado = false));

        Assert.Null(resultado.Parecer);
        Assert.Empty(resultado.Consumo);
        Assert.Empty(chat.Historicos);
        Assert.Contains(ServicoParecer.LimitacaoDesabilitado, resultado.Resultado.Limitacoes);
        Assert.Equal(StatusAnalise.Concluida, resultado.Resultado.Status);
    }

    [Fact]
    public async Task RespostaValida_AceitaComTrechosCitadosEConsumoRegistrado()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise));
        var registro = new RegistroConsumoFalso();

        var resultado = await Servico(chat, registro: registro).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.NotNull(resultado.Parecer);
        Assert.Equal(StatusAnalise.Concluida, resultado.Resultado.Status);
        Assert.Equal("trecho-CRZ-NFE-EFD-001", Assert.Single(resultado.TrechosCitados).TrechoId);
        var consumo = Assert.Single(resultado.Consumo);
        Assert.Equal(("PARECER", 1, "SUCESSO", "RESPOSTA_VALIDA"), (consumo.Operacao, consumo.Tentativa, consumo.Status, consumo.Resultado));
        Assert.Equal((Execucao, Correlacao, "modelo-teste"), (consumo.ExecucaoId, consumo.CorrelacaoId, consumo.Modelo));
        Assert.Equal((1000L, 500L), (consumo.TokensEntrada!.Value, consumo.TokensSaida!.Value));
        Assert.Null(consumo.CustoEstimado);
        Assert.Null(consumo.TabelaPrecos);
        Assert.Equal(resultado.Consumo, registro.Registros);
    }

    [Fact]
    public async Task DadosEnviados_DelimitadosEComCnpjMascarado()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise));

        await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        var historico = chat.Historicos[0];
        Assert.Equal(AuthorRole.System, historico[0].Role);
        Assert.Equal(Prompt.Texto, historico[0].Content);
        var dados = historico[1].Content!;
        Assert.StartsWith("<<<DADOS\n", dados, StringComparison.Ordinal);
        Assert.EndsWith("\nDADOS>>>", dados, StringComparison.Ordinal);
        Assert.DoesNotContain(analise.RaizCnpj + "0001", dados, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaidaEstruturadaEstrita_SolicitadaAoProvedor()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise));

        await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        var configuracao = Assert.IsType<OpenAIPromptExecutionSettings>(chat.Configuracoes[0]);
        Assert.Equal("modelo-teste", configuracao.ModelId);
        Assert.NotNull(configuracao.ResponseFormat);
        Assert.Null(configuracao.Temperature);
    }

    [Fact]
    public async Task CustoComTabelaDePrecos_Estimado()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise));
        var custos = new OpcoesCustos
        {
            Versao = "teste-1",
            Moeda = "USD",
            Modelos = { ["modelo-teste"] = new PrecoModelo { EntradaPorMilhao = 1m, SaidaPorMilhao = 2m } },
        };

        var resultado = await Servico(chat, custos: custos).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        var consumo = Assert.Single(resultado.Consumo);
        Assert.Equal(0.002m, consumo.CustoEstimado);
        Assert.Equal("teste-1 (USD)", consumo.TabelaPrecos);
    }

    [Fact]
    public async Task TokensDesconhecidos_FicamNulosESemCusto()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise).ToJsonString(), entrada: null, saida: null);
        var custos = new OpcoesCustos { Versao = "teste-1", Moeda = "USD", Modelos = { ["modelo-teste"] = new PrecoModelo { EntradaPorMilhao = 1m } } };

        var resultado = await Servico(chat, custos: custos).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        var consumo = Assert.Single(resultado.Consumo);
        Assert.Null(consumo.TokensEntrada);
        Assert.Null(consumo.TokensSaida);
        Assert.Null(consumo.CustoEstimado);
    }

    [Fact]
    public async Task ModeloForaDaTabela_SemCusto()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise));
        var custos = new OpcoesCustos { Versao = "teste-1", Moeda = "USD", Modelos = { ["outro-modelo"] = new PrecoModelo { EntradaPorMilhao = 1m } } };

        var resultado = await Servico(chat, custos: custos).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Null(Assert.Single(resultado.Consumo).CustoEstimado);
    }

    [Fact]
    public async Task SaidaInvalidaSeguidaDeValida_AceitaAposCorrecao()
    {
        var analise = Analise();
        var invalida = ParecerValido(analise);
        invalida["achados"]![0]!["severidade"] = "ALERTA";
        var chat = new ChatFalso().Responder(invalida).Responder(ParecerValido(analise));

        var resultado = await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.NotNull(resultado.Parecer);
        Assert.Equal(["RESPOSTA_INVALIDA", "RESPOSTA_VALIDA"], resultado.Consumo.Select(c => c.Resultado));
        Assert.Equal([1, 2], resultado.Consumo.Select(c => c.Tentativa));
        var correcao = chat.Historicos[1][^1];
        Assert.Equal(AuthorRole.User, correcao.Role);
        Assert.Contains("ACHADO_RECLASSIFICADO", correcao.Content, StringComparison.Ordinal);
    }

    /// <summary>US7 cenário 3: referência inválida em todas as tentativas resulta em FALHA_TECNICA sem aproveitar conteúdo.</summary>
    [Fact]
    public async Task ReferenciaInvalidaEmTodasAsTentativas_FalhaTecnicaSemConteudoDoModelo()
    {
        var analise = Analise();
        var invalida = ParecerValido(analise);
        invalida["sintese"] = "Texto que não pode ser aproveitado.";
        invalida["achados"]![0]!["referencias"] = new JsonArray("trecho-inventado");
        var chat = new ChatFalso().Responder(invalida).Responder(invalida).Responder(invalida);

        var resultado = await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Null(resultado.Parecer);
        Assert.Empty(resultado.TrechosCitados);
        Assert.Equal(StatusAnalise.FalhaTecnica, resultado.Resultado.Status);
        Assert.Equal(3, chat.Historicos.Count);
        Assert.All(resultado.Consumo, c => Assert.Equal("RESPOSTA_INVALIDA", c.Resultado));
        var limitacao = resultado.Resultado.Limitacoes[^1];
        Assert.Contains("REFERENCIA_INEXISTENTE", limitacao, StringComparison.Ordinal);
        Assert.DoesNotContain(resultado.Resultado.Limitacoes, l => l.Contains("Texto que não pode", StringComparison.Ordinal));
        Assert.True(resultado.Resultado.RevisaoHumana.Requerida);
        Assert.Contains("Há falha técnica.", resultado.Resultado.RevisaoHumana.Motivos);
        Assert.Same(analise.Achados[0], Assert.Single(resultado.Resultado.Achados));
    }

    [Fact]
    public async Task FalhaTransitoriaEsgotada_FalhaTecnicaComUmRegistroPorTentativa()
    {
        var chat = new ChatFalso().Falhar(new HttpOperationException(HttpStatusCode.ServiceUnavailable, null, "indisponível", null), vezes: 3);

        var resultado = await Gerar(Servico(chat));

        Assert.Null(resultado.Parecer);
        Assert.Equal(StatusAnalise.FalhaTecnica, resultado.Resultado.Status);
        Assert.Equal(3, resultado.Consumo.Count);
        Assert.All(resultado.Consumo, c => Assert.Equal(("FALHA", "HTTP_503"), (c.Status, c.CodigoErro)));
        Assert.All(resultado.Consumo, c => Assert.Null(c.TokensEntrada));
        Assert.Contains("provedor do modelo indisponível", resultado.Resultado.Limitacoes[^1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task FalhaTransitoriaRecuperada_Aceita()
    {
        var analise = Analise();
        var chat = new ChatFalso()
            .Falhar(new HttpOperationException(HttpStatusCode.TooManyRequests, null, "limite", null))
            .Responder(ParecerValido(analise));

        var resultado = await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.NotNull(resultado.Parecer);
        Assert.Equal(["FALHA", "SUCESSO"], resultado.Consumo.Select(c => c.Status));
        Assert.Equal("HTTP_429", resultado.Consumo[0].CodigoErro);
    }

    [Fact]
    public async Task FalhaNaoTransitoria_SemRetentativa()
    {
        var chat = new ChatFalso().Falhar(new HttpOperationException(HttpStatusCode.Unauthorized, null, "chave inválida", null), vezes: 3);

        var resultado = await Gerar(Servico(chat));

        Assert.Equal(StatusAnalise.FalhaTecnica, resultado.Resultado.Status);
        Assert.Equal("HTTP_401", Assert.Single(resultado.Consumo).CodigoErro);
        Assert.Single(chat.Historicos);
    }

    [Fact]
    public async Task Timeout_RegistradoComoFalhaTecnica()
    {
        var chat = new ChatFalso().Travar();

        var resultado = await Gerar(Servico(chat, configurar: o => { o.TimeoutSegundos = 1; o.RetentativasTransitorias = 0; }));

        Assert.Equal(StatusAnalise.FalhaTecnica, resultado.Resultado.Status);
        Assert.Equal("TIMEOUT", Assert.Single(resultado.Consumo).CodigoErro);
    }

    [Fact]
    public async Task CancelamentoDoChamador_Propagado()
    {
        using var cancelamento = new CancellationTokenSource();
        await cancelamento.CancelAsync();
        var chat = new ChatFalso().Travar();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Servico(chat).GerarAsync(Analise(), Execucao, Correlacao, cancelamento.Token));
    }

    [Fact]
    public async Task PersistenciaDoConsumoFalha_ResultadoPreservado()
    {
        var analise = Analise();
        var chat = new ChatFalso().Responder(ParecerValido(analise));

        var resultado = await Servico(chat, registro: new RegistroConsumoFalso { Falhar = true }).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.NotNull(resultado.Parecer);
        Assert.Single(resultado.Consumo);
    }

    [Fact]
    public async Task FerramentaAutorizada_TrechoRecuperadoPodeSerCitado()
    {
        var analise = Analise();
        var parecer = ParecerValido(analise);
        parecer["fontesCitadas"]!.AsArray().Add("trecho-CRZ-NFE-EFD-002");
        var baseFalsa = new BaseConhecimentoFalsa { Consumo = new ConsumoEmbeddings("OpenAI", "embeddings-teste", 12) };
        var chat = new ChatFalso().ChamarFerramenta(Consulta("c1", "CRZ-NFE-EFD-002")).Responder(parecer);

        var resultado = await Servico(chat, baseFalsa).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.NotNull(resultado.Parecer);
        var consulta = Assert.Single(baseFalsa.Consultas);
        Assert.Equal(["CRZ-NFE-EFD-002"], consulta.RegraIds);
        Assert.Equal(Correlacao, consulta.CorrelacaoId);
        Assert.Contains("trecho-CRZ-NFE-EFD-002", ResultadoFerramenta(chat, 1), StringComparison.Ordinal);
        Assert.Contains(resultado.TrechosCitados, t => t.TrechoId == "trecho-CRZ-NFE-EFD-002");
        Assert.Equal(["CHAMADA_FERRAMENTA", "RESPOSTA_VALIDA", null], resultado.Consumo.Select(c => c.Resultado));
        var embeddings = resultado.Consumo[^1];
        Assert.Equal(("EMBEDDING_CONSULTA_RAG", "embeddings-teste", 12L), (embeddings.Operacao, embeddings.Modelo, embeddings.TokensEntrada!.Value));
    }

    [Fact]
    public async Task FerramentaNaoAutorizada_NaoExecutada()
    {
        var analise = Analise();
        var baseFalsa = new BaseConhecimentoFalsa();
        var chat = new ChatFalso().ChamarFerramenta(Consulta("c1", "CRZ-NFE-EFD-002")).Responder(ParecerValido(analise));

        var resultado = await Servico(chat, baseFalsa, configurar: o => o.FerramentasAutorizadas = []).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Empty(baseFalsa.Consultas);
        Assert.Contains("FERRAMENTA_NAO_AUTORIZADA", ResultadoFerramenta(chat, 1), StringComparison.Ordinal);
        Assert.NotNull(resultado.Parecer);
    }

    [Fact]
    public async Task FerramentaInexistente_NaoExecutada()
    {
        var analise = Analise();
        var chat = new ChatFalso()
            .ChamarFerramenta(new FunctionCallContent("excluir", "arquivos", "c1", []))
            .Responder(ParecerValido(analise));

        await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Contains("FERRAMENTA_NAO_AUTORIZADA", ResultadoFerramenta(chat, 1), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LimiteDeChamadas_RecusaExcedenteEDesligaFerramentas()
    {
        var analise = Analise();
        var baseFalsa = new BaseConhecimentoFalsa();
        var chat = new ChatFalso()
            .ChamarFerramenta(Consulta("c1", "CRZ-NFE-EFD-002"), Consulta("c2", "CRZ-NFE-EFD-003"))
            .Responder(ParecerValido(analise));

        await Servico(chat, baseFalsa, configurar: o => o.MaximoChamadasFerramenta = 1).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Single(baseFalsa.Consultas);
        Assert.Contains("LIMITE_DE_CHAMADAS_ATINGIDO", ResultadoFerramenta(chat, 1), StringComparison.Ordinal);
        Assert.IsType<AutoFunctionChoiceBehavior>(chat.Configuracoes[0]!.FunctionChoiceBehavior);
        Assert.IsType<NoneFunctionChoiceBehavior>(chat.Configuracoes[1]!.FunctionChoiceBehavior);
    }

    [Theory]
    [InlineData("DROP TABLE rag_trechos")]
    [InlineData("crz-nfe-efd-001")]
    public async Task ArgumentoInvalido_RecusadoSemConsultarABase(string regraId)
    {
        var analise = Analise();
        var baseFalsa = new BaseConhecimentoFalsa();
        var chat = new ChatFalso().ChamarFerramenta(Consulta("c1", regraId)).Responder(ParecerValido(analise));

        await Servico(chat, baseFalsa).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Empty(baseFalsa.Consultas);
        Assert.Contains("\"erro\"", ResultadoFerramenta(chat, 1), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ArgumentosExcessivos_Recusados()
    {
        var analise = Analise();
        var baseFalsa = new BaseConhecimentoFalsa();
        var ids = Enumerable.Range(1, 11).Select(i => $"CRZ-NFE-EFD-{i:000}").ToArray();
        var chat = new ChatFalso().ChamarFerramenta(Consulta("c1", ids)).Responder(ParecerValido(analise));

        await Servico(chat, baseFalsa).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Empty(baseFalsa.Consultas);
    }

    [Fact]
    public async Task FalhaDaFerramenta_DevolvidaComoErroAoModelo()
    {
        var analise = Analise();
        var baseFalsa = new BaseConhecimentoFalsa { Falha = new HttpRequestException("rede") };
        var chat = new ChatFalso().ChamarFerramenta(Consulta("c1", "CRZ-NFE-EFD-002")).Responder(ParecerValido(analise));

        var resultado = await Servico(chat, baseFalsa).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        var retorno = ResultadoFerramenta(chat, 1);
        Assert.Contains("FALHA_DA_FERRAMENTA", retorno, StringComparison.Ordinal);
        Assert.DoesNotContain("rede", retorno, StringComparison.Ordinal);
        Assert.NotNull(resultado.Parecer);
    }

    [Fact]
    public async Task BaseIndisponivelNaFerramenta_InformadaAoModelo()
    {
        var analise = Analise();
        var chat = new ChatFalso().ChamarFerramenta(Consulta("c1", "CRZ-NFE-EFD-002")).Responder(ParecerValido(analise));

        await Servico(chat, new BaseConhecimentoFalsa { Disponivel = false }).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.Contains("BASE_INDISPONIVEL", ResultadoFerramenta(chat, 1), StringComparison.Ordinal);
    }

    [Fact]
    public async Task IndicioIaAceito_ExigeRevisaoHumana()
    {
        var analise = Analise(revisao: new RevisaoHumana(false, []));
        var parecer = ParecerValido(analise);
        parecer["indiciosIa"] = new JsonArray(Indicio("trecho-CRZ-NFE-EFD-001"));
        var chat = new ChatFalso().Responder(parecer);

        var resultado = await Servico(chat).GerarAsync(analise, Execucao, Correlacao, CancellationToken.None);

        Assert.NotNull(resultado.Parecer);
        Assert.True(resultado.Resultado.RevisaoHumana.Requerida);
        Assert.Contains("Há indício identificado pela IA.", resultado.Resultado.RevisaoHumana.Motivos);
        Assert.Same(analise.Achados[0], Assert.Single(resultado.Resultado.Achados));
    }
}
