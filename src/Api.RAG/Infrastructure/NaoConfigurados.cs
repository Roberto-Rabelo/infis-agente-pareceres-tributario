using Api.RAG.Application;
using Api.RAG.Domain;
using Microsoft.Extensions.AI;

namespace Api.RAG.Infrastructure;

/// <summary>Sem string de conexão configurada a base fica indisponível (falha fechada), nunca vazia.</summary>
public sealed class RepositorioNaoConfigurado : IRepositorioConhecimento, IRegistroConsumo
{
    private static BaseIndisponivelException Erro() => new("Base de conhecimento não configurada.");

    public Task<Documento?> ObterPorChaveVersaoAsync(string chave, string versao, CancellationToken cancelamento) => throw Erro();

    public Task<Documento?> ObterAtivoAsync(string chave, CancellationToken cancelamento) => throw Erro();

    public Task SalvarAsync(Documento documento, IReadOnlyList<TrechoIndexado> trechos, Guid? substituirId, CancellationToken cancelamento) => throw Erro();

    public Task<IReadOnlyList<Documento>> ListarAsync(CancellationToken cancelamento) => throw Erro();

    public Task<IReadOnlyList<TrechoEncontrado>> BuscarPorRegrasAsync(IReadOnlyList<string> regraIds, FiltroBusca filtro, int limite, CancellationToken cancelamento) =>
        throw Erro();

    public Task<IReadOnlyList<TrechoEncontrado>> BuscarSemanticaAsync(float[] vetor, FiltroBusca filtro, int limite, CancellationToken cancelamento) =>
        throw Erro();

    public Task RegistrarAsync(RegistroConsumoEmbeddings registro, CancellationToken cancelamento) => throw Erro();
}

/// <summary>Sem chave do provedor configurada, toda geração falha e é registrada como falha.</summary>
public sealed class GeradorNaoConfigurado : IEmbeddingGenerator<string, Embedding<float>>
{
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Provedor de embeddings não configurado.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
