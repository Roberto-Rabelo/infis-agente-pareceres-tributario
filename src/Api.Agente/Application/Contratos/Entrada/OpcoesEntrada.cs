namespace Api.Agente.Application.Contratos.Entrada;

public sealed class OpcoesEntrada
{
    public const string Secao = "Entrada";

    public long TamanhoMaximoBytes { get; set; } = 50L * 1024 * 1024;

    public int ProfundidadeMaximaJson { get; set; } = 64;

    public int MaximoObrigacoesPorLote { get; set; } = 500;
}
