namespace Api.Agente.Endpoints;

/// <summary>
/// Leitura do corpo bruto com limite de tamanho (constitution 5.1). O corpo não passa pelo model binding do MVC:
/// a desserialização estrita e os erros padronizados são responsabilidade do contrato de entrada.
/// </summary>
public static class CorpoRequisicao
{
    /// <returns>Os bytes lidos, ou nulo se o corpo exceder <paramref name="limite"/>.</returns>
    public static async Task<byte[]?> LerLimitadoAsync(HttpRequest requisicao, long limite, CancellationToken cancelamento)
    {
        if (requisicao.ContentLength > limite)
        {
            return null;
        }

        using var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int lidos;
        while ((lidos = await requisicao.Body.ReadAsync(buffer, cancelamento).ConfigureAwait(false)) > 0)
        {
            if (memoria.Length + lidos > limite)
            {
                return null;
            }

            await memoria.WriteAsync(buffer.AsMemory(0, lidos), cancelamento).ConfigureAwait(false);
        }

        return memoria.ToArray();
    }
}
