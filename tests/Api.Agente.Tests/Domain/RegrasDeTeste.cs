using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;
using Api.Agente.Domain.Motor;

namespace Api.Agente.Tests.Domain;

/// <summary>Regras fictícias para testar o motor e o catálogo; não representam regras fiscais.</summary>
internal static class RegrasDeTeste
{
    public static readonly FonteOficial FonteConferida = new("Emissor de teste", "Documento de teste", "1.0", "Seção 1", true);

    public static Regra Cruzamento(
        string id,
        TipoRelacao relacao = TipoRelacao.PresencaDireta,
        StatusRegra status = StatusRegra.Aprovada,
        TipoObrigacao[]? obrigacoes = null,
        string[]? dependeDe = null,
        DateOnly? vigenciaInicio = null,
        DateOnly? vigenciaFim = null,
        Dictionary<TipoObrigacao, IReadOnlyList<string>>? versoes = null) => new()
    {
        Id = id,
        Titulo = "Regra de teste " + id,
        Tipo = TipoRegra.Cruzamento,
        Relacao = relacao,
        Obrigacoes = versoes ?? (obrigacoes ?? [TipoObrigacao.NFe, TipoObrigacao.EfdIcmsIpi])
            .ToDictionary(t => t, _ => (IReadOnlyList<string>)[]),
        Evidencia = Evidencia.Manual,
        Severidades = new Dictionary<string, Severidade> { [Regra.SeveridadePadrao] = Severidade.ErroGrave, ["temporal"] = Severidade.Alerta },
        ResultadoEsperado = "Resultado de teste.",
        Fontes = [FonteConferida],
        HipotesesLegitimas = ["Hipótese de teste."],
        VigenciaInicio = vigenciaInicio ?? new DateOnly(2026, 1, 1),
        VigenciaFim = vigenciaFim,
        Status = status,
        AprovadoPor = status == StatusRegra.Aprovada ? "Responsável de teste" : null,
        DependeDe = dependeDe ?? [],
        Implementacao = Implementacao.Deterministica,
        Testes = ["Teste.De.Exemplo"],
    };

    public static ObrigacaoRecebida Obrigacao(string id, TipoObrigacao tipo, string versao = "1")
    {
        Cnpj.TentarCriar("11222333000181", out var cnpj, out _);
        return new ObrigacaoRecebida(
            id, tipo, versao, cnpj, null, null, Periodo.Mes(2026, 3), Finalidade.Original,
            new Identificacao("R", null, null, null),
            new ValidacaoOrigem("a", "1", DateTimeOffset.UnixEpoch, StatusValidacaoOrigem.Aprovado, []),
            new Dictionary<string, IReadOnlyList<OcorrenciaRegistro>>());
    }

    public static ContextoCruzamento Contexto(params ObrigacaoRecebida[] aptas) =>
        Contexto(aptas.Select(o => o.Tipo).ToHashSet(), aptas);

    public static ContextoCruzamento Contexto(HashSet<TipoObrigacao> recebidos, params ObrigacaoRecebida[] aptas) =>
        new("11222333", Periodo.Mes(2026, 3), aptas, recebidos, ParametrosComparacao.Exata);
}

internal sealed class ValidadorFalso(string regraId, Func<ContextoCruzamento, Regra, IReadOnlyList<Achado>> validar) : IValidadorCruzamento
{
    public List<string> Chamadas { get; } = [];

    public string RegraId { get; } = regraId;

    public IReadOnlyList<Achado> Validar(ContextoCruzamento contexto, Regra regra)
    {
        Chamadas.Add(regra.Id);
        return validar(contexto, regra);
    }

    public static ValidadorFalso SemAchados(string regraId) => new(regraId, (_, _) => []);

    public static ValidadorFalso ComInconsistencia(string regraId) =>
        new(regraId, (_, regra) => [regra.CriarInconsistencia("Divergência de teste.", [new LocalizacaoEvidencia("nfe"), new LocalizacaoEvidencia("efd")])]);
}
