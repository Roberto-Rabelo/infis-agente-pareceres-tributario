using System.Text.RegularExpressions;

namespace Api.Agente.Endpoints;

/// <summary>Identificadores de execução e de correlação propagados em logs e respostas (P13).</summary>
public static partial class Correlacao
{
    public const string Cabecalho = "X-Correlation-Id";
    private const string ChaveExecucao = "execucaoId";
    private const string ChaveCorrelacao = "correlacaoId";

    public static string Execucao(HttpContext contexto) => (string)contexto.Items[ChaveExecucao]!;

    public static string DaRequisicao(HttpContext contexto) => (string)contexto.Items[ChaveCorrelacao]!;

    public static IApplicationBuilder UsarCorrelacao(this IApplicationBuilder app) => app.Use(async (contexto, proximo) =>
    {
        var recebido = contexto.Request.Headers[Cabecalho].ToString();
        var correlacao = PadraoValido().IsMatch(recebido) ? recebido : Guid.NewGuid().ToString("N");
        var execucao = Guid.NewGuid().ToString("N");
        contexto.Items[ChaveExecucao] = execucao;
        contexto.Items[ChaveCorrelacao] = correlacao;
        contexto.Response.Headers[Cabecalho] = correlacao;

        var logger = contexto.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Correlacao");
        using (logger.BeginScope(new Dictionary<string, object> { [ChaveExecucao] = execucao, [ChaveCorrelacao] = correlacao }))
        {
            await proximo(contexto).ConfigureAwait(false);
        }
    });

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex PadraoValido();
}
