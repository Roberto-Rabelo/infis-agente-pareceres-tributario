using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Agente.Application.Analise;
using Api.Agente.Domain;
using Json.Schema;

namespace Api.Agente.Application.Parecer;

public sealed record ErroValidacaoParecer(string Codigo, string Mensagem);

public sealed record ResultadoValidacaoParecer(ParecerLlm? Parecer, IReadOnlyList<ErroValidacaoParecer> Erros)
{
    public bool Valido => Parecer is not null && Erros.Count == 0;
}

/// <summary>Validação estrutural (schema) e semântica (P2, P3, P4, P9, P15) da resposta do LLM.</summary>
public sealed partial class ValidadorParecer(JsonSchema schema)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    // O JsonSchema.Net registra o $id em um registro global; carregar o mesmo arquivo duas vezes no processo falha.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<JsonSchema>> Carregados = new(StringComparer.OrdinalIgnoreCase);

    public static ValidadorParecer DoArquivo(string caminho) =>
        new(Carregados.GetOrAdd(Path.GetFullPath(caminho), p => new Lazy<JsonSchema>(() => JsonSchema.FromFile(p))).Value);

    public ResultadoValidacaoParecer Validar(string? resposta, ResultadoAnalise analise, DadosParecer dados, IReadOnlySet<string> trechosRecuperados)
    {
        if (string.IsNullOrWhiteSpace(resposta))
        {
            return Falha("RESPOSTA_VAZIA", "O modelo não retornou conteúdo.");
        }

        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(resposta);
        }
        catch (JsonException)
        {
            return Falha("JSON_INVALIDO", "A resposta não é um JSON válido.");
        }

        using (documento)
        {
            var avaliacao = schema.Evaluate(documento.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!avaliacao.IsValid)
            {
                var detalhes = (avaliacao.Details ?? [])
                    .Where(d => d.Errors is { Count: > 0 })
                    .Select(d => $"{d.InstanceLocation}: {string.Join("; ", d.Errors!.Values)}")
                    .Take(10);
                return Falha("SCHEMA_INVALIDO", "Resposta fora do contrato parecer 1.0.0: " + string.Join(" | ", detalhes));
            }

            ParecerLlm? parecer;
            try
            {
                parecer = documento.RootElement.Deserialize<ParecerLlm>(Json);
            }
            catch (JsonException)
            {
                return Falha("SCHEMA_INVALIDO", "Resposta fora do contrato parecer 1.0.0.");
            }

            var erros = ValidarSemantica(parecer!, analise, dados, trechosRecuperados);
            return new ResultadoValidacaoParecer(erros.Count == 0 ? parecer : null, erros);
        }
    }

    private static List<ErroValidacaoParecer> ValidarSemantica(
        ParecerLlm parecer, ResultadoAnalise analise, DadosParecer dados, IReadOnlySet<string> trechos)
    {
        var erros = new List<ErroValidacaoParecer>();
        var status = MontadorDadosParecer.Codigo(analise.Status);
        if (parecer.Status != status)
        {
            erros.Add(new("STATUS_DIVERGENTE", $"O status deve ser {status}, determinado pelo sistema."));
        }

        foreach (var duplicado in parecer.Achados.GroupBy(a => a.AchadoId).Where(g => g.Count() > 1))
        {
            erros.Add(new("ACHADO_DUPLICADO", $"Achado {duplicado.Key} repetido."));
        }

        foreach (var (id, esperado) in dados.Achados)
        {
            var recebido = parecer.Achados.FirstOrDefault(a => a.AchadoId == id);
            if (recebido is null)
            {
                erros.Add(new("ACHADO_OMITIDO", $"Achado {id} ({esperado.RegraId}) não consta do parecer."));
                continue;
            }

            var severidade = esperado.Severidade is { } s ? MontadorDadosParecer.Codigo(s) : null;
            if (recebido.RegraId != esperado.RegraId || recebido.Natureza != MontadorDadosParecer.Codigo(esperado.Natureza) || recebido.Severidade != severidade)
            {
                erros.Add(new("ACHADO_RECLASSIFICADO", $"Achado {id}: regra, natureza e severidade devem ser {esperado.RegraId}, {MontadorDadosParecer.Codigo(esperado.Natureza)}, {severidade ?? "null"}."));
            }

            var temTrecho = analise.Fundamentacao.Any(f => f.RegraId == esperado.RegraId && f.Referencias.Count > 0);
            if (esperado.Natureza == Natureza.InconsistenciaCruzamento && temTrecho && recebido.Referencias.Count == 0)
            {
                erros.Add(new("ACHADO_SEM_REFERENCIA", $"Achado {id}: cite ao menos um trecho recebido para {esperado.RegraId}."));
            }
        }

        foreach (var extra in parecer.Achados.Where(a => !dados.Achados.ContainsKey(a.AchadoId)))
        {
            erros.Add(new("ACHADO_NAO_RECEBIDO", $"Achado {extra.AchadoId} não foi produzido pelos validadores; use indiciosIa."));
        }

        foreach (var indicio in parecer.IndiciosIa.Where(i => i.Referencias.Count == 0))
        {
            erros.Add(new("INDICIO_SEM_REFERENCIA", $"Indício sem trecho de apoio: {Resumo(indicio.Descricao)}."));
        }

        var citadas = parecer.Achados.SelectMany(a => a.Referencias)
            .Concat(parecer.IndiciosIa.SelectMany(i => i.Referencias))
            .Concat(parecer.FontesCitadas)
            .Distinct(StringComparer.Ordinal);
        foreach (var inexistente in citadas.Where(r => !trechos.Contains(r)))
        {
            erros.Add(new("REFERENCIA_INEXISTENTE", $"Referência {inexistente} não foi recuperada nesta execução."));
        }

        if (!parecer.RevisaoHumana.Requerida && (analise.RevisaoHumana.Requerida || parecer.IndiciosIa.Count > 0))
        {
            erros.Add(new("REVISAO_HUMANA_OMITIDA", "A revisão humana é obrigatória nesta análise (P15)."));
        }

        var textos = new[] { parecer.Identificacao, parecer.Sintese }
            .Concat(parecer.Achados.Select(a => a.Analise))
            .Concat(parecer.IndiciosIa.Select(i => i.Descricao))
            .Concat(parecer.Limitacoes);
        foreach (var termo in textos.SelectMany(t => AfirmacaoProibida().Matches(t)).Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            erros.Add(new("AFIRMACAO_PROIBIDA", $"Remova \"{termo}\": o parecer não declara conformidade, cobertura total nem consequência jurídica."));
        }

        return erros;
    }

    private static string Resumo(string texto) => texto.Length <= 80 ? texto : texto[..80] + "…";

    /// <summary>Conformidade (P4), cobertura e limitações são fatos do sistema; consequências jurídicas não constam dos dados (P1).</summary>
    [GeneratedRegex(
        @"\b(em conformidade|conformidade fiscal|est[aá] conforme|est[aã]o conformes|situa[cç][aã]o regular|regularidade fiscal|inadimpl\w*|penalidades?|multas?|san[cç][oõ]es|fraude|sonega[cç][aã]o|todas as regras|n[aã]o h[aá] limita[cç][oõ]es|sem limita[cç][oõ]es|nenhuma limita[cç][aã]o)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AfirmacaoProibida();

    private static ResultadoValidacaoParecer Falha(string codigo, string mensagem) => new(null, [new(codigo, mensagem)]);
}
