using Api.Agente.Application.Analise;
using Api.Agente.Domain;
using Api.Agente.Domain.Motor;

namespace Api.Agente.Application.Parecer;

/// <summary>
/// Fatos do sistema nunca dependem da redação do modelo (P2, P4): hipóteses legítimas, escopo e limitações
/// são preenchidos aqui, depois da validação, a partir do resultado determinístico.
/// </summary>
public static class ConsolidadorParecer
{
    private const string EvidenciaInsuficiente = "evidências insuficientes em parte do universo da regra.";

    public static ParecerLlm Aplicar(ParecerLlm parecer, ResultadoAnalise analise, DadosParecer dados) => parecer with
    {
        EscopoELimitacoes = Escopo(analise),
        Achados = [.. parecer.Achados.Select(a => a with { HipotesesLegitimas = dados.Achados[a.AchadoId].HipotesesLegitimas })],
        Limitacoes = [.. Pendentes(analise)
            .Select(r => $"{r.RegraId}: {r.Motivo ?? EvidenciaInsuficiente}")
            .Concat(analise.Limitacoes)
            .Concat(parecer.Limitacoes)
            .Distinct(StringComparer.Ordinal)],
    };

    private static string Escopo(ResultadoAnalise analise)
    {
        var executadas = analise.Regras.Count(r => r.Situacao is SituacaoRegra.Executada or SituacaoRegra.ComInconsistencia);
        var texto = $"Obrigações recebidas: {analise.Aptidao.Count}; aptas ao cruzamento: {analise.Aptidao.Count(a => a.Apta)}. "
            + $"Regras de cruzamento executadas: {executadas} de {analise.Regras.Count}.";
        var pendentes = Pendentes(analise).Select(r => $"{r.RegraId} ({MontadorDadosParecer.Codigo(r.Situacao)})").ToList();
        if (pendentes.Count > 0)
        {
            texto += $" Não concluídas: {string.Join(", ", pendentes)}.";
        }

        return texto + " Este parecer cobre somente essas regras e não atesta a regularidade do contribuinte.";
    }

    private static IEnumerable<ResultadoRegra> Pendentes(ResultadoAnalise analise) =>
        analise.Regras.Where(r => r.Situacao is SituacaoRegra.AusenciaEvidencias or SituacaoRegra.NaoConclusiva or SituacaoRegra.FalhaTecnica);
}
