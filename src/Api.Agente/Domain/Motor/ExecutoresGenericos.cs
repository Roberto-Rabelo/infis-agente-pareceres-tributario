using System.Globalization;
using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Domain.Motor;

/// <summary>
/// Executores genéricos por tipo de relação e operador (spec 004, D405). Leem da própria regra o que comparar, por qual chave
/// e com qual tolerância. Uma regra que não cabe neles exige validador dedicado (D406), e o motivo fica registrado.
/// </summary>
public static class ExecutoresGenericos
{
    public static (IValidadorCruzamento? Validador, string? Motivo) Criar(Regra regra, CatalogoParametros parametros)
    {
        var operador = regra.Declaracao?.Operador;
        switch (regra.Relacao, operador)
        {
            case (TipoRelacao.Exclusividade, "FILTRA"):
                return ExecutorExclusao.Criar(regra);
            case (TipoRelacao.PresencaDireta or TipoRelacao.PresencaReversa, "EXISTE"):
                if (regra.Declaracao?.CondicaoDescritiva is { } condicao)
                {
                    return (null, $"presença com condição em texto livre ('{condicao}').");
                }

                return Com(regra, parametros, r => new ExecutorPresenca(regra.Id, r));
            case (TipoRelacao.Identidade, "IGUAL"):
            case (TipoRelacao.Limite, "MENOR_OU_IGUAL" or "MAIOR_OU_IGUAL"):
                return Com(regra, parametros, r => r.Origem.Elemento.Campos.Count > 0 && r.Origem.Elemento.Campos.Count == r.Destino.Elemento.Campos.Count
                    ? new ExecutorComparacao(regra.Id, r, operador!)
                    : null, "origem e destino devem ter o mesmo número de campos.");
            default:
                return (null, $"sem executor genérico para {regra.Relacao} {operador}.");
        }
    }

    private static (IValidadorCruzamento?, string?) Com(
        Regra regra,
        CatalogoParametros parametros,
        Func<RegraInterpretada, IValidadorCruzamento?> criar,
        string? motivoSeNulo = null)
    {
        var (interpretada, motivo) = InterpretadorRegra.Interpretar(regra, parametros);
        if (interpretada is null)
        {
            return (null, motivo);
        }

        var validador = criar(interpretada);
        return validador is null ? (null, motivoSeNulo) : (validador, null);
    }

    internal static Achado Ausencia(Regra regra, string descricao, params LocalizacaoEvidencia[] evidencias) => new()
    {
        RegraId = regra.Id,
        Natureza = Natureza.AusenciaEvidencias,
        Descricao = descricao,
        Evidencias = evidencias,
    };

    internal static string Rotulo(TipoObrigacao tipo) => tipo switch
    {
        TipoObrigacao.NFe => "NF-e",
        TipoObrigacao.NFCe => "NFC-e",
        TipoObrigacao.NFSe => "NFS-e",
        TipoObrigacao.EfdIcmsIpi => "EFD ICMS/IPI",
        TipoObrigacao.EfdContribuicoes => "EFD-Contribuições",
        TipoObrigacao.Ecd => "ECD",
        TipoObrigacao.Ecf => "ECF",
        TipoObrigacao.Dimp => "DIMP",
        _ => tipo.ToString(),
    };

    /// <summary>Documentos e linhas excluídos pelas regras EXC citadas em <c>filtrosExclusao</c> (FR-018).</summary>
    internal static bool Excluido(ContextoCruzamento contexto, Regra regra, string chave) =>
        regra.Declaracao?.FiltrosExclusao is { Count: > 0 } filtros
        && contexto.Exclusoes.Any(e => e.Chave == chave && filtros.Contains(e.RegraId, StringComparer.Ordinal));
}

/// <summary>PRE_DIRETA / PRE_REVERSA com EXISTE: cada item da origem precisa de correspondente no destino pela chave.</summary>
public sealed class ExecutorPresenca(string regraId, RegraInterpretada interpretada) : IValidadorCruzamento
{
    public string RegraId => regraId;

    public IReadOnlyList<Achado> Validar(ContextoCruzamento contexto, Regra regra)
    {
        var chaves = interpretada.Destino.Itens(contexto, regra)
            .Select(i => i.Chave(interpretada.Chave))
            .Where(c => c is not null)
            .ToHashSet(StringComparer.Ordinal);
        var achados = new List<Achado>();
        foreach (var item in interpretada.Origem.Itens(contexto, regra))
        {
            var chave = item.Chave(interpretada.Chave);
            if (chave is null)
            {
                achados.Add(ExecutoresGenericos.Ausencia(regra, $"{ExecutoresGenericos.Rotulo(item.Obrigacao.Tipo)} {item.Obrigacao.Id}: {interpretada.Chave} não informado.", item.Evidencia(interpretada.Chave)));
            }
            else if (!ExecutoresGenericos.Excluido(contexto, regra, chave) && !chaves.Contains(chave))
            {
                achados.Add(regra.CriarInconsistencia(
                    $"{ExecutoresGenericos.Rotulo(item.Obrigacao.Tipo)} {chave} sem correspondente em {ExecutoresGenericos.Rotulo(interpretada.Destino.Tipo)}.",
                    [item.Evidencia(item.EhDocumento ? null : interpretada.Chave)]));
            }
        }

        return achados;
    }
}

/// <summary>
/// IDT (IGUAL) e LIM (MENOR_OU_IGUAL, MAIOR_OU_IGUAL) entre documento e escrituração casados pela chave.
/// Universo: documentos e linhas regulares (sem cancelamento, <c>COD_SIT</c> regular) que casam; os demais são das regras de presença.
/// </summary>
public sealed class ExecutorComparacao(string regraId, RegraInterpretada interpretada, string operador) : IValidadorCruzamento
{
    public string RegraId => regraId;

    public IReadOnlyList<Achado> Validar(ContextoCruzamento contexto, Regra regra)
    {
        var achados = new List<Achado>();
        var origemEhDocumento = !interpretada.Origem.Elemento.EhSped;
        var documentos = (origemEhDocumento ? interpretada.Origem : interpretada.Destino).Itens(contexto, regra)
            .Where(i => !i.NaoRegular)
            .Select(i => (Chave: i.Chave(interpretada.Chave), Item: i))
            .Where(p => p.Chave is not null)
            .GroupBy(p => p.Chave!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Item, StringComparer.Ordinal);
        var lancamentos = (origemEhDocumento ? interpretada.Destino : interpretada.Origem).Itens(contexto, regra).Where(i => !i.NaoRegular);

        foreach (var lancamento in lancamentos)
        {
            if (lancamento.Chave(interpretada.Chave) is not { } chave || !documentos.TryGetValue(chave, out var documento)
                || ExecutoresGenericos.Excluido(contexto, regra, chave))
            {
                continue;
            }

            var (origem, destino) = origemEhDocumento ? (documento, lancamento) : (lancamento, documento);
            for (var i = 0; i < interpretada.Origem.Elemento.Campos.Count; i++)
            {
                if (Comparar(regra, chave, documento, origem, interpretada.Origem.Elemento.Campos[i], destino, interpretada.Destino.Elemento.Campos[i]) is { } achado)
                {
                    achados.Add(achado);
                }
            }
        }

        return achados;
    }

    private Achado? Comparar(Regra regra, string chave, ItemObrigacao documento, ItemObrigacao origem, CampoElemento campoOrigem, ItemObrigacao destino, CampoElemento campoDestino)
    {
        var rotulo = ExecutoresGenericos.Rotulo(documento.Obrigacao.Tipo);
        var textoOrigem = origem.Campo(campoOrigem.Nome);
        var textoDestino = destino.Campo(campoDestino.Nome);
        LocalizacaoEvidencia[] evidencias = [origem.Evidencia(campoOrigem.Nome), destino.Evidencia(campoDestino.Nome)];
        if (string.IsNullOrWhiteSpace(textoOrigem) || string.IsNullOrWhiteSpace(textoDestino))
        {
            return ExecutoresGenericos.Ausencia(regra, $"{rotulo} {chave}: {campoOrigem.Nome} ou {campoDestino.Nome} não informados.", evidencias);
        }

        // Data explícita (modificador "(data)") ou dois valores que só fazem sentido como data (ex.: dhEmi x DT_E_S).
        if (campoOrigem.Modificador == "data" || campoDestino.Modificador == "data"
            || (InterpretadorRegra.Numero(textoOrigem) is null && InterpretadorRegra.Data(textoOrigem) is not null && InterpretadorRegra.Data(textoDestino) is not null))
        {
            return CompararDatas(regra, rotulo, chave, campoOrigem.Nome, textoOrigem, campoDestino.Nome, textoDestino, evidencias);
        }

        var numeroOrigem = InterpretadorRegra.Numero(textoOrigem);
        var numeroDestino = InterpretadorRegra.Numero(textoDestino);
        if (numeroOrigem is { } o && numeroDestino is { } d)
        {
            var parametros = new ParametrosComparacao(interpretada.Tolerancia, null);
            var comparacao = operador switch
            {
                "MENOR_OU_IGUAL" => ComparadorValores.Limite(o, d, parametros),
                "MAIOR_OU_IGUAL" => ComparadorValores.Limite(d, o, parametros),
                _ => ComparadorValores.Comparar(o, d, parametros),
            };
            return comparacao.Diverge
                ? regra.CriarInconsistencia(
                    Descrever(rotulo, chave, campoOrigem.Nome, Formatar(o), campoDestino.Nome, Formatar(d)),
                    evidencias,
                    valorOrigem: o,
                    valorDestino: d,
                    toleranciaAplicada: comparacao.ToleranciaAplicada)
                : null;
        }

        if (operador != "IGUAL")
        {
            return ExecutoresGenericos.Ausencia(regra, $"{rotulo} {chave}: {campoOrigem.Nome} ou {campoDestino.Nome} não numéricos.", evidencias);
        }

        return string.Equals(textoOrigem.Trim(), textoDestino.Trim(), StringComparison.Ordinal)
            ? null
            : regra.CriarInconsistencia(Descrever(rotulo, chave, campoOrigem.Nome, textoOrigem, campoDestino.Nome, textoDestino), evidencias);
    }

    private Achado? CompararDatas(Regra regra, string rotulo, string chave, string campoOrigem, string textoOrigem, string campoDestino, string textoDestino, LocalizacaoEvidencia[] evidencias)
    {
        if (InterpretadorRegra.Data(textoOrigem) is not { } o || InterpretadorRegra.Data(textoDestino) is not { } d)
        {
            return ExecutoresGenericos.Ausencia(regra, $"{rotulo} {chave}: {campoOrigem} ou {campoDestino} sem data válida.", evidencias);
        }

        var diverge = operador switch
        {
            "MENOR_OU_IGUAL" => d > o,
            "MAIOR_OU_IGUAL" => d < o,
            _ => d != o,
        };
        return diverge
            ? regra.CriarInconsistencia(Descrever(rotulo, chave, campoOrigem, o.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), campoDestino, d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), evidencias)
            : null;
    }

    private string Descrever(string rotulo, string chave, string campoOrigem, string valorOrigem, string campoDestino, string valorDestino) => operador switch
    {
        "MENOR_OU_IGUAL" => $"{rotulo} {chave}: {campoDestino} {valorDestino} excede {campoOrigem} {valorOrigem}.",
        "MAIOR_OU_IGUAL" => $"{rotulo} {chave}: {campoDestino} {valorDestino} é menor que {campoOrigem} {valorOrigem}.",
        _ => $"{rotulo} {chave}: {campoOrigem} {valorOrigem} x {campoDestino} {valorDestino}.",
    };

    private static string Formatar(decimal valor) => "R$ " + valor.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>EXC com FILTRA: documentos da origem que atendem aos filtros saem do universo das regras que citam a exclusão.</summary>
public sealed class ExecutorExclusao(string regraId, LadoInterpretado origem) : IValidadorCruzamento
{
    public string RegraId => regraId;

    public static (IValidadorCruzamento?, string?) Criar(Regra regra)
    {
        if (regra.Declaracao is not { Origem: { Tipo: { } tipo } lado } declaracao)
        {
            return (null, "exclusão sem obrigação de origem recebida no lote.");
        }

        if (declaracao.AplicavelSe is not null || declaracao.CondicaoDescritiva is not null)
        {
            return (null, "exclusão com condição em texto livre.");
        }

        if (tipo is not (TipoObrigacao.NFe or TipoObrigacao.NFCe or TipoObrigacao.NFSe))
        {
            return (null, "exclusão genérica suportada só para documentos (chave de acesso).");
        }

        var (filtros, motivo) = InterpretadorRegra.Filtros(lado.Filtro);
        if (filtros is not { Count: > 0 })
        {
            return (null, motivo ?? "exclusão sem filtro interpretável.");
        }

        return (new ExecutorExclusao(regra.Id, new LadoInterpretado(tipo, new ElementoObrigacao(tipo, null, [], true), filtros)), null);
    }

    public IReadOnlyList<Achado> Validar(ContextoCruzamento contexto, Regra regra)
    {
        foreach (var item in origem.Itens(contexto, regra))
        {
            if (item.Chave("CHV") is { } chave)
            {
                contexto.RegistrarExclusao(regra.Id, chave, regra.Titulo);
            }
        }

        return [];
    }
}
