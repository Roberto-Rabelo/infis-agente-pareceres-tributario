using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Application.PreCondicoes;

public sealed record AdvertenciaOrigem(
    string ObrigacaoId,
    string AgenteOrigem,
    string? Codigo,
    string Mensagem,
    string? Registro,
    string? Campo,
    long? Linha)
{
    public Severidade Severidade => Severidade.Alerta;
}

public sealed record AptidaoObrigacao(
    string ObrigacaoId,
    TipoObrigacao? Tipo,
    bool Apta,
    IReadOnlyList<string> RegrasNaoAtendidas,
    string? MotivoDescarte,
    IReadOnlyList<AdvertenciaOrigem> AdvertenciasOrigem);

public sealed record ResultadoPreCondicoes(
    IReadOnlyList<ObrigacaoRecebida> Aptas,
    IReadOnlyList<AptidaoObrigacao> Aptidao,
    IReadOnlyList<Achado> Achados);

/// <summary>Pré-condições PRE-001 a PRE-005 (constitution P5, P6, P18; spec §5.1).</summary>
public sealed class VerificadorPreCondicoes
{
    public const string Contrato = "PRE-001";
    public const string Versao = "PRE-002";
    public const string ValidacaoDeOrigem = "PRE-003";
    public const string EscrituracaoVigente = "PRE-004";
    public const string GrupoCruzamento = "PRE-005";

    public static readonly IReadOnlyList<string> Todas = [Contrato, Versao, ValidacaoDeOrigem, EscrituracaoVigente, GrupoCruzamento];

    public VerificadorPreCondicoes(CatalogoVersoesLayout _)
    {
    }

    public ResultadoPreCondicoes Verificar(LoteLido lote, IReadOnlyList<ObrigacaoMapeada> mapeadas)
    {
        var achados = new List<Achado>();
        var naoAtendidas = mapeadas.ToDictionary<ObrigacaoMapeada, ObrigacaoMapeada, List<string>>(
            m => m, _ => [], ReferenceEqualityComparer.Instance);
        var idsVistos = new HashSet<string>(StringComparer.Ordinal);
        var candidatas = new List<(ObrigacaoMapeada Mapeada, ObrigacaoRecebida Obrigacao)>();

        foreach (var mapeada in mapeadas)
        {
            var falhas = naoAtendidas[mapeada];
            if (!idsVistos.Add(mapeada.Id))
            {
                Registrar(achados, falhas, Achado.PreCondicao(Contrato, "Identificador de obrigação repetido no lote.", new LocalizacaoEvidencia(mapeada.Id, Campo: "id")));
                continue;
            }

            if (mapeada.Obrigacao is not { } obrigacao)
            {
                Registrar(achados, falhas, Achado.PreCondicao(Contrato, "Contrato de entrada não atendido: " + string.Join(" ", mapeada.Erros), new LocalizacaoEvidencia(mapeada.Id)));
                continue;
            }

            if (VerificarValidacaoOrigem(obrigacao) is { } erroValidacao)
            {
                Registrar(achados, falhas, Achado.PreCondicao(ValidacaoDeOrigem, erroValidacao, new LocalizacaoEvidencia(obrigacao.Id, Campo: "validacaoOrigem")));
            }

            if (obrigacao.Cnpj.Raiz != lote.RaizCnpj)
            {
                Registrar(achados, falhas, Achado.PreCondicao(GrupoCruzamento, "Contribuinte da obrigação difere do contribuinte do lote.", new LocalizacaoEvidencia(obrigacao.Id, Campo: "contribuinte.cnpj")));
            }

            if (!obrigacao.Periodo.SobrepoeA(lote.Periodo))
            {
                Registrar(achados, falhas, Achado.PreCondicao(GrupoCruzamento, $"Período da obrigação ({obrigacao.Periodo}) não se sobrepõe ao período do lote ({lote.Periodo}).", new LocalizacaoEvidencia(obrigacao.Id, Campo: "periodo")));
            }

            if (falhas.Count == 0)
            {
                candidatas.Add((mapeada, obrigacao));
            }
        }

        var descartes = DeterminarVigentes(candidatas.Select(c => c.Obrigacao).ToList(), achados, naoAtendidas, candidatas);

        var aptas = candidatas
            .Where(c => naoAtendidas[c.Mapeada].Count == 0 && !descartes.ContainsKey(c.Obrigacao.Id))
            .Select(c => c.Obrigacao)
            .ToList();

        var aptidao = mapeadas.Select(m => new AptidaoObrigacao(
            m.Id,
            m.Tipo,
            naoAtendidas[m].Count == 0 && m.Obrigacao is not null && !descartes.ContainsKey(m.Obrigacao.Id),
            naoAtendidas[m].Distinct().ToList(),
            m.Obrigacao is not null ? descartes.GetValueOrDefault(m.Obrigacao.Id) : null,
            Advertencias(m.Obrigacao))).ToList();

        return new ResultadoPreCondicoes(aptas, aptidao, achados);
    }

    /// <summary>Critério de aptidão proposto (D25): relatório presente, APROVADO e sem ocorrências do tipo ERRO.</summary>
    private static string? VerificarValidacaoOrigem(ObrigacaoRecebida obrigacao) => obrigacao.ValidacaoOrigem switch
    {
        null => "Relatório de validação de origem ausente.",
        { Status: StatusValidacaoOrigem.Reprovado } => "Obrigação reprovada na validação de origem.",
        var v when v.Ocorrencias.Any(o => o.Tipo == TipoOcorrenciaOrigem.Erro) =>
            "Validação de origem registra erros; a obrigação não é apta ao cruzamento.",
        _ => null,
    };

    /// <summary>
    /// PRE-004: para o mesmo tipo, CNPJ e período, a vigente é a que nenhuma outra substitui (cadeia reciboAnterior → recibo).
    /// Cadeia ambígua torna todas não aptas. Documentos repetidos pela chave de acesso mantêm o primeiro.
    /// </summary>
    private static Dictionary<string, string> DeterminarVigentes(
        IReadOnlyList<ObrigacaoRecebida> obrigacoes,
        List<Achado> achados,
        Dictionary<ObrigacaoMapeada, List<string>> naoAtendidas,
        List<(ObrigacaoMapeada Mapeada, ObrigacaoRecebida Obrigacao)> candidatas)
    {
        var descartes = new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> FalhasDe(ObrigacaoRecebida o) => naoAtendidas[candidatas.First(c => ReferenceEquals(c.Obrigacao, o)).Mapeada];

        foreach (var grupo in obrigacoes.Where(o => o.EhDocumento).GroupBy(o => (o.Tipo, o.Identificacao.ChaveAcesso)))
        {
            var primeira = grupo.First();
            foreach (var repetida in grupo.Skip(1))
            {
                descartes[repetida.Id] = $"Documento repetido no lote (mesma chave de acesso de {primeira.Id}).";
            }
        }

        foreach (var grupo in obrigacoes.Where(o => !o.EhDocumento).GroupBy(o => (o.Tipo, o.Cnpj, o.Periodo)).Where(g => g.Count() > 1))
        {
            var referenciados = grupo
                .Select(o => o.Identificacao.ReciboAnterior)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToHashSet(StringComparer.Ordinal);
            var vigentes = grupo
                .Where(o => string.IsNullOrWhiteSpace(o.Identificacao.Recibo) || !referenciados.Contains(o.Identificacao.Recibo))
                .ToList();

            if (vigentes.Count == 1)
            {
                foreach (var substituida in grupo.Where(o => !ReferenceEquals(o, vigentes[0])))
                {
                    descartes[substituida.Id] = $"Escrituração substituída por {vigentes[0].Id}.";
                }

                continue;
            }

            foreach (var obrigacao in grupo)
            {
                Registrar(
                    achados,
                    FalhasDe(obrigacao),
                    Achado.PreCondicao(
                        EscrituracaoVigente,
                        "Mais de uma escrituração candidata a vigente para o mesmo tipo, CNPJ e período; não é possível determinar a vigente.",
                        new LocalizacaoEvidencia(obrigacao.Id, Campo: "identificacao")));
            }
        }

        return descartes;
    }

    private static List<AdvertenciaOrigem> Advertencias(ObrigacaoRecebida? obrigacao) =>
        obrigacao?.ValidacaoOrigem is { } validacao
            ? validacao.Ocorrencias
                .Where(o => o.Tipo == TipoOcorrenciaOrigem.Advertencia)
                .Select(o => new AdvertenciaOrigem(obrigacao.Id, validacao.Agente, o.Codigo, o.Mensagem, o.Registro, o.Campo, o.Linha))
                .ToList()
            : [];

    private static void Registrar(List<Achado> achados, List<string> falhas, Achado achado)
    {
        achados.Add(achado);
        falhas.Add(achado.RegraId);
    }
}
