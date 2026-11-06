namespace Api.Agente.Application.A2A;

public sealed class OpcoesA2A
{
    public const string Secao = "A2A";

    public string Url { get; set; } = "http://localhost:5117/a2a";

    public string IdAgente { get; set; } = "agente-parecer-tributario";

    public string Nome { get; set; } = "Agente Parecer Tributário";

    public string Descricao { get; set; } =
        "Cruzamento de obrigações fiscais já validadas na origem e parecer técnico. Não certifica conformidade.";
}
