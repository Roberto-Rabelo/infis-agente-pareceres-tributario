namespace Api.Agente.Application.Parecer;

/// <summary>Resposta do LLM aceita após validação (contracts/saida/parecer.v1.schema.json).</summary>
public sealed record ParecerLlm(
    string Status,
    string Identificacao,
    string EscopoELimitacoes,
    string Sintese,
    IReadOnlyList<AchadoParecer> Achados,
    IReadOnlyList<IndicioIa> IndiciosIa,
    IReadOnlyList<string> FontesCitadas,
    IReadOnlyList<string> Limitacoes,
    RevisaoParecer RevisaoHumana)
{
    public const string VersaoContrato = "1.0.0";
}

public sealed record AchadoParecer(
    string AchadoId,
    string RegraId,
    string Natureza,
    string? Severidade,
    IReadOnlyList<string> HipotesesLegitimas,
    string Analise,
    IReadOnlyList<string> Referencias);

public sealed record IndicioIa(string Descricao, string Severidade, IReadOnlyList<string> Obrigacoes, IReadOnlyList<string> Referencias);

public sealed record RevisaoParecer(bool Requerida, IReadOnlyList<string> Motivos);

/// <summary>Uma linha por chamada ao provedor, inclusive retentativas e falhas (constitution 4.5). Desconhecido = nulo.</summary>
public sealed record RegistroConsumoLlm(
    Guid Id,
    string ExecucaoId,
    string CorrelacaoId,
    string Servico,
    string Provedor,
    string Modelo,
    string Operacao,
    int Tentativa,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    long DuracaoMs,
    string Status,
    string? Resultado,
    long? TokensEntrada,
    long? TokensSaida,
    string? CodigoErro,
    decimal? CustoEstimado,
    string? TabelaPrecos);
