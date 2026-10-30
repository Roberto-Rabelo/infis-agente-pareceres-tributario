using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Contratos;
using Api.Agente.Domain;

namespace Api.Agente.Application.Parecer;

public sealed record DadosParecer(string Json, IReadOnlyDictionary<string, Achado> Achados);

/// <summary>
/// Monta os dados enviados ao LLM com minimização (FR-045, D19 pendente): CNPJ mascarado, evidências sem o valor
/// bruto dos campos e trechos só da fundamentação. Cada achado recebe um <c>achadoId</c> do sistema.
/// </summary>
public static class MontadorDadosParecer
{
    public static DadosParecer Montar(ResultadoAnalise resultado)
    {
        var achados = resultado.Achados
            .Select((a, i) => (Id: "A" + (i + 1).ToString("000", CultureInfo.InvariantCulture), Achado: a))
            .ToList();

        var dados = new JsonObject
        {
            ["contribuinte"] = Mascaramento.Documento(resultado.RaizCnpj),
            ["periodo"] = new JsonObject { ["inicio"] = Data(resultado.Periodo.Inicio), ["fim"] = Data(resultado.Periodo.Fim) },
            ["status"] = Codigo(resultado.Status),
            ["obrigacoes"] = new JsonArray([.. resultado.Cobertura.Select(c => (JsonNode)new JsonObject
            {
                ["obrigacaoId"] = c.ObrigacaoId,
                ["tipo"] = Codigos.De(c.Tipo),
                ["estabelecimento"] = Mascaramento.Documento(c.Cnpj),
                ["periodo"] = $"{Data(c.Periodo.Inicio)}/{Data(c.Periodo.Fim)}",
                ["apta"] = c.Apta,
            })]),
            ["regras"] = new JsonArray([.. resultado.Regras.Select(r => (JsonNode)new JsonObject
            {
                ["regraId"] = r.RegraId,
                ["situacao"] = Codigo(r.Situacao),
                ["obrigatoria"] = r.Obrigatoria,
            })]),
            ["achados"] = new JsonArray([.. achados.Select(a => (JsonNode)Achado(a.Id, a.Achado))]),
            ["trechos"] = new JsonArray([.. resultado.Fundamentacao
                .SelectMany(f => f.Referencias)
                .DistinctBy(r => r.TrechoId)
                .Select(r => (JsonNode)new JsonObject
                {
                    ["trechoId"] = r.TrechoId,
                    ["documento"] = r.DocumentoTitulo,
                    ["tipo"] = r.TipoDocumento,
                    ["versao"] = r.DocumentoVersao,
                    ["secao"] = r.Secao,
                    ["regraIds"] = new JsonArray([.. r.RegraIds.Select(id => (JsonNode)id)]),
                    ["texto"] = r.Texto,
                })]),
            ["limitacoes"] = new JsonArray([.. resultado.Limitacoes.Select(l => (JsonNode)l)]),
            ["revisaoHumana"] = new JsonObject
            {
                ["requerida"] = resultado.RevisaoHumana.Requerida,
                ["motivos"] = new JsonArray([.. resultado.RevisaoHumana.Motivos.Select(m => (JsonNode)m)]),
            },
            ["parametros"] = new JsonObject
            {
                ["tolerancia"] = resultado.Parametros.Tolerancia,
                ["materialidade"] = resultado.Parametros.Materialidade,
            },
        };

        return new DadosParecer(dados.ToJsonString(), achados.ToDictionary(a => a.Id, a => a.Achado, StringComparer.Ordinal));
    }

    public static string Codigo<T>(T valor) where T : struct, Enum => JsonNamingPolicy.SnakeCaseUpper.ConvertName(valor.ToString());

    private static JsonObject Achado(string id, Achado a) => new()
    {
        ["achadoId"] = id,
        ["regraId"] = a.RegraId,
        ["natureza"] = Codigo(a.Natureza),
        ["severidade"] = a.Severidade is { } s ? Codigo(s) : null,
        ["descricao"] = a.Descricao,
        ["valorOrigem"] = a.ValorOrigem,
        ["valorDestino"] = a.ValorDestino,
        ["diferenca"] = a.Diferenca,
        ["toleranciaAplicada"] = a.ToleranciaAplicada,
        ["hipotesesLegitimas"] = new JsonArray([.. a.HipotesesLegitimas.Select(h => (JsonNode)h)]),
        ["obrigacoesFaltantes"] = new JsonArray([.. a.ObrigacoesFaltantes.Select(f => (JsonNode)new JsonObject
        {
            ["tipo"] = Codigos.De(f.Tipo),
            ["periodo"] = $"{Data(f.Periodo.Inicio)}/{Data(f.Periodo.Fim)}",
            ["motivo"] = f.Motivo,
        })]),
        ["evidencias"] = new JsonArray([.. a.Evidencias.Select(e => (JsonNode)new JsonObject
        {
            ["obrigacaoId"] = e.ObrigacaoId,
            ["registro"] = e.Registro,
            ["ocorrencia"] = e.Ocorrencia,
            ["campo"] = e.Campo,
            ["linha"] = e.Linha,
        })]),
    };

    private static string Data(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
