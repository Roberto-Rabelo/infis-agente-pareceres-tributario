using Api.RAG.Domain;

namespace Api.RAG.Application;

public interface IRepositorioConhecimento
{
    Task<Documento?> ObterPorChaveVersaoAsync(string chave, string versao, CancellationToken cancelamento);

    Task<Documento?> ObterAtivoAsync(string chave, CancellationToken cancelamento);

    /// <summary>Grava documento e trechos atomicamente e marca <paramref name="substituirId"/> como substituído.</summary>
    Task SalvarAsync(Documento documento, IReadOnlyList<TrechoIndexado> trechos, Guid? substituirId, CancellationToken cancelamento);

    Task<IReadOnlyList<Documento>> ListarAsync(CancellationToken cancelamento);

    Task<IReadOnlyList<TrechoEncontrado>> BuscarPorRegrasAsync(IReadOnlyList<string> regraIds, FiltroBusca filtro, int limite, CancellationToken cancelamento);

    Task<IReadOnlyList<TrechoEncontrado>> BuscarSemanticaAsync(float[] vetor, FiltroBusca filtro, int limite, CancellationToken cancelamento);
}

/// <summary>Uma linha por chamada ao provedor (constitution 4.5). Valores desconhecidos ficam nulos, nunca zero.</summary>
public sealed record RegistroConsumoEmbeddings(
    Guid Id,
    string? CorrelacaoId,
    string Servico,
    string Provedor,
    string Modelo,
    string Operacao,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    long DuracaoMs,
    string Status,
    int QuantidadeTextos,
    long? TokensEntrada,
    string? CodigoErro);

public interface IRegistroConsumo
{
    Task RegistrarAsync(RegistroConsumoEmbeddings registro, CancellationToken cancelamento);
}

public sealed class BaseIndisponivelException(string mensagem, Exception? interna = null) : Exception(mensagem, interna);

public sealed class ProvedorEmbeddingsException(string codigo, string mensagem, Exception? interna = null) : Exception(mensagem, interna)
{
    public string Codigo { get; } = codigo;
}

public sealed class ErroValidacaoException(string codigo, string mensagem) : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
}

public sealed class ConflitoVersaoException(string mensagem) : Exception(mensagem);
