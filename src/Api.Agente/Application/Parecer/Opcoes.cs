namespace Api.Agente.Application.Parecer;

/// <summary>ADR D10 (modelo e saída estruturada) e D17 (retentativas), propostos. Modelo sem valor padrão: escolha pendente.</summary>
public sealed class OpcoesParecer
{
    public const string Secao = "Parecer";

    public bool Habilitado { get; set; }

    public string Provedor { get; set; } = "OpenAI";

    public string Modelo { get; set; } = string.Empty;

    public string VersaoPrompt { get; set; } = "1.1.0";

    public int TimeoutSegundos { get; set; } = 60;

    public int RetentativasTransitorias { get; set; } = 2;

    public int RetentativasSaidaInvalida { get; set; } = 2;

    public int EsperaRetentativaMs { get; set; } = 500;

    public int MaximoChamadasFerramenta { get; set; } = 3;

    /// <summary>Lista de permissão: a escolha do modelo não autoriza a execução (P11).</summary>
    public IReadOnlyList<string> FerramentasAutorizadas { get; set; } = [PluginBaseConhecimento.NomeCompleto];

    /// <summary>Nulo = padrão do modelo (sem avaliação que justifique outro valor).</summary>
    public double? Temperatura { get; set; }

    public int? MaximoTokensSaida { get; set; }
}

/// <summary>Tabela de preços versionada e externa ao código (constitution 4.5). Vazia = custo desconhecido.</summary>
public sealed class OpcoesCustos
{
    public const string Secao = "Custos";

    public string Versao { get; set; } = string.Empty;

    public string Moeda { get; set; } = string.Empty;

    public Dictionary<string, PrecoModelo> Modelos { get; set; } = [];
}

public sealed class PrecoModelo
{
    public decimal EntradaPorMilhao { get; set; }

    public decimal SaidaPorMilhao { get; set; }
}

public static class CalculadoraCusto
{
    /// <summary>Custo estimado só quando a tabela tem o modelo e todos os tokens são conhecidos.</summary>
    public static (decimal? Custo, string? Tabela) Estimar(OpcoesCustos custos, string modelo, long? entrada, long? saida)
    {
        if (string.IsNullOrWhiteSpace(custos.Versao) || !custos.Modelos.TryGetValue(modelo, out var preco) || entrada is null || saida is null)
        {
            return (null, null);
        }

        var custo = ((entrada.Value * preco.EntradaPorMilhao) + (saida.Value * preco.SaidaPorMilhao)) / 1_000_000m;
        return (decimal.Round(custo, 8), $"{custos.Versao} ({custos.Moeda})");
    }
}

public interface IRegistroConsumoLlm
{
    Task RegistrarAsync(RegistroConsumoLlm registro, CancellationToken cancelamento);
}
