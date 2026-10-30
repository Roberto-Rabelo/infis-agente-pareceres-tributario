using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Api.RAG.Application;

public sealed record ResultadoEmbeddings(IReadOnlyList<float[]> Vetores, long? TokensEntrada);

/// <summary>
/// Gera embeddings em lotes com timeout explícito, valida a dimensão e registra o consumo de cada chamada,
/// inclusive as que falham (P13). Não há retentativa automática neste incremento.
/// </summary>
public sealed partial class ProvedorEmbeddings(
    IEmbeddingGenerator<string, Embedding<float>> gerador,
    IOptions<OpcoesEmbeddings> opcoes,
    IRegistroConsumo consumo,
    TimeProvider relogio,
    ILogger<ProvedorEmbeddings> logger)
{
    public const string Servico = "Api.RAG";
    private readonly OpcoesEmbeddings _opcoes = opcoes.Value;

    public string Modelo => _opcoes.Modelo;

    public int Dimensao => _opcoes.Dimensao;

    public async Task<ResultadoEmbeddings> GerarAsync(IReadOnlyList<string> textos, string operacao, string? correlacaoId, CancellationToken cancelamento)
    {
        var vetores = new List<float[]>(textos.Count);
        long? tokens = 0;
        foreach (var lote in textos.Chunk(Math.Max(1, _opcoes.TamanhoLote)))
        {
            var (gerados, tokensLote) = await GerarLoteAsync(lote, operacao, correlacaoId, cancelamento).ConfigureAwait(false);
            vetores.AddRange(gerados);
            tokens = tokens is null || tokensLote is null ? null : tokens + tokensLote;
        }

        return new ResultadoEmbeddings(vetores, tokens);
    }

    private async Task<(IReadOnlyList<float[]> Vetores, long? Tokens)> GerarLoteAsync(
        string[] lote, string operacao, string? correlacaoId, CancellationToken cancelamento)
    {
        var inicio = relogio.GetUtcNow();
        string? codigoErro = null;
        long? tokens = null;
        try
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
            limite.CancelAfter(TimeSpan.FromSeconds(_opcoes.TimeoutSegundos));
            var gerados = await gerador
                .GenerateAsync(lote, new EmbeddingGenerationOptions { ModelId = _opcoes.Modelo, Dimensions = _opcoes.Dimensao }, limite.Token)
                .ConfigureAwait(false);
            tokens = gerados.Usage?.InputTokenCount;

            if (gerados.Count != lote.Length || gerados.Any(e => e.Vector.Length != _opcoes.Dimensao))
            {
                codigoErro = "DIMENSAO_INCOMPATIVEL";
                throw new ProvedorEmbeddingsException(codigoErro, "O provedor retornou embeddings incompatíveis com a dimensão configurada.");
            }

            return (gerados.Select(e => e.Vector.ToArray()).ToList(), tokens);
        }
        catch (ProvedorEmbeddingsException)
        {
            throw;
        }
        catch (OperationCanceledException excecao) when (!cancelamento.IsCancellationRequested)
        {
            codigoErro = "TIMEOUT";
            throw new ProvedorEmbeddingsException("PROVEDOR_EMBEDDINGS_INDISPONIVEL", "Tempo limite excedido no provedor de embeddings.", excecao);
        }
        catch (OperationCanceledException)
        {
            codigoErro = "CANCELADO";
            throw;
        }
#pragma warning disable CA1031 // Qualquer falha do provedor vira erro técnico tipado; o detalhe não é exposto.
        catch (Exception excecao) when (excecao is not OperationCanceledException)
#pragma warning restore CA1031
        {
            codigoErro = "FALHA_PROVEDOR";
            LogFalhaProvedor(logger, excecao.GetType().Name);
            throw new ProvedorEmbeddingsException("PROVEDOR_EMBEDDINGS_INDISPONIVEL", "Provedor de embeddings indisponível.", excecao);
        }
        finally
        {
            var fim = relogio.GetUtcNow();
            await consumo.RegistrarAsync(
                new RegistroConsumoEmbeddings(
                    Guid.CreateVersion7(),
                    correlacaoId,
                    Servico,
                    _opcoes.Provedor,
                    _opcoes.Modelo,
                    operacao,
                    inicio,
                    fim,
                    (long)(fim - inicio).TotalMilliseconds,
                    codigoErro is null ? "SUCESSO" : "FALHA",
                    lote.Length,
                    tokens,
                    codigoErro),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha no provedor de embeddings: {Tipo}")]
    private static partial void LogFalhaProvedor(ILogger logger, string tipo);
}
