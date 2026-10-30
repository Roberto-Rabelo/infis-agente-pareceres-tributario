using System.Text.Json;
using System.Text.Json.Serialization;
using Api.RAG.Application;
using Api.RAG.Domain;

namespace Api.RAG.Endpoints;

public sealed record ErroPadronizado(string Codigo, string Mensagem);

public sealed record DocumentoEntradaDto
{
    public required string Chave { get; init; }

    public required string Titulo { get; init; }

    public required string Tipo { get; init; }

    public required string Emissor { get; init; }

    public required string Versao { get; init; }

    public DateOnly? DataPublicacao { get; init; }

    public DateOnly? VigenciaInicio { get; init; }

    public DateOnly? VigenciaFim { get; init; }

    public required string Origem { get; init; }

    public IReadOnlyList<string> Obrigacoes { get; init; } = [];

    public required string Formato { get; init; }

    public required string Conteudo { get; init; }
}

public sealed record DocumentoDto(
    Guid Id,
    string Chave,
    string Titulo,
    string Tipo,
    string Emissor,
    string Versao,
    DateOnly? DataPublicacao,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    string Origem,
    string HashConteudo,
    IReadOnlyList<string> Obrigacoes,
    string Status,
    string ModeloEmbedding,
    DateTimeOffset IngeridoEm,
    DateTimeOffset? SubstituidoEm)
{
    public static DocumentoDto De(Documento d) => new(
        d.Id,
        d.Chave,
        d.Titulo,
        CodigosTipoDocumento.De(d.Tipo),
        d.Emissor,
        d.Versao,
        d.DataPublicacao,
        d.VigenciaInicio,
        d.VigenciaFim,
        d.Origem,
        d.HashConteudo,
        d.Obrigacoes,
        d.Status == StatusDocumento.Ativo ? "ATIVO" : "SUBSTITUIDO",
        d.ModeloEmbedding,
        d.IngeridoEm,
        d.SubstituidoEm);
}

public sealed record ResultadoIngestaoDto(string Situacao, DocumentoDto Documento, int Trechos, Guid? SubstituiuDocumentoId)
{
    public static ResultadoIngestaoDto De(ResultadoIngestao r) => new(
        r.Situacao == SituacaoIngestao.Criado ? "CRIADO" : "JA_EXISTENTE",
        DocumentoDto.De(r.Documento),
        r.QuantidadeTrechos,
        r.SubstituiuDocumentoId);
}

/// <summary>Saída versionada da ferramenta MCP <c>consultar_base_conhecimento</c>.</summary>
public sealed record RespostaConsultaMcp(
    string Versao,
    IReadOnlyList<TrechoMcp> Trechos,
    IReadOnlyList<string> Limitacoes,
    ConsumoMcp? Consumo,
    ErroPadronizado? Erro)
{
    public const string VersaoAtual = "1.0.0";

    public static RespostaConsultaMcp De(ResultadoConsulta r) => new(
        VersaoAtual,
        r.Trechos.Select(TrechoMcp.De).ToList(),
        r.Limitacoes,
        r.Consumo is { } c ? new ConsumoMcp(c.Provedor, c.Modelo, c.QuantidadeTextos, c.TokensEntrada) : null,
        null);

    public static RespostaConsultaMcp Falha(string codigo, string mensagem) => new(VersaoAtual, [], [], null, new ErroPadronizado(codigo, mensagem));
}

public sealed record TrechoMcp(
    Guid TrechoId,
    FonteMcp Fonte,
    string Secao,
    int LinhaInicio,
    int LinhaFim,
    string Texto,
    IReadOnlyList<string> RegraIds,
    string? RegraPrincipal,
    IReadOnlyList<string> Obrigacoes,
    string Motivo,
    double? Similaridade)
{
    public static TrechoMcp De(TrechoEncontrado e) => new(
        e.TrechoId,
        new FonteMcp(
            e.Documento.Id,
            e.Documento.Chave,
            e.Documento.Titulo,
            CodigosTipoDocumento.De(e.Documento.Tipo),
            e.Documento.Emissor,
            e.Documento.Versao,
            e.Documento.Origem,
            e.Documento.HashConteudo,
            e.Documento.VigenciaInicio,
            e.Documento.VigenciaFim),
        e.Trecho.Secao,
        e.Trecho.LinhaInicio,
        e.Trecho.LinhaFim,
        e.Trecho.Texto,
        e.Trecho.RegraIds,
        e.Trecho.RegraPrincipal,
        e.Trecho.Obrigacoes,
        e.Motivo == MotivoRecuperacao.Regra ? "REGRA" : "SEMANTICA",
        e.Similaridade);
}

public sealed record FonteMcp(
    Guid DocumentoId,
    string Chave,
    string Titulo,
    string Tipo,
    string Emissor,
    string Versao,
    string Origem,
    string Hash,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim);

public sealed record ConsumoMcp(string Provedor, string Modelo, int QuantidadeTextos, long? TokensEntrada);

public static class OpcoesJson
{
    public static readonly JsonSerializerOptions Entrada = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16,
    };
}
