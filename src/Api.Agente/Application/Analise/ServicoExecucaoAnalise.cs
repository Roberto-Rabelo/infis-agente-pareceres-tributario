using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Application.Analise;

public sealed record ResultadoExecucaoAnalise(
    LoteLido? Lote,
    ResultadoAnalise? Resultado,
    ParecerLlm? Parecer,
    IReadOnlyList<ReferenciaConhecimento> TrechosCitados,
    IReadOnlyList<RegistroConsumoLlm> Consumo,
    ErroLote? Erro);

/// <summary>Etapas 2 a 9 do fluxo (constitution 3.4), compartilhado por REST e A2A.</summary>
public sealed partial class ServicoExecucaoAnalise(
    LeitorLote leitor,
    ServicoAnalise analise,
    ServicoFundamentacao fundamentacao,
    ServicoParecer parecer,
    ILogger<ServicoExecucaoAnalise> logger)
{
    public string VersaoCatalogo => analise.VersaoCatalogo;

    public async Task<ResultadoExecucaoAnalise> ExecutarAsync(
        ReadOnlyMemory<byte> corpo,
        string execucaoId,
        string correlacaoId,
        CancellationToken cancelamento)
    {
        var leitura = leitor.Ler(corpo.Span);
        if (leitura.Lote is not { } lote)
        {
            LogLoteRejeitado(logger, leitura.Erro!.Codigo);
            return new ResultadoExecucaoAnalise(null, null, null, [], [], leitura.Erro);
        }

        var fundamentado = await fundamentacao
            .FundamentarAsync(analise.Analisar(lote), correlacaoId, cancelamento)
            .ConfigureAwait(false);
        var gerado = await parecer
            .GerarAsync(fundamentado, execucaoId, correlacaoId, cancelamento)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            var raizMascarada = Mascaramento.Documento(lote.RaizCnpj);
            LogAnalise(logger, raizMascarada, lote.Obrigacoes.Count, gerado.Resultado.Status, gerado.Resultado.Achados.Count);
        }
        return new ResultadoExecucaoAnalise(lote, gerado.Resultado, gerado.Parecer, gerado.TrechosCitados, gerado.Consumo, null);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lote rejeitado: {Codigo}")]
    private static partial void LogLoteRejeitado(ILogger logger, string codigo);

    [LoggerMessage(Level = LogLevel.Information, Message = "Análise concluída para contribuinte {Raiz}: {Obrigacoes} obrigações, status {Status}, {Achados} achados")]
    private static partial void LogAnalise(ILogger logger, string raiz, int obrigacoes, StatusAnalise status, int achados);
}
