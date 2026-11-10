namespace Api.Agente.Application.A2A;

public sealed class OpcoesA2A
{
    public const string Secao = "A2A";

    /// <summary>Variável de ambiente com a chave Bearer compartilhada da SOFIA (skill sofia-a2a-autenticacao).</summary>
    public const string VariavelToken = "SOFIA_A2A_TOKEN";

    public const int TamanhoMinimoChave = 16;

    public string Url { get; set; } = "http://localhost:5117/a2a";

    public string IdAgente { get; set; } = "agente-parecer-tributario";

    public string Nome { get; set; } = "Agente Parecer Tributário";

    /// <summary>Preenchida somente a partir de <see cref="VariavelToken"/>; segredo, nunca versionado nem logado.</summary>
    public string? ChaveIntegracao { get; set; }

    /// <summary>Sem chave válida o card e o RPC ficam indisponíveis (503): nunca há acesso livre por configuração ausente.</summary>
    public bool IntegracaoDisponivel => (ChaveIntegracao?.Trim().Length ?? 0) >= TamanhoMinimoChave;

    public string Descricao { get; set; } =
        "Cruzamento de obrigações fiscais já validadas na origem e parecer técnico. Não certifica conformidade.";
}
