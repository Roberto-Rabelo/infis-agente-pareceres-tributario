namespace Api.Agente.Domain;

/// <summary>
/// CNPJ alfanumérico: 12 posições alfanuméricas + 2 dígitos verificadores numéricos.
/// Valor de cada caractere = código ASCII − 48; pesos e módulo 11 do algoritmo tradicional.
/// Nunca é convertido para número (doc 04 §2).
/// </summary>
public readonly record struct Cnpj
{
    public const int TamanhoRaiz = 8;
    private const int Tamanho = 14;
    private static readonly int[] PesosPrimeiroDv = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosSegundoDv = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Cnpj(string valor) => Valor = valor;

    public string Valor { get; }

    public string Raiz => Valor[..TamanhoRaiz];

    public override string ToString() => Valor;

    public static string Normalizar(string entrada) =>
        new string(entrada.Where(c => c is not ('.' or '/' or '-' or ' ')).ToArray()).ToUpperInvariant();

    public static bool TentarCriar(string? entrada, out Cnpj cnpj, out string? erro)
    {
        cnpj = default;
        if (string.IsNullOrWhiteSpace(entrada))
        {
            erro = "CNPJ não informado.";
            return false;
        }

        var valor = Normalizar(entrada);
        if (valor.Length != Tamanho)
        {
            erro = "CNPJ deve ter 14 posições.";
            return false;
        }

        for (var i = 0; i < 12; i++)
        {
            if (!char.IsAsciiDigit(valor[i]) && !char.IsAsciiLetterUpper(valor[i]))
            {
                erro = "CNPJ contém caractere inválido nas 12 primeiras posições.";
                return false;
            }
        }

        if (!char.IsAsciiDigit(valor[12]) || !char.IsAsciiDigit(valor[13]))
        {
            erro = "Dígitos verificadores do CNPJ devem ser numéricos.";
            return false;
        }

        if (valor.All(c => c == valor[0]))
        {
            erro = "CNPJ inválido.";
            return false;
        }

        var dv1 = CalcularDv(valor.AsSpan(0, 12), PesosPrimeiroDv);
        var dv2 = CalcularDv(valor.AsSpan(0, 13), PesosSegundoDv);
        if (valor[12] - '0' != dv1 || valor[13] - '0' != dv2)
        {
            erro = "Dígito verificador do CNPJ não confere.";
            return false;
        }

        cnpj = new Cnpj(valor);
        erro = null;
        return true;
    }

    /// <summary>Aceita raiz (8 posições) ou CNPJ completo e devolve a raiz normalizada.</summary>
    public static bool TentarObterRaiz(string? entrada, out string raiz, out string? erro)
    {
        raiz = string.Empty;
        if (string.IsNullOrWhiteSpace(entrada))
        {
            erro = "CNPJ do contribuinte não informado.";
            return false;
        }

        var valor = Normalizar(entrada);
        if (valor.Length == TamanhoRaiz)
        {
            if (!valor.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)))
            {
                erro = "Raiz do CNPJ contém caractere inválido.";
                return false;
            }

            raiz = valor;
            erro = null;
            return true;
        }

        if (!TentarCriar(valor, out var cnpj, out erro))
        {
            return false;
        }

        raiz = cnpj.Raiz;
        return true;
    }

    private static int CalcularDv(ReadOnlySpan<char> base_, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < base_.Length; i++)
        {
            soma += (base_[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
