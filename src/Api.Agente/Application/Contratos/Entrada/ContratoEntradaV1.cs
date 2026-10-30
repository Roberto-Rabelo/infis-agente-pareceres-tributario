using System.Text.Json;

namespace Api.Agente.Application.Contratos.Entrada;

#pragma warning disable CA1002, CA2227 // DTOs de desserialização do contrato publicado em contracts/entrada/lote.v1.schema.json.

public sealed class LoteEntradaDto
{
    public required string VersaoContrato { get; init; }

    public string? LoteId { get; init; }

    public required ContribuinteLoteDto Contribuinte { get; init; }

    public required PeriodoDto Periodo { get; init; }

    public required List<JsonElement> Obrigacoes { get; init; }
}

public sealed class ContribuinteLoteDto
{
    public required string Cnpj { get; init; }
}

public sealed class PeriodoDto
{
    public required DateOnly Inicio { get; init; }

    public required DateOnly Fim { get; init; }
}

public sealed class ObrigacaoDto
{
    public required string Id { get; init; }

    public required string Tipo { get; init; }

    public string? VersaoLayout { get; init; }

    public required ContribuinteObrigacaoDto Contribuinte { get; init; }

    public required PeriodoDto Periodo { get; init; }

    public required string Finalidade { get; init; }

    public required IdentificacaoDto Identificacao { get; init; }

    public ValidacaoOrigemDto? ValidacaoOrigem { get; init; }

    public required Dictionary<string, List<OcorrenciaRegistroDto>> Registros { get; init; }
}

public sealed class ContribuinteObrigacaoDto
{
    public required string Cnpj { get; init; }

    public string? InscricaoEstadual { get; init; }

    public string? Uf { get; init; }
}

public sealed class IdentificacaoDto
{
    public string? Recibo { get; init; }

    public string? ReciboAnterior { get; init; }

    public string? Hash { get; init; }

    public string? ChaveAcesso { get; init; }
}

public sealed class ValidacaoOrigemDto
{
    public required string Agente { get; init; }

    public required string VersaoValidador { get; init; }

    public required DateTimeOffset DataValidacao { get; init; }

    public required string Status { get; init; }

    public required List<OcorrenciaValidacaoDto> Ocorrencias { get; init; }
}

public sealed class OcorrenciaValidacaoDto
{
    public required string Tipo { get; init; }

    public string? Codigo { get; init; }

    public required string Mensagem { get; init; }

    public string? Registro { get; init; }

    public string? Campo { get; init; }

    public long? Linha { get; init; }
}

public sealed class OcorrenciaRegistroDto
{
    public long? Linha { get; init; }

    public required Dictionary<string, string?> Campos { get; init; }
}

#pragma warning restore CA1002, CA2227
