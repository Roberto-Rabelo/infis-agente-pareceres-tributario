using Api.Agente.Application.Conhecimento;
using Api.Agente.Application.Contratos.Entrada;
using Api.Agente.Application.PreCondicoes;
using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;
using Api.Agente.Domain.Motor;

namespace Api.Agente.Application.Analise;

public sealed record ItemCobertura(string ObrigacaoId, TipoObrigacao Tipo, string Cnpj, string? InscricaoEstadual, Periodo Periodo, bool Apta);

public sealed record RevisaoHumana(bool Requerida, IReadOnlyList<string> Motivos);

public sealed record ResultadoAnalise(
    StatusAnalise Status,
    string RaizCnpj,
    Periodo Periodo,
    IReadOnlyList<AptidaoObrigacao> Aptidao,
    IReadOnlyList<ItemCobertura> Cobertura,
    IReadOnlyList<Achado> Achados,
    IReadOnlyList<ResultadoRegra> Regras,
    ParametrosComparacao Parametros,
    RevisaoHumana RevisaoHumana)
{
    /// <summary>Trechos da base de conhecimento por regra com achado (etapa 6; ver ServicoFundamentacao).</summary>
    public IReadOnlyList<FundamentacaoAchado> Fundamentacao { get; init; } = [];

    /// <summary>Limitações e validações não realizadas (P4, P7).</summary>
    public IReadOnlyList<string> Limitacoes { get; init; } = [];
}

/// <summary>Fluxo da constitution 3.4, etapas 2 a 5. A etapa 6 está em ServicoFundamentacao; o LLM ainda não foi integrado.</summary>
public sealed class ServicoAnalise
{
    private readonly CatalogoRegras _catalogo;
    private readonly VerificadorPreCondicoes _preCondicoes;
    private readonly MotorRegras _motor;
    private readonly ParametrosComparacao _parametros;
    private readonly LeitorLote _leitor;

    public ServicoAnalise(
        CatalogoRegras catalogo,
        VerificadorPreCondicoes preCondicoes,
        MotorRegras motor,
        ParametrosComparacao parametros,
        LeitorLote leitor)
    {
        _catalogo = catalogo;
        _preCondicoes = preCondicoes;
        _motor = motor;
        _parametros = parametros;
        _leitor = leitor;
    }

    public string VersaoCatalogo => _catalogo.Versao;

    public ResultadoAnalise Analisar(LoteLido lote)
    {
        var mapeadas = lote.Obrigacoes.Select(o => MapeadorObrigacao.Mapear(o, _leitor.OpcoesJson)).ToList();
        var preCondicoes = _preCondicoes.Verificar(lote, mapeadas);

        var tiposRecebidos = mapeadas.Where(m => m.Tipo is not null).Select(m => m.Tipo!.Value).ToHashSet();
        var contexto = new ContextoCruzamento(lote.RaizCnpj, lote.Periodo, preCondicoes.Aptas, tiposRecebidos, _parametros);
        var motor = _motor.Executar(_catalogo, contexto);

        var achados = preCondicoes.Achados.Concat(motor.Achados).ToList();
        var aptas = preCondicoes.Aptas.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);
        var cobertura = mapeadas
            .Where(m => m.Obrigacao is not null)
            .Select(m => m.Obrigacao!)
            .Select(o => new ItemCobertura(o.Id, o.Tipo, o.Cnpj.Valor, o.InscricaoEstadual, o.Periodo, aptas.Contains(o.Id)))
            .ToList();

        var status = DeterminarStatus(motor, achados);
        return new ResultadoAnalise(
            status,
            lote.RaizCnpj,
            lote.Periodo,
            preCondicoes.Aptidao,
            cobertura,
            achados,
            motor.Regras,
            _parametros,
            DeterminarRevisaoHumana(status, motor, achados))
        {
            Limitacoes = [CoberturaCatalogo(_catalogo)],
        };
    }

    /// <summary>FR-410: quanto do catálogo de coesão executa hoje. Regra proposta não executa até ser aprovada (FR-402).</summary>
    internal static string CoberturaCatalogo(CatalogoRegras catalogo)
    {
        var coesao = catalogo.Regras.Where(r => r.Tipo == TipoRegra.Cruzamento).ToList();
        var aprovadas = coesao.Count(r => r.Status == StatusRegra.Aprovada);
        return $"Catálogo {catalogo.Versao}: {aprovadas} de {coesao.Count} regras de cruzamento aprovadas e executadas conforme as obrigações recebidas; "
            + $"{coesao.Count - aprovadas} ainda propostas, não executadas até aprovação.";
    }

    /// <summary>P4 / FR-038 / FR-039: CONCLUIDA só quando todas as validações obrigatórias foram executadas.</summary>
    internal static StatusAnalise DeterminarStatus(ResultadoMotor motor, IReadOnlyList<Achado> achados)
    {
        var executadas = motor.Regras.Count(r => r.Situacao is SituacaoRegra.Executada or SituacaoRegra.ComInconsistencia);
        if (executadas == 0)
        {
            return StatusAnalise.Inconclusiva;
        }

        var pendentes = motor.Regras.Any(r => r.Obrigatoria && r.Situacao is SituacaoRegra.AusenciaEvidencias or SituacaoRegra.NaoConclusiva or SituacaoRegra.FalhaTecnica);
        var preCondicaoNaoAtendida = achados.Any(a => a.Natureza == Natureza.PreCondicaoNaoAtendida);
        return pendentes || preCondicaoNaoAtendida ? StatusAnalise.ConcluidaParcial : StatusAnalise.Concluida;
    }

    /// <summary>Critérios mínimos da P15.</summary>
    internal static RevisaoHumana DeterminarRevisaoHumana(StatusAnalise status, ResultadoMotor motor, IReadOnlyList<Achado> achados)
    {
        var motivos = new List<string>();
        if (achados.Any(a => a.Severidade == Severidade.ErroGrave))
        {
            motivos.Add("Há erro grave.");
        }

        if (achados.Any(a => a.Natureza == Natureza.IndicioIa))
        {
            motivos.Add("Há indício identificado pela IA.");
        }

        if (achados.Any(a => a.Natureza == Natureza.PreCondicaoNaoAtendida))
        {
            motivos.Add("Há obrigação com pré-condição de entrada não atendida.");
        }

        if (achados.Any(a => a.Natureza == Natureza.AusenciaEvidencias))
        {
            motivos.Add("Há cruzamentos sem evidências suficientes.");
        }

        if (achados.Any(a => a.Natureza == Natureza.FalhaTecnica))
        {
            motivos.Add("Há falha técnica.");
        }

        if (motor.Regras.Any(r => r.Obrigatoria && r.Situacao == SituacaoRegra.NaoConclusiva))
        {
            motivos.Add("Há validações obrigatórias não realizadas.");
        }

        if (status == StatusAnalise.Inconclusiva)
        {
            motivos.Add("Nenhum cruzamento foi executado.");
        }

        return new RevisaoHumana(motivos.Count > 0, motivos);
    }
}
