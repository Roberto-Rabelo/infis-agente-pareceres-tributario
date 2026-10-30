using System.Net;
using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Domain;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;
using ChatResponseFormat = OpenAI.Chat.ChatResponseFormat;
using ChatTokenUsage = OpenAI.Chat.ChatTokenUsage;

namespace Api.Agente.Application.Parecer;

public sealed record PromptParecer(string Versao, string Texto)
{
    public static PromptParecer DoArquivo(string diretorioBase, string versao) =>
        new(versao, File.ReadAllText(Path.Combine(diretorioBase, "prompts", "parecer", $"v{versao}.md")));
}

/// <summary>Schema do contrato enviado ao provedor como saída estruturada estrita (sem <c>$schema</c>/<c>$id</c>).</summary>
public sealed record SchemaSaidaParecer(string ParaProvedor)
{
    public static string Caminho(string diretorioBase) => Path.Combine(diretorioBase, "contracts", "saida", "parecer.v1.schema.json");

    public static SchemaSaidaParecer DoArquivo(string diretorioBase)
    {
        var schema = JsonNode.Parse(File.ReadAllText(Caminho(diretorioBase)))!.AsObject();
        schema.Remove("$schema");
        schema.Remove("$id");
        return new SchemaSaidaParecer(schema.ToJsonString());
    }
}

public sealed record ResultadoParecer(
    ResultadoAnalise Resultado,
    ParecerLlm? Parecer,
    IReadOnlyList<ReferenciaConhecimento> TrechosCitados,
    IReadOnlyList<RegistroConsumoLlm> Consumo);

/// <summary>
/// Etapas 7 a 9 do fluxo (constitution 3.4): LLM com ferramenta controlada, saída estruturada e validação.
/// A chamada de ferramenta é manual (sem invocação automática) para que autorização, limite e registro
/// fiquem com o sistema, independentemente da escolha do modelo (P11).
/// </summary>
public sealed partial class ServicoParecer(
    IOptions<OpcoesParecer> opcoes,
    IOptions<OpcoesCustos> custos,
    IOptions<OpcoesBaseConhecimento> opcoesBase,
    PromptParecer prompt,
    ValidadorParecer validador,
    SchemaSaidaParecer schemaSaida,
    IBaseConhecimento baseConhecimento,
    IRegistroConsumoLlm registroConsumo,
    TimeProvider relogio,
    ILogger<ServicoParecer> logger,
    IChatCompletionService? chat = null)
{
    public const string Servico = "Api.Agente";
    public const string LimitacaoDesabilitado = "Parecer por LLM desabilitado; retornados apenas os resultados determinísticos.";
    private readonly OpcoesParecer _opcoes = opcoes.Value;

    public async Task<ResultadoParecer> GerarAsync(ResultadoAnalise resultado, string execucaoId, string correlacaoId, CancellationToken cancelamento)
    {
        if (!_opcoes.Habilitado || chat is null)
        {
            return new ResultadoParecer(resultado with { Limitacoes = [.. resultado.Limitacoes, LimitacaoDesabilitado] }, null, [], []);
        }

        var dados = MontadorDadosParecer.Montar(resultado);
        var contexto = new ContextoExecucaoParecer(execucaoId, correlacaoId, resultado.Periodo.Inicio);
        contexto.Registrar(resultado.Fundamentacao.SelectMany(f => f.Referencias));
        var kernel = new Kernel();
        kernel.ImportPluginFromObject(
            new PluginBaseConhecimento(baseConhecimento, contexto, opcoesBase.Value.LimitePorConsulta, relogio), PluginBaseConhecimento.Nome);

        var historico = new ChatHistory();
        historico.AddSystemMessage(prompt.Texto);
        historico.AddUserMessage($"<<<DADOS\n{dados.Json}\nDADOS>>>");

        var consumo = new List<RegistroConsumoLlm>();
        var execucao = new Execucao(contexto, consumo);
        IReadOnlyList<ErroValidacaoParecer> ultimosErros = [];
        string motivoFalha = "resposta inválida";
        for (var tentativa = 0; tentativa <= _opcoes.RetentativasSaidaInvalida; tentativa++)
        {
            var resposta = await ObterRespostaFinalAsync(kernel, historico, execucao, cancelamento).ConfigureAwait(false);
            if (resposta is null)
            {
                motivoFalha = "provedor do modelo indisponível";
                break;
            }

            var validacao = validador.Validar(resposta, resultado, dados, contexto.Trechos.Keys.ToHashSet(StringComparer.Ordinal));
            MarcarUltimo(consumo, validacao.Valido ? "RESPOSTA_VALIDA" : "RESPOSTA_INVALIDA");
            if (validacao.Valido)
            {
                RegistrarEmbeddings(contexto, consumo);
                await PersistirAsync(consumo).ConfigureAwait(false);
                return Aceitar(resultado, validacao.Parecer!, contexto, consumo);
            }

            ultimosErros = validacao.Erros;
            var codigos = string.Join(", ", ultimosErros.Select(e => e.Codigo).Distinct());
            LogRespostaInvalida(logger, tentativa + 1, codigos);
            historico.AddAssistantMessage(resposta);
            historico.AddUserMessage(
                "A resposta foi rejeitada pelo validador do sistema. Corrija e reenvie o JSON completo:\n" +
                string.Join('\n', ultimosErros.Select(e => $"- {e.Codigo}: {e.Mensagem}")));
        }

        RegistrarEmbeddings(contexto, consumo);
        await PersistirAsync(consumo).ConfigureAwait(false);
        var detalhe = ultimosErros.Count > 0 ? $" ({string.Join(", ", ultimosErros.Select(e => e.Codigo).Distinct())})" : string.Empty;
        return Falhar(resultado, $"Parecer não gerado: {motivoFalha}{detalhe}; nenhum conteúdo do modelo foi aproveitado.", consumo);
    }

    private sealed record Execucao(ContextoExecucaoParecer Contexto, List<RegistroConsumoLlm> Consumo)
    {
        public int ChamadasFerramenta { get; set; }
    }

    /// <summary>Executa o ciclo modelo → ferramenta → modelo até obter texto; nulo se o provedor falhar.</summary>
    private async Task<string?> ObterRespostaFinalAsync(Kernel kernel, ChatHistory historico, Execucao execucao, CancellationToken cancelamento)
    {
        while (true)
        {
            var esgotado = execucao.ChamadasFerramenta >= _opcoes.MaximoChamadasFerramenta;
            var mensagem = await ChamarModeloAsync(kernel, historico, esgotado, execucao, cancelamento).ConfigureAwait(false);
            if (mensagem is null)
            {
                return null;
            }

            var chamadas = FunctionCallContent.GetFunctionCalls(mensagem).ToList();
            if (chamadas.Count == 0)
            {
                return mensagem.Content;
            }

            MarcarUltimo(execucao.Consumo, "CHAMADA_FERRAMENTA");
            historico.Add(mensagem);
            foreach (var chamada in chamadas)
            {
                var resultado = await ExecutarFerramentaAsync(kernel, chamada, execucao, cancelamento).ConfigureAwait(false);
                historico.Add(new FunctionResultContent(chamada, resultado).ToChatMessage());
            }
        }
    }

    private async Task<object> ExecutarFerramentaAsync(Kernel kernel, FunctionCallContent chamada, Execucao execucao, CancellationToken cancelamento)
    {
        var nome = $"{chamada.PluginName}-{chamada.FunctionName}";
        if (!_opcoes.FerramentasAutorizadas.Contains(nome, StringComparer.Ordinal))
        {
            LogFerramenta(logger, nome, "NAO_AUTORIZADA");
            return """{"erro":"FERRAMENTA_NAO_AUTORIZADA"}""";
        }

        if (execucao.ChamadasFerramenta >= _opcoes.MaximoChamadasFerramenta)
        {
            LogFerramenta(logger, nome, "LIMITE_ATINGIDO");
            return """{"erro":"LIMITE_DE_CHAMADAS_ATINGIDO"}""";
        }

        execucao.ChamadasFerramenta++;
        try
        {
            var resultado = await chamada.InvokeAsync(kernel, cancelamento).ConfigureAwait(false);
            LogFerramenta(logger, nome, "EXECUTADA");
            return resultado.Result ?? """{"erro":"SEM_RESULTADO"}""";
        }
#pragma warning disable CA1031 // A falha da ferramenta é devolvida ao modelo como erro; nunca como sucesso.
        catch (Exception erro) when (erro is not OperationCanceledException || !cancelamento.IsCancellationRequested)
#pragma warning restore CA1031
        {
            var situacao = "FALHA_" + erro.GetType().Name;
            LogFerramenta(logger, nome, situacao);
            return """{"erro":"FALHA_DA_FERRAMENTA"}""";
        }
    }

    /// <summary>Uma chamada ao provedor com timeout e retentativas para falhas transitórias; cada tentativa gera registro.</summary>
    private async Task<ChatMessageContent?> ChamarModeloAsync(Kernel kernel, ChatHistory historico, bool semFerramentas, Execucao execucao, CancellationToken cancelamento)
    {
        for (var retentativa = 0; ; retentativa++)
        {
            var inicio = relogio.GetUtcNow();
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
            limite.CancelAfter(TimeSpan.FromSeconds(_opcoes.TimeoutSegundos));
            string? codigoErro;
            try
            {
                var mensagem = await chat!.GetChatMessageContentAsync(historico, Configuracao(semFerramentas), kernel, limite.Token).ConfigureAwait(false);
                var (entrada, saida) = Tokens(mensagem);
                execucao.Consumo.Add(Registro(execucao.Contexto, execucao.Consumo.Count + 1, "PARECER", _opcoes.Modelo, inicio, "SUCESSO", null, entrada, saida, null));
                return mensagem;
            }
            catch (OperationCanceledException) when (!cancelamento.IsCancellationRequested)
            {
                codigoErro = "TIMEOUT";
            }
            catch (HttpOperationException erro)
            {
                codigoErro = erro.StatusCode is { } status ? "HTTP_" + (int)status : "HTTP";
                if (!Transitorio(erro.StatusCode))
                {
                    return FalhaDefinitiva(execucao, inicio, codigoErro);
                }
            }
#pragma warning disable CA1031 // Falha não prevista do conector: registrada e tratada como definitiva.
            catch (Exception erro) when (erro is not OperationCanceledException)
#pragma warning restore CA1031
            {
                return FalhaDefinitiva(execucao, inicio, "FALHA_" + erro.GetType().Name);
            }

            execucao.Consumo.Add(Registro(execucao.Contexto, execucao.Consumo.Count + 1, "PARECER", _opcoes.Modelo, inicio, "FALHA", null, null, null, codigoErro));
            LogFalhaModelo(logger, codigoErro);
            if (retentativa >= _opcoes.RetentativasTransitorias)
            {
                return null;
            }

            var espera = TimeSpan.FromMilliseconds(_opcoes.EsperaRetentativaMs * Math.Pow(2, retentativa));
            await Task.Delay(espera, relogio, cancelamento).ConfigureAwait(false);
        }
    }

    private ChatMessageContent? FalhaDefinitiva(Execucao execucao, DateTimeOffset inicio, string codigoErro)
    {
        execucao.Consumo.Add(Registro(execucao.Contexto, execucao.Consumo.Count + 1, "PARECER", _opcoes.Modelo, inicio, "FALHA", null, null, null, codigoErro));
        LogFalhaModelo(logger, codigoErro);
        return null;
    }

    private OpenAIPromptExecutionSettings Configuracao(bool semFerramentas) => new()
    {
        ModelId = _opcoes.Modelo,
        Temperature = _opcoes.Temperatura,
        MaxTokens = _opcoes.MaximoTokensSaida,
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("parecer_v1", BinaryData.FromString(schemaSaida.ParaProvedor), jsonSchemaIsStrict: true),
        FunctionChoiceBehavior = semFerramentas ? FunctionChoiceBehavior.None() : FunctionChoiceBehavior.Auto(autoInvoke: false),
    };

    private static bool Transitorio(HttpStatusCode? status) =>
        status is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static (long? Entrada, long? Saida) Tokens(ChatMessageContent mensagem) =>
        mensagem.Metadata?.TryGetValue("Usage", out var uso) == true && uso is ChatTokenUsage u
            ? (u.InputTokenCount, u.OutputTokenCount)
            : (null, null);

    private static ResultadoParecer Aceitar(ResultadoAnalise resultado, ParecerLlm parecer, ContextoExecucaoParecer contexto, List<RegistroConsumoLlm> consumo)
    {
        var motivos = resultado.RevisaoHumana.Motivos.ToList();
        if (parecer.IndiciosIa.Count > 0)
        {
            motivos.Add("Há indício identificado pela IA.");
        }

        var citados = parecer.Achados.SelectMany(a => a.Referencias)
            .Concat(parecer.IndiciosIa.SelectMany(i => i.Referencias))
            .Concat(parecer.FontesCitadas)
            .Distinct(StringComparer.Ordinal)
            .Select(id => contexto.Trechos[id])
            .ToList();
        var final = resultado with { RevisaoHumana = new RevisaoHumana(motivos.Count > 0, motivos) };
        return new ResultadoParecer(final, parecer, citados, consumo);
    }

    /// <summary>US7 cenário 3: esgotadas as tentativas, falha técnica sem aproveitar conteúdo inválido (P9).</summary>
    private static ResultadoParecer Falhar(ResultadoAnalise resultado, string limitacao, List<RegistroConsumoLlm> consumo)
    {
        var motivos = resultado.RevisaoHumana.Motivos.Append("Há falha técnica.").Distinct(StringComparer.Ordinal).ToList();
        var final = resultado with
        {
            Status = StatusAnalise.FalhaTecnica,
            Limitacoes = [.. resultado.Limitacoes, limitacao],
            RevisaoHumana = new RevisaoHumana(true, motivos),
        };
        return new ResultadoParecer(final, null, [], consumo);
    }

    private static void MarcarUltimo(List<RegistroConsumoLlm> consumo, string resultado)
    {
        if (consumo.Count > 0 && consumo[^1].Status == "SUCESSO")
        {
            consumo[^1] = consumo[^1] with { Resultado = resultado };
        }
    }

    private void RegistrarEmbeddings(ContextoExecucaoParecer contexto, List<RegistroConsumoLlm> consumo)
    {
        foreach (var (uso, inicio, fim) in contexto.ConsumoEmbeddings)
        {
            consumo.Add(Registro(contexto, consumo.Count + 1, "EMBEDDING_CONSULTA_RAG", uso.Modelo, inicio, "SUCESSO", null, uso.TokensEntrada, null, null, uso.Provedor, fim));
        }

        contexto.ConsumoEmbeddings.Clear();
    }

    /// <summary>Persistência best effort: a falha do banco é registrada em log e não descarta o resultado da análise.</summary>
    private async Task PersistirAsync(List<RegistroConsumoLlm> consumo)
    {
        foreach (var registro in consumo)
        {
            try
            {
                await registroConsumo.RegistrarAsync(registro, CancellationToken.None).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Estratégia documentada no ADR D17: não perder o resultado operacional.
            catch (Exception erro)
#pragma warning restore CA1031
            {
                LogFalhaConsumo(logger, erro.GetType().Name);
            }
        }
    }

    private RegistroConsumoLlm Registro(
        ContextoExecucaoParecer contexto, int tentativa, string operacao, string modelo, DateTimeOffset inicio, string status,
        string? resultado, long? entrada, long? saida, string? codigoErro, string? provedor = null, DateTimeOffset? fim = null)
    {
        var termino = fim ?? relogio.GetUtcNow();
        var (custo, tabela) = CalculadoraCusto.Estimar(custos.Value, modelo, entrada, operacao == "PARECER" ? saida : 0);
        return new RegistroConsumoLlm(
            Guid.CreateVersion7(), contexto.ExecucaoId, contexto.CorrelacaoId, Servico, provedor ?? _opcoes.Provedor, modelo, operacao, tentativa,
            inicio, termino, (long)(termino - inicio).TotalMilliseconds, status, resultado, entrada, saida, codigoErro, custo, tabela);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resposta do modelo rejeitada na tentativa {Tentativa}: {Codigos}")]
    private static partial void LogRespostaInvalida(ILogger logger, int tentativa, string codigos);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ferramenta {Ferramenta}: {Situacao}")]
    private static partial void LogFerramenta(ILogger logger, string ferramenta, string situacao);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha na chamada ao modelo: {Codigo}")]
    private static partial void LogFalhaModelo(ILogger logger, string codigo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao persistir consumo do LLM: {Tipo}")]
    private static partial void LogFalhaConsumo(ILogger logger, string tipo);
}
