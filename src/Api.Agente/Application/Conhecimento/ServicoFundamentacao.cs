using Api.Agente.Application.Analise;
using Api.Agente.Domain;
using Microsoft.Extensions.Options;

namespace Api.Agente.Application.Conhecimento;

public sealed record FundamentacaoAchado(string RegraId, IReadOnlyList<ReferenciaConhecimento> Referencias);

/// <summary>
/// Etapa 6 do fluxo (constitution 3.4): recupera por MCP os trechos das regras que geraram achados, para que o LLM
/// analise as inconsistências com referências rastreáveis. Não altera natureza, severidade nem descrição dos achados (P2).
/// Pré-condições (PRE-*) não são consultadas: a fonte delas é a constitution, não a base de conhecimento.
/// </summary>
public sealed class ServicoFundamentacao(IBaseConhecimento baseConhecimento, IOptions<OpcoesBaseConhecimento> opcoes)
{
    private readonly OpcoesBaseConhecimento _opcoes = opcoes.Value;

    public async Task<ResultadoAnalise> FundamentarAsync(ResultadoAnalise resultado, string correlacaoId, CancellationToken cancelamento)
    {
        var regraIds = resultado.Achados
            .Select(a => a.RegraId)
            .Where(id => !id.StartsWith("PRE-", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        if (regraIds.Count == 0)
        {
            return resultado;
        }

        var referencias = new List<ReferenciaConhecimento>();
        var limitacoes = new List<string>();
        foreach (var lote in regraIds.Chunk(Math.Max(1, _opcoes.RegrasPorConsulta)))
        {
            var consulta = new ConsultaFundamentacao(lote, resultado.Periodo.Inicio, _opcoes.LimitePorConsulta, correlacaoId);
            var obtido = await baseConhecimento.ConsultarAsync(consulta, cancelamento).ConfigureAwait(false);
            limitacoes.AddRange(obtido.Limitacoes);
            if (!obtido.Disponivel)
            {
                return ComFalha(resultado, limitacoes);
            }

            referencias.AddRange(obtido.Referencias);
        }

        var fundamentacao = regraIds
            .Select(id => new FundamentacaoAchado(
                id,
                referencias.Where(r => r.RegraIds.Contains(id)).DistinctBy(r => r.TrechoId).ToList()))
            .ToList();
        return resultado with
        {
            Fundamentacao = fundamentacao,
            Limitacoes = [.. resultado.Limitacoes, .. limitacoes.Distinct(StringComparer.Ordinal)],
        };
    }

    /// <summary>P4: sem fundamentação a análise não pode ser declarada concluída.</summary>
    private static ResultadoAnalise ComFalha(ResultadoAnalise resultado, List<string> limitacoes) => resultado with
    {
        Status = resultado.Status == StatusAnalise.Concluida ? StatusAnalise.ConcluidaParcial : resultado.Status,
        RevisaoHumana = new RevisaoHumana(true, [.. resultado.RevisaoHumana.Motivos, "Fundamentação na base de conhecimento não realizada."]),
        Limitacoes = [.. resultado.Limitacoes, .. limitacoes.Distinct(StringComparer.Ordinal)],
    };
}
