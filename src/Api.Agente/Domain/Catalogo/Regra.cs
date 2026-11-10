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

    /// <summary>Passo de <c>cadeias.json &gt; ordemExecucao</c> (0 = pré-condições). Ordena a execução antes do tipo de relação.</summary>
    public int Passo { get; init; }

    /// <summary>Campos declarativos do catálogo de coesão, interpretados pelos executores genéricos (spec 004, D405).</summary>
    public DeclaracaoRegra? Declaracao { get; init; }

    /// <summary>Nomes <c>PARAM:*</c> citados pela regra. Regra aprovada exige todos com valor (FR-403/404).</summary>
    public IReadOnlyList<string> ParametrosUsados { get; init; } = [];

    /// <summary>O que impede a aprovação da regra como está no catálogo (ex.: severidade sem correspondência). Regra aprovada não pode ter pendências.</summary>
    public IReadOnlyList<string> Pendencias { get; init; } = [];

    /// <summary>Dependências do catálogo dispensadas na aprovação, com o motivo registrado pelo aprovador.</summary>
    public IReadOnlyDictionary<string, string> DependenciasDispensadas { get; init; } = new Dictionary<string, string>();

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
            Descricao = Descrever(descricao),
            Evidencias = evidencias,
            ValorOrigem = valorOrigem,
            ValorDestino = valorDestino,
            Diferenca = valorOrigem is not null && valorDestino is not null ? valorOrigem - valorDestino : null,
            ToleranciaAplicada = toleranciaAplicada,
            HipotesesLegitimas = HipotesesReais(),
        };
    }

    private string Descrever(string descricao) =>
        descricao.Contains(Id, StringComparison.Ordinal)
            ? descricao
            : $"{descricao} Critério: {Titulo} ({Id}).";

    private IReadOnlyList<string> HipotesesReais() =>
        [.. HipotesesLegitimas.Where(h =>
            !string.IsNullOrWhiteSpace(h)
            && !h.Contains("Nenhuma hipótese legítima prevista nos leiautes", StringComparison.OrdinalIgnoreCase))];
}

/// <summary>Um lado da relação (origem ou destino), como escrito no catálogo.</summary>
/// <param name="Tipo">Obrigação reconhecida; nulo quando o catálogo cita evidência externa (ex.: <c>EXTERNO</c>).</param>
/// <param name="CodigoObrigacao">Código como está no catálogo.</param>
/// <param name="Elemento">Registro/campo (SPED) ou grupo/campo (XML), em texto.</param>
/// <param name="Filtro">Filtros separados por ponto e vírgula, em texto.</param>
/// <param name="Agregacao">Nível de agregação (MES, ANO, CONTA...), quando houver.</param>
public sealed record LadoRegra(TipoObrigacao? Tipo, string CodigoObrigacao, string? Elemento, string? Filtro, string? Agregacao);

public sealed record DeclaracaoRegra
{
    public LadoRegra? Origem { get; init; }

    public LadoRegra? Destino { get; init; }

    public IReadOnlyList<string> ChaveJuncao { get; init; } = [];

    public string? Operador { get; init; }

    /// <summary>Referência <c>PARAM:NOME</c> ou nulo (igualdade exata).</summary>
    public string? Tolerancia { get; init; }

    public string? AplicavelSe { get; init; }

    public string? CondicaoDescritiva { get; init; }

    public IReadOnlyList<string> FiltrosExclusao { get; init; } = [];

    public IReadOnlyList<string> Cadeias { get; init; } = [];
}

public sealed record ParametroRegra(string Nome, string Tipo, bool Definido, decimal? Numero, string? Decisao);

/// <summary>Parâmetros <c>PARAM:*</c> do projeto (docs/regras/parametros.json). Nenhum é definido em norma: são decisões do projeto.</summary>
public sealed record CatalogoParametros(IReadOnlyDictionary<string, ParametroRegra> Itens)
{
    public const string Prefixo = "PARAM:";

    public static CatalogoParametros Vazio { get; } = new(new Dictionary<string, ParametroRegra>());

    public static string Nome(string referencia) =>
        referencia.StartsWith(Prefixo, StringComparison.Ordinal) ? referencia[Prefixo.Length..] : referencia;

    public ParametroRegra? Obter(string referencia) => Itens.GetValueOrDefault(Nome(referencia));

    public decimal? Numero(string referencia) => Obter(referencia)?.Numero;
}

public sealed record CatalogoRegras(string Versao, IReadOnlyList<Regra> Regras)
{
    public CatalogoParametros Parametros { get; init; } = CatalogoParametros.Vazio;

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
