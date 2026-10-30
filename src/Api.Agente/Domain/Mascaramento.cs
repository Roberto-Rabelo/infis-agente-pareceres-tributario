namespace Api.Agente.Domain;

/// <summary>Mascaramento de dados sensíveis para logs (constitution 5.1; FR-048).</summary>
public static class Mascaramento
{
    public static string Documento(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        var visivel = Math.Min(2, valor.Length / 4);
        return string.Concat(valor.AsSpan(0, visivel), new string('*', valor.Length - (2 * visivel)), valor.AsSpan(valor.Length - visivel));
    }
}
