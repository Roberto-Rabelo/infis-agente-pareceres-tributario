using System.Text.Json;
using Api.Agente.Application.Conhecimento;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Api.Agente.Infrastructure.Conhecimento;

/// <summary>
/// Cliente MCP da Api.RAG (P12). Uma sessão por consulta (servidor sem estado), timeout explícito,
/// correlação propagada e falha convertida em limitação, sem expor detalhes internos.
/// </summary>
public sealed partial class ClienteMcpBaseConhecimento(
    IHttpClientFactory fabricaHttp,
    IOptions<OpcoesBaseConhecimento> opcoes,
    ILoggerFactory loggers,
    ILogger<ClienteMcpBaseConhecimento> logger) : IBaseConhecimento
{
    public const string NomeHttp = "BaseConhecimento";
    public const string Ferramenta = "consultar_base_conhecimento";
    public const string MotivoIndisponivel = "Base de conhecimento indisponível; fundamentação não realizada.";
    public const string MotivoNaoConfigurada = "Base de conhecimento não configurada; fundamentação não realizada.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };
    private readonly OpcoesBaseConhecimento _opcoes = opcoes.Value;

    public async Task<ResultadoBaseConhecimento> ConsultarAsync(ConsultaFundamentacao consulta, CancellationToken cancelamento)
    {
        if (!Uri.TryCreate(_opcoes.Endpoint, UriKind.Absolute, out var endpoint) || string.IsNullOrWhiteSpace(_opcoes.TokenAcesso))
        {
            return ResultadoBaseConhecimento.Indisponivel(MotivoNaoConfigurada);
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
        limite.CancelAfter(TimeSpan.FromSeconds(_opcoes.TimeoutSegundos));
        try
        {
            var transporte = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = endpoint,
                    TransportMode = HttpTransportMode.StreamableHttp,
                    AdditionalHeaders = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer " + _opcoes.TokenAcesso,
                        ["X-Correlation-Id"] = consulta.CorrelacaoId,
                    },
                },
                fabricaHttp.CreateClient(NomeHttp),
                loggers,
                ownsHttpClient: true);
            await using var cliente = await McpClient.CreateAsync(transporte, null, loggers, limite.Token).ConfigureAwait(false);
            var argumentos = new Dictionary<string, object?>
            {
                ["regraIds"] = consulta.RegraIds,
                ["dataReferencia"] = consulta.DataReferencia.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["limite"] = consulta.Limite,
            };
            if (!string.IsNullOrWhiteSpace(consulta.Pergunta))
            {
                argumentos["pergunta"] = consulta.Pergunta;
            }

            if (consulta.Obrigacoes.Count > 0)
            {
                argumentos["obrigacoes"] = consulta.Obrigacoes;
            }
            var resultado = await cliente.CallToolAsync(Ferramenta, argumentos, null, null, limite.Token).ConfigureAwait(false);
            return resultado.IsError == true || resultado.StructuredContent is not { } conteudo
                ? ResultadoBaseConhecimento.Indisponivel(MotivoIndisponivel)
                : Interpretar(JsonSerializer.SerializeToElement(conteudo));
        }
        catch (OperationCanceledException) when (!cancelamento.IsCancellationRequested)
        {
            LogFalha(logger, "TIMEOUT");
            return ResultadoBaseConhecimento.Indisponivel(MotivoIndisponivel);
        }
#pragma warning disable CA1031 // Falha de transporte, protocolo ou contrato vira limitação registrada (P4).
        catch (Exception erro) when (erro is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogFalha(logger, erro.GetType().Name);
            return ResultadoBaseConhecimento.Indisponivel(MotivoIndisponivel);
        }
    }

    /// <summary>Valida a resposta da ferramenta contra o contrato 1.x; qualquer desvio torna a base indisponível.</summary>
    internal static ResultadoBaseConhecimento Interpretar(JsonElement conteudo)
    {
        RespostaDto? resposta;
        try
        {
            resposta = conteudo.Deserialize<RespostaDto>(Json);
        }
        catch (JsonException)
        {
            return ResultadoBaseConhecimento.Indisponivel(MotivoIndisponivel);
        }

        if (resposta is null || !resposta.Versao.StartsWith("1.", StringComparison.Ordinal))
        {
            return ResultadoBaseConhecimento.Indisponivel(MotivoIndisponivel);
        }

        if (resposta.Erro is { } erro)
        {
            return ResultadoBaseConhecimento.Indisponivel($"A base de conhecimento recusou a consulta ({erro.Codigo}); fundamentação não realizada.");
        }

        var referencias = (resposta.Trechos ?? []).Select(t => new ReferenciaConhecimento(
            t.TrechoId,
            t.Fonte.Chave,
            t.Fonte.Titulo,
            t.Fonte.Tipo,
            t.Fonte.Versao,
            t.Fonte.Origem,
            t.Fonte.Hash,
            t.Secao,
            t.LinhaInicio,
            t.LinhaFim,
            t.Texto,
            t.RegraIds ?? [],
            t.RegraPrincipal,
            t.Motivo,
            t.Similaridade)).ToList();
        return new ResultadoBaseConhecimento(true, referencias, resposta.Limitacoes ?? [])
        {
            Consumo = resposta.Consumo is { } c ? new ConsumoEmbeddings(c.Provedor, c.Modelo, c.TokensEntrada) : null,
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Consulta MCP à base de conhecimento falhou: {Tipo}")]
    private static partial void LogFalha(ILogger logger, string tipo);

    // Campos nulos são omitidos pelo servidor MCP; por isso os opcionais têm valor padrão.
    private sealed record RespostaDto(
        string Versao,
        IReadOnlyList<TrechoDto>? Trechos = null,
        IReadOnlyList<string>? Limitacoes = null,
        ConsumoDto? Consumo = null,
        ErroDto? Erro = null);

    private sealed record ErroDto(string Codigo, string Mensagem);

    private sealed record ConsumoDto(string Provedor, string Modelo, int QuantidadeTextos, long? TokensEntrada = null);

    private sealed record TrechoDto(
        string TrechoId,
        FonteDto Fonte,
        string Secao,
        int LinhaInicio,
        int LinhaFim,
        string Texto,
        string Motivo,
        IReadOnlyList<string>? RegraIds = null,
        string? RegraPrincipal = null,
        double? Similaridade = null);

    private sealed record FonteDto(string Chave, string Titulo, string Tipo, string Versao, string Origem, string Hash);
}
