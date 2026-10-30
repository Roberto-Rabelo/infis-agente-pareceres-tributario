using System.Text.Json;
using Api.Agente.Application.Contratos;
using Api.Agente.Application.PreCondicoes;
using Api.Agente.Domain;
using Api.Agente.Domain.Catalogo;

namespace Api.Agente.Infrastructure.Catalogo;

public sealed class OpcoesCatalogo
{
    public const string Secao = "Catalogo";

    public string Caminho { get; set; } = "catalog";
}

#pragma warning disable CA1032, CA1064 // Exceção de inicialização: o serviço não sobe com catálogo inválido.
public sealed class CatalogoInvalidoException(IReadOnlyList<string> erros)
    : Exception("Catálogo inválido:" + Environment.NewLine + string.Join(Environment.NewLine, erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;
}
#pragma warning restore CA1032, CA1064

/// <summary>Formato físico do catálogo (D21, proposta): catalog/regras.json e catalog/versoes-layout.json.</summary>
public static class CarregadorCatalogo
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
        var dto = Ler<CatalogoDto>(Path.Combine(diretorio, "regras.json"));
        var erros = new List<string>();
        var regras = dto.Regras.Select(r => Converter(r, erros)).Where(r => r is not null).Select(r => r!).ToList();
        var catalogo = new CatalogoRegras(dto.Versao, regras);

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
    private sealed record CatalogoDto(string Versao, IReadOnlyList<RegraDto> Regras);

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

    private sealed record FonteDto(string Emissor, string Documento, string Versao, string Localizacao, bool Conferida);

    private sealed record VersoesDto(IReadOnlyList<VersaoDto> Versoes, IReadOnlyList<string>? Pendencias);

    private sealed record VersaoDto(string Tipo, string Versao, DateOnly? VigenciaInicio, DateOnly? VigenciaFim, bool Conferido, string Fonte);
#pragma warning restore CA1812
}
