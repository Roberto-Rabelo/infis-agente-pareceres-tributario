using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Contratos;
using Microsoft.SemanticKernel;

namespace Api.Agente.Application.Parecer;

/// <summary>Estado de uma execução de parecer: trechos recuperados (para validar referências) e consumo de embeddings.</summary>
public sealed class ContextoExecucaoParecer(string execucaoId, string correlacaoId, DateOnly dataReferencia)
{
    public string ExecucaoId { get; } = execucaoId;

    public string CorrelacaoId { get; } = correlacaoId;

    public DateOnly DataReferencia { get; } = dataReferencia;

    public Dictionary<string, ReferenciaConhecimento> Trechos { get; } = new(StringComparer.Ordinal);

    public List<(ConsumoEmbeddings Consumo, DateTimeOffset Inicio, DateTimeOffset Fim)> ConsumoEmbeddings { get; } = [];

    public void Registrar(IEnumerable<ReferenciaConhecimento> referencias)
    {
        foreach (var referencia in referencias)
        {
            Trechos.TryAdd(referencia.TrechoId, referencia);
        }
    }
}

/// <summary>
/// Plugin SK de consulta à base de conhecimento (somente leitura, via MCP). Os argumentos são validados aqui, sem
/// confiar no schema comunicado ao modelo; a autorização e o limite de chamadas ficam com o orquestrador (P11).
/// </summary>
public sealed partial class PluginBaseConhecimento(IBaseConhecimento baseConhecimento, ContextoExecucaoParecer contexto, int limiteTrechos, TimeProvider relogio)
{
    public const string Nome = "base_conhecimento";
    public const string Funcao = "consultar";
    public const string NomeCompleto = Nome + "-" + Funcao;
    private const int MaximoPergunta = 500;
    private const int MaximoRegras = 10;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [KernelFunction(Funcao)]
    [Description("Recupera trechos adicionais da base de cruzamentos por identificador de regra ou pergunta. Retorna trechoId citáveis.")]
    public async Task<string> ConsultarAsync(
        [Description("Pergunta objetiva sobre o cruzamento (até 500 caracteres).")] string? pergunta = null,
        [Description("Identificadores de regra, ex.: CRZ-NFE-EFD-001, ENT-003, M1-P05 (até 10).")] string[]? regraIds = null,
        [Description("Obrigações para filtrar: NFE, NFCE, NFSE, DIMP, EFD_ICMS_IPI, EFD_CONTRIBUICOES, ECD, ECF.")] string[]? obrigacoes = null,
        CancellationToken cancelamento = default)
    {
        var ids = regraIds ?? [];
        var tipos = obrigacoes ?? [];
        var erro = Validar(pergunta, ids, tipos);
        if (erro is not null)
        {
            return Serializar(new { erro });
        }

        var inicio = relogio.GetUtcNow();
        var resultado = await baseConhecimento.ConsultarAsync(
            new ConsultaFundamentacao(ids, contexto.DataReferencia, limiteTrechos, contexto.CorrelacaoId) { Pergunta = pergunta, Obrigacoes = tipos },
            cancelamento).ConfigureAwait(false);
        if (resultado.Consumo is { } consumo)
        {
            contexto.ConsumoEmbeddings.Add((consumo, inicio, relogio.GetUtcNow()));
        }

        if (!resultado.Disponivel)
        {
            return Serializar(new { erro = "BASE_INDISPONIVEL", limitacoes = resultado.Limitacoes });
        }

        contexto.Registrar(resultado.Referencias);
        return Serializar(new
        {
            trechos = resultado.Referencias.Select(r => new { r.TrechoId, documento = r.DocumentoTitulo, tipo = r.TipoDocumento, versao = r.DocumentoVersao, r.Secao, r.RegraIds, r.Texto }),
            limitacoes = resultado.Limitacoes,
        });
    }

    private static string? Validar(string? pergunta, string[] ids, string[] tipos)
    {
        if (string.IsNullOrWhiteSpace(pergunta) && ids.Length == 0)
        {
            return "Informe pergunta ou regraIds.";
        }

        if (pergunta?.Length > MaximoPergunta)
        {
            return $"A pergunta excede {MaximoPergunta} caracteres.";
        }

        if (ids.Length > MaximoRegras || !ids.All(id => IdRegra().IsMatch(id)))
        {
            return $"Informe até {MaximoRegras} identificadores de regra válidos.";
        }

        return tipos.All(t => Codigos.TentarTipoObrigacao(t, out _)) ? null : "Código de obrigação desconhecido.";
    }

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, Json);

    [GeneratedRegex(@"^(?:(?:CRZ|INT|PRE|EXC|ENT)(?:-[A-Z]{2,5})*-[0-9]{3}|M[0-9]{1,2}-[A-Z]?[0-9]{2})$")]
    private static partial Regex IdRegra();
}
