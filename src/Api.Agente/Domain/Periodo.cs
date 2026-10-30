namespace Api.Agente.Domain;

public sealed record Periodo
{
    public Periodo(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
        {
            throw new ArgumentException("Fim do período anterior ao início.", nameof(fim));
        }

        Inicio = inicio;
        Fim = fim;
    }

    public DateOnly Inicio { get; }

    public DateOnly Fim { get; }

    public static bool TentarCriar(DateOnly inicio, DateOnly fim, out Periodo? periodo)
    {
        periodo = fim < inicio ? null : new Periodo(inicio, fim);
        return periodo is not null;
    }

    public static Periodo Mes(int ano, int mes)
    {
        var inicio = new DateOnly(ano, mes, 1);
        return new Periodo(inicio, inicio.AddMonths(1).AddDays(-1));
    }

    public static Periodo Ano(int ano) => new(new DateOnly(ano, 1, 1), new DateOnly(ano, 12, 31));

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;

    public bool Contem(Periodo outro) => outro.Inicio >= Inicio && outro.Fim <= Fim;

    public bool SobrepoeA(Periodo outro) => Inicio <= outro.Fim && outro.Inicio <= Fim;

    /// <summary>Meses (primeiro dia) que têm ao menos um dia no período; base para agregação mensal → anual.</summary>
    public IReadOnlyList<DateOnly> Meses()
    {
        var meses = new List<DateOnly>();
        for (var mes = new DateOnly(Inicio.Year, Inicio.Month, 1); mes <= Fim; mes = mes.AddMonths(1))
        {
            meses.Add(mes);
        }

        return meses;
    }

    public override string ToString() => $"{Inicio:yyyy-MM-dd}/{Fim:yyyy-MM-dd}";
}
