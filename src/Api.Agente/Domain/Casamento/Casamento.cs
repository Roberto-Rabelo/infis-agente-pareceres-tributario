namespace Api.Agente.Domain.Casamento;

/// <summary>Chave primária de documento: 44 posições (doc 03 §0). Com CNPJ alfanumérico, a chave pode conter letras.</summary>
public static class ChaveAcesso
{
    public const int Tamanho = 44;

    public static bool TentarNormalizar(string? entrada, out string chave)
    {
        chave = string.Empty;
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return false;
        }

        var valor = new string(entrada.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (valor.StartsWith("NFE", StringComparison.Ordinal) && valor.Length == Tamanho + 3)
        {
            valor = valor[3..];
        }

        if (valor.Length != Tamanho || !valor.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)))
        {
            return false;
        }

        chave = valor;
        return true;
    }
}

/// <summary>Chave secundária quando não há chave de acesso: CNPJ emitente + modelo + série + número + data de emissão (doc 03 §0).</summary>
public sealed record ChaveSecundaria(string CnpjEmitente, string Modelo, string Serie, string Numero, DateOnly DataEmissao)
{
    public static ChaveSecundaria Criar(Cnpj emitente, string modelo, string serie, string numero, DateOnly dataEmissao) =>
        new(emitente.Valor, SemZerosAEsquerda(modelo), SemZerosAEsquerda(serie), SemZerosAEsquerda(numero), dataEmissao);

    private static string SemZerosAEsquerda(string valor)
    {
        var limpo = valor.Trim().TrimStart('0');
        return limpo.Length == 0 ? "0" : limpo;
    }
}

/// <summary>Casamento de participantes sempre pelo CNPJ/CPF resolvido via 0150, nunca pelo COD_PART (NUCLEO-1 §3.2).</summary>
public sealed class ResolvedorParticipantes
{
    private readonly Dictionary<string, string> _documentoPorCodigo = new(StringComparer.Ordinal);

    public ResolvedorParticipantes(ObrigacaoRecebida escrituracao)
    {
        foreach (var ocorrencia in escrituracao.Registro("0150"))
        {
            var codigo = ocorrencia.Campos.GetValueOrDefault("COD_PART");
            var documento = ocorrencia.Campos.GetValueOrDefault("CNPJ") is { Length: > 0 } cnpj
                ? Cnpj.Normalizar(cnpj)
                : ocorrencia.Campos.GetValueOrDefault("CPF");
            if (!string.IsNullOrWhiteSpace(codigo) && !string.IsNullOrWhiteSpace(documento))
            {
                _documentoPorCodigo[codigo] = documento;
            }
        }
    }

    public string? Resolver(string? codPart) =>
        codPart is not null && _documentoPorCodigo.TryGetValue(codPart, out var documento) ? documento : null;
}

public enum PapelContribuinte
{
    Emitente,
    Destinatario,
}

/// <param name="IndOper">0 = entrada; 1 = saída (C100.IND_OPER).</param>
/// <param name="IndEmit">0 = emissão própria; 1 = terceiros (C100.IND_EMIT).</param>
/// <param name="RequerConferenciaCfop">Caso que deve ser confirmado pelo CFOP (NUCLEO-1 §3.5, última linha).</param>
public sealed record Sentido(int IndOper, int IndEmit, bool RequerConferenciaCfop);

/// <summary>Tabela de correspondência tpNF × papel do contribuinte × IND_OPER × IND_EMIT (NUCLEO-1 §3.5).</summary>
public static class TabelaSentido
{
    public static Sentido Resolver(int tpNF, PapelContribuinte papel) => (tpNF, papel) switch
    {
        (1, PapelContribuinte.Emitente) => new Sentido(1, 0, false),
        (0, PapelContribuinte.Emitente) => new Sentido(0, 0, false),
        (1, PapelContribuinte.Destinatario) => new Sentido(0, 1, false),
        (0, PapelContribuinte.Destinatario) => new Sentido(1, 1, true),
        _ => throw new ArgumentOutOfRangeException(nameof(tpNF), tpNF, "tpNF deve ser 0 (entrada) ou 1 (saída)."),
    };
}
