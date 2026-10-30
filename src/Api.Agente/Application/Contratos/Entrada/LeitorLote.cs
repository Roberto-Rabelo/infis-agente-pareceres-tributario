using System.Text.Json;
using Api.Agente.Domain;

namespace Api.Agente.Application.Contratos.Entrada;

public sealed record ErroLote(string Codigo, string Mensagem);

public sealed record ObrigacaoBruta(int Indice, JsonElement Conteudo);

public sealed record LoteLido(string VersaoContrato, string? LoteId, string RaizCnpj, Periodo Periodo, IReadOnlyList<ObrigacaoBruta> Obrigacoes);

public sealed record ResultadoLeitura(LoteLido? Lote, ErroLote? Erro);

/// <summary>Leitura estrita do envelope do lote. Erros aqui rejeitam o lote inteiro (FR-002).</summary>
public sealed class LeitorLote
{
    public const int VersaoMajorSuportada = 1;

    private readonly OpcoesEntrada _opcoes;
    private readonly JsonSerializerOptions _json;

    public LeitorLote(OpcoesEntrada opcoes)
    {
        _opcoes = opcoes;
        _json = OpcoesJsonEntrada.Criar(opcoes.ProfundidadeMaximaJson);
    }

    public JsonSerializerOptions OpcoesJson => _json;

    public ResultadoLeitura Ler(ReadOnlySpan<byte> corpo)
    {
        LoteEntradaDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<LoteEntradaDto>(corpo, _json);
        }
        catch (JsonException ex)
        {
            return Falha("CONTRATO_LOTE_INVALIDO", $"Lote não atende ao contrato de entrada (caminho: {ex.Path ?? "$"}).");
        }

        if (dto is null)
        {
            return Falha("CONTRATO_LOTE_INVALIDO", "Lote vazio.");
        }

        if (!VersaoSuportada(dto.VersaoContrato))
        {
            return Falha("VERSAO_CONTRATO_NAO_SUPORTADA", $"Versão do contrato de entrada não suportada. Suportada: {VersaoMajorSuportada}.x.x.");
        }

        if (dto.Obrigacoes.Count == 0)
        {
            return Falha("LOTE_SEM_OBRIGACOES", "O lote não contém obrigações.");
        }

        if (dto.Obrigacoes.Count > _opcoes.MaximoObrigacoesPorLote)
        {
            return Falha("LOTE_EXCEDE_LIMITE", $"O lote excede o máximo de {_opcoes.MaximoObrigacoesPorLote} obrigações.");
        }

        if (!Cnpj.TentarObterRaiz(dto.Contribuinte.Cnpj, out var raiz, out var erroCnpj))
        {
            return Falha("CONTRIBUINTE_INVALIDO", erroCnpj!);
        }

        if (!Periodo.TentarCriar(dto.Periodo.Inicio, dto.Periodo.Fim, out var periodo))
        {
            return Falha("PERIODO_INVALIDO", "Fim do período do lote anterior ao início.");
        }

        var obrigacoes = dto.Obrigacoes.Select((elemento, indice) => new ObrigacaoBruta(indice, elemento)).ToList();
        return new ResultadoLeitura(new LoteLido(dto.VersaoContrato, dto.LoteId, raiz, periodo!, obrigacoes), null);
    }

    private static bool VersaoSuportada(string versao)
    {
        var partes = versao.Split('.');
        return partes.Length == 3
            && partes.All(p => p.Length > 0 && p.All(char.IsAsciiDigit))
            && int.TryParse(partes[0], out var major)
            && major == VersaoMajorSuportada;
    }

    private static ResultadoLeitura Falha(string codigo, string mensagem) => new(null, new ErroLote(codigo, mensagem));
}

public static class OpcoesJsonEntrada
{
    public static JsonSerializerOptions Criar(int profundidadeMaxima) => new(JsonSerializerDefaults.Web)
    {
        MaxDepth = profundidadeMaxima,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
    };
}
