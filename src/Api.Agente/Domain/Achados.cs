namespace Api.Agente.Domain;

/// <summary>Localização da evidência em uma obrigação recebida (constitution P5; FR-028).</summary>
public sealed record LocalizacaoEvidencia(
    string ObrigacaoId,
    string? Registro = null,
    int? Ocorrencia = null,
    string? Campo = null,
    long? Linha = null,
    string? Valor = null);

public sealed record ObrigacaoFaltante(TipoObrigacao Tipo, Periodo Periodo, string Motivo);

public sealed record Achado
{
    public required string RegraId { get; init; }

    public required Natureza Natureza { get; init; }

    public Severidade? Severidade { get; init; }

    public required string Descricao { get; init; }

    public IReadOnlyList<LocalizacaoEvidencia> Evidencias { get; init; } = [];

    public decimal? ValorOrigem { get; init; }

    public decimal? ValorDestino { get; init; }

    public decimal? Diferenca { get; init; }

    public decimal? ToleranciaAplicada { get; init; }

    public IReadOnlyList<string> HipotesesLegitimas { get; init; } = [];

    public IReadOnlyList<ObrigacaoFaltante> ObrigacoesFaltantes { get; init; } = [];

    public static Achado PreCondicao(string regraId, string descricao, params LocalizacaoEvidencia[] evidencias) => new()
    {
        RegraId = regraId,
        Natureza = Natureza.PreCondicaoNaoAtendida,
        Descricao = descricao,
        Evidencias = evidencias,
    };
}
