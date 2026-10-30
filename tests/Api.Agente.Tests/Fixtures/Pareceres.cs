using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Api.Agente.Tests.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;
using static Api.Agente.Tests.Fixtures.Lotes;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Api.Agente.Tests.Fixtures;

/// <summary>Provedor de chat roteirizado: nenhum teste de CI chama um LLM real (P17).</summary>
public sealed class ChatFalso : IChatCompletionService
{
    private readonly Queue<Func<CancellationToken, Task<ChatMessageContent>>> _respostas = new();

    public List<IReadOnlyList<ChatMessageContent>> Historicos { get; } = [];

    public List<PromptExecutionSettings?> Configuracoes { get; } = [];

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public ChatFalso Responder(string json, int? entrada = 1000, int? saida = 500)
    {
        var metadados = entrada is null || saida is null
            ? null
            : new Dictionary<string, object?> { ["Usage"] = OpenAIChatModelFactory.ChatTokenUsage(outputTokenCount: saida.Value, inputTokenCount: entrada.Value) };
        _respostas.Enqueue(_ => Task.FromResult(new ChatMessageContent(AuthorRole.Assistant, json, metadata: metadados)));
        return this;
    }

    public ChatFalso Responder(JsonNode json) => Responder(json.ToJsonString());

    public ChatFalso ChamarFerramenta(params FunctionCallContent[] chamadas)
    {
        _respostas.Enqueue(_ =>
        {
            var mensagem = new ChatMessageContent(AuthorRole.Assistant, content: null);
            foreach (var chamada in chamadas)
            {
                mensagem.Items.Add(chamada);
            }

            return Task.FromResult(mensagem);
        });
        return this;
    }

    public ChatFalso Falhar(Exception erro, int vezes = 1)
    {
        for (var i = 0; i < vezes; i++)
        {
            _respostas.Enqueue(_ => Task.FromException<ChatMessageContent>(erro));
        }

        return this;
    }

    public ChatFalso Travar()
    {
        _respostas.Enqueue(async cancelamento =>
        {
            await Task.Delay(Timeout.Infinite, cancelamento);
            throw new InvalidOperationException("Inalcançável.");
        });
        return this;
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        Historicos.Add([.. chatHistory]);
        Configuracoes.Add(executionSettings);
        if (!_respostas.TryDequeue(out var resposta))
        {
            throw new InvalidOperationException("Sem resposta roteirizada.");
        }

        return [await resposta(cancellationToken)];
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

public sealed class RegistroConsumoFalso : IRegistroConsumoLlm
{
    public List<RegistroConsumoLlm> Registros { get; } = [];

    public bool Falhar { get; set; }

    public Task RegistrarAsync(RegistroConsumoLlm registro, CancellationToken cancelamento)
    {
        if (Falhar)
        {
            throw new InvalidOperationException("Banco indisponível.");
        }

        Registros.Add(registro);
        return Task.CompletedTask;
    }
}

public static class Pareceres
{
    public const string Hipotese = "Hipótese legítima de teste.";

    public static readonly ValidadorParecer Validador = ValidadorParecer.DoArquivo(SchemaSaidaParecer.Caminho(AppContext.BaseDirectory));

    public static readonly PromptParecer Prompt = PromptParecer.DoArquivo(AppContext.BaseDirectory, new OpcoesParecer().VersaoPrompt);

    public static readonly SchemaSaidaParecer Schema = SchemaSaidaParecer.DoArquivo(AppContext.BaseDirectory);

    public static Achado Cruzamento(string regraId = "CRZ-NFE-EFD-001", Severidade? severidade = Severidade.ErroGrave) => new()
    {
        RegraId = regraId,
        Natureza = Natureza.InconsistenciaCruzamento,
        Severidade = severidade,
        Descricao = "NF-e autorizada ausente no C100.",
        HipotesesLegitimas = [Hipotese],
    };

    /// <summary>Análise com um achado de cruzamento já fundamentado pelo trecho <c>trecho-CRZ-NFE-EFD-001</c>.</summary>
    public static ResultadoAnalise Analise(StatusAnalise status = StatusAnalise.Concluida, RevisaoHumana? revisao = null, params Achado[] achados)
    {
        var lista = achados.Length > 0 ? achados : [Cruzamento()];
        return Analisar(Lote(EfdIcmsIpi(), NFe())) with
        {
            Status = status,
            Achados = lista,
            RevisaoHumana = revisao ?? new RevisaoHumana(true, ["Há achado de cruzamento."]),
            Fundamentacao = [.. lista.Select(a => a.RegraId).Distinct().Select(id => new FundamentacaoAchado(id, [BaseConhecimentoFalsa.Referencia(id)]))],
        };
    }

    /// <summary>Resposta que atende ao contrato e às regras semânticas para a análise informada.</summary>
    public static JsonObject ParecerValido(ResultadoAnalise analise)
    {
        var dados = MontadorDadosParecer.Montar(analise);
        var trechos = analise.Fundamentacao.SelectMany(f => f.Referencias).Select(r => r.TrechoId).Distinct().ToList();
        return new JsonObject
        {
            ["status"] = MontadorDadosParecer.Codigo(analise.Status),
            ["identificacao"] = "Contribuinte 11.222.333/****-**, período 2026-03.",
            ["escopoELimitacoes"] = "Sem limitações adicionais.",
            ["sintese"] = "Há inconsistência de cruzamento a revisar.",
            ["achados"] = new JsonArray([.. dados.Achados.Select(par => (JsonNode)new JsonObject
            {
                ["achadoId"] = par.Key,
                ["regraId"] = par.Value.RegraId,
                ["natureza"] = MontadorDadosParecer.Codigo(par.Value.Natureza),
                ["severidade"] = par.Value.Severidade is { } s ? MontadorDadosParecer.Codigo(s) : null,
                ["hipotesesLegitimas"] = new JsonArray([.. par.Value.HipotesesLegitimas.Select(h => (JsonNode)h)]),
                ["analise"] = "Análise do achado.",
                ["referencias"] = new JsonArray([.. analise.Fundamentacao.Where(f => f.RegraId == par.Value.RegraId)
                    .SelectMany(f => f.Referencias).Select(r => (JsonNode)r.TrechoId)]),
            })]),
            ["indiciosIa"] = new JsonArray(),
            ["fontesCitadas"] = new JsonArray([.. trechos.Select(t => (JsonNode)t)]),
            ["limitacoes"] = new JsonArray(),
            ["revisaoHumana"] = new JsonObject { ["requerida"] = true, ["motivos"] = new JsonArray("Há achado de cruzamento.") },
        };
    }

    public static JsonObject Indicio(params string[] referencias) => new()
    {
        ["descricao"] = "Possível divergência adicional.",
        ["severidade"] = "ALERTA",
        ["obrigacoes"] = new JsonArray("NFE", "EFD_ICMS_IPI"),
        ["referencias"] = new JsonArray([.. referencias.Select(r => (JsonNode)r)]),
    };

    public static ServicoParecer Servico(
        ChatFalso? chat,
        BaseConhecimentoFalsa? baseConhecimento = null,
        RegistroConsumoFalso? registro = null,
        Action<OpcoesParecer>? configurar = null,
        OpcoesCustos? custos = null)
    {
        var opcoes = new OpcoesParecer { Habilitado = true, Modelo = "modelo-teste", EsperaRetentativaMs = 0, TimeoutSegundos = 5 };
        configurar?.Invoke(opcoes);
        return new ServicoParecer(
            Options.Create(opcoes),
            Options.Create(custos ?? new OpcoesCustos()),
            Options.Create(new OpcoesBaseConhecimento()),
            Prompt,
            Validador,
            Schema,
            baseConhecimento ?? new BaseConhecimentoFalsa(),
            registro ?? new RegistroConsumoFalso(),
            TimeProvider.System,
            NullLogger<ServicoParecer>.Instance,
            chat);
    }

    public static FunctionCallContent Consulta(string id, params string[] regraIds) =>
        new(PluginBaseConhecimento.Funcao, PluginBaseConhecimento.Nome, id, new KernelArguments { ["regraIds"] = regraIds });
}
