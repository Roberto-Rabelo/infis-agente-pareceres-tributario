namespace Api.RAG.Application;

/// <summary>ADR D11 (proposto). Modelo e dimensão precisam ser compatíveis com a coluna vetorial; trocar exige reindexação.</summary>
public sealed class OpcoesEmbeddings
{
    public const string Secao = "Embeddings";

    public string Provedor { get; set; } = "OpenAI";

    public string Modelo { get; set; } = string.Empty;

    public int Dimensao { get; set; }

    public int TamanhoLote { get; set; } = 64;

    public int TimeoutSegundos { get; set; } = 30;
}

/// <summary>ADR D12 (provisório): valores sem avaliação de recuperação; ver specs/001-cruzamento-obrigacoes/plan.md.</summary>
public sealed class OpcoesSegmentacao
{
    public const string Secao = "Segmentacao";

    public int MaximoCaracteres { get; set; } = 1500;
}

/// <summary>ADR D12 (provisório). <see cref="LimiarSimilaridade"/> nulo = sem corte até existir avaliação (P7).</summary>
public sealed class OpcoesConsulta
{
    public const string Secao = "Consulta";

    public int LimitePadrao { get; set; } = 8;

    public int LimiteMaximo { get; set; } = 30;

    public double? LimiarSimilaridade { get; set; }

    public int MaximoCaracteresPergunta { get; set; } = 2000;

    public int MaximoRegraIds { get; set; } = 50;
}

public sealed class OpcoesIngestao
{
    public const string Secao = "Ingestao";

    public long TamanhoMaximoBytes { get; set; } = 5 * 1024 * 1024;
}
