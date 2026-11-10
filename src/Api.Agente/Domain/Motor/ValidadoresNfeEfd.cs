using System.Globalization;
using Api.Agente.Domain.Casamento;
using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Domain.Motor;

public sealed class ValidadorRegra(string regraId, Func<ContextoCruzamento, Regra, IEnumerable<Achado>> validar) : IValidadorCruzamento
{
    public string RegraId => regraId;

    public IReadOnlyList<Achado> Validar(ContextoCruzamento contexto, Regra regra) => validar(contexto, regra).ToList();
}

/// <summary>
/// Validadores dedicados das regras M1 do NUCLEO-2 (NF-e × EFD ICMS/IPI) que não cabem nos executores genéricos (spec 004, D406):
/// mês de alocação, evento de cancelamento, papel do estabelecimento e normalização de modelo/série/número.
/// M1-I03, M1-I05 e M1-I06 rodam pelo <see cref="ExecutorComparacao"/>. O casamento é pela chave de acesso e o sentido por IND_OPER/IND_EMIT.
/// Cancelamento é lido do grupo <c>evento</c> da NF-e (<c>tpEvento</c> 110111). Dado ausente vira ausência de evidências, nunca erro (P5).
/// </summary>
public static class ValidadoresNfeEfd
{
    private const string EventoCancelamento = "110111";
    private const string ModeloNfe = "55";
    private static readonly string[] SituacoesRegulares = ["00", "01", "06", "07", "08"];
    private static readonly string[] SituacoesCanceladas = ["02", "03"];

    public static IReadOnlyList<IValidadorCruzamento> Todos() =>
    [
        new ValidadorRegra("M1-P01", NotaPropriaEscriturada),
        new ValidadorRegra("M1-P02", NotaCanceladaEscriturada),
        new ValidadorRegra("M1-R01", EscrituracaoRegularComNota),
        new ValidadorRegra("M1-R02", EscrituracaoCanceladaComEvento),
        new ValidadorRegra("M1-R03", EmissaoPropriaDoEstabelecimento),
        new ValidadorRegra("M1-R04", EntradaDestinadaAoEstabelecimento),
        new ValidadorRegra("M1-I01", IdentificacaoIgual),
    ];

    private static IEnumerable<Achado> NotaPropriaEscriturada(ContextoCruzamento contexto, Regra regra)
    {
        foreach (var nota in Notas(contexto, regra).Where(n => !n.Cancelada))
        {
            if (nota.Emitente is null || nota.Emissao is null)
            {
                yield return Ausencia(regra, $"NF-e {nota.Chave}: emitente ou data de emissão não informados.", nota.Evidencia("emit", "CNPJ"));
            }
            else if (nota.Emitente.StartsWith(contexto.RaizCnpj, StringComparison.Ordinal))
            {
                var efd = Escrituracao(contexto, regra, nota);
                if (efd is null)
                {
                    yield return Ausencia(regra, $"NF-e {nota.Chave}: EFD ICMS/IPI do estabelecimento {nota.Emitente} não recebida para o mês da emissão.", nota.Evidencia("emit", "CNPJ"));
                }
                else if (!Lancamentos(efd).Any(l => l.Chave == nota.Chave && l.Proprio && l.Regular))
                {
                    yield return regra.CriarInconsistencia(
                        $"NF-e {nota.Chave} emitida em {nota.Emissao:yyyy-MM-dd} (R$ {nota.Campo("ICMSTot", "vNF")}) não está escriturada na EFD ICMS/IPI de {nota.Emitente}. Indício de omissão de saída.",
                        [nota.Evidencia("ide", "nNF"), new LocalizacaoEvidencia(efd.Id, "C100", Campo: "CHV_NFE")]);
                }
            }
        }
    }

    private static IEnumerable<Achado> NotaCanceladaEscriturada(ContextoCruzamento contexto, Regra regra)
    {
        foreach (var nota in Notas(contexto, regra).Where(n => n.Cancelada && n.Emitente is not null && n.Emitente.StartsWith(contexto.RaizCnpj, StringComparison.Ordinal)))
        {
            var efd = Escrituracao(contexto, regra, nota);
            if (efd is null)
            {
                yield return Ausencia(regra, $"NF-e {nota.Chave}: EFD ICMS/IPI do estabelecimento {nota.Emitente} não recebida para o mês da emissão.", nota.Evidencia("emit", "CNPJ"));
            }
            else if (!Lancamentos(efd).Any(l => l.Chave == nota.Chave && l.Cancelado))
            {
                yield return regra.CriarInconsistencia(
                    $"NF-e {nota.Chave} cancelada não está escriturada com COD_SIT 02 ou 03 na EFD ICMS/IPI de {nota.Emitente}.",
                    [nota.Evidencia("evento", "tpEvento"), new LocalizacaoEvidencia(efd.Id, "C100", Campo: "COD_SIT")]);
            }
        }
    }

    private static IEnumerable<Achado> EscrituracaoRegularComNota(ContextoCruzamento contexto, Regra regra)
    {
        var notas = NotasPorChave(contexto, regra);
        foreach (var lancamento in LancamentosNfe(contexto, regra).Where(l => l.Regular))
        {
            if (!notas.TryGetValue(lancamento.Chave ?? string.Empty, out var nota))
            {
                yield return Ausencia(regra, $"C100 {lancamento.Chave}: XML da NF-e não fornecido ao agente; lastro não verificável.", lancamento.Evidencia("CHV_NFE"));
            }
            else if (nota.Cancelada)
            {
                yield return regra.CriarInconsistencia(
                    $"C100 {lancamento.Chave} escriturado como regular (COD_SIT {lancamento.Situacao}), mas a NF-e está cancelada.",
                    [lancamento.Evidencia("COD_SIT"), nota.Evidencia("evento", "tpEvento")]);
            }
        }
    }

    private static IEnumerable<Achado> EscrituracaoCanceladaComEvento(ContextoCruzamento contexto, Regra regra)
    {
        var notas = NotasPorChave(contexto, regra);
        foreach (var lancamento in LancamentosNfe(contexto, regra).Where(l => l.Cancelado))
        {
            if (!notas.TryGetValue(lancamento.Chave ?? string.Empty, out var nota))
            {
                yield return Ausencia(regra, $"C100 {lancamento.Chave}: XML e eventos da NF-e não fornecidos ao agente; cancelamento não verificável.", lancamento.Evidencia("CHV_NFE"));
            }
            else if (!nota.Cancelada)
            {
                yield return regra.CriarInconsistencia(
                    $"C100 {lancamento.Chave} escriturado como cancelado (COD_SIT {lancamento.Situacao}), sem evento de cancelamento na NF-e. Indício de débito omitido.",
                    [lancamento.Evidencia("COD_SIT"), nota.Evidencia("ide", "nNF")]);
            }
        }
    }

    private static IEnumerable<Achado> EmissaoPropriaDoEstabelecimento(ContextoCruzamento contexto, Regra regra) =>
        Pares(contexto, regra).Where(p => p.Lancamento.Proprio).SelectMany(p => CnpjIgual(regra, p.Lancamento, p.Nota, p.Nota.Emitente, "emit", "emissão própria"));

    private static IEnumerable<Achado> EntradaDestinadaAoEstabelecimento(ContextoCruzamento contexto, Regra regra) =>
        Pares(contexto, regra).Where(p => p.Lancamento.EntradaDeTerceiros).SelectMany(p => CnpjIgual(regra, p.Lancamento, p.Nota, p.Nota.Destinatario, "dest", "entrada"));

    private static IEnumerable<Achado> CnpjIgual(Regra regra, Lancamento lancamento, Nota nota, string? cnpjNota, string grupo, string papel)
    {
        if (cnpjNota is null)
        {
            yield return Ausencia(regra, $"NF-e {nota.Chave}: {grupo}/CNPJ não informado.", nota.Evidencia(grupo, "CNPJ"));
        }
        else if (cnpjNota != lancamento.Efd.Cnpj.Valor)
        {
            yield return regra.CriarInconsistencia(
                $"NF-e {nota.Chave} escriturada como {papel} de {lancamento.Efd.Cnpj.Valor}, mas {grupo}/CNPJ do XML é {cnpjNota}.",
                [new LocalizacaoEvidencia(lancamento.Efd.Id, "0000", Campo: "CNPJ", Valor: lancamento.Efd.Cnpj.Valor), nota.Evidencia(grupo, "CNPJ")]);
        }
    }

    private static IEnumerable<Achado> IdentificacaoIgual(ContextoCruzamento contexto, Regra regra)
    {
        foreach (var (lancamento, nota) in Pares(contexto, regra))
        {
            foreach (var (campoEfd, campoXml) in new[] { ("COD_MOD", "mod"), ("SER", "serie"), ("NUM_DOC", "nNF") })
            {
                if (SemZeros(lancamento.Campo(campoEfd)) != SemZeros(nota.Campo("ide", campoXml)))
                {
                    yield return regra.CriarInconsistencia(
                        $"NF-e {nota.Chave}: {campoEfd} escriturado {lancamento.Campo(campoEfd)} x XML {nota.Campo("ide", campoXml)}.",
                        [lancamento.Evidencia(campoEfd), nota.Evidencia("ide", campoXml)]);
                }
            }

            var data = Data(lancamento.Campo("DT_DOC"));
            if (data is null || nota.Emissao is null)
            {
                yield return Ausencia(regra, $"NF-e {nota.Chave}: DT_DOC ou dhEmi não informados.", lancamento.Evidencia("DT_DOC"));
            }
            else if (data != nota.Emissao)
            {
                yield return regra.CriarInconsistencia(
                    $"NF-e {nota.Chave}: DT_DOC escriturado {data:yyyy-MM-dd} x dhEmi {nota.Emissao:yyyy-MM-dd}.",
                    [lancamento.Evidencia("DT_DOC"), nota.Evidencia("ide", "dhEmi")],
                    chaveSeveridade: "atenuada");
            }
        }
    }

    private static Achado Ausencia(Regra regra, string descricao, params LocalizacaoEvidencia[] evidencias) => new()
    {
        RegraId = regra.Id,
        Natureza = Natureza.AusenciaEvidencias,
        Descricao = descricao,
        Evidencias = evidencias,
    };

    private static List<Nota> Notas(ContextoCruzamento contexto, Regra regra) =>
        contexto.ObrigacoesPara(regra, TipoObrigacao.NFe).Select(o => new Nota(o)).ToList();

    private static Dictionary<string, Nota> NotasPorChave(ContextoCruzamento contexto, Regra regra) =>
        Notas(contexto, regra).Where(n => n.Chave is not null).ToDictionary(n => n.Chave!, StringComparer.Ordinal);

    private static ObrigacaoRecebida? Escrituracao(ContextoCruzamento contexto, Regra regra, Nota nota) =>
        contexto.ObrigacoesPara(regra, TipoObrigacao.EfdIcmsIpi)
            .FirstOrDefault(e => e.Cnpj.Valor == nota.Emitente && nota.Emissao is { } data && e.Periodo.Contem(data));

    private static IEnumerable<Lancamento> Lancamentos(ObrigacaoRecebida efd) =>
        efd.Registro("C100").Select((registro, indice) => new Lancamento(efd, registro, indice));

    private static IEnumerable<Lancamento> LancamentosNfe(ContextoCruzamento contexto, Regra regra) =>
        contexto.ObrigacoesPara(regra, TipoObrigacao.EfdIcmsIpi).SelectMany(Lancamentos).Where(l => l.Campo("COD_MOD") == ModeloNfe);

    /// <summary>C100 regulares com a NF-e correspondente, não cancelada: universo das regras de identidade e limite.</summary>
    private static IEnumerable<(Lancamento Lancamento, Nota Nota)> Pares(ContextoCruzamento contexto, Regra regra)
    {
        var notas = NotasPorChave(contexto, regra);
        foreach (var lancamento in LancamentosNfe(contexto, regra).Where(l => l.Regular))
        {
            if (notas.TryGetValue(lancamento.Chave ?? string.Empty, out var nota) && !nota.Cancelada)
            {
                yield return (lancamento, nota);
            }
        }
    }

    private static string SemZeros(string? valor)
    {
        var limpo = (valor ?? string.Empty).Trim().TrimStart('0');
        return limpo.Length == 0 ? "0" : limpo;
    }

    private static DateOnly? Data(string? texto) =>
        texto is { Length: >= 10 } && DateOnly.TryParseExact(texto[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data : null;

    private sealed record Nota(ObrigacaoRecebida Obrigacao)
    {
        public string? Chave => Obrigacao.Identificacao.ChaveAcesso;

        public string? Emitente => Documento("emit");

        public string? Destinatario => Documento("dest");

        public DateOnly? Emissao => Data(Campo("ide", "dhEmi"));

        public bool Cancelada => Obrigacao.Registro("evento").Any(e => e.Campos.GetValueOrDefault("tpEvento") == EventoCancelamento);

        public string? Campo(string grupo, string campo) =>
            Obrigacao.Registro(grupo) is [var primeiro, ..] ? primeiro.Campos.GetValueOrDefault(campo) : null;

        public LocalizacaoEvidencia Evidencia(string grupo, string campo) =>
            new(Obrigacao.Id, grupo, 0, campo, Valor: Campo(grupo, campo));

        private string? Documento(string grupo) =>
            Campo(grupo, "CNPJ") is { Length: > 0 } cnpj ? Cnpj.Normalizar(cnpj) : null;
    }

    private sealed record Lancamento(ObrigacaoRecebida Efd, OcorrenciaRegistro Registro, int Indice)
    {
        public string? Chave => ChaveAcesso.TentarNormalizar(Campo("CHV_NFE"), out var chave) ? chave : null;

        public string? Situacao => Campo("COD_SIT");

        public bool Regular => SituacoesRegulares.Contains(Situacao);

        public bool Cancelado => SituacoesCanceladas.Contains(Situacao);

        public bool Proprio => Campo("IND_EMIT") == "0";

        public bool EntradaDeTerceiros => Campo("IND_EMIT") == "1" && Campo("IND_OPER") == "0";

        public string? Campo(string campo) => Registro.Campos.GetValueOrDefault(campo);

        public LocalizacaoEvidencia Evidencia(string campo) => new(Efd.Id, "C100", Indice, campo, Registro.Linha, Campo(campo));
    }
}
