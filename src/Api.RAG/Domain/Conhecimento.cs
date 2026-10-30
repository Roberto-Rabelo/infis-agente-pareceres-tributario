namespace Api.RAG.Domain;

/// <summary>
/// Classificação da fonte (constitution P1/P7). <see cref="BaseRegrasInterna"/> é o material de estudo do projeto
/// (pasta <c>docs/</c>): orienta a análise, mas não é fonte oficial e não pode fundamentar sozinho uma conclusão.
/// </summary>
public enum TipoDocumento
{
    ManualOficial,
    Legislacao,
    BaseRegrasInterna,
}

public enum StatusDocumento
{
    Ativo,
    Substituido,
}

public sealed record Documento(
    Guid Id,
    string Chave,
    string Titulo,
    TipoDocumento Tipo,
    string Emissor,
    string Versao,
    DateOnly? DataPublicacao,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    string Origem,
    string HashConteudo,
    IReadOnlyList<string> Obrigacoes,
    StatusDocumento Status,
    string ModeloEmbedding,
    int DimensaoEmbedding,
    DateTimeOffset IngeridoEm,
    DateTimeOffset? SubstituidoEm);

/// <summary>Trecho de um documento com a localização exigida pela P7.</summary>
public sealed record Trecho(
    int Ordem,
    string Secao,
    int LinhaInicio,
    int LinhaFim,
    string Texto,
    string HashTexto,
    IReadOnlyList<string> RegraIds,
    string? RegraPrincipal,
    IReadOnlyList<string> Obrigacoes);

public sealed record TrechoIndexado(Guid Id, Trecho Trecho, float[] Embedding);

public enum MotivoRecuperacao
{
    Regra,
    Semantica,
}

public sealed record TrechoEncontrado(Guid TrechoId, Documento Documento, Trecho Trecho, MotivoRecuperacao Motivo, double? Similaridade);

/// <summary>Filtros comuns às buscas. Listas vazias não filtram.</summary>
public sealed record FiltroBusca(
    IReadOnlyList<string> Obrigacoes,
    IReadOnlyList<TipoDocumento> TiposDocumento,
    DateOnly? DataReferencia,
    string ModeloEmbedding);

public static class CodigosTipoDocumento
{
    private static readonly Dictionary<TipoDocumento, string> Codigos = new()
    {
        [TipoDocumento.ManualOficial] = "MANUAL_OFICIAL",
        [TipoDocumento.Legislacao] = "LEGISLACAO",
        [TipoDocumento.BaseRegrasInterna] = "BASE_REGRAS_INTERNA",
    };

    public static string De(TipoDocumento tipo) => Codigos[tipo];

    public static bool TentarDe(string? codigo, out TipoDocumento tipo)
    {
        foreach (var (chave, valor) in Codigos)
        {
            if (string.Equals(valor, codigo, StringComparison.Ordinal))
            {
                tipo = chave;
                return true;
            }
        }

        tipo = default;
        return false;
    }
}

public static class CodigosObrigacao
{
    public const string Nfe = "NFE";
    public const string Nfce = "NFCE";
    public const string Nfse = "NFSE";
    public const string Dimp = "DIMP";
    public const string EfdIcmsIpi = "EFD_ICMS_IPI";
    public const string EfdContribuicoes = "EFD_CONTRIBUICOES";
    public const string Ecd = "ECD";
    public const string Ecf = "ECF";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.Ordinal)
    {
        Nfe, Nfce, Nfse, Dimp, EfdIcmsIpi, EfdContribuicoes, Ecd, Ecf,
    };
}
