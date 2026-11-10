using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Application.A2A;
using Api.Agente.Application.Analise;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Infrastructure.Hospedagem;
using Microsoft.Extensions.Options;

namespace Api.Agente.Endpoints.A2A;

public static class A2AEndpoints
{
    public const string PoliticaCors = "SofiaBrowser";

    private static readonly JsonSerializerOptions JsonSofia = new(JsonSerializerDefaults.Web);

    public static void MapServidorA2A(this WebApplication app)
    {
        app.MapGet("/.well-known/agent-card.json", Card).AllowAnonymous();
        var rpc = app.MapPost("/a2a", Enviar).RequireAuthorization();
        LimitadorRequisicoes.ComLimiteSeConfigurado(rpc, app.Configuration);
    }

    private static IResult Card(HttpContext http, IOptions<OpcoesA2A> opcoes)
    {
        var a = opcoes.Value;
        var esquema = http.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? http.Request.Scheme;
        var url = ResolvedorUrlA2A.Resolver(a.Url, esquema, http.Request.Host.Value ?? "localhost");
        var card = new JsonObject
        {
            ["protocolVersion"] = "0.3.0",
            ["name"] = a.Nome,
            ["description"] = a.Descricao,
            ["url"] = url,
            ["version"] = "1.0.0",
            ["capabilities"] = new JsonObject { ["streaming"] = false, ["pushNotifications"] = false },
            ["defaultInputModes"] = new JsonArray("application/json", "text/plain"),
            ["defaultOutputModes"] = new JsonArray("application/json"),
            ["skills"] = new JsonArray(new JsonObject
            {
                ["id"] = "analisar_documentos",
                ["name"] = "Analisar documentos fiscais",
                ["description"] = a.Descricao,
                ["tags"] = new JsonArray("fiscal", "tributario", "cruzamento"),
                ["inputModes"] = new JsonArray("application/json"),
                ["outputModes"] = new JsonArray("application/json"),
            }),
            ["preferredTransport"] = "JSONRPC",
            ["securitySchemes"] = new JsonObject
            {
                ["bearer"] = new JsonObject { ["type"] = "http", ["scheme"] = "bearer", ["bearerFormat"] = "JWT" },
            },
            ["security"] = new JsonArray(new JsonObject { ["bearer"] = new JsonArray() }),
        };
        return Results.Json(card);
    }

    private static async Task<IResult> Enviar(
        HttpContext http,
        ServicoExecucaoAnalise execucao,
        IOptions<OpcoesA2A> opcoesA2A,
        IOptions<OpcoesEntrada> opcoesEntrada,
        CancellationToken cancelamento)
    {
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
            var id = raiz.TryGetProperty("id", out var idEl) ? JsonNode.Parse(idEl.GetRawText()) : null;
            if (!raiz.TryGetProperty("jsonrpc", out var versao) || versao.GetString() != "2.0")
            {
                return Results.Json(RpcErro(id, -32600, "jsonrpc deve ser 2.0."));
            }

            if (!raiz.TryGetProperty("method", out var metodo) || metodo.GetString() != "message/send")
            {
                return Results.Json(RpcErro(id, -32601, "Método não suportado. Use message/send."));
            }

            if (!TentarTexto(raiz, out var texto))
            {
                return Results.Json(RpcErro(id, -32602, "params.message.parts deve conter um TextPart."));
            }

            var contextIdCliente = ContextIdDaMensagem(raiz);

            var correlacao = Correlacao.DaRequisicao(http);
            var execucaoId = Correlacao.Execucao(http);
            var entrada = InterpretadorEntradaA2A.Interpretar(texto, execucaoId, correlacao);
            var agente = new IdentidadeAgenteSofia(opcoesA2A.Value.IdAgente, opcoesA2A.Value.Nome);

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

            return Results.Json(RpcTarefa(id, contextIdCliente ?? resultado.IdExecucao, resultado));
        }
    }

    private static string? ContextIdDaMensagem(JsonElement raiz) =>
        raiz.TryGetProperty("params", out var p)
        && p.TryGetProperty("message", out var m)
        && m.TryGetProperty("contextId", out var c)
        && c.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(c.GetString())
            ? c.GetString()
            : null;

    private static bool TentarTexto(JsonElement raiz, out byte[] texto)
    {
        texto = [];
        if (!raiz.TryGetProperty("params", out var parametros))
        {
            return false;
        }

        if (!parametros.TryGetProperty("message", out var mensagem))
        {
            return false;
        }

        if (!mensagem.TryGetProperty("parts", out var partes) || partes.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var parte in partes.EnumerateArray())
        {
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

    private static object RpcErro(JsonNode? id, int codigo, string mensagem) => new
    {
        jsonrpc = "2.0",
        id,
        error = new { code = codigo, message = mensagem },
    };

    private static object RpcTarefa(JsonNode? id, string contextId, ResultadoAnaliseSofia resultado)
    {
        var payload = JsonSerializer.Serialize(resultado, JsonSofia);
        var taskId = Guid.NewGuid().ToString();
        return new
        {
            jsonrpc = "2.0",
            id,
            result = new
            {
                kind = "task",
                id = taskId,
                contextId,
                status = new { state = "completed", timestamp = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture) },
                artifacts = new[]
                {
                    new
                    {
                        artifactId = "resultado-analise-sofia",
                        name = "ResultadoAnaliseSofia",
                        parts = new[] { new { kind = "text", text = payload } },
                    },
                },
            },
        };
    }
}
