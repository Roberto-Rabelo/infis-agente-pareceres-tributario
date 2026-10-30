using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Domain.Motor;

public interface IValidadorCruzamento
{
    string RegraId { get; }

    IReadOnlyList<Achado> Validar(ContextoCruzamento contexto, Regra regra);
}

/// <param name="RegraId">Identificador da regra no catálogo.</param>
/// <param name="Situacao">Resultado da execução da regra.</param>
/// <param name="Motivo">Por que a regra não foi executada ou não concluiu; nulo quando executada.</param>
/// <param name="Obrigatoria">Conta para a P4: regra aprovada, vigente e com ao menos uma de suas obrigações no lote.</param>
public sealed record ResultadoRegra(string RegraId, SituacaoRegra Situacao, string? Motivo, bool Obrigatoria);

public sealed record ResultadoMotor(IReadOnlyList<Achado> Achados, IReadOnlyList<ResultadoRegra> Regras);

public sealed class MotorRegras
{
    private readonly Dictionary<string, IValidadorCruzamento> _validadores;

    public MotorRegras(IEnumerable<IValidadorCruzamento> validadores)
    {
        _validadores = validadores.ToDictionary(v => v.RegraId);
    }

    public ResultadoMotor Executar(CatalogoRegras catalogo, ContextoCruzamento contexto)
    {
        var achados = new List<Achado>();
        var resultados = new Dictionary<string, ResultadoRegra>();

        var aprovadas = catalogo.Regras
            .Where(r => r.Tipo == TipoRegra.Cruzamento && r.Status == StatusRegra.Aprovada)
            .OrderBy(r => OrdemExecucao.De(r.Relacao!.Value))
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .ToList();
        var porId = aprovadas.ToDictionary(r => r.Id);

        void Processar(Regra regra)
        {
            if (resultados.ContainsKey(regra.Id))
            {
                return;
            }

            foreach (var dependencia in regra.DependeDe)
            {
                if (porId.TryGetValue(dependencia, out var regraDependencia))
                {
                    Processar(regraDependencia);
                }
            }

            var (resultado, achadosRegra) = ExecutarRegra(regra, contexto, resultados);
            resultados[regra.Id] = resultado;
            achados.AddRange(achadosRegra);
        }

        foreach (var regra in aprovadas)
        {
            Processar(regra);
        }

        return new ResultadoMotor(achados, resultados.Values.ToList());
    }

    private (ResultadoRegra, IReadOnlyList<Achado>) ExecutarRegra(
        Regra regra,
        ContextoCruzamento contexto,
        Dictionary<string, ResultadoRegra> anteriores)
    {
        if (!regra.VigenteEm(contexto.Periodo))
        {
            return (new ResultadoRegra(regra.Id, SituacaoRegra.NaoAplicavel, "Regra fora de vigência para o período do lote.", false), []);
        }

        if (!regra.Obrigacoes.Keys.Any(contexto.TiposRecebidos.Contains))
        {
            return (new ResultadoRegra(regra.Id, SituacaoRegra.NaoAplicavel, "Nenhuma das obrigações da regra foi recebida no lote.", false), []);
        }

        foreach (var dependencia in regra.DependeDe)
        {
            if (!anteriores.TryGetValue(dependencia, out var anterior))
            {
                return (new ResultadoRegra(regra.Id, SituacaoRegra.NaoConclusiva, $"Depende de {dependencia}, que não está aprovada.", true), []);
            }

            if (anterior.Situacao is not (SituacaoRegra.Executada or SituacaoRegra.NaoAplicavel))
            {
                return (new ResultadoRegra(regra.Id, SituacaoRegra.NaoConclusiva, $"Depende de {dependencia}, que terminou como {anterior.Situacao}.", true), []);
            }
        }

        var faltantes = regra.Obrigacoes.Keys
            .Where(tipo => contexto.ObrigacoesPara(regra, tipo).Count == 0)
            .Select(tipo => new ObrigacaoFaltante(
                tipo,
                contexto.Periodo,
                contexto.TiposRecebidos.Contains(tipo)
                    ? "Recebida, mas não apta ao cruzamento ou em versão não coberta pela regra."
                    : "Não recebida no lote."))
            .ToList();
        if (faltantes.Count > 0)
        {
            var achado = new Achado
            {
                RegraId = regra.Id,
                Natureza = Natureza.AusenciaEvidencias,
                Descricao = $"Cruzamento não realizado: faltam obrigações aptas ({string.Join(", ", faltantes.Select(f => f.Tipo))}).",
                ObrigacoesFaltantes = faltantes,
            };
            return (new ResultadoRegra(regra.Id, SituacaoRegra.AusenciaEvidencias, achado.Descricao, true), [achado]);
        }

        if (!_validadores.TryGetValue(regra.Id, out var validador))
        {
            return FalhaTecnica(regra, "Regra aprovada sem validador registrado.");
        }

        IReadOnlyList<Achado> achados;
        try
        {
            achados = validador.Validar(contexto, regra);
        }
#pragma warning disable CA1031 // Falha de um validador vira achado de falha técnica, nunca resultado presumido (P11).
        catch (Exception)
#pragma warning restore CA1031
        {
            return FalhaTecnica(regra, "Erro na execução do validador.");
        }

        var situacao = achados.Any(a => a.Natureza == Natureza.InconsistenciaCruzamento)
            ? SituacaoRegra.ComInconsistencia
            : SituacaoRegra.Executada;
        return (new ResultadoRegra(regra.Id, situacao, null, true), achados);
    }

    private static (ResultadoRegra, IReadOnlyList<Achado>) FalhaTecnica(Regra regra, string motivo)
    {
        var achado = new Achado { RegraId = regra.Id, Natureza = Natureza.FalhaTecnica, Descricao = motivo };
        return (new ResultadoRegra(regra.Id, SituacaoRegra.FalhaTecnica, motivo, true), [achado]);
    }
}
