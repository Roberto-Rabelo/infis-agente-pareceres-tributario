using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Domain.Motor;

public sealed record Exclusao(string RegraId, string Chave, string Motivo);

/// <summary>Obrigações aptas de um mesmo grupo de cruzamento (contribuinte e período do lote).</summary>
public sealed class ContextoCruzamento
{
    private readonly List<Exclusao> _exclusoes = [];

    public ContextoCruzamento(
        string raizCnpj,
        Periodo periodo,
        IReadOnlyList<ObrigacaoRecebida> aptas,
        IReadOnlySet<TipoObrigacao> tiposRecebidos,
        ParametrosComparacao parametros)
    {
        RaizCnpj = raizCnpj;
        Periodo = periodo;
        Aptas = aptas;
        TiposRecebidos = tiposRecebidos;
        Parametros = parametros;
    }

    public string RaizCnpj { get; }

    public Periodo Periodo { get; }

    public IReadOnlyList<ObrigacaoRecebida> Aptas { get; }

    /// <summary>Tipos presentes no lote, aptos ou não. Distingue "não recebida" de "não apta".</summary>
    public IReadOnlySet<TipoObrigacao> TiposRecebidos { get; }

    public ParametrosComparacao Parametros { get; }

    public IReadOnlyList<Exclusao> Exclusoes => _exclusoes;

    /// <summary>Registrado pelas regras EXC, que rodam antes das regras de presença (FR-018).</summary>
    public void RegistrarExclusao(string regraId, string chave, string motivo) =>
        _exclusoes.Add(new Exclusao(regraId, chave, motivo));

    public bool EstaExcluido(string chave) => _exclusoes.Exists(e => e.Chave == chave);

    public IReadOnlyList<ObrigacaoRecebida> ObrigacoesPara(Regra regra, TipoObrigacao tipo)
    {
        var versoes = regra.Obrigacoes.TryGetValue(tipo, out var lista) ? lista : [];
        return Aptas
            .Where(o => o.Tipo == tipo && (versoes.Count == 0 || versoes.Contains(o.VersaoLayout!)))
            .ToList();
    }
}
