namespace Api.Agente.Application.Conhecimento;

public sealed class OpcoesBaseConhecimento
{
    public const string Secao = "BaseConhecimento";

    /// <summary>URL do servidor MCP da Api.RAG. Vazio = base não configurada (a análise registra a limitação).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Token de serviço (D13/D15 pendentes); vem de variável de ambiente ou secret manager.</summary>
    public string TokenAcesso { get; set; } = string.Empty;

    public int TimeoutSegundos { get; set; } = 10;

    /// <summary>D12 (provisório, sem avaliação): trechos por consulta.</summary>
    public int LimitePorConsulta { get; set; } = 20;

    public int RegrasPorConsulta { get; set; } = 20;
}

public sealed record ConsultaFundamentacao(IReadOnlyList<string> RegraIds, DateOnly DataReferencia, int Limite, string CorrelacaoId)
{
    public string? Pergunta { get; init; }

    public IReadOnlyList<string> Obrigacoes { get; init; } = [];
}

/// <summary>Consumo de embeddings informado pela Api.RAG para a consulta (registrado pela Api.Agente, constitution 4.6).</summary>
public sealed record ConsumoEmbeddings(string Provedor, string Modelo, long? TokensEntrada);

/// <summary>Trecho recuperado da Api.RAG. O texto é dado não confiável (P8) e nunca altera achados determinísticos.</summary>
public sealed record ReferenciaConhecimento(
    string TrechoId,
    string DocumentoChave,
    string DocumentoTitulo,
    string TipoDocumento,
    string DocumentoVersao,
    string Origem,
    string HashDocumento,
    string Secao,
    int LinhaInicio,
    int LinhaFim,
    string Texto,
    IReadOnlyList<string> RegraIds,
    string? RegraPrincipal,
    string Motivo,
    double? Similaridade);

public sealed record ResultadoBaseConhecimento(bool Disponivel, IReadOnlyList<ReferenciaConhecimento> Referencias, IReadOnlyList<string> Limitacoes)
{
    public ConsumoEmbeddings? Consumo { get; init; }

    public static ResultadoBaseConhecimento Indisponivel(string motivo) => new(false, [], [motivo]);
}

/// <summary>Porta para a Api.RAG; a implementação usa somente MCP (P12).</summary>
public interface IBaseConhecimento
{
    Task<ResultadoBaseConhecimento> ConsultarAsync(ConsultaFundamentacao consulta, CancellationToken cancelamento);
}
