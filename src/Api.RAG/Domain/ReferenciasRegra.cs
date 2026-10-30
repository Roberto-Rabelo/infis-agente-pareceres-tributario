using System.Text.RegularExpressions;

namespace Api.RAG.Domain;

/// <summary>
/// Extrai identificadores de regra da base de conhecimento (convenções de docs/03, docs/02, NUCLEO-1 e NUCLEO-2)
/// e as obrigações que eles e os títulos de seção indicam.
/// </summary>
public static partial class ReferenciasRegra
{
    /// <summary>Segmentos de ID definidos na convenção <c>CRZ-&lt;ORIGEM&gt;-&lt;DESTINO&gt;-nnn</c> e <c>INT-&lt;OBRIGAÇÃO&gt;-nnn</c>.</summary>
    private static readonly Dictionary<string, string[]> Segmentos = new(StringComparer.Ordinal)
    {
        ["NFE"] = [CodigosObrigacao.Nfe, CodigosObrigacao.Nfce],
        ["NFS"] = [CodigosObrigacao.Nfse],
        ["DOC"] = [CodigosObrigacao.Nfe, CodigosObrigacao.Nfce, CodigosObrigacao.Nfse],
        ["EFD"] = [CodigosObrigacao.EfdIcmsIpi],
        ["CTB"] = [CodigosObrigacao.EfdContribuicoes],
        ["ECD"] = [CodigosObrigacao.Ecd],
        ["ECF"] = [CodigosObrigacao.Ecf],
        ["DIMP"] = [CodigosObrigacao.Dimp],
    };

    private static readonly (Regex Padrao, string Codigo)[] NomesObrigacao =
    [
        (NomeNfce(), CodigosObrigacao.Nfce),
        (NomeNfe(), CodigosObrigacao.Nfe),
        (NomeNfse(), CodigosObrigacao.Nfse),
        (NomeEfdIcms(), CodigosObrigacao.EfdIcmsIpi),
        (NomeEfdContribuicoes(), CodigosObrigacao.EfdContribuicoes),
        (NomeEcd(), CodigosObrigacao.Ecd),
        (NomeEcf(), CodigosObrigacao.Ecf),
        (NomeDimp(), CodigosObrigacao.Dimp),
    ];

    public static bool EhIdValido(string id) => IdCompleto().IsMatch(id);

    public static IReadOnlyList<string> Extrair(string texto) =>
        Id().Matches(texto).Select(m => m.Value).Distinct(StringComparer.Ordinal).ToList();

    public static IReadOnlyList<string> ObrigacoesDosIds(IEnumerable<string> ids)
    {
        var codigos = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            var partes = id.Split('-');
            if (partes[0] is not ("CRZ" or "INT" or "EXC"))
            {
                continue;
            }

            foreach (var parte in partes.Skip(1).SkipLast(1))
            {
                if (Segmentos.TryGetValue(parte, out var tipos))
                {
                    codigos.UnionWith(tipos);
                }
            }
        }

        return codigos.ToList();
    }

    /// <summary>Obrigações nomeadas no texto (usado apenas em títulos de seção, onde o nome define o escopo).</summary>
    public static IReadOnlyList<string> ObrigacoesNomeadas(string texto)
    {
        var codigos = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (padrao, codigo) in NomesObrigacao)
        {
            if (padrao.IsMatch(texto))
            {
                codigos.Add(codigo);
            }
        }

        return codigos.ToList();
    }

    [GeneratedRegex(@"\b(?:(?:CRZ|INT|PRE|EXC|ENT)(?:-[A-Z]{2,5})*-[0-9]{3}|M[0-9]{1,2}-[A-Z]?[0-9]{2})\b")]
    private static partial Regex Id();

    [GeneratedRegex(@"^(?:(?:CRZ|INT|PRE|EXC|ENT)(?:-[A-Z]{2,5})*-[0-9]{3}|M[0-9]{1,2}-[A-Z]?[0-9]{2})$")]
    private static partial Regex IdCompleto();

    [GeneratedRegex(@"\bNFC-e\b")]
    private static partial Regex NomeNfce();

    [GeneratedRegex(@"\bNF-e\b")]
    private static partial Regex NomeNfe();

    [GeneratedRegex(@"\bNFS-e\b")]
    private static partial Regex NomeNfse();

    [GeneratedRegex(@"\bEFD[ -]ICMS")]
    private static partial Regex NomeEfdIcms();

    [GeneratedRegex(@"\bEFD[ -]Contribui")]
    private static partial Regex NomeEfdContribuicoes();

    [GeneratedRegex(@"\bECD\b")]
    private static partial Regex NomeEcd();

    [GeneratedRegex(@"\bECF\b")]
    private static partial Regex NomeEcf();

    [GeneratedRegex(@"\bDIMP\b")]
    private static partial Regex NomeDimp();
}
