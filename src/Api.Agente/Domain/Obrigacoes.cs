namespace Api.Agente.Domain;

public sealed record OcorrenciaRegistro(long? Linha, IReadOnlyDictionary<string, string?> Campos);

public sealed record Identificacao(string? Recibo, string? ReciboAnterior, string? Hash, string? ChaveAcesso);

public sealed record OcorrenciaOrigem(
    TipoOcorrenciaOrigem Tipo,
    string? Codigo,
    string Mensagem,
    string? Registro,
    string? Campo,
    long? Linha);

public sealed record ValidacaoOrigem(
    string Agente,
    string VersaoValidador,
    DateTimeOffset DataValidacao,
    StatusValidacaoOrigem Status,
    IReadOnlyList<OcorrenciaOrigem> Ocorrencias);

/// <summary>Obrigação normalizada recebida do agente de origem (constitution P18).</summary>
public sealed record ObrigacaoRecebida(
    string Id,
    TipoObrigacao Tipo,
    string? VersaoLayout,
    Cnpj Cnpj,
    string? InscricaoEstadual,
    string? Uf,
    Periodo Periodo,
    Finalidade Finalidade,
    Identificacao Identificacao,
    ValidacaoOrigem? ValidacaoOrigem,
    IReadOnlyDictionary<string, IReadOnlyList<OcorrenciaRegistro>> Registros)
{
    public bool EhDocumento => Tipo is TipoObrigacao.NFe or TipoObrigacao.NFCe or TipoObrigacao.NFSe;

    public IReadOnlyList<OcorrenciaRegistro> Registro(string codigo) =>
        Registros.TryGetValue(codigo, out var ocorrencias) ? ocorrencias : [];
}
