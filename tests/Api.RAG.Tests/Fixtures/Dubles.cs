using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Api.RAG.Application;
using Api.RAG.Domain;
using Microsoft.Extensions.AI;

namespace Api.RAG.Tests.Fixtures;

/// <summary>Embedding determinístico por saco de palavras (hash em buckets), suficiente para ordenar por similaridade.</summary>
public sealed partial class GeradorFalso(int dimensao) : IEmbeddingGenerator<string, Embedding<float>>
{
    public int Chamadas { get; private set; }

    public bool Falhar { get; set; }

    public int? DimensaoRetornada { get; set; }

    public bool InformarUso { get; set; } = true;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Chamadas++;
        if (Falhar)
        {
            throw new HttpRequestException("falha simulada");
        }

        var textos = values.ToList();
        var resultado = new GeneratedEmbeddings<Embedding<float>>(textos.Select(t => new Embedding<float>(Vetor(t, DimensaoRetornada ?? dimensao))))
        {
            Usage = InformarUso ? new UsageDetails { InputTokenCount = textos.Sum(t => Palavras(t).Count()) } : null,
        };
        return Task.FromResult(resultado);
    }

    public static float[] Vetor(string texto, int dimensao)
    {
        var vetor = new float[dimensao];
        foreach (var palavra in Palavras(texto))
        {
            vetor[(int)((uint)StringComparer.Ordinal.GetHashCode(palavra) % (uint)dimensao)] += 1;
        }

        var norma = MathF.Sqrt(vetor.Sum(v => v * v));
        return norma == 0 ? vetor : vetor.Select(v => v / norma).ToArray();
    }

    private static IEnumerable<string> Palavras(string texto) =>
        Palavra().Matches(texto.ToUpperInvariant()).Select(m => m.Value).Where(p => p.Length > 2);

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    [GeneratedRegex(@"[\p{L}\p{N}_]+")]
    private static partial Regex Palavra();
}

public sealed class RepositorioEmMemoria : IRepositorioConhecimento, IRegistroConsumo
{
    private readonly ConcurrentDictionary<Guid, Documento> _documentos = new();
    private readonly ConcurrentDictionary<Guid, (Guid DocumentoId, TrechoIndexado Trecho)> _trechos = new();

    public ConcurrentQueue<RegistroConsumoEmbeddings> Consumo { get; } = new();

    public bool Indisponivel { get; set; }

    public IReadOnlyCollection<Documento> Documentos => _documentos.Values.ToList();

    public int QuantidadeTrechos => _trechos.Count;

    public Task<Documento?> ObterPorChaveVersaoAsync(string chave, string versao, CancellationToken cancelamento) =>
        Executar(() => _documentos.Values.SingleOrDefault(d => d.Chave == chave && d.Versao == versao));

    public Task<Documento?> ObterAtivoAsync(string chave, CancellationToken cancelamento) =>
        Executar(() => _documentos.Values.SingleOrDefault(d => d.Chave == chave && d.Status == StatusDocumento.Ativo));

    public Task SalvarAsync(Documento documento, IReadOnlyList<TrechoIndexado> trechos, Guid? substituirId, CancellationToken cancelamento) =>
        Executar(() =>
        {
            if (substituirId is { } id)
            {
                _documentos[id] = _documentos[id] with { Status = StatusDocumento.Substituido, SubstituidoEm = documento.IngeridoEm };
            }

            _documentos[documento.Id] = documento;
            foreach (var trecho in trechos)
            {
                _trechos[trecho.Id] = (documento.Id, trecho);
            }

            return true;
        });

    public Task<IReadOnlyList<Documento>> ListarAsync(CancellationToken cancelamento) =>
        Executar<IReadOnlyList<Documento>>(() => _documentos.Values.OrderBy(d => d.Chave).ToList());

    public Task<IReadOnlyList<TrechoEncontrado>> BuscarPorRegrasAsync(
        IReadOnlyList<string> regraIds, FiltroBusca filtro, int limite, CancellationToken cancelamento) =>
        Executar<IReadOnlyList<TrechoEncontrado>>(() => Candidatos(filtro)
            .Where(c => c.Trecho.Trecho.RegraIds.Intersect(regraIds).Any())
            .OrderByDescending(c => c.Trecho.Trecho.RegraPrincipal is { } p && regraIds.Contains(p))
            .ThenBy(c => c.Documento.Chave)
            .ThenBy(c => c.Trecho.Trecho.Ordem)
            .Take(limite)
            .Select(c => new TrechoEncontrado(c.Trecho.Id, c.Documento, c.Trecho.Trecho, MotivoRecuperacao.Regra, null))
            .ToList());

    public Task<IReadOnlyList<TrechoEncontrado>> BuscarSemanticaAsync(float[] vetor, FiltroBusca filtro, int limite, CancellationToken cancelamento) =>
        Executar<IReadOnlyList<TrechoEncontrado>>(() => Candidatos(filtro)
            .Select(c => (c, Similaridade: (double)c.Trecho.Embedding.Zip(vetor, (a, b) => a * b).Sum()))
            .OrderByDescending(x => x.Similaridade)
            .ThenBy(x => x.c.Trecho.Id)
            .Take(limite)
            .Select(x => new TrechoEncontrado(x.c.Trecho.Id, x.c.Documento, x.c.Trecho.Trecho, MotivoRecuperacao.Semantica, x.Similaridade))
            .ToList());

    public Task RegistrarAsync(RegistroConsumoEmbeddings registro, CancellationToken cancelamento)
    {
        Consumo.Enqueue(registro);
        return Task.CompletedTask;
    }

    private IEnumerable<(Documento Documento, TrechoIndexado Trecho)> Candidatos(FiltroBusca filtro) =>
        _trechos.Values
            .Select(t => (Documento: _documentos[t.DocumentoId], t.Trecho))
            .Where(c => c.Documento.Status == StatusDocumento.Ativo && c.Documento.ModeloEmbedding == filtro.ModeloEmbedding)
            .Where(c => filtro.TiposDocumento.Count == 0 || filtro.TiposDocumento.Contains(c.Documento.Tipo))
            .Where(c => filtro.Obrigacoes.Count == 0 || c.Trecho.Trecho.Obrigacoes.Count == 0 || c.Trecho.Trecho.Obrigacoes.Intersect(filtro.Obrigacoes).Any())
            .Where(c => filtro.DataReferencia is not { } data
                || ((c.Documento.VigenciaInicio is null || c.Documento.VigenciaInicio <= data) && (c.Documento.VigenciaFim is null || c.Documento.VigenciaFim >= data)));

    private Task<T> Executar<T>(Func<T> acao) =>
        Indisponivel ? Task.FromException<T>(new BaseIndisponivelException("indisponível")) : Task.FromResult(acao());
}
