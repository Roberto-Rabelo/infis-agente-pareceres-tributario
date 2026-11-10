using System.Text.RegularExpressions;

namespace Api.Agente.Domain.Catalogo;

/// <summary>Garante os campos mínimos da constitution 4.2 e as condições de aprovação (P1; FR-011 a FR-014).</summary>
public static partial class ValidadorCatalogo
{
    public static IReadOnlyList<string> Validar(CatalogoRegras catalogo)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(catalogo.Versao))
        {
            erros.Add("Catálogo sem versão.");
        }

        foreach (var duplicado in catalogo.Regras.GroupBy(r => r.Id).Where(g => g.Count() > 1))
        {
            erros.Add($"Regra {duplicado.Key}: identificador duplicado.");
        }

        var ids = catalogo.Regras.Select(r => r.Id).ToHashSet();
        var aprovadas = catalogo.Regras.Where(r => r.Status == StatusRegra.Aprovada).Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        bool aprovada(string id) => aprovadas.Contains(id);
        foreach (var regra in catalogo.Regras)
        {
            erros.AddRange(ValidarRegra(regra, ids, catalogo.Parametros, aprovada).Select(e => $"Regra {regra.Id}: {e}"));
        }

        erros.AddRange(DetectarCiclos(catalogo));
        erros.AddRange(ValidarOrdemDasDependencias(catalogo));
        return erros;
    }

    private static IEnumerable<string> ValidarRegra(Regra regra, HashSet<string> ids, CatalogoParametros parametros, Func<string, bool>? porIdAprovada)
    {
        if (!PadraoId().IsMatch(regra.Id))
        {
            yield return "identificador fora do padrão (ex.: PRE-001, PAR-001, CNT-001, CRZ-NFE-EFD-001, ENT-001, M1-P01).";
        }

        if (string.IsNullOrWhiteSpace(regra.Titulo))
        {
            yield return "sem título.";
        }

        if (string.IsNullOrWhiteSpace(regra.ResultadoEsperado))
        {
            yield return "sem resultado esperado.";
        }

        if (regra.Fontes.Count == 0)
        {
            yield return "sem fonte oficial (P1).";
        }

        if (regra.VigenciaFim is { } fim && fim < regra.VigenciaInicio)
        {
            yield return "fim de vigência anterior ao início.";
        }

        if (regra.Implementacao == Implementacao.AssistidaIa && string.IsNullOrWhiteSpace(regra.JustificativaImplementacao))
        {
            yield return "implementação assistida por IA sem justificativa.";
        }

        foreach (var dependencia in regra.DependeDe)
        {
            if (!ids.Contains(dependencia))
            {
                yield return $"depende de regra inexistente {dependencia}.";
            }
            else if (dependencia == regra.Id)
            {
                yield return "depende de si mesma.";
            }
        }

        if (regra.Status != StatusRegra.Aprovada)
        {
            yield break;
        }

        // Daqui em diante: condições de aprovação. Regra proposta pode estar incompleta no catálogo, mas não executa (FR-402).
        foreach (var fonte in regra.Fontes)
        {
            if (string.IsNullOrWhiteSpace(fonte.Emissor) || string.IsNullOrWhiteSpace(fonte.Documento)
                || string.IsNullOrWhiteSpace(fonte.Versao) || string.IsNullOrWhiteSpace(fonte.Localizacao))
            {
                yield return "fonte sem emissor, documento, versão ou localização.";
            }
        }

        if (regra.Tipo == TipoRegra.Cruzamento)
        {
            if (regra.Relacao is null)
            {
                yield return "regra de cruzamento sem tipo de relação.";
            }

            if (regra.Evidencia is null)
            {
                yield return "regra de cruzamento sem tipo de evidência.";
            }

            if (!regra.Severidades.ContainsKey(Regra.SeveridadePadrao))
            {
                yield return "regra de cruzamento sem severidade padrão.";
            }

            // Filtro de exclusividade e compatibilidade de regime podem olhar uma única obrigação.
            var minimo = regra.Relacao is TipoRelacao.Exclusividade or TipoRelacao.Parametro ? 1 : 2;
            if (regra.Obrigacoes.Count < minimo)
            {
                yield return minimo == 2
                    ? "regra de cruzamento deve envolver ao menos duas obrigações."
                    : "regra deve envolver ao menos uma obrigação recebida no lote.";
            }
        }

        if (string.IsNullOrWhiteSpace(regra.AprovadoPor))
        {
            yield return "aprovada sem responsável pela aprovação.";
        }

        if (regra.Fontes.Any(f => !f.Conferida))
        {
            yield return "aprovada com fonte não conferida no documento oficial (FR-012).";
        }

        if (regra.Fontes.Any(ContemMarcaDeConferencia) || ContemMarca(regra.ResultadoEsperado))
        {
            yield return "aprovada com item marcado como '(conferir)' ou 'verificar' (FR-013).";
        }

        if (regra.Testes.Count == 0)
        {
            yield return "aprovada sem testes associados (P17).";
        }

        foreach (var pendencia in regra.Pendencias)
        {
            yield return $"aprovada com pendência no catálogo: {pendencia}";
        }

        foreach (var nome in regra.ParametrosUsados)
        {
            if (parametros.Obter(nome) is not { Definido: true })
            {
                yield return $"aprovada usando PARAM:{nome} sem valor definido (FR-404).";
            }
        }

        foreach (var dependencia in regra.DependeDe)
        {
            if (porIdAprovada is not null && !porIdAprovada(dependencia))
            {
                yield return $"aprovada dependendo de {dependencia}, que não está aprovada (dispensar com motivo em aprovacoes.json ou aprovar a dependência).";
            }
        }
    }

    private static IEnumerable<string> DetectarCiclos(CatalogoRegras catalogo)
    {
        var porId = catalogo.Regras.GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());
        var visitando = new HashSet<string>();
        var concluidas = new HashSet<string>();
        var ciclos = new List<string>();

        void Visitar(string id)
        {
            if (concluidas.Contains(id) || !porId.TryGetValue(id, out var regra))
            {
                return;
            }

            if (!visitando.Add(id))
            {
                ciclos.Add($"Regra {id}: dependência circular.");
                return;
            }

            foreach (var dependencia in regra.DependeDe.Where(d => d != id))
            {
                Visitar(dependencia);
            }

            visitando.Remove(id);
            concluidas.Add(id);
        }

        foreach (var id in porId.Keys)
        {
            Visitar(id);
        }

        return ciclos.Distinct();
    }

    /// <summary>Uma regra não pode depender de outra de passo posterior (cadeias.json) nem, no mesmo passo, de relação posterior na ordem PAR → EXC → PRE → IDT → AGR/LIM → CNT.</summary>
    private static IEnumerable<string> ValidarOrdemDasDependencias(CatalogoRegras catalogo)
    {
        var porId = catalogo.Regras.GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());
        // Só bloqueia aprovação: o motor executa dependências antes (recursão), e a ordem errada é pendência de conteúdo do catálogo.
        foreach (var regra in catalogo.Regras.Where(r => r.Relacao is not null && r.Status == StatusRegra.Aprovada))
        {
            foreach (var dependencia in regra.DependeDe)
            {
                if (porId.TryGetValue(dependencia, out var outra) && outra.Relacao is { } relacaoOutra
                    && (outra.Passo > regra.Passo
                        || (outra.Passo == regra.Passo && OrdemExecucao.De(relacaoOutra) > OrdemExecucao.De(regra.Relacao!.Value))))
                {
                    yield return $"Regra {regra.Id}: depende de {dependencia}, que executa em fase posterior.";
                }
            }
        }
    }

    private static bool ContemMarcaDeConferencia(FonteOficial fonte) =>
        ContemMarca(fonte.Documento) || ContemMarca(fonte.Versao) || ContemMarca(fonte.Localizacao);

    private static bool ContemMarca(string texto) =>
        texto.Contains("conferir", StringComparison.OrdinalIgnoreCase)
        || texto.Contains("verificar", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^(?:(PRE|ENT|CRZ|EXC|PAR|CNT)(-[A-Z]{2,5})*-[0-9]{3}|M[0-9]{1,2}-[A-Z]?[0-9]{2})$")]
    private static partial Regex PadraoId();
}
