using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Application.A2A;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Infrastructure.Hospedagem;
using Microsoft.Extensions.Options;

namespace Api.Agente.Endpoints.A2A;

/// <summary>Servidor A2A 0.3.0 JSON-RPC para a SOFIA (skills sofia-a2a-implementacao e sofia-a2a-autenticacao).</summary>
public static class A2AEndpoints
{
    public const string PoliticaCors = "SofiaBrowser";

    /// <summary>Nome do Artifact exigido pelo codec A2A da SOFIA.</summary>
    public const string NomeArtifact = "analise-sofia-v1";

    // Códigos de erro A2A 0.3.0 (JSON-RPC).
    public const int ErroTarefaNaoEncontrada = -32001;
    public const int ErroTarefaNaoCancelavel = -32002;

    private static readonly JsonSerializerOptions JsonSofia = new(JsonSerializerDefaults.Web);

    public static void MapServidorA2A(this WebApplication app)
    {
        app.MapGet(AutenticacaoChaveIntegracao.CaminhoCard, Card).RequireAuthorization();
        var rpc = app.MapPost(ResolvedorUrlA2A.CaminhoRpc, Rpc).RequireAuthorization();
        LimitadorRequisicoes.ComLimiteSeConfigurado(rpc, app.Configuration);
    }

    private static IResult Card(HttpContext http, IOptions<OpcoesA2A> opcoes)
    {
        var a = opcoes.Value;
        if (!a.IntegracaoDisponivel)
        {
            return Indisponivel(new { codigo = "A2A_INDISPONIVEL", mensagem = "Integração A2A indisponível." });
        }

        var esquema = http.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? http.Request.Scheme;
        var url = ResolvedorUrlA2A.Resolver(a.Url, esquema, http.Request.Host.Value ?? "localhost");
        var card = new JsonObject
        {
            ["protocolVersion"] = "0.3.0",
            ["name"] = a.Nome,
            ["description"] = a.Descricao,
            ["url"] = url,
            ["preferredTransport"] = "JSONRPC",
            ["version"] = "1.0.0",
            ["capabilities"] = new JsonObject { ["streaming"] = false, ["pushNotifications"] = false },
            ["defaultInputModes"] = new JsonArray("text/plain", "application/json"),
            ["defaultOutputModes"] = new JsonArray("text/plain", "application/json"),
            ["skills"] = new JsonArray(new JsonObject
            {
                ["id"] = "analisar_documentos",
                ["name"] = "Analisar documentos fiscais",
                ["description"] = a.Descricao,
                ["tags"] = new JsonArray("infis", "fiscal", "tributario", "cruzamento", "sofia"),
                ["inputModes"] = new JsonArray("text/plain", "application/json"),
                ["outputModes"] = new JsonArray("text/plain", "application/json"),
            }),
            ["securitySchemes"] = new JsonObject
            {
                ["sofiaService"] = new JsonObject { ["type"] = "http", ["scheme"] = "bearer" },
            },
            ["security"] = new JsonArray(new JsonObject { ["sofiaService"] = new JsonArray() }),
        };
        return Results.Json(card);
    }

    private static async Task<IResult> Rpc(
        HttpContext http,
        ServicoExecucaoAnalise execucao,
        RepositorioTarefasA2A tarefas,
        IOptions<OpcoesA2A> opcoesA2A,
        IOptions<OpcoesEntrada> opcoesEntrada,
        CancellationToken cancelamento)
    {
        if (!opcoesA2A.Value.IntegracaoDisponivel)
        {
            return Indisponivel(RpcErro(null, -32603, "Integração A2A indisponível."));
        }

        var limite = opcoesEntrada.Value.TamanhoMaximoBytes;
        var corpo = await CorpoRequisicao.LerLimitadoAsync(http.Request, limite, cancelamento).ConfigureAwait(false);
        if (corpo is null)
        {
            return Results.Json(RpcErro(null, -32600, "Payload excede o limite configurado."), statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        JsonDocument rpc;
        try
        {
            rpc = JsonDocument.Parse(corpo);
        }
        catch (JsonException)
        {
            return Results.Json(RpcErro(null, -32700, "JSON-RPC inválido."));
        }

        using (rpc)
        {
            var raiz = rpc.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
            {
                return Results.Json(RpcErro(null, -32600, "Requisição JSON-RPC deve ser um objeto."));
            }

            var id = raiz.TryGetProperty("id", out var idEl) ? JsonNode.Parse(idEl.GetRawText()) : null;
            if (!raiz.TryGetProperty("jsonrpc", out var versao) || versao.ValueKind != JsonValueKind.String || versao.GetString() != "2.0")
            {
                return Results.Json(RpcErro(id, -32600, "jsonrpc deve ser 2.0."));
            }

            var metodo = raiz.TryGetProperty("method", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            return metodo switch
            {
                "message/send" => await EnviarMensagem(http, raiz, id, execucao, tarefas, opcoesA2A.Value, cancelamento).ConfigureAwait(false),
                "tasks/get" => ObterTarefa(raiz, id, tarefas),
                "tasks/cancel" => CancelarTarefa(raiz, id, tarefas),
                _ => Results.Json(RpcErro(id, -32601, "Método não suportado. Use message/send, tasks/get ou tasks/cancel.")),
            };
        }
    }

    private static async Task<IResult> EnviarMensagem(
        HttpContext http,
        JsonElement raiz,
        JsonNode? id,
        ServicoExecucaoAnalise execucao,
        RepositorioTarefasA2A tarefas,
        OpcoesA2A opcoesA2A,
        CancellationToken cancelamento)
    {
        if (!TentarTexto(raiz, out var texto))
        {
            return Results.Json(RpcErro(id, -32602, "params.message.parts deve conter um TextPart."));
        }

        var contextIdCliente = TextoEm(raiz, "params", "message", "contextId");
        var correlacao = Correlacao.DaRequisicao(http);
        var execucaoId = Correlacao.Execucao(http);
        var entrada = InterpretadorEntradaA2A.Interpretar(texto, execucaoId, correlacao);
        var agente = new IdentidadeAgenteSofia(opcoesA2A.IdAgente, opcoesA2A.Nome);

        ResultadoAnaliseSofia resultado;
        if (entrada.Erro is { } erroSofia)
        {
            resultado = new ResultadoAnaliseSofia(
                ResultadoAnaliseSofia.Versao,
                entrada.IdExecucao,
                entrada.IdSolicitacao,
                agente,
                "erro",
                "Solicitação rejeitada antes do cruzamento.",
                [],
                [],
                erroSofia);
        }
        else
        {
            var analise = await execucao
                .ExecutarAsync(entrada.CorpoLote, entrada.IdExecucao, entrada.IdSolicitacao, cancelamento)
                .ConfigureAwait(false);
            resultado = InterpretadorEntradaA2A.Mapear(analise, agente, entrada.IdExecucao, entrada.IdSolicitacao, entrada.IdDocumento);
        }

        var tarefa = Tarefa(contextIdCliente ?? resultado.IdExecucao, resultado);
        tarefas.Guardar(tarefa);
        return Results.Json(RpcResultado(id, tarefa));
    }

    private static IResult ObterTarefa(JsonElement raiz, JsonNode? id, RepositorioTarefasA2A tarefas)
    {
        if (TextoEm(raiz, "params", "id") is not { } idTarefa)
        {
            return Results.Json(RpcErro(id, -32602, "params.id (ID da Task) é obrigatório."));
        }

        return tarefas.Obter(idTarefa) is { } tarefa
            ? Results.Json(RpcResultado(id, tarefa))
            : Results.Json(RpcErro(id, ErroTarefaNaoEncontrada, "Tarefa não encontrada."));
    }

    private static IResult CancelarTarefa(JsonElement raiz, JsonNode? id, RepositorioTarefasA2A tarefas)
    {
        if (TextoEm(raiz, "params", "id") is not { } idTarefa)
        {
            return Results.Json(RpcErro(id, -32602, "params.id (ID da Task) é obrigatório."));
        }

        // As tarefas concluem dentro de message/send; uma tarefa existente já está em estado terminal.
        return tarefas.Obter(idTarefa) is null
            ? Results.Json(RpcErro(id, ErroTarefaNaoEncontrada, "Tarefa não encontrada."))
            : Results.Json(RpcErro(id, ErroTarefaNaoCancelavel, "Tarefa já concluída; não pode ser cancelada."));
    }

    private static bool TentarTexto(JsonElement raiz, out byte[] texto)
    {
        texto = [];
        if (!raiz.TryGetProperty("params", out var parametros) || parametros.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!parametros.TryGetProperty("message", out var mensagem) || mensagem.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!mensagem.TryGetProperty("parts", out var partes) || partes.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var parte in partes.EnumerateArray())
        {
            if (parte.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var kind = parte.TryGetProperty("kind", out var k) ? k.GetString() : parte.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (kind is not ("text" or "TextPart") || !parte.TryGetProperty("text", out var campo) || campo.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            texto = System.Text.Encoding.UTF8.GetBytes(campo.GetString() ?? "");
            return texto.Length > 0;
        }

        return false;
    }

    private static string? TextoEm(JsonElement raiz, params string[] caminho)
    {
        var atual = raiz;
        foreach (var nome in caminho)
        {
            if (atual.ValueKind != JsonValueKind.Object || !atual.TryGetProperty(nome, out atual))
            {
                return null;
            }
        }

        return atual.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(atual.GetString()) ? atual.GetString() : null;
    }

    private static IResult Indisponivel(object corpo) =>
        Results.Json(corpo, statusCode: StatusCodes.Status503ServiceUnavailable);

    private static JsonObject RpcErro(JsonNode? id, int codigo, string mensagem) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = codigo, ["message"] = mensagem },
    };

    private static JsonObject RpcResultado(JsonNode? id, JsonObject tarefa) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["result"] = tarefa,
    };

    private static JsonObject Tarefa(string contextId, ResultadoAnaliseSofia resultado)
    {
        var taskId = Guid.NewGuid().ToString();
        return new JsonObject
        {
            ["kind"] = "task",
            ["id"] = taskId,
            ["contextId"] = contextId,
            ["status"] = new JsonObject
            {
                ["state"] = "completed",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            },
            ["artifacts"] = new JsonArray(new JsonObject
            {
                ["artifactId"] = "artifact-" + taskId,
                ["name"] = NomeArtifact,
                ["parts"] = new JsonArray(new JsonObject
                {
                    ["kind"] = "text",
                    ["text"] = JsonSerializer.Serialize(resultado, JsonSofia),
                }),
            }),
        };
    }
}
