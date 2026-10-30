namespace Api.Agente.Domain.Catalogo;

public sealed record FonteOficial(string Emissor, string Documento, string Versao, string Localizacao, bool Conferida);

public sealed record Regra
{
    public const string SeveridadePadrao = "padrao";

    public required string Id { get; init; }

    public required string Titulo { get; init; }

    public required TipoRegra Tipo { get; init; }

    public TipoRelacao? Relacao { get; init; }

    /// <summary>Obrigações envolvidas e versões de layout aplicáveis. Lista vazia = todas as versões suportadas.</summary>
    public IReadOnlyDictionary<TipoObrigacao, IReadOnlyList<string>> Obrigacoes { get; init; } =
        new Dictionary<TipoObrigacao, IReadOnlyList<string>>();

    public Evidencia? Evidencia { get; init; }

    /// <summary>Severidades definidas pela fonte oficial; a chave <see cref="SeveridadePadrao"/> é obrigatória em regras de cruzamento.</summary>
    public IReadOnlyDictionary<string, Severidade> Severidades { get; init; } = new Dictionary<string, Severidade>();

    public required string ResultadoEsperado { get; init; }

    public IReadOnlyList<FonteOficial> Fontes { get; init; } = [];

    public IReadOnlyList<string> HipotesesLegitimas { get; init; } = [];

    public required DateOnly VigenciaInicio { get; init; }

    public DateOnly? VigenciaFim { get; init; }

    public required StatusRegra Status { get; init; }

    public string? AprovadoPor { get; init; }

    public IReadOnlyList<string> DependeDe { get; init; } = [];

    public required Implementacao Implementacao { get; init; }

    public string? JustificativaImplementacao { get; init; }

    public IReadOnlyList<string> Testes { get; init; } = [];

    public bool VigenteEm(Periodo periodo) =>
        VigenciaInicio <= periodo.Fim && (VigenciaFim is null || VigenciaFim >= periodo.Inicio);

    /// <summary>Achado de inconsistência com severidade obtida do catálogo, nunca do validador (P3).</summary>
    public Achado CriarInconsistencia(
        string descricao,
        IReadOnlyList<LocalizacaoEvidencia> evidencias,
        string chaveSeveridade = SeveridadePadrao,
        decimal? valorOrigem = null,
        decimal? valorDestino = null,
        decimal? toleranciaAplicada = null)
    {
        if (!Severidades.TryGetValue(chaveSeveridade, out var severidade))
        {
            throw new InvalidOperationException($"Regra {Id} não define a severidade '{chaveSeveridade}'.");
        }

        return new Achado
        {
            RegraId = Id,
            Natureza = Natureza.InconsistenciaCruzamento,
            Severidade = severidade,
            Descricao = descricao,
            Evidencias = evidencias,
            ValorOrigem = valorOrigem,
            ValorDestino = valorDestino,
            Diferenca = valorOrigem is not null && valorDestino is not null ? valorOrigem - valorDestino : null,
            ToleranciaAplicada = toleranciaAplicada,
            HipotesesLegitimas = HipotesesLegitimas,
        };
    }
}

public sealed record CatalogoRegras(string Versao, IReadOnlyList<Regra> Regras)
{
    public Regra? Obter(string id) => Regras.FirstOrDefault(r => r.Id == id);

    public Regra ObterObrigatoria(string id) =>
        Obter(id) ?? throw new InvalidOperationException($"Regra {id} ausente do catálogo {Versao}.");
}

public sealed record VersaoLayout(
    TipoObrigacao Tipo,
    string Versao,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    bool Conferido,
    string Fonte)
{
    public bool VigenteEm(Periodo periodo) =>
        (VigenciaInicio is null || VigenciaInicio <= periodo.Fim) && (VigenciaFim is null || VigenciaFim >= periodo.Inicio);
}

public sealed record CatalogoVersoesLayout(IReadOnlyList<VersaoLayout> Versoes)
{
    public IEnumerable<VersaoLayout> Suportadas(TipoObrigacao tipo) => Versoes.Where(v => v.Tipo == tipo && v.Conferido);
}
