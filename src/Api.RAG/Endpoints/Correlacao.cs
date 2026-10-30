using System.Text.RegularExpressions;

namespace Api.RAG.Endpoints;

public static partial class Correlacao
{
    public const string Cabecalho = "X-Correlation-Id";

    /// <summary>Aceita apenas o formato acordado; valores fora dele são descartados (P8).</summary>
    public static string? DaRequisicao(HttpContext? contexto)
    {
        var valor = contexto?.Request.Headers[Cabecalho].ToString();
        return !string.IsNullOrEmpty(valor) && Formato().IsMatch(valor) ? valor : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex Formato();
}
