namespace Api.Agente.Domain;

public enum TipoObrigacao
{
    NFe,
    NFCe,
    NFSe,
    Dimp,
    EfdIcmsIpi,
    EfdContribuicoes,
    Ecd,
    Ecf,
}

public enum Finalidade
{
    Original,
    Substituta,
}

public enum StatusValidacaoOrigem
{
    Aprovado,
    Reprovado,
}

public enum TipoOcorrenciaOrigem
{
    Erro,
    Advertencia,
}

/// <summary>Natureza do achado (constitution P3). Valores mutuamente exclusivos.</summary>
public enum Natureza
{
    PreCondicaoNaoAtendida,
    InconsistenciaCruzamento,
    IndicioIa,
    AusenciaEvidencias,
    FalhaTecnica,
}

/// <summary>Escala mínima da constitution (P3). Níveis adicionais dependem da decisão D4/C1.</summary>
public enum Severidade
{
    ErroGrave,
    Alerta,
}

public enum Evidencia
{
    Pva,
    Manual,
    Norma,
    Derivada,
}

public enum TipoRegra
{
    PreCondicao,
    Cruzamento,
}

/// <summary>Tipos de relação do NUCLEO-1 §2. A ordem dos valores não define a ordem de execução; ver <see cref="OrdemExecucao"/>.</summary>
public enum TipoRelacao
{
    Parametro,
    Exclusividade,
    PresencaDireta,
    PresencaReversa,
    Identidade,
    Agregacao,
    Limite,
    Continuidade,
}

public enum StatusRegra
{
    Proposta,
    Aprovada,
    Suspensa,
}

public enum Implementacao
{
    Deterministica,
    AssistidaIa,
}

public enum StatusAnalise
{
    Concluida,
    ConcluidaParcial,
    Inconclusiva,
    FalhaTecnica,
}

public enum SituacaoRegra
{
    Executada,
    ComInconsistencia,
    AusenciaEvidencias,
    NaoConclusiva,
    NaoAplicavel,
    FalhaTecnica,
}

public static class OrdemExecucao
{
    /// <summary>PAR → EXC → PRE→/←PRE → IDT → AGR/LIM → CNT (NUCLEO-1 §2).</summary>
    public static int De(TipoRelacao relacao) => relacao switch
    {
        TipoRelacao.Parametro => 0,
        TipoRelacao.Exclusividade => 1,
        TipoRelacao.PresencaDireta or TipoRelacao.PresencaReversa => 2,
        TipoRelacao.Identidade => 3,
        TipoRelacao.Agregacao or TipoRelacao.Limite => 4,
        TipoRelacao.Continuidade => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(relacao), relacao, null),
    };
}
