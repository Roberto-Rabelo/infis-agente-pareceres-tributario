using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Endpoints;

/// <summary>Autenticação real (sem o esquema de teste): chave de integração do orquestrador vale só em /a2a.</summary>
public sealed class ChaveIntegracaoA2ATests : IDisposable
{
    private const string Chave = "chave-de-teste-integracao-a2a";

    private readonly WebApplicationFactory<Program> _comChave = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b => b.UseEnvironment(FabricaApi.Ambiente).UseSetting("A2A:ChaveIntegracao", Chave));

    private readonly WebApplicationFactory<Program> _semChave = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b => b.UseEnvironment(FabricaApi.Ambiente));

    [Fact]
    public async Task Bearer_ComChave_200()
    {
        using var cliente = _comChave.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Chave);

        using var resposta = await cliente.PostAsync("/a2a", Rpc());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task XApiKey_ComChave_200()
    {
        using var cliente = _comChave.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-API-Key", Chave);

        using var resposta = await cliente.PostAsync("/a2a", Rpc());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("X-API-Key")]
    public async Task ChaveErrada_401(string onde)
    {
        using var cliente = _comChave.CreateClient();
        Apresentar(cliente, onde, "chave-errada-de-integracao");

        using var resposta = await cliente.PostAsync("/a2a", Rpc());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ChaveNaoConfigurada_401()
    {
        using var cliente = _semChave.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Chave);

        using var resposta = await cliente.PostAsync("/a2a", Rpc());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("X-API-Key")]
    public async Task Rest_NaoAceitaChaveDeIntegracao_401(string onde)
    {
        using var cliente = _comChave.CreateClient();
        Apresentar(cliente, onde, Chave);

        using var resposta = await cliente.PostAsync("/api/v1/analises", Json(Bytes(Lote(EfdIcmsIpi()))));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    public void Dispose()
    {
        _comChave.Dispose();
        _semChave.Dispose();
    }

    private static void Apresentar(HttpClient cliente, string onde, string valor)
    {
        if (onde == "Bearer")
        {
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", valor);
        }
        else
        {
            cliente.DefaultRequestHeaders.Add("X-API-Key", valor);
        }
    }

    private static ByteArrayContent Rpc()
    {
        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "message/send",
            ["params"] = new JsonObject
            {
                ["message"] = new JsonObject
                {
                    ["role"] = "user",
                    ["messageId"] = "msg-chave",
                    ["parts"] = new JsonArray(new JsonObject { ["kind"] = "text", ["text"] = Lote(EfdIcmsIpi(), NFe()).ToJsonString() }),
                },
            },
        };
        return Json(System.Text.Encoding.UTF8.GetBytes(envelope.ToJsonString()));
    }

    private static ByteArrayContent Json(byte[] corpo)
    {
        var conteudo = new ByteArrayContent(corpo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return conteudo;
    }
}
