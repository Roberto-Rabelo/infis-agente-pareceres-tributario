using System.Text.Json;
using Api.Agente.Application.Contratos;
using Api.Agente.Application.PreCondicoes;
using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Infrastructure.Catalogo;

public sealed class OpcoesCatalogo
{
    public const string Secao = "Catalogo";

    /// <summary>Diretório de <c>versoes-layout.json</c>.</summary>
    public string Caminho { get; set; } = "catalog";

    /// <summary>Diretório do catálogo de coesão (cópia de <c>docs/regras</c>): regras, aprovações, fontes, parâmetros e cadeias.</summary>
    public string CaminhoRegras { get; set; } = "regras";
}

#pragma warning disable CA1032, CA1064 // Exceção de inicialização: o serviço não sobe com catálogo inválido.
public sealed class CatalogoInvalidoException(IReadOnlyList<string> erros)
    : Exception("Catálogo inválido:" + Environment.NewLine + string.Join(Environment.NewLine, erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;
}
#pragma warning restore CA1032, CA1064

/// <summary>
/// Catálogo executável (spec 004, D401/D402): as regras de coesão de <c>docs/regras/0*.json</c>, as pré-condições de
/// <c>precondicoes.json</c> e o status de <c>aprovacoes.json</c>. Versões de layout em <c>catalog/versoes-layout.json</c>.
/// </summary>
public static partial class CarregadorCatalogo
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
    };

    public static string ResolverDiretorio(string caminho) =>
        Path.IsPathRooted(caminho) ? caminho : Path.Combine(AppContext.BaseDirectory, caminho);

    public static CatalogoRegras CarregarRegras(string diretorio)
    {
        var erros = new List<string>();
        var parametros = CarregarParametros(Path.Combine(diretorio, "parametros.json"), erros);
        var fontes = CarregarFontes(Path.Combine(diretorio, "fontes.json"), erros);
        var passos = CarregarPassos(Path.Combine(diretorio, "cadeias.json"), erros);
        var (versao, aprovacoes) = CarregarAprovacoes(Path.Combine(diretorio, "aprovacoes.json"), erros);

        var preCondicoes = Ler<CatalogoDto>(Path.Combine(diretorio, "precondicoes.json"));
        var regras = preCondicoes.Regras.Select(r => Converter(r, erros)).Where(r => r is not null).Select(r => r!).ToList();

        var arquivos = Directory.GetFiles(diretorio, "0*.json").Order(StringComparer.Ordinal).ToList();
        if (arquivos.Count == 0)
        {
            erros.Add($"Nenhum arquivo de regras (0*.json) em {diretorio}.");
        }

        foreach (var arquivo in arquivos)
        {
            using var documento = LerDocumento(arquivo);
            foreach (var elemento in documento.RootElement.GetProperty("regras").EnumerateArray())
            {
                if (ConverterCoesao(elemento, fontes, passos, aprovacoes, erros) is { } regra)
                {
                    regras.Add(regra);
                }
            }
        }

        foreach (var id in aprovacoes.Keys.Where(id => regras.TrueForAll(r => r.Id != id)))
        {
            erros.Add($"Aprovação de regra inexistente no catálogo: {id}.");
        }

        var catalogo = new CatalogoRegras(versao, regras) { Parametros = parametros };
        erros.AddRange(ValidadorCatalogo.Validar(catalogo));
        foreach (var id in VerificadorPreCondicoes.Todas)
        {
            if (catalogo.Obter(id) is not { Status: StatusRegra.Aprovada, Tipo: TipoRegra.PreCondicao })
            {
                erros.Add($"Regra {id}: pré-condição obrigatória ausente ou não aprovada.");
            }
        }

        return erros.Count > 0 ? throw new CatalogoInvalidoException(erros) : catalogo;
    }

    public static CatalogoVersoesLayout CarregarVersoes(string diretorio)
    {
        var dto = Ler<VersoesDto>(Path.Combine(diretorio, "versoes-layout.json"));
        var erros = new List<string>();
        var versoes = new List<VersaoLayout>();
        foreach (var v in dto.Versoes)
        {
            if (!Codigos.TentarTipoObrigacao(v.Tipo, out var tipo))
            {
                erros.Add($"Versão de layout com tipo desconhecido '{v.Tipo}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(v.Versao) || string.IsNullOrWhiteSpace(v.Fonte))
            {
                erros.Add($"Versão de layout de {v.Tipo} sem versão ou fonte.");
                continue;
            }

            versoes.Add(new VersaoLayout(tipo, v.Versao, v.VigenciaInicio, v.VigenciaFim, v.Conferido, v.Fonte));
        }

        return erros.Count > 0 ? throw new CatalogoInvalidoException(erros) : new CatalogoVersoesLayout(versoes);
    }

    private static readonly Dictionary<string, TipoObrigacao> AliasesObrigacao = new(StringComparer.Ordinal)
    {
        ["EFD_CONTRIB"] = TipoObrigacao.EfdContribuicoes,
    };

    private static Regra? ConverterCoesao(
        JsonElement r,
        Dictionary<string, FonteOficial> fontes,
        List<(int Passo, string Prefixo)> passos,
        Dictionary<string, AprovacaoDto> aprovacoes,
        List<string> erros)
    {
        var id = Texto(r, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            erros.Add("Regra do catálogo de coesão sem id.");
            return null;
        }

        var pendencias = new List<string>();
        TipoRelacao? relacao = Codigos.TentarRelacao(Texto(r, "tipoRelacao"), out var rel) ? rel : null;
        if (relacao is null)
        {
            erros.Add($"Regra {id}: tipoRelacao inválido.");
            return null;
        }

        var origem = Lado(r, "origem");
        var destino = Lado(r, "destino");
        var obrigacoes = new Dictionary<TipoObrigacao, IReadOnlyList<string>>();
        foreach (var lado in new[] { origem, destino })
        {
            if (lado is null)
            {
                continue;
            }

            if (lado.Tipo is { } tipo)
            {
                obrigacoes[tipo] = [];
            }
            else
            {
                pendencias.Add($"obrigação '{lado.CodigoObrigacao}' não é recebida no lote (evidência externa).");
            }
        }

        var severidades = new Dictionary<string, Severidade>(StringComparer.Ordinal);
        AdicionarSeveridade(severidades, Regra.SeveridadePadrao, Texto(r, "severidade"), pendencias);
        if (r.TryGetProperty("severidadeAtenuada", out var atenuada) && atenuada.ValueKind == JsonValueKind.Object)
        {
            AdicionarSeveridade(severidades, ChaveSeveridadeAtenuada, Texto(atenuada, "severidade"), pendencias);
        }

        var listaFontes = new List<FonteOficial>();
        foreach (var fonte in Lista(r, "fontes"))
        {
            if (fontes.TryGetValue(fonte, out var oficial))
            {
                listaFontes.Add(oficial);
            }
            else
            {
                erros.Add($"Regra {id}: fonte {fonte} ausente de fontes.json.");
            }
        }

        if (Texto(r, "executor") == "AGENTE_ORIGEM")
        {
            pendencias.Add("executada pelo agente de origem; o agente de coesão só lê o resultado (FR-409).");
        }

        var dependencias = Lista(r, "dependeDe");
        var aprovacao = aprovacoes.GetValueOrDefault(id);
        var dispensadas = aprovacao?.DependenciasDispensadas ?? [];
        foreach (var (dependencia, motivo) in dispensadas)
        {
            if (!dependencias.Contains(dependencia, StringComparer.Ordinal) || string.IsNullOrWhiteSpace(motivo))
            {
                erros.Add($"Regra {id}: dispensa de {dependencia} inválida (não é dependência do catálogo ou não tem motivo).");
            }
        }

        var status = StatusRegra.Proposta;
        if (aprovacao is not null)
        {
            status = aprovacao.Status switch
            {
                "APROVADA" => StatusRegra.Aprovada,
                "SUSPENSA" => StatusRegra.Suspensa,
                "PROPOSTA" => StatusRegra.Proposta,
                _ => Erro<StatusRegra>(erros, id, "status da aprovação"),
            };
        }

        var vigenciaInicio = DateOnly.TryParse(Texto(r, "vigenciaInicio"), System.Globalization.CultureInfo.InvariantCulture, out var inicio) ? inicio : Erro<DateOnly>(erros, id, "vigenciaInicio");
        DateOnly? vigenciaFim = DateOnly.TryParse(Texto(r, "vigenciaFim"), System.Globalization.CultureInfo.InvariantCulture, out var fim) ? fim : null;
        var evidencia = Texto(r, "evidencia") switch
        {
            "PVA" => Evidencia.Pva,
            "MANUAL" => Evidencia.Manual,
            "NORMA" => Evidencia.Norma,
            "DERIVADA" => Evidencia.Derivada,
            _ => (Evidencia?)null,
        };

        return new Regra
        {
            Id = id,
            Titulo = Texto(r, "titulo") ?? string.Empty,
            Tipo = TipoRegra.Cruzamento,
            Relacao = relacao,
            Obrigacoes = obrigacoes,
            Evidencia = evidencia,
            Severidades = severidades,
            ResultadoEsperado = Texto(r, "mensagemParecer") ?? Texto(r, "condicaoDescritiva") ?? Texto(r, "titulo") ?? string.Empty,
            Fontes = aprovacao?.Fontes is { Count: > 0 } conferidas
                ? conferidas.Select(f => new FonteOficial(f.Emissor, f.Documento, f.Versao, f.Localizacao, f.Conferida)).ToList()
                : listaFontes,
            HipotesesLegitimas = Lista(r, "hipotesesLegitimas"),
            VigenciaInicio = vigenciaInicio,
            VigenciaFim = vigenciaFim,
            Status = status,
            AprovadoPor = aprovacao?.AprovadoPor,
            DependeDe = dependencias.Where(d => !dispensadas.ContainsKey(d)).ToList(),
            DependenciasDispensadas = dispensadas,
            Implementacao = Implementacao.Deterministica,
            Testes = aprovacao?.Testes ?? [],
            Passo = PassoDe(id, passos, erros),
            ParametrosUsados = [.. ReferenciaParametro().Matches(r.GetRawText()).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal)],
            Pendencias = pendencias,
            Declaracao = new DeclaracaoRegra
            {
                Origem = origem,
                Destino = destino,
                ChaveJuncao = Lista(r, "chaveJuncao"),
                Operador = Texto(r, "operador"),
                Tolerancia = Texto(r, "tolerancia"),
                AplicavelSe = Texto(r, "aplicavelSe"),
                CondicaoDescritiva = Texto(r, "condicaoDescritiva"),
                FiltrosExclusao = Lista(r, "filtrosExclusao"),
                Cadeias = Lista(r, "cadeias"),
            },
        };
    }

    /// <summary>Chave da severidade de <c>severidadeAtenuada</c> no mapa de severidades da regra.</summary>
    public const string ChaveSeveridadeAtenuada = "atenuada";

    private static void AdicionarSeveridade(Dictionary<string, Severidade> severidades, string chave, string? valor, List<string> pendencias)
    {
        // ESCRITURACAO_GRAVE e ESTRUTURA_GRAVE do catálogo correspondem a ERRO_GRAVE (mesma correspondência das regras M1 aprovadas).
        Severidade? severidade = valor switch
        {
            "ESCRITURACAO_GRAVE" or "ESTRUTURA_GRAVE" or "ERRO_GRAVE" => Severidade.ErroGrave,
            "ALERTA" => Severidade.Alerta,
            _ => null,
        };
        if (severidade is { } s)
        {
            severidades[chave] = s;
        }
        else
        {
            pendencias.Add($"severidade '{valor}' sem correspondência no domínio (ERRO_GRAVE/ALERTA; ver T001 da spec 001).");
        }
    }

    private static LadoRegra? Lado(JsonElement r, string nome)
    {
        if (!r.TryGetProperty(nome, out var lado) || lado.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var codigo = Texto(lado, "obrigacao") ?? string.Empty;
        TipoObrigacao? tipo = Codigos.TentarTipoObrigacao(codigo, out var t) ? t
            : AliasesObrigacao.TryGetValue(codigo, out var alias) ? alias : null;
        return new LadoRegra(tipo, codigo, Texto(lado, "elemento"), Texto(lado, "filtro"), Texto(lado, "agregacao"));
    }

    private static int PassoDe(string id, List<(int Passo, string Prefixo)> passos, List<string> erros)
    {
        foreach (var (passo, prefixo) in passos)
        {
            if (id.StartsWith(prefixo, StringComparison.Ordinal))
            {
                return passo;
            }
        }

        erros.Add($"Regra {id}: sem passo em cadeias.json > ordemExecucao.");
        return int.MaxValue;
    }

    private static CatalogoParametros CarregarParametros(string arquivo, List<string> erros)
    {
        using var documento = LerDocumento(arquivo);
        var itens = new Dictionary<string, ParametroRegra>(StringComparer.Ordinal);
        foreach (var p in documento.RootElement.GetProperty("parametros").EnumerateArray())
        {
            var nome = Texto(p, "nome");
            if (string.IsNullOrWhiteSpace(nome))
            {
                erros.Add("Parâmetro sem nome em parametros.json.");
                continue;
            }

            var definido = p.TryGetProperty("valor", out var valor) && valor.ValueKind != JsonValueKind.Null;
            decimal? numero = definido && valor.ValueKind == JsonValueKind.Number ? valor.GetDecimal() : null;
            itens[nome] = new ParametroRegra(nome, Texto(p, "tipo") ?? string.Empty, definido, numero, Texto(p, "decisao"));
        }

        return new CatalogoParametros(itens);
    }

    private static Dictionary<string, FonteOficial> CarregarFontes(string arquivo, List<string> erros)
    {
        using var documento = LerDocumento(arquivo);
        var fontes = new Dictionary<string, FonteOficial>(StringComparer.Ordinal);
        foreach (var f in documento.RootElement.GetProperty("fontes").EnumerateArray())
        {
            var id = Texto(f, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                erros.Add("Fonte sem id em fontes.json.");
                continue;
            }

            fontes[id] = new FonteOficial(
                Texto(f, "orgao") ?? string.Empty,
                Texto(f, "documento") ?? string.Empty,
                Texto(f, "versao") ?? string.Empty,
                "fontes.json#" + id,
                f.TryGetProperty("verificadoEmFontePrimaria", out var v) && v.ValueKind == JsonValueKind.True);
        }

        return fontes;
    }

    private static List<(int Passo, string Prefixo)> CarregarPassos(string arquivo, List<string> erros)
    {
        using var documento = LerDocumento(arquivo);
        var passos = new List<(int, string)>();
        foreach (var passo in documento.RootElement.GetProperty("ordemExecucao").EnumerateArray())
        {
            var numero = passo.GetProperty("passo").GetInt32();
            passos.AddRange(Lista(passo, "prefixos").Select(p => (numero, p)));
        }

        if (passos.Count == 0)
        {
            erros.Add("cadeias.json sem ordemExecucao.");
        }

        // Prefixo mais longo primeiro: "M1-" não pode capturar "M10-".
        return [.. passos.OrderByDescending(p => p.Item2.Length)];
    }

    private static (string Versao, Dictionary<string, AprovacaoDto> Aprovacoes) CarregarAprovacoes(string arquivo, List<string> erros)
    {
        var dto = Ler<AprovacoesDto>(arquivo);
        var aprovacoes = new Dictionary<string, AprovacaoDto>(StringComparer.Ordinal);
        foreach (var a in dto.Aprovacoes)
        {
            if (!aprovacoes.TryAdd(a.Id, a))
            {
                erros.Add($"Aprovação duplicada para {a.Id}.");
            }
        }

        return (dto.Versao, aprovacoes);
    }

    private static JsonDocument LerDocumento(string arquivo)
    {
        if (!File.Exists(arquivo))
        {
            throw new CatalogoInvalidoException([$"Arquivo de catálogo não encontrado: {arquivo}"]);
        }

        try
        {
            return JsonDocument.Parse(File.ReadAllBytes(arquivo));
        }
        catch (JsonException ex)
        {
            throw new CatalogoInvalidoException([$"Arquivo de catálogo inválido: {arquivo} (linha {ex.LineNumber})."]);
        }
    }

    private static string? Texto(JsonElement objeto, string nome) =>
        objeto.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    private static List<string> Lista(JsonElement objeto, string nome) =>
        objeto.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.Array
            ? [.. valor.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!)]
            : [];

    [System.Text.RegularExpressions.GeneratedRegex(@"PARAM:([A-Z0-9_]+)")]
    private static partial System.Text.RegularExpressions.Regex ReferenciaParametro();

    private static T Ler<T>(string arquivo)
    {
        if (!File.Exists(arquivo))
        {
            throw new CatalogoInvalidoException([$"Arquivo de catálogo não encontrado: {arquivo}"]);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllBytes(arquivo), Json)
                ?? throw new CatalogoInvalidoException([$"Arquivo de catálogo vazio: {arquivo}"]);
        }
        catch (JsonException ex)
        {
            throw new CatalogoInvalidoException([$"Arquivo de catálogo inválido: {arquivo} (caminho: {ex.Path})."]);
        }
    }

    private static Regra? Converter(RegraDto r, List<string> erros)
    {
        var errosAntes = erros.Count;
        var tipo = r.Tipo switch
        {
            "PRE_CONDICAO" => TipoRegra.PreCondicao,
            "CRUZAMENTO" => TipoRegra.Cruzamento,
            _ => Erro<TipoRegra>(erros, r.Id, "tipo"),
        };
        TipoRelacao? relacao = r.Relacao is null ? null
            : Codigos.TentarRelacao(r.Relacao, out var rel) ? rel : Erro<TipoRelacao>(erros, r.Id, "relação");
        Evidencia? evidencia = r.Evidencia switch
        {
            null => null,
            "PVA" => Evidencia.Pva,
            "MANUAL" => Evidencia.Manual,
            "NORMA" => Evidencia.Norma,
            "DERIVADA" => Evidencia.Derivada,
            _ => Erro<Evidencia>(erros, r.Id, "evidência"),
        };
        var status = r.Status switch
        {
            "PROPOSTA" => StatusRegra.Proposta,
            "APROVADA" => StatusRegra.Aprovada,
            "SUSPENSA" => StatusRegra.Suspensa,
            _ => Erro<StatusRegra>(erros, r.Id, "status"),
        };
        var implementacao = r.Implementacao switch
        {
            "DETERMINISTICA" => Implementacao.Deterministica,
            "ASSISTIDA_IA" => Implementacao.AssistidaIa,
            _ => Erro<Implementacao>(erros, r.Id, "implementação"),
        };

        var severidades = new Dictionary<string, Severidade>(StringComparer.Ordinal);
        foreach (var (chave, valor) in r.Severidades ?? [])
        {
            severidades[chave] = valor switch
            {
                "ERRO_GRAVE" => Severidade.ErroGrave,
                "ALERTA" => Severidade.Alerta,
                _ => Erro<Severidade>(erros, r.Id, $"severidade '{chave}'"),
            };
        }

        var obrigacoes = new Dictionary<TipoObrigacao, IReadOnlyList<string>>();
        foreach (var (codigo, versoes) in r.Obrigacoes ?? [])
        {
            if (Codigos.TentarTipoObrigacao(codigo, out var tipoObrigacao))
            {
                obrigacoes[tipoObrigacao] = versoes;
            }
            else
            {
                Erro<TipoObrigacao>(erros, r.Id, $"obrigação '{codigo}'");
            }
        }

        if (erros.Count > errosAntes)
        {
            return null;
        }

        return new Regra
        {
            Id = r.Id,
            Titulo = r.Titulo,
            Tipo = tipo,
            Relacao = relacao,
            Obrigacoes = obrigacoes,
            Evidencia = evidencia,
            Severidades = severidades,
            ResultadoEsperado = r.ResultadoEsperado,
            Fontes = r.Fontes.Select(f => new FonteOficial(f.Emissor, f.Documento, f.Versao, f.Localizacao, f.Conferida)).ToList(),
            HipotesesLegitimas = r.HipotesesLegitimas ?? [],
            VigenciaInicio = r.VigenciaInicio,
            VigenciaFim = r.VigenciaFim,
            Status = status,
            AprovadoPor = r.AprovadoPor,
            DependeDe = r.DependeDe ?? [],
            Implementacao = implementacao,
            JustificativaImplementacao = r.JustificativaImplementacao,
            Testes = r.Testes ?? [],
        };
    }

    private static T Erro<T>(List<string> erros, string id, string campo)
        where T : struct
    {
        erros.Add($"Regra {id}: valor inválido para {campo}.");
        return default;
    }

#pragma warning disable CA1812 // Instanciadas pelo System.Text.Json.
    private sealed record CatalogoDto(string? Descricao, IReadOnlyList<RegraDto> Regras);

    private sealed record RegraDto(
        string Id,
        string Titulo,
        string Tipo,
        string? Relacao,
        Dictionary<string, List<string>>? Obrigacoes,
        string? Evidencia,
        Dictionary<string, string>? Severidades,
        string ResultadoEsperado,
        IReadOnlyList<FonteDto> Fontes,
        List<string>? HipotesesLegitimas,
        DateOnly VigenciaInicio,
        DateOnly? VigenciaFim,
        string Status,
        string? AprovadoPor,
        List<string>? DependeDe,
        string Implementacao,
        string? JustificativaImplementacao,
        List<string>? Testes);

    private sealed record AprovacoesDto(string Versao, string? Descricao, IReadOnlyList<AprovacaoDto> Aprovacoes);

    private sealed record AprovacaoDto(
        string Id,
        string Status,
        string? AprovadoPor,
        List<string>? Testes,
        List<FonteDto>? Fontes,
        Dictionary<string, string>? DependenciasDispensadas);

    private sealed record FonteDto(string Emissor, string Documento, string Versao, string Localizacao, bool Conferida);

    private sealed record VersoesDto(IReadOnlyList<VersaoDto> Versoes, IReadOnlyList<string>? Pendencias);

    private sealed record VersaoDto(string Tipo, string Versao, DateOnly? VigenciaInicio, DateOnly? VigenciaFim, bool Conferido, string Fonte);
#pragma warning restore CA1812
}
