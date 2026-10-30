using System.Text.Json;
using Api.RAG.Application;
using Api.RAG.Domain;
using Api.RAG.Endpoints.Documentacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.RAG.Endpoints;

/// <summary>Administração da base de conhecimento: ingestão e listagem de documentos (constitution 3.3).</summary>
[ApiController]
[Route("api/v1/documentos")]
[Authorize(Policy = Papeis.PoliticaAdministracao)]
[Produces("application/json")]
[Tags("Documentos")]
public sealed partial class DocumentosController(
    ServicoIngestao servico,
    IRepositorioConhecimento repositorio,
    IOptions<OpcoesIngestao> opcoes,
    ILogger<DocumentosController> logger) : ControllerBase
{
    /// <summary>Ingere um documento: segmenta, gera embeddings e indexa os trechos.</summary>
    /// <remarks>
    /// A combinação `chave` + `versao` identifica o documento (P7). Reenviar o mesmo conteúdo é idempotente (200);
    /// uma nova versão da mesma chave substitui a anterior, que continua identificável.
    ///
    /// O corpo é lido de forma estrita: campos não previstos rejeitam o documento.
    /// </remarks>
    /// <response code="201">Documento novo ingerido.</response>
    /// <response code="200">Mesma chave, versão e conteúdo já ingeridos; nada foi alterado.</response>
    /// <response code="400">Documento fora do contrato ou com tipo inválido.</response>
    /// <response code="409">A versão já foi ingerida com conteúdo diferente.</response>
    /// <response code="413">Documento acima de `Ingestao:TamanhoMaximoBytes`.</response>
    /// <response code="415">Corpo diferente de `application/json`.</response>
    /// <response code="503">Provedor de embeddings ou banco indisponível.</response>
    [HttpPost]
    [CorpoJson(typeof(DocumentoEntradaDto))]
    [ProducesResponseType<ResultadoIngestaoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ResultadoIngestaoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroPadronizado>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroPadronizado>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroPadronizado>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ErroPadronizado>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ErroPadronizado>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ingerir(CancellationToken cancelamento)
    {
        if (!Request.HasJsonContentType())
        {
            return Erro(StatusCodes.Status415UnsupportedMediaType, "TIPO_CONTEUDO_NAO_SUPORTADO", "Envie application/json.");
        }

        var limite = opcoes.Value.TamanhoMaximoBytes;
        var corpo = await CorpoRequisicao.LerLimitadoAsync(Request, limite, cancelamento).ConfigureAwait(false);
        if (corpo is null)
        {
            return Erro(StatusCodes.Status413PayloadTooLarge, "DOCUMENTO_EXCEDE_TAMANHO", $"O documento excede {limite} bytes.");
        }

        DocumentoEntradaDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DocumentoEntradaDto>(corpo, OpcoesJson.Entrada);
        }
        catch (JsonException)
        {
            return Erro(StatusCodes.Status400BadRequest, "CONTRATO_DOCUMENTO_INVALIDO", "JSON inválido ou com campos não previstos.");
        }

        if (dto is null || !CodigosTipoDocumento.TentarDe(dto.Tipo, out var tipo))
        {
            return Erro(StatusCodes.Status400BadRequest, "TIPO_INVALIDO", "Tipos aceitos: MANUAL_OFICIAL, LEGISLACAO, BASE_REGRAS_INTERNA.");
        }

        var comando = new ComandoIngestao(
            dto.Chave, dto.Titulo, tipo, dto.Emissor, dto.Versao, dto.DataPublicacao, dto.VigenciaInicio, dto.VigenciaFim,
            dto.Origem, dto.Obrigacoes, dto.Formato, dto.Conteudo, Correlacao.DaRequisicao(HttpContext));
        try
        {
            var resultado = await servico.IngerirAsync(comando, cancelamento).ConfigureAwait(false);
            LogIngestao(logger, resultado.Documento.Chave, resultado.Documento.Versao, resultado.Situacao, resultado.QuantidadeTrechos);
            var dtoResultado = ResultadoIngestaoDto.De(resultado);
            return resultado.Situacao == SituacaoIngestao.Criado
                ? StatusCode(StatusCodes.Status201Created, dtoResultado)
                : Ok(dtoResultado);
        }
        catch (ErroValidacaoException erro)
        {
            return Erro(StatusCodes.Status400BadRequest, erro.Codigo, erro.Message);
        }
        catch (ConflitoVersaoException erro)
        {
            return Erro(StatusCodes.Status409Conflict, "VERSAO_JA_INGERIDA", erro.Message);
        }
        catch (ProvedorEmbeddingsException erro)
        {
            return Erro(StatusCodes.Status503ServiceUnavailable, erro.Codigo, erro.Message);
        }
        catch (BaseIndisponivelException)
        {
            return BaseIndisponivel();
        }
    }

    /// <summary>Lista os documentos da base, ativos e substituídos.</summary>
    /// <response code="200">Documentos ingeridos.</response>
    /// <response code="503">Banco indisponível.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroPadronizado>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Listar(CancellationToken cancelamento)
    {
        try
        {
            var documentos = await repositorio.ListarAsync(cancelamento).ConfigureAwait(false);
            return Ok(documentos.Select(DocumentoDto.De).ToList());
        }
        catch (BaseIndisponivelException)
        {
            return BaseIndisponivel();
        }
    }

    private ObjectResult BaseIndisponivel() =>
        Erro(StatusCodes.Status503ServiceUnavailable, "BASE_INDISPONIVEL", "Base de conhecimento indisponível.");

    private ObjectResult Erro(int status, string codigo, string mensagem) => StatusCode(status, new ErroPadronizado(codigo, mensagem));

    [LoggerMessage(Level = LogLevel.Information, Message = "Documento {Chave} versão {Versao}: {Situacao}, {Trechos} trechos")]
    private static partial void LogIngestao(ILogger logger, string chave, string versao, SituacaoIngestao situacao, int trechos);
}
