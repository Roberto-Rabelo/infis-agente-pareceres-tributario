using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Endpoints;

/// <summary>
/// Skill sofia-a2a-autenticacao com a autenticação real (sem o esquema de teste): chave Bearer compartilhada
/// em SOFIA_A2A_TOKEN protege o Agent Card e o RPC; a API REST segue só com JWT.
/// </summary>
public sealed class ChaveIntegracaoA2ATests : IDisposable
{
    private const string Chave = "chave-sintetica-de-teste-a2a";
    private const string Card = "/.well-known/agent-card.json";

    private readonly LogsCapturados _logs = new();
    private readonly WebApplicationFactory<Program> _comChave;
    private readonly WebApplicationFactory<Program> _semChave = Fabrica(null);
    private readonly WebApplicationFactory<Program> _chaveCurta = Fabrica("curta");

    public ChaveIntegracaoA2ATests()
    {
        _comChave = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseEnvironment(FabricaApi.Ambiente)
            .UseSetting("SOFIA_A2A_TOKEN", Chave)
            .ConfigureServices(s => s.AddLogging(l => l.AddProvider(_logs).SetMinimumLevel(LogLevel.Trace))));
    }

    [Fact]
    public async Task Card_ComChave_200()
    {
        using var cliente = Cliente(_comChave, Chave);

        using var resposta = await cliente.GetAsync(Card);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.DoesNotContain(Chave, await resposta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("chave-incorreta-de-teste")]
    [InlineData("CHAVE-SINTETICA-DE-TESTE-A2A")]
    public async Task Card_SemChaveOuIncorreta_401ComWwwAuthenticate(string? chave)
    {
        using var cliente = Cliente(_comChave, chave);

        using var resposta = await cliente.GetAsync(Card);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.StartsWith("Bearer", resposta.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Card_CabecalhoMalformado_401()
    {
        using var cliente = _comChave.CreateClient();
        cliente.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic " + Chave);

        using var resposta = await cliente.GetAsync(Card);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Rpc_ComChave_TaskConcluida()
    {
        using var cliente = Cliente(_comChave, Chave);

        using var resposta = await cliente.PostAsync("/a2a", Rpc());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("task", json.RootElement.GetProperty("result").GetProperty("kind").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("chave-incorreta-de-teste")]
    public async Task Rpc_SemChaveOuIncorreta_401(string? chave)
    {
        using var cliente = Cliente(_comChave, chave);

        using var resposta = await cliente.PostAsync("/a2a", Rpc());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.StartsWith("Bearer", resposta.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChaveNaUrlOuEmOutroCabecalho_NaoAceita()
    {
        using var cliente = _comChave.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-API-Key", Chave);

        using var resposta = await cliente.PostAsync("/a2a?token=" + Chave, Rpc());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData(Card)]
    [InlineData("/a2a")]
    public async Task ConfiguracaoAusenteOuCurta_NuncaAcessoLivre(string rota)
    {
        foreach (var fabrica in new[] { _semChave, _chaveCurta })
        {
            foreach (var chave in new[] { null, Chave, "curta" })
            {
                using var cliente = Cliente(fabrica, chave);
                using var resposta = rota == Card ? await cliente.GetAsync(rota) : await cliente.PostAsync(rota, Rpc());

                Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            }
        }
    }

    [Fact]
    public async Task Rest_NaoAceitaChaveCompartilhada_401()
    {
        using var cliente = Cliente(_comChave, Chave);

        using var resposta = await cliente.PostAsync("/api/v1/analises", Json(Bytes(Lote(EfdIcmsIpi()))));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Logs_NaoExpoemChave()
    {
        foreach (var chave in new[] { Chave, "chave-incorreta-de-teste" })
        {
            using var cliente = Cliente(_comChave, chave);
            using var _ = await cliente.GetAsync(Card);
            using var __ = await cliente.PostAsync("/a2a", Rpc());
        }

        Assert.NotEmpty(_logs.Mensagens);
        Assert.DoesNotContain(_logs.Mensagens, m => m.Contains(Chave, StringComparison.Ordinal));
        Assert.DoesNotContain(_logs.Mensagens, m => m.Contains("chave-incorreta-de-teste", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _comChave.Dispose();
        _semChave.Dispose();
        _chaveCurta.Dispose();
    }

    private static WebApplicationFactory<Program> Fabrica(string? chave) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b =>
        {
            b.UseEnvironment(FabricaApi.Ambiente);
            if (chave is not null)
            {
                b.UseSetting("SOFIA_A2A_TOKEN", chave);
            }
        });

    private static HttpClient Cliente(WebApplicationFactory<Program> fabrica, string? chave)
    {
        var cliente = fabrica.CreateClient();
        if (chave is not null)
        {
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", chave);
        }

        return cliente;
    }

    private static ByteArrayContent Rpc()
    {
        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = "rpc-chave",
            ["method"] = "message/send",
            ["params"] = new JsonObject
            {
                ["message"] = new JsonObject
                {
                    ["kind"] = "message",
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

    private sealed class LogsCapturados : ILoggerProvider
    {
        public ConcurrentQueue<string> Mensagens { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Registrador(Mensagens);

        public void Dispose()
        {
        }

        private sealed class Registrador(ConcurrentQueue<string> destino) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                destino.Enqueue(formatter(state, exception) + " " + exception);
        }
    }
}
