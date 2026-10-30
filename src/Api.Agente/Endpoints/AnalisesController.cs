using Api.Agente.Application.Analise;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Api.Agente.Endpoints.Documentacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Agente.Endpoints;

/// <summary>Análise de cruzamento entre obrigações fiscais de um mesmo contribuinte e período.</summary>
[ApiController]
[Route("api/v1/analises")]
[Authorize]
[Produces("application/json")]
[Tags("Análises")]
public sealed partial class AnalisesController(
    LeitorLote leitor,
    ServicoAnalise servico,
    ServicoFundamentacao fundamentacao,
    ServicoParecer parecer,
    IOptions<OpcoesParecer> opcoesParecer,
    IOptions<OpcoesEntrada> opcoesEntrada,
    ILogger<AnalisesController> logger,
    TimeProvider relogio) : ControllerBase
{
    /// <summary>Analisa um lote de obrigações e emite o parecer técnico.</summary>
    /// <remarks>
    /// Fluxo da constitution 3.4: contrato de entrada, pré-condições PRE-001 a PRE-005, regras de cruzamento do catálogo,
    /// fundamentação na base de conhecimento (MCP) e, quando habilitado, parecer do LLM validado contra
    /// `contracts/saida/parecer.v1.schema.json`.
    ///
    /// O corpo segue `contracts/entrada/lote.v1.schema.json` e é lido de forma estrita: campos não previstos,
    /// comentários e números como texto rejeitam o lote inteiro.
    ///
    /// Obrigação inválida não rejeita o lote: ela aparece em `resultado.aptidao` e gera achado de pré-condição.
    /// </remarks>
    /// <response code="200">Lote processado. O envelope traz os resultados determinísticos e, quando gerado, o parecer validado.</response>
    /// <response code="400">Lote rejeitado: envelope fora do contrato, versão não suportada, sem obrigações, contribuinte ou período inválido.</response>
    /// <response code="413">Lote acima de `Entrada:TamanhoMaximoBytes`.</response>
    [HttpPost]
    [CorpoJson(typeof(LoteEntradaDto), Exemplo = "exemplos/lote-exemplo.json")]
    [ProducesResponseType<EnvelopeAnalise>(StatusCodes.Status200OK)]
    [ProducesResponseType<EnvelopeAnalise>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<EnvelopeAnalise>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Analisar(CancellationToken cancelamento)
    {
        var inicio = relogio.GetUtcNow();
        var limite = opcoesEntrada.Value.TamanhoMaximoBytes;
        var corpo = await CorpoRequisicao.LerLimitadoAsync(Request, limite, cancelamento).ConfigureAwait(false);
        if (corpo is null)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, Envelope(inicio, null, StatusTecnico.Rejeitado, null,
                new ErroPadronizado("LOTE_EXCEDE_TAMANHO", $"O lote excede {limite} bytes.")));
        }

        var leitura = leitor.Ler(corpo);
        if (leitura.Lote is not { } lote)
        {
            LogLoteRejeitado(logger, leitura.Erro!.Codigo);
            return BadRequest(Envelope(inicio, null, StatusTecnico.Rejeitado, null,
                new ErroPadronizado(leitura.Erro!.Codigo, leitura.Erro.Mensagem)));
        }

        var correlacao = Correlacao.DaRequisicao(HttpContext);
        var fundamentado = await fundamentacao
            .FundamentarAsync(servico.Analisar(lote), correlacao, cancelamento)
            .ConfigureAwait(false);
        var gerado = await parecer
            .GerarAsync(fundamentado, Correlacao.Execucao(HttpContext), correlacao, cancelamento)
            .ConfigureAwait(false);
        var resultado = gerado.Resultado;
        var raizMascarada = Mascaramento.Documento(lote.RaizCnpj);
        LogAnalise(logger, raizMascarada, lote.Obrigacoes.Count, resultado.Status, resultado.Achados.Count);
        return Ok(Envelope(inicio, lote.VersaoContrato, StatusTecnico.Processado, resultado, null) with
        {
            Parecer = gerado.Parecer,
            Consumo = gerado.Consumo,
            VersaoContratoParecer = gerado.Parecer is null ? null : ParecerLlm.VersaoContrato,
            VersaoPrompt = gerado.Parecer is null ? null : opcoesParecer.Value.VersaoPrompt,
            TrechosCitados = gerado.TrechosCitados,
        });
    }

    private EnvelopeAnalise Envelope(
        DateTimeOffset inicio,
        string? versaoContrato,
        string statusTecnico,
        ResultadoAnalise? resultado,
        ErroPadronizado? erro)
    {
        var fim = relogio.GetUtcNow();
        return new EnvelopeAnalise(
            EnvelopeAnalise.Versao,
            versaoContrato,
            servico.VersaoCatalogo,
            Correlacao.Execucao(HttpContext),
            Correlacao.DaRequisicao(HttpContext),
            statusTecnico,
            inicio,
            fim,
            (long)(fim - inicio).TotalMilliseconds,
            resultado,
            erro,
            Parecer: null,
            Consumo: []);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lote rejeitado: {Codigo}")]
    private static partial void LogLoteRejeitado(ILogger logger, string codigo);

    [LoggerMessage(Level = LogLevel.Information, Message = "Análise concluída para contribuinte {Raiz}: {Obrigacoes} obrigações, status {Status}, {Achados} achados")]
    private static partial void LogAnalise(ILogger logger, string raiz, int obrigacoes, StatusAnalise status, int achados);
}
