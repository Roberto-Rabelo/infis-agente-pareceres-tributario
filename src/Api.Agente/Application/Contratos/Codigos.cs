using Api.Agente.Domain;

namespace Api.Agente.Application.Contratos;

/// <summary>Códigos textuais usados nos contratos JSON (entrada, catálogo e saída).</summary>
public static class Codigos
{
    private static readonly Dictionary<string, TipoObrigacao> TiposObrigacao = new(StringComparer.Ordinal)
    {
        ["NFE"] = TipoObrigacao.NFe,
        ["NFCE"] = TipoObrigacao.NFCe,
        ["NFSE"] = TipoObrigacao.NFSe,
        ["DIMP"] = TipoObrigacao.Dimp,
        ["EFD_ICMS_IPI"] = TipoObrigacao.EfdIcmsIpi,
        ["EFD_CONTRIBUICOES"] = TipoObrigacao.EfdContribuicoes,
        ["ECD"] = TipoObrigacao.Ecd,
        ["ECF"] = TipoObrigacao.Ecf,
    };

    private static readonly Dictionary<TipoObrigacao, string> CodigosObrigacao =
        TiposObrigacao.ToDictionary(par => par.Value, par => par.Key);

    private static readonly Dictionary<string, TipoRelacao> Relacoes = new(StringComparer.Ordinal)
    {
        ["PAR"] = TipoRelacao.Parametro,
        ["EXC"] = TipoRelacao.Exclusividade,
        ["PRE_DIRETA"] = TipoRelacao.PresencaDireta,
        ["PRE_REVERSA"] = TipoRelacao.PresencaReversa,
        ["IDT"] = TipoRelacao.Identidade,
        ["AGR"] = TipoRelacao.Agregacao,
        ["LIM"] = TipoRelacao.Limite,
        ["CNT"] = TipoRelacao.Continuidade,
    };

    public static IEnumerable<string> CodigosTipoObrigacao => TiposObrigacao.Keys;

    public static bool TentarTipoObrigacao(string? codigo, out TipoObrigacao tipo) =>
        TiposObrigacao.TryGetValue(codigo ?? string.Empty, out tipo);

    public static string De(TipoObrigacao tipo) => CodigosObrigacao[tipo];

    public static bool TentarRelacao(string? codigo, out TipoRelacao relacao) =>
        Relacoes.TryGetValue(codigo ?? string.Empty, out relacao);

    public static bool TentarFinalidade(string? codigo, out Finalidade finalidade)
    {
        (var ok, finalidade) = codigo switch
        {
            "ORIGINAL" => (true, Finalidade.Original),
            "SUBSTITUTA" => (true, Finalidade.Substituta),
            _ => (false, default),
        };
        return ok;
    }

    public static bool TentarStatusValidacao(string? codigo, out StatusValidacaoOrigem status)
    {
        (var ok, status) = codigo switch
        {
            "APROVADO" => (true, StatusValidacaoOrigem.Aprovado),
            "REPROVADO" => (true, StatusValidacaoOrigem.Reprovado),
            _ => (false, default),
        };
        return ok;
    }

    public static bool TentarTipoOcorrencia(string? codigo, out TipoOcorrenciaOrigem tipo)
    {
        (var ok, tipo) = codigo switch
        {
            "ERRO" => (true, TipoOcorrenciaOrigem.Erro),
            "ADVERTENCIA" => (true, TipoOcorrenciaOrigem.Advertencia),
            _ => (false, default),
        };
        return ok;
    }
}
