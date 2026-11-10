using System.Globalization;
using System.Text.RegularExpressions;
using Api.Agente.Domain.Casamento;
using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Domain.Motor;

/// <summary>Campo de um elemento do catálogo; <paramref name="Modificador"/> <c>data</c> compara só a data de um valor data-hora.</summary>
public sealed record CampoElemento(string Nome, string? Modificador);

/// <summary>
/// Elemento de origem/destino interpretado (spec 004, D404). SPED: <c>REGISTRO.CAMPO[, CAMPO]</c>.
/// XML (NF-e, NFC-e, NFS-e): <c>[caminho/]grupo/campo[, campo]</c>, em que o grupo é a chave em <c>registros</c> do lote.
/// <c>infNFe/@Id</c> e equivalentes designam a chave de acesso do documento.
/// </summary>
public sealed record ElementoObrigacao(TipoObrigacao Tipo, string? Grupo, IReadOnlyList<CampoElemento> Campos, bool ChaveDoDocumento)
{
    public bool EhSped => Tipo is not (TipoObrigacao.NFe or TipoObrigacao.NFCe or TipoObrigacao.NFSe);
}

/// <summary>Predicado de filtro do catálogo. Só os formatos abaixo são interpretados; o resto vira pendência.</summary>
public abstract record FiltroElemento
{
    public abstract bool Atende(ItemObrigacao item);

    /// <summary><c>CAMPO=valor</c> (ou <c>REGISTRO.CAMPO=valor</c>).</summary>
    public sealed record Igual(string Campo, string Valor) : FiltroElemento
    {
        public override bool Atende(ItemObrigacao item) => item.Campo(Campo) == Valor;
    }

    /// <summary><c>CAMPO in (a,b,c)</c>.</summary>
    public sealed record Em(string Campo, IReadOnlyList<string> Valores) : FiltroElemento
    {
        public override bool Atende(ItemObrigacao item) => item.Campo(Campo) is { } v && Valores.Contains(v, StringComparer.Ordinal);
    }

    /// <summary>Documento sem evento de cancelamento (<c>autorizada</c>, <c>sem 110111</c>).</summary>
    public sealed record NaoCancelado : FiltroElemento
    {
        public override bool Atende(ItemObrigacao item) => !item.NaoRegular;
    }

    /// <summary>Documento com evento de cancelamento homologado (<c>evento 110111 homologado</c>).</summary>
    public sealed record ComCancelamento : FiltroElemento
    {
        public override bool Atende(ItemObrigacao item) => item.NaoRegular;
    }
}

/// <summary>
/// Uma ocorrência do universo de uma regra: o documento (NF-e/NFS-e) ou uma linha de registro SPED.
/// </summary>
public sealed record ItemObrigacao(ObrigacaoRecebida Obrigacao, string? Grupo, OcorrenciaRegistro? Registro, int Indice)
{
    public const string EventoCancelamento = "110111";

    /// <summary>Situações regulares de <c>COD_SIT</c> (tabela 4.1.2 do Guia Prático da EFD).</summary>
    public static readonly IReadOnlyList<string> SituacoesRegulares = ["00", "01", "06", "07", "08"];

    public bool EhDocumento => Obrigacao.EhDocumento;

    /// <summary>Documento cancelado (evento 110111) ou linha SPED com <c>COD_SIT</c> fora das situações regulares.</summary>
    public bool NaoRegular => EhDocumento
        ? Obrigacao.Registro("evento").Any(e => e.Campos.GetValueOrDefault("tpEvento") == EventoCancelamento)
        : Campo("COD_SIT") is { } situacao && !SituacoesRegulares.Contains(situacao, StringComparer.Ordinal);

    /// <summary>Campo do item. Em documento, procura no grupo do elemento; aceita <c>grupo/campo</c> e <c>REGISTRO.CAMPO</c>.</summary>
    public string? Campo(string nome)
    {
        var separador = nome.LastIndexOfAny(['/', '.']);
        if (separador > 0)
        {
            var grupo = nome[..separador];
            var campo = nome[(separador + 1)..];
            if (!EhDocumento && grupo == Grupo)
            {
                return Registro?.Campos.GetValueOrDefault(campo);
            }

            return EhDocumento ? PrimeiroDoGrupo(grupo[(grupo.LastIndexOf('/') + 1)..], campo) : null;
        }

        return EhDocumento ? PrimeiroDoGrupo(Grupo, nome) : Registro?.Campos.GetValueOrDefault(nome);
    }

    /// <summary>Valor da chave de junção: chave de acesso do documento ou campo da linha SPED, normalizado.</summary>
    public string? Chave(string chaveJuncao)
    {
        if (EhDocumento)
        {
            return Obrigacao.Identificacao.ChaveAcesso is { } c && ChaveAcesso.TentarNormalizar(c, out var n) ? n : null;
        }

        var valor = Campo(chaveJuncao);
        return ChaveAcesso.TentarNormalizar(valor, out var normalizada) ? normalizada : valor?.Trim();
    }

    public LocalizacaoEvidencia Evidencia(string? campo) => EhDocumento
        ? new LocalizacaoEvidencia(Obrigacao.Id, Grupo, 0, campo, Valor: campo is null ? null : Campo(campo))
        : new LocalizacaoEvidencia(Obrigacao.Id, Grupo, Indice, campo, Registro?.Linha, campo is null ? null : Campo(campo));

    private string? PrimeiroDoGrupo(string? grupo, string campo) =>
        grupo is not null && Obrigacao.Registro(grupo) is [var primeiro, ..] ? primeiro.Campos.GetValueOrDefault(campo) : null;
}

/// <summary>Lado interpretado: obrigação, elemento e filtros.</summary>
public sealed record LadoInterpretado(TipoObrigacao Tipo, ElementoObrigacao Elemento, IReadOnlyList<FiltroElemento> Filtros)
{
    /// <summary>Universo do lado: documentos (um item por documento) ou linhas do registro SPED, já filtrados.</summary>
    public IEnumerable<ItemObrigacao> Itens(ContextoCruzamento contexto, Regra regra) =>
        contexto.ObrigacoesPara(regra, Tipo)
            .SelectMany(o => Elemento.EhSped
                ? o.Registro(Elemento.Grupo!).Select((r, i) => new ItemObrigacao(o, Elemento.Grupo, r, i))
                : [new ItemObrigacao(o, Elemento.Grupo, null, 0)])
            .Where(i => Filtros.All(f => f.Atende(i)));
}

/// <summary>Regra interpretada para os executores genéricos, ou o motivo de não ser interpretável.</summary>
public sealed record RegraInterpretada(
    LadoInterpretado Origem,
    LadoInterpretado Destino,
    string Chave,
    decimal Tolerancia,
    string? ParametroTolerancia);

public static partial class InterpretadorRegra
{
    /// <summary>
    /// Interpreta os campos declarativos de uma regra. Sem chute: qualquer elemento, filtro, chave ou condição fora do formato
    /// suportado devolve o motivo, e a regra só executa com validador dedicado.
    /// </summary>
    public static (RegraInterpretada? Regra, string? Motivo) Interpretar(Regra regra, CatalogoParametros parametros)
    {
        if (regra.Declaracao is not { } d)
        {
            return (null, "regra sem campos declarativos.");
        }

        if (d.AplicavelSe is not null)
        {
            return (null, $"condição de aplicabilidade em texto livre ('{d.AplicavelSe}').");
        }

        if (d.Origem is not { Tipo: { } tipoOrigem } origem || d.Destino is not { Tipo: { } tipoDestino } destino)
        {
            return (null, "origem e destino precisam ser obrigações recebidas no lote.");
        }

        if (d.ChaveJuncao is not [var chave] || !ChaveSimples().IsMatch(chave))
        {
            return (null, $"chave de junção não suportada ({string.Join(", ", d.ChaveJuncao)}); só chave única de documento.");
        }

        var (elementoOrigem, motivoOrigem) = Elemento(tipoOrigem, origem.Elemento);
        var (elementoDestino, motivoDestino) = Elemento(tipoDestino, destino.Elemento);
        if (elementoOrigem is null || elementoDestino is null)
        {
            return (null, motivoOrigem ?? motivoDestino);
        }

        if (elementoOrigem.EhSped == elementoDestino.EhSped)
        {
            return (null, "junção suportada só entre documento e escrituração.");
        }

        var (filtrosOrigem, motivoFiltroOrigem) = Filtros(origem.Filtro);
        var (filtrosDestino, motivoFiltroDestino) = Filtros(destino.Filtro);
        if (filtrosOrigem is null || filtrosDestino is null)
        {
            return (null, motivoFiltroOrigem ?? motivoFiltroDestino);
        }

        decimal tolerancia = 0m;
        if (d.Tolerancia is { } referencia)
        {
            if (parametros.Obter(referencia) is not { Tipo: "decimal" } parametro)
            {
                return (null, $"tolerância {referencia} não é um parâmetro decimal.");
            }

            if (parametro.Numero is not { } valor)
            {
                return (null, $"tolerância {referencia} sem valor definido.");
            }

            tolerancia = valor;
        }

        return (new RegraInterpretada(
            new LadoInterpretado(tipoOrigem, elementoOrigem, filtrosOrigem),
            new LadoInterpretado(tipoDestino, elementoDestino, filtrosDestino),
            chave,
            tolerancia,
            d.Tolerancia), null);
    }

    public static (ElementoObrigacao? Elemento, string? Motivo) Elemento(TipoObrigacao tipo, string? texto)
    {
        var valor = texto?.Trim() ?? string.Empty;
        var documento = tipo is TipoObrigacao.NFe or TipoObrigacao.NFCe or TipoObrigacao.NFSe;
        if (documento && ChaveDocumento().IsMatch(valor))
        {
            return (new ElementoObrigacao(tipo, null, [], ChaveDoDocumento: true), null);
        }

        if (!documento && ElementoSped().Match(valor) is { Success: true } sped)
        {
            var campos = sped.Groups["campos"].Value.Split(',', StringSplitOptions.TrimEntries).Select(c => new CampoElemento(c, null)).ToList();
            return (new ElementoObrigacao(tipo, sped.Groups["registro"].Value, campos, false), null);
        }

        if (documento && ElementoXml().Match(valor) is { Success: true } xml)
        {
            var caminho = xml.Groups["caminho"].Value.Split('/');
            var primeiro = caminho[^1];
            var grupo = caminho.Length > 1 ? caminho[^2] : null;
            if (grupo is null)
            {
                return (null, $"elemento '{valor}' sem grupo XML.");
            }

            var campos = new List<CampoElemento> { Campo(primeiro) };
            campos.AddRange(xml.Groups["resto"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Campo));
            return (new ElementoObrigacao(tipo, grupo, campos, false), null);
        }

        return (null, $"elemento '{valor}' fora do formato interpretável.");
    }

    public static (IReadOnlyList<FiltroElemento>? Filtros, string? Motivo) Filtros(string? texto)
    {
        var filtros = new List<FiltroElemento>();
        foreach (var termo in (texto ?? string.Empty).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (FiltroIgual().Match(termo) is { Success: true } igual)
            {
                filtros.Add(new FiltroElemento.Igual(igual.Groups["campo"].Value, igual.Groups["valor"].Value));
            }
            else if (FiltroEm().Match(termo) is { Success: true } em)
            {
                filtros.Add(new FiltroElemento.Em(em.Groups["campo"].Value, em.Groups["valores"].Value.Split(',', StringSplitOptions.TrimEntries)));
            }
            else if (termo is "autorizada" or "sem 110111" or "sem evento 110111")
            {
                filtros.Add(new FiltroElemento.NaoCancelado());
            }
            else if (termo is "evento 110111 homologado")
            {
                filtros.Add(new FiltroElemento.ComCancelamento());
            }
            else
            {
                return (null, $"filtro '{termo}' fora do formato interpretável.");
            }
        }

        return (filtros, null);
    }

    public static decimal? Numero(string? texto) =>
        decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) ? valor : null;

    public static DateOnly? Data(string? texto) =>
        texto is { Length: >= 10 } && DateOnly.TryParseExact(texto[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data : null;

    private static CampoElemento Campo(string texto) =>
        CampoComModificador().Match(texto) is { Success: true } m
            ? new CampoElemento(m.Groups["nome"].Value, m.Groups["mod"].Success ? m.Groups["mod"].Value : null)
            : new CampoElemento(texto, null);

    [GeneratedRegex(@"^[A-Z][A-Z0-9_]*$")]
    private static partial Regex ChaveSimples();

    [GeneratedRegex(@"^(infNFe|infNFSe|infDPS)/@Id$")]
    private static partial Regex ChaveDocumento();

    [GeneratedRegex(@"^(?<registro>[A-Z0-9]{4})\.(?<campos>[A-Z_0-9]+(\s*,\s*[A-Z_0-9]+)*)$")]
    private static partial Regex ElementoSped();

    [GeneratedRegex(@"^(?<caminho>[A-Za-z][A-Za-z0-9]*(/[A-Za-z][A-Za-z0-9]*)+(\(data\))?)(?<resto>(\s*,\s*[A-Za-z][A-Za-z0-9]*(\(data\))?)*)$")]
    private static partial Regex ElementoXml();

    [GeneratedRegex(@"^(?<nome>[A-Za-z][A-Za-z0-9]*)(\((?<mod>data)\))?$")]
    private static partial Regex CampoComModificador();

    [GeneratedRegex(@"^(?<campo>([A-Z0-9]{4}\.)?[A-Z_][A-Z_0-9]*)\s*=\s*(?<valor>[A-Za-z0-9]+)$")]
    private static partial Regex FiltroIgual();

    [GeneratedRegex(@"^(?<campo>([A-Z0-9]{4}\.)?[A-Z_][A-Z_0-9]*) in \((?<valores>[A-Za-z0-9]+(\s*,\s*[A-Za-z0-9]+)*)\)$")]
    private static partial Regex FiltroEm();
}
