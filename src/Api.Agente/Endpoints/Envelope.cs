using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Parecer;
using Api.Agente.Application.Contratos;
using Api.Agente.Domain;

namespace Api.Agente.Endpoints;

/// <summary>Envelope da API (constitution 4.4). Todos os campos são preenchidos pelo sistema (P10).</summary>
public sealed record EnvelopeAnalise(
    string VersaoEnvelope,
    string? VersaoContratoEntrada,
    string VersaoCatalogo,
    string ExecucaoId,
    string CorrelacaoId,
    string StatusTecnico,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    long DuracaoMs,
    ResultadoAnalise? Resultado,
    ErroPadronizado? Erro,
    ParecerLlm? Parecer,
    IReadOnlyList<RegistroConsumoLlm> Consumo)
{
    /// <summary>1.1.0: acrescenta versões do contrato do parecer e do prompt e os trechos citados (compatível com 1.0.0).</summary>
    public const string Versao = "1.1.0";

    public string? VersaoContratoParecer { get; init; }

    public string? VersaoPrompt { get; init; }

    /// <summary>Trechos efetivamente citados pelo parecer, resolvidos pelo sistema (P9, SC-005).</summary>
    public IReadOnlyList<ReferenciaConhecimento> TrechosCitados { get; init; } = [];
}

public sealed record ErroPadronizado(string Codigo, string Mensagem);

public static class StatusTecnico
{
    public const string Processado = "PROCESSADO";
    public const string Rejeitado = "REJEITADO";
    public const string Erro = "ERRO";
}

public static class OpcoesJsonSaida
{
    public static void Configurar(JsonSerializerOptions opcoes)
    {
        opcoes.Converters.Add(new ConversorTipoObrigacao());
        opcoes.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
        opcoes.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    }

    private sealed class ConversorTipoObrigacao : JsonConverter<TipoObrigacao>
    {
        public override TipoObrigacao Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Codigos.TentarTipoObrigacao(reader.GetString(), out var tipo) ? tipo : throw new JsonException("Tipo de obrigação desconhecido.");

        public override void Write(Utf8JsonWriter writer, TipoObrigacao value, JsonSerializerOptions options) =>
            writer.WriteStringValue(Codigos.De(value));

        public override TipoObrigacao ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Read(ref reader, typeToConvert, options);

        public override void WriteAsPropertyName(Utf8JsonWriter writer, TipoObrigacao value, JsonSerializerOptions options) =>
            writer.WritePropertyName(Codigos.De(value));
    }
}
