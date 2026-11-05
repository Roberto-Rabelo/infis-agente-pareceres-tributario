namespace Api.Agente.Application.A2A;

/// <summary>
/// URL pública do RPC A2A. Configuração explícita prevalece; senão usa o host da requisição
/// (necessário no Container Runtime da Vercel, onde a URL só existe após o deploy).
/// </summary>
public static class ResolvedorUrlA2A
{
    public const string CaminhoRpc = "/a2a";

    public static string Resolver(string? configurada, string esquema, string host)
    {
        if (!string.IsNullOrWhiteSpace(configurada))
        {
            return configurada.TrimEnd('/');
        }

        var proto = string.IsNullOrWhiteSpace(esquema) ? "https" : esquema;
        var autoridade = string.IsNullOrWhiteSpace(host) ? "localhost" : host.TrimEnd('/');
        return $"{proto}://{autoridade}{CaminhoRpc}";
    }
}
