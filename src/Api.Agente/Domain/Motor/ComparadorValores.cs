namespace Api.Agente.Domain.Motor;

/// <summary>Nenhuma norma define tolerância nem materialidade (doc 03 §0): ambas são parâmetros informados no parecer (FR-024, FR-025).</summary>
public sealed record ParametrosComparacao(decimal Tolerancia, decimal? Materialidade)
{
    public static ParametrosComparacao Exata { get; } = new(0m, null);
}

public sealed record ResultadoComparacao(decimal Origem, decimal Destino, decimal Diferenca, decimal ToleranciaAplicada, bool Diverge)
{
    /// <summary>Nulo quando a materialidade não foi parametrizada: a regra deve usar a severidade padrão.</summary>
    public bool? Material { get; init; }
}

public static class ComparadorValores
{
    public static ResultadoComparacao Comparar(decimal origem, decimal destino, ParametrosComparacao parametros)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parametros.Tolerancia);
        var diferenca = origem - destino;
        var diverge = Math.Abs(diferenca) > parametros.Tolerancia;
        return new ResultadoComparacao(origem, destino, diferenca, parametros.Tolerancia, diverge)
        {
            Material = parametros.Materialidade is { } limite ? diverge && Math.Abs(diferenca) >= limite : null,
        };
    }

    /// <summary>Relação LIM: destino não pode exceder origem além da tolerância.</summary>
    public static ResultadoComparacao Limite(decimal limite, decimal valor, ParametrosComparacao parametros)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parametros.Tolerancia);
        var excesso = valor - limite;
        var diverge = excesso > parametros.Tolerancia;
        return new ResultadoComparacao(limite, valor, excesso, parametros.Tolerancia, diverge)
        {
            Material = parametros.Materialidade is { } materialidade ? diverge && excesso >= materialidade : null,
        };
    }
}
