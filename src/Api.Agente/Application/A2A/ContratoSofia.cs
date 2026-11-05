using System.Globalization;
using System.Text;
using System.Text.Json;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;

namespace Api.Agente.Application.A2A;

public sealed record IdentidadeAgenteSofia(string IdAgente, string Nome);

public sealed record ErroSofia(string Codigo, string Mensagem);

public sealed record AchadoSofia(
    string IdAchado,
    string Descricao,
    IReadOnlyList<string> IdDocumentos,
    IReadOnlyList<string> Evidencias,
    string? Severidade);

public sealed record ResultadoAnaliseSofia(
    string VersaoContrato,
    string IdExecucao,
    string IdSolicitacao,
    IdentidadeAgenteSofia Agente,
    string Status,
    string Analise,
    IReadOnlyList<AchadoSofia> Achados,
    IReadOnlyList<string> Observacoes,
    ErroSofia? Erro)
{
    public const string Versao = "1.0";
}

public sealed record EntradaA2AInterpretada(
    byte[]? CorpoLote,
    string IdExecucao,
    string IdSolicitacao,
    ErroSofia? Erro);

public static class InterpretadorEntradaA2A
{
    public static EntradaA2AInterpretada Interpretar(ReadOnlySpan<byte> texto, string execucaoPadrao, string solicitacaoPadrao)
    {
        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(texto.ToArray(), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow });
        }
        catch (JsonException)
        {
            return Falha("CONTRATO_A2A_INVALIDO", "O TextPart não é JSON válido.");
        }

        using (documento)
        {
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
            {
                return Falha("CONTRATO_A2A_INVALIDO", "O TextPart deve ser um objeto JSON.");
            }

            if (raiz.TryGetProperty("obrigacoes", out _))
            {
                return new EntradaA2AInterpretada(texto.ToArray(), Uuid(execucaoPadrao), Uuid(solicitacaoPadrao), null);
            }

            return InterpretarSofia(raiz, execucaoPadrao, solicitacaoPadrao);
        }
    }

    private static EntradaA2AInterpretada InterpretarSofia(JsonElement raiz, string execucaoPadrao, string solicitacaoPadrao)
    {
        if (!EhTexto(raiz, "versaoContrato", out var versao) || versao != ResultadoAnaliseSofia.Versao)
        {
            return Falha("CONTRATO_SOFIA_INVALIDO", "versaoContrato SOFIA deve ser 1.0.");
        }

        if (!EhUuid(raiz, "idExecucao", out var idExecucao) || !EhUuid(raiz, "idSolicitacao", out var idSolicitacao))
        {
            return Falha("CONTRATO_SOFIA_INVALIDO", "idExecucao e idSolicitacao devem ser UUID.");
        }

        _ = execucaoPadrao;
        _ = solicitacaoPadrao;

        if (!raiz.TryGetProperty("usuario", out var usuario)
            || !EhTexto(usuario, "nome", out var nome)
            || nome.Length == 0)
        {
            return ErroNegocio(idExecucao, idSolicitacao, "CONTRATO_SOFIA_INVALIDO", "usuario.nome é obrigatório.");
        }

        _ = nome;

        if (!EhTexto(raiz, "pergunta", out var pergunta) || pergunta.Length == 0)
        {
            return ErroNegocio(idExecucao, idSolicitacao, "CONTRATO_SOFIA_INVALIDO", "pergunta é obrigatória.");
        }

        if (!raiz.TryGetProperty("documentos", out var documentos) || documentos.ValueKind != JsonValueKind.Array || documentos.GetArrayLength() == 0)
        {
            return ErroNegocio(idExecucao, idSolicitacao, "CONTRATO_SOFIA_INVALIDO", "documentos deve ter ao menos um item.");
        }

        foreach (var doc in documentos.EnumerateArray())
        {
            if (!EhTexto(doc, "idDocumento", out var id) || id.Length == 0 || !EhTexto(doc, "nome", out var nomeDoc) || nomeDoc.Length == 0)
            {
                return ErroNegocio(idExecucao, idSolicitacao, "CONTRATO_SOFIA_INVALIDO", "Cada documento exige idDocumento e nome.");
            }

            if (!EhTexto(doc, "texto", out var textoDoc) || textoDoc.Length == 0)
            {
                return ErroNegocio(idExecucao, idSolicitacao, "DOCUMENTO_SEM_TEXTO", "Documento sem texto; este agente não extrai conteúdo.");
            }

            var bytes = Encoding.UTF8.GetBytes(textoDoc);
            try
            {
                using var lote = JsonDocument.Parse(bytes);
                if (lote.RootElement.TryGetProperty("obrigacoes", out _))
                {
                    return new EntradaA2AInterpretada(bytes, idExecucao, idSolicitacao, null);
                }
            }
            catch (JsonException)
            {
                // Próximo documento pode ser o lote.
            }
        }

        return ErroNegocio(idExecucao, idSolicitacao, "LOTE_AUSENTE", "Nenhum documento contém JSON lote.v1.");
    }

    public static ResultadoAnaliseSofia Mapear(
        ResultadoExecucaoAnalise execucao,
        IdentidadeAgenteSofia agente,
        string idExecucao,
        string idSolicitacao)
    {
        if (execucao.Erro is { } erro)
        {
            return new ResultadoAnaliseSofia(
                ResultadoAnaliseSofia.Versao,
                idExecucao,
                idSolicitacao,
                agente,
                "erro",
                "Não foi possível executar o cruzamento com o lote recebido.",
                [],
                [],
                new ErroSofia(erro.Codigo, erro.Mensagem));
        }

        var resultado = execucao.Resultado!;
        var achados = resultado.Achados.Select((a, i) => new AchadoSofia(
            "ach-" + (i + 1).ToString("000", CultureInfo.InvariantCulture),
            a.Descricao,
            a.Evidencias.Select(e => e.ObrigacaoId).Distinct(StringComparer.Ordinal).ToList(),
            Evidencias(a),
            SeveridadeSofia(a.Severidade))).ToList();

        return new ResultadoAnaliseSofia(
            ResultadoAnaliseSofia.Versao,
            idExecucao,
            idSolicitacao,
            agente,
            StatusSofia(resultado.Status),
            Analise(execucao.Parecer, resultado),
            achados,
            resultado.Limitacoes,
            resultado.Status == StatusAnalise.FalhaTecnica
                ? new ErroSofia("FALHA_TECNICA", "Falha técnica no processamento.")
                : null);
    }

    private static string Analise(ParecerLlm? parecer, ResultadoAnalise resultado)
    {
        if (!string.IsNullOrWhiteSpace(parecer?.Sintese))
        {
            return parecer.Sintese;
        }

        return resultado.Achados.Count == 0
            ? "Cruzamento concluído sem achados determinísticos."
            : string.Join(" ", resultado.Achados.Select(a => a.Descricao));
    }

    private static List<string> Evidencias(Achado achado)
    {
        var itens = achado.Evidencias
            .Where(e => e.Campo is not null || e.Valor is not null)
            .Select(e => string.Join(' ', new[] { e.Registro, e.Campo, e.Valor }.Where(s => !string.IsNullOrEmpty(s))))
            .Where(s => s.Length > 0)
            .ToList();
        return itens.Count == 0 ? [achado.Descricao] : itens;
    }

    private static string StatusSofia(StatusAnalise status) => status switch
    {
        StatusAnalise.Concluida => "concluido",
        StatusAnalise.FalhaTecnica => "erro",
        _ => "parcial",
    };

    private static string? SeveridadeSofia(Severidade? severidade) => severidade switch
    {
        Severidade.ErroGrave => "alta",
        Severidade.Alerta => "media",
        _ => null,
    };

    private static bool EhTexto(JsonElement objeto, string nome, out string valor)
    {
        valor = "";
        if (!objeto.TryGetProperty(nome, out var p) || p.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        valor = p.GetString() ?? "";
        return true;
    }

    private static bool EhUuid(JsonElement objeto, string nome, out string valor)
    {
        valor = "";
        return EhTexto(objeto, nome, out valor) && Guid.TryParse(valor, out _);
    }

    private static string Uuid(string candidato) =>
        Guid.TryParse(candidato, out var g) ? g.ToString() : Guid.NewGuid().ToString();

    private static EntradaA2AInterpretada Falha(string codigo, string mensagem) =>
        ErroNegocio(Guid.Empty.ToString(), Guid.Empty.ToString(), codigo, mensagem);

    private static EntradaA2AInterpretada ErroNegocio(string idExecucao, string idSolicitacao, string codigo, string mensagem) =>
        new(null, idExecucao, idSolicitacao, new ErroSofia(codigo, mensagem));
}
