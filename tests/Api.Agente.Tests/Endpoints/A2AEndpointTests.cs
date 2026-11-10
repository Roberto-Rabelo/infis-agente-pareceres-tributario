using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Tests.Fixtures;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Endpoints;

public class A2AEndpointTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task AgentCard_Autenticado_PerfilCompativelComSofia()
    {
        using var cliente = Autenticado();

        using var resposta = await cliente.GetAsync("/.well-known/agent-card.json");
        using var json = await Json(resposta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var raiz = json.RootElement;
        Assert.Equal("0.3.0", raiz.GetProperty("protocolVersion").GetString());
        Assert.Equal("JSONRPC", raiz.GetProperty("preferredTransport").GetString());
        Assert.Equal("http://localhost/a2a", raiz.GetProperty("url").GetString());
        Assert.Equal("Agente Parecer Tributário", raiz.GetProperty("name").GetString());
        Assert.Equal("analisar_documentos", raiz.GetProperty("skills")[0].GetProperty("id").GetString());
        Assert.Contains("text/plain", raiz.GetProperty("defaultInputModes").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("text/plain", raiz.GetProperty("defaultOutputModes").EnumerateArray().Select(e => e.GetString()));
        Assert.False(raiz.GetProperty("capabilities").GetProperty("streaming").GetBoolean());
        var esquema = raiz.GetProperty("securitySchemes").GetProperty("sofiaService");
        Assert.Equal(("http", "bearer"), (esquema.GetProperty("type").GetString(), esquema.GetProperty("scheme").GetString()));
        Assert.True(raiz.GetProperty("security")[0].TryGetProperty("sofiaService", out _));
        Assert.DoesNotContain("chave-sintetica", raiz.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentCard_SemAutenticacao_401()
    {
        using var cliente = fabrica.CreateClient();

        using var resposta = await cliente.GetAsync("/.well-known/agent-card.json");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task A2A_SemAutenticacao_401()
    {
        using var cliente = fabrica.CreateClient();

        using var resposta = await cliente.PostAsync("/a2a", Rpc(Lote(EfdIcmsIpi(), NFe())));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Preflight_OrigemPermitida_EcoaAllowOrigin()
    {
        using var cliente = fabrica.CreateClient();
        using var pedido = new HttpRequestMessage(HttpMethod.Options, "/a2a");
        pedido.Headers.Add("Origin", "http://localhost:4200");
        pedido.Headers.Add("Access-Control-Request-Method", "POST");
        pedido.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        using var resposta = await cliente.SendAsync(pedido);

        Assert.Equal("http://localhost:4200", resposta.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_OrigemBloqueada_SemPermissaoParaEla()
    {
        using var cliente = fabrica.CreateClient();
        using var pedido = new HttpRequestMessage(HttpMethod.Options, "/api/v1/analises");
        pedido.Headers.Add("Origin", "https://site-nao-autorizado.example");
        pedido.Headers.Add("Access-Control-Request-Method", "POST");

        using var resposta = await cliente.SendAsync(pedido);

        Assert.False(resposta.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores)
            && valores.Contains("https://site-nao-autorizado.example", StringComparer.Ordinal));
    }

    [Fact]
    public async Task SemOrigin_AgentCard_200()
    {
        using var cliente = Autenticado();

        using var resposta = await cliente.GetAsync("/.well-known/agent-card.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task MessageSend_LoteDireto_MesmosAchadosQueRest()
    {
        var lote = Lote(EfdIcmsIpi(), NFe());
        using var cliente = Autenticado();

        using var rest = await cliente.PostAsync("/api/v1/analises", Conteudo(Bytes(lote)));
        using var a2a = await cliente.PostAsync("/a2a", Rpc(lote));
        using var jsonRest = await Json(rest);
        using var jsonA2a = await Json(a2a);

        Assert.Equal(HttpStatusCode.OK, rest.StatusCode);
        Assert.Equal(HttpStatusCode.OK, a2a.StatusCode);

        var descricoesRest = jsonRest.RootElement.GetProperty("resultado").GetProperty("achados")
            .EnumerateArray().Select(a => a.GetProperty("descricao").GetString()).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var artifact = jsonA2a.RootElement.GetProperty("result").GetProperty("artifacts")[0].GetProperty("parts")[0].GetProperty("text").GetString()!;
        using var sofia = JsonDocument.Parse(artifact);
        var statusSofia = sofia.RootElement.GetProperty("status").GetString();
        Assert.True(statusSofia is "concluido" or "parcial", statusSofia);
        var descricoesA2a = sofia.RootElement.GetProperty("achados")
            .EnumerateArray().Select(a => a.GetProperty("descricao").GetString()).OrderBy(s => s, StringComparer.Ordinal).ToList();
        Assert.Equal(descricoesRest, descricoesA2a);
    }

    [Fact]
    public async Task MessageSend_EnvelopeSofia_EcoaIds()
    {
        var idExecucao = "8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e";
        var idSolicitacao = "e1b3a4cb-d729-4bd4-b796-b9c94be07b46";
        var sofia = EnvelopeSofia(idExecucao, idSolicitacao, Lote(EfdIcmsIpi(), NFe()));
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync("/a2a", Rpc(sofia));
        using var json = await Json(resposta);
        using var artifact = JsonDocument.Parse(json.RootElement.GetProperty("result").GetProperty("artifacts")[0].GetProperty("parts")[0].GetProperty("text").GetString()!);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(idExecucao, artifact.RootElement.GetProperty("idExecucao").GetString());
        Assert.Equal(idSolicitacao, artifact.RootElement.GetProperty("idSolicitacao").GetString());
        Assert.Equal("agente-parecer-tributario", artifact.RootElement.GetProperty("agente").GetProperty("idAgente").GetString());
        var tarefa = json.RootElement.GetProperty("result");
        Assert.Equal("task", tarefa.GetProperty("kind").GetString());
        Assert.Equal("completed", tarefa.GetProperty("status").GetProperty("state").GetString());
        Assert.Equal(idExecucao, tarefa.GetProperty("contextId").GetString());
        foreach (var achado in artifact.RootElement.GetProperty("achados").EnumerateArray())
        {
            Assert.Equal(["doc-lote"], achado.GetProperty("idDocumentos").EnumerateArray().Select(e => e.GetString()));
        }
    }

    [Fact]
    public async Task MessageSend_ArtifactUnicoAnaliseSofiaV1()
    {
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync("/a2a", Rpc(Lote(EfdIcmsIpi(), NFe()), rpcId: "rpc-001"));
        using var json = await Json(resposta);

        Assert.Equal("rpc-001", json.RootElement.GetProperty("id").GetString());
        var artifacts = json.RootElement.GetProperty("result").GetProperty("artifacts");
        Assert.Equal(1, artifacts.GetArrayLength());
        Assert.Equal("analise-sofia-v1", artifacts[0].GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(artifacts[0].GetProperty("artifactId").GetString()));
        var partes = artifacts[0].GetProperty("parts");
        Assert.Equal(1, partes.GetArrayLength());
        Assert.Equal("text", partes[0].GetProperty("kind").GetString());
    }

    [Fact]
    public async Task TasksGet_DevolveMesmaTarefaEContexto()
    {
        using var cliente = Autenticado();
        using var envio = await cliente.PostAsync("/a2a", Rpc(Lote(EfdIcmsIpi(), NFe()), contextId: "ctx-get"));
        using var jsonEnvio = await Json(envio);
        var tarefa = jsonEnvio.RootElement.GetProperty("result");
        var taskId = tarefa.GetProperty("id").GetString()!;

        using var consulta = await cliente.PostAsync("/a2a", RpcTarefa("tasks/get", taskId));
        using var jsonConsulta = await Json(consulta);

        var obtida = jsonConsulta.RootElement.GetProperty("result");
        Assert.Equal("rpc-tarefa", jsonConsulta.RootElement.GetProperty("id").GetString());
        Assert.Equal(taskId, obtida.GetProperty("id").GetString());
        Assert.Equal("ctx-get", obtida.GetProperty("contextId").GetString());
        Assert.Equal("completed", obtida.GetProperty("status").GetProperty("state").GetString());
        Assert.Equal(
            tarefa.GetProperty("artifacts")[0].GetProperty("parts")[0].GetProperty("text").GetString(),
            obtida.GetProperty("artifacts")[0].GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task TasksCancel_TarefaConcluida_ErroNaoCancelavel()
    {
        using var cliente = Autenticado();
        using var envio = await cliente.PostAsync("/a2a", Rpc(Lote(EfdIcmsIpi(), NFe())));
        using var jsonEnvio = await Json(envio);
        var taskId = jsonEnvio.RootElement.GetProperty("result").GetProperty("id").GetString()!;

        using var cancelamento = await cliente.PostAsync("/a2a", RpcTarefa("tasks/cancel", taskId));
        using var json = await Json(cancelamento);

        Assert.Equal(-32002, json.RootElement.GetProperty("error").GetProperty("code").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("result", out _));
    }

    [Theory]
    [InlineData("tasks/get")]
    [InlineData("tasks/cancel")]
    public async Task Tarefas_IdInexistente_ErroTaskNotFound(string metodo)
    {
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync("/a2a", RpcTarefa(metodo, Guid.NewGuid().ToString()));
        using var json = await Json(resposta);

        Assert.Equal(-32001, json.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"r1\",\"method\":\"tasks/inexistente\"}", -32601)]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"r1\",\"method\":\"tasks/get\",\"params\":{}}", -32602)]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"r1\",\"method\":\"message/send\",\"params\":{\"message\":{\"parts\":[]}}}", -32602)]
    [InlineData("{nao e json", -32700)]
    [InlineData("[1,2]", -32600)]
    public async Task RpcInvalido_ErroJsonRpcSemStackTrace(string corpo, int codigo)
    {
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync("/a2a", Conteudo(System.Text.Encoding.UTF8.GetBytes(corpo)));
        var texto = await resposta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(texto);

        Assert.Equal(codigo, json.RootElement.GetProperty("error").GetProperty("code").GetInt32());
        Assert.DoesNotContain("   at ", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MessageSend_ContextIdDoCliente_Ecoado()
    {
        var sofia = EnvelopeSofia("8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e", "e1b3a4cb-d729-4bd4-b796-b9c94be07b46", Lote(EfdIcmsIpi(), NFe()));
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync("/a2a", Rpc(sofia, contextId: "ctx-orquestrador-1"));
        using var json = await Json(resposta);

        Assert.Equal("ctx-orquestrador-1", json.RootElement.GetProperty("result").GetProperty("contextId").GetString());
    }

    [Fact]
    public async Task MessageSend_DocumentoSemTexto_StatusErro()
    {
        var sofia = new JsonObject
        {
            ["versaoContrato"] = "1.0",
            ["idExecucao"] = "8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e",
            ["idSolicitacao"] = "e1b3a4cb-d729-4bd4-b796-b9c94be07b46",
            ["usuario"] = new JsonObject { ["nome"] = "Usuario Teste" },
            ["pergunta"] = "Analisar",
            ["documentos"] = new JsonArray(new JsonObject
            {
                ["idDocumento"] = "doc-1",
                ["nome"] = "vazio.txt",
                ["texto"] = "",
            }),
        };
        using var cliente = Autenticado();

        using var resposta = await cliente.PostAsync("/a2a", Rpc(sofia));
        using var json = await Json(resposta);
        using var artifact = JsonDocument.Parse(json.RootElement.GetProperty("result").GetProperty("artifacts")[0].GetProperty("parts")[0].GetProperty("text").GetString()!);

        Assert.Equal("erro", artifact.RootElement.GetProperty("status").GetString());
        Assert.Equal("DOCUMENTO_SEM_TEXTO", artifact.RootElement.GetProperty("erro").GetProperty("codigo").GetString());
        Assert.Equal("8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e", artifact.RootElement.GetProperty("idExecucao").GetString());
    }

    private HttpClient Autenticado()
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AutenticacaoTeste.Esquema, "agente-orquestrador");
        return cliente;
    }

    private static ByteArrayContent Rpc(JsonNode payload, string? contextId = null, string rpcId = "rpc-1")
    {
        var mensagem = new JsonObject
        {
            ["role"] = "user",
            ["messageId"] = "msg-teste",
            ["parts"] = new JsonArray(new JsonObject { ["kind"] = "text", ["text"] = payload.ToJsonString() }),
        };
        if (contextId is not null)
        {
            mensagem["contextId"] = contextId;
        }

        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = rpcId,
            ["method"] = "message/send",
            ["params"] = new JsonObject { ["message"] = mensagem },
        };
        return Conteudo(System.Text.Encoding.UTF8.GetBytes(envelope.ToJsonString()));
    }

    private static ByteArrayContent RpcTarefa(string metodo, string taskId)
    {
        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = "rpc-tarefa",
            ["method"] = metodo,
            ["params"] = new JsonObject { ["id"] = taskId },
        };
        return Conteudo(System.Text.Encoding.UTF8.GetBytes(envelope.ToJsonString()));
    }

    private static JsonObject EnvelopeSofia(string idExecucao, string idSolicitacao, JsonObject lote) => new()
    {
        ["versaoContrato"] = "1.0",
        ["idExecucao"] = idExecucao,
        ["idSolicitacao"] = idSolicitacao,
        ["usuario"] = new JsonObject { ["nome"] = "Usuario Teste" },
        ["pergunta"] = "Verifique divergências fiscais nos documentos.",
        ["documentos"] = new JsonArray(new JsonObject
        {
            ["idDocumento"] = "doc-lote",
            ["nome"] = "lote.json",
            ["tipo"] = "application/json",
            ["texto"] = lote.ToJsonString(),
        }),
    };

    private static ByteArrayContent Conteudo(byte[] corpo)
    {
        var conteudo = new ByteArrayContent(corpo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return conteudo;
    }

    private static async Task<JsonDocument> Json(HttpResponseMessage resposta) =>
        await JsonDocument.ParseAsync(await resposta.Content.ReadAsStreamAsync());
}
