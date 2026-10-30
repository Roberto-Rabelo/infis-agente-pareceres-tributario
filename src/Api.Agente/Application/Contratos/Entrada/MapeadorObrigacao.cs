using System.Text.Json;
using Api.Agente.Domain;
using Api.Agente.Domain.Casamento;

namespace Api.Agente.Application.Contratos.Entrada;

/// <param name="Id">Identificador informado pela origem, ou <c>#posição</c> no lote quando ausente.</param>
/// <param name="Tipo">Preenchido quando o tipo informado é reconhecido, mesmo que a obrigação seja inválida (cobertura).</param>
/// <param name="Obrigacao">Obrigação convertida; nula quando o contrato não foi atendido.</param>
/// <param name="Erros">Violações do contrato da obrigação (PRE-001).</param>
public sealed record ObrigacaoMapeada(string Id, TipoObrigacao? Tipo, ObrigacaoRecebida? Obrigacao, IReadOnlyList<string> Erros)
{
    public bool Valida => Obrigacao is not null;
}

/// <summary>Converte uma obrigação do contrato v1 para o domínio. Erros aqui são PRE-001 (contrato), nunca rejeitam o lote.</summary>
public static class MapeadorObrigacao
{
    public static ObrigacaoMapeada Mapear(ObrigacaoBruta bruta, JsonSerializerOptions opcoes)
    {
        var idInformado = LerTexto(bruta.Conteudo, "id") ?? $"#{bruta.Indice}";
        TipoObrigacao? tipoInformado = Codigos.TentarTipoObrigacao(LerTexto(bruta.Conteudo, "tipo"), out var t) ? t : null;

        ObrigacaoDto? dto;
        try
        {
            dto = bruta.Conteudo.Deserialize<ObrigacaoDto>(opcoes);
        }
        catch (JsonException ex)
        {
            return new ObrigacaoMapeada(idInformado, tipoInformado, null, [$"Estrutura inválida (caminho: {ex.Path ?? "$"})."]);
        }

        if (dto is null)
        {
            return new ObrigacaoMapeada(idInformado, tipoInformado, null, ["Obrigação vazia."]);
        }

        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            erros.Add("Identificador da obrigação não informado.");
        }

        if (tipoInformado is null)
        {
            erros.Add("Tipo de obrigação desconhecido.");
        }

        if (!Cnpj.TentarCriar(dto.Contribuinte.Cnpj, out var cnpj, out var erroCnpj))
        {
            erros.Add(erroCnpj!);
        }

        if (!Periodo.TentarCriar(dto.Periodo.Inicio, dto.Periodo.Fim, out var periodo))
        {
            erros.Add("Fim do período anterior ao início.");
        }

        if (!Codigos.TentarFinalidade(dto.Finalidade, out var finalidade))
        {
            erros.Add("Finalidade deve ser ORIGINAL ou SUBSTITUTA.");
        }

        var identificacao = MapearIdentificacao(dto.Identificacao, tipoInformado, erros);
        var validacao = MapearValidacao(dto.ValidacaoOrigem, erros);

        if (erros.Count > 0)
        {
            return new ObrigacaoMapeada(idInformado, tipoInformado, null, erros);
        }

        var registros = dto.Registros.ToDictionary(
            par => par.Key,
            par => (IReadOnlyList<OcorrenciaRegistro>)par.Value
                .Select(o => new OcorrenciaRegistro(o.Linha, o.Campos))
                .ToList(),
            StringComparer.Ordinal);

        var obrigacao = new ObrigacaoRecebida(
            dto.Id,
            tipoInformado!.Value,
            string.IsNullOrWhiteSpace(dto.VersaoLayout) ? null : dto.VersaoLayout.Trim(),
            cnpj,
            dto.Contribuinte.InscricaoEstadual,
            dto.Contribuinte.Uf,
            periodo!,
            finalidade,
            identificacao,
            validacao,
            registros);
        return new ObrigacaoMapeada(dto.Id, tipoInformado, obrigacao, []);
    }

    private static Identificacao MapearIdentificacao(IdentificacaoDto dto, TipoObrigacao? tipo, List<string> erros)
    {
        string? chave = null;
        if (dto.ChaveAcesso is not null && !ChaveAcesso.TentarNormalizar(dto.ChaveAcesso, out chave))
        {
            erros.Add("Chave de acesso deve ter 44 posições alfanuméricas.");
        }

        var ehDocumento = tipo is TipoObrigacao.NFe or TipoObrigacao.NFCe or TipoObrigacao.NFSe;
        if (ehDocumento && dto.ChaveAcesso is null)
        {
            erros.Add("Documento fiscal sem chave de acesso.");
        }

        if (tipo is not null && !ehDocumento && string.IsNullOrWhiteSpace(dto.Recibo) && string.IsNullOrWhiteSpace(dto.Hash))
        {
            erros.Add("Escrituração sem recibo nem hash.");
        }

        return new Identificacao(dto.Recibo, dto.ReciboAnterior, dto.Hash, chave);
    }

    private static ValidacaoOrigem? MapearValidacao(ValidacaoOrigemDto? dto, List<string> erros)
    {
        if (dto is null)
        {
            return null;
        }

        if (!Codigos.TentarStatusValidacao(dto.Status, out var status))
        {
            erros.Add("Status da validação de origem deve ser APROVADO ou REPROVADO.");
        }

        var ocorrencias = new List<OcorrenciaOrigem>();
        foreach (var ocorrencia in dto.Ocorrencias)
        {
            if (!Codigos.TentarTipoOcorrencia(ocorrencia.Tipo, out var tipo))
            {
                erros.Add("Tipo de ocorrência da validação de origem deve ser ERRO ou ADVERTENCIA.");
                continue;
            }

            ocorrencias.Add(new OcorrenciaOrigem(tipo, ocorrencia.Codigo, ocorrencia.Mensagem, ocorrencia.Registro, ocorrencia.Campo, ocorrencia.Linha));
        }

        return new ValidacaoOrigem(dto.Agente, dto.VersaoValidador, dto.DataValidacao, status, ocorrencias);
    }

    private static string? LerTexto(JsonElement elemento, string propriedade) =>
        elemento.ValueKind == JsonValueKind.Object
        && elemento.TryGetProperty(propriedade, out var valor)
        && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
}
