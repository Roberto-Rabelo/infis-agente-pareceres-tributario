using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Api.Agente.Infrastructure.Catalogo;
using Api.Agente.Tests.Fixtures;
using Json.Schema;
using static Api.Agente.Tests.Fixtures.Lotes;

namespace Api.Agente.Tests.Governanca;

public class GovernancaTests
{
    private static readonly string[] NamespacesProibidosNoDominio =
    [
        "Api.Agente.Application",
        "Api.Agente.Infrastructure",
        "Api.Agente.Endpoints",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions",
        "System.Text.Json",
    ];

    internal static readonly Lazy<JsonSchema> SchemaEntrada = new(() =>
        JsonSchema.FromFile(Path.Combine(AppContext.BaseDirectory, "contracts", "entrada", "lote.v1.schema.json")));

    [Fact]
    public void CatalogoReal_CarregaSemErros()
    {
        var diretorio = CarregadorCatalogo.ResolverDiretorio("catalog");

        Assert.NotEmpty(CarregadorCatalogo.CarregarRegras(diretorio).Regras);
        Assert.NotEmpty(CarregadorCatalogo.CarregarVersoes(diretorio).Versoes);
    }

    /// <summary>Rastreabilidade regra → teste (constitution 6.2): todo teste citado no catálogo existe.</summary>
    [Fact]
    public void CatalogoReal_TestesCitadosExistem()
    {
        var catalogo = CarregadorCatalogo.CarregarRegras(CarregadorCatalogo.ResolverDiretorio("catalog"));
        var testes = typeof(GovernancaTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods().Where(m => m.GetCustomAttributes<FactAttribute>(true).Any()).Select(m => $"{t.Name}.{m.Name}"))
            .ToHashSet();

        var ausentes = catalogo.Regras.SelectMany(r => r.Testes.Where(t => !testes.Contains(t)).Select(t => $"{r.Id}: {t}")).ToList();

        Assert.Empty(ausentes);
    }

    [Fact]
    public void ContratoEntrada_FixturesValidasAtendemAoSchemaPublicado()
    {
        using var documento = JsonDocument.Parse(Bytes(Lote("11222333", "2026-01-01", "2026-12-31", EfdIcmsIpi(), Ecd(), NFe(), EfdIcmsIpi(cnpj: CnpjAlfanumerico))));

        var resultado = SchemaEntrada.Value.Evaluate(documento.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void ContratoEntrada_SchemaRejeitaCampoNaoPrevistoESemVersao()
    {
        var efd = EfdIcmsIpi();
        efd.Remove("versaoLayout");
        efd["extra"] = 1;
        using var documento = JsonDocument.Parse(Bytes(Lote(efd)));

        Assert.False(SchemaEntrada.Value.Evaluate(documento.RootElement).IsValid);
    }

    /// <summary>P12: o domínio não depende de aplicação, infraestrutura, endpoints, ASP.NET nem serialização.</summary>
    [Fact]
    public void Dominio_SemDependenciasProibidas()
    {
        var violacoes = typeof(Cnpj).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Api.Agente.Domain", StringComparison.Ordinal) == true)
            .SelectMany(t => TiposReferenciados(t).Where(Proibido).Select(r => $"{t.FullName} → {r.FullName}"))
            .Distinct()
            .ToList();

        Assert.Empty(violacoes);
    }

    /// <summary>P10: a saída do LLM não tem campos operacionais; eles são preenchidos pelo sistema no envelope.</summary>
    [Fact]
    public void ContratoSaida_SemCamposOperacionais()
    {
        string[] operacionais = ["execucao", "correlacao", "consumo", "token", "custo", "modelo", "versao", "inicio", "fim", "duracao", "prompt"];
        var schema = JsonNode.Parse(File.ReadAllText(SchemaSaidaParecer.Caminho(AppContext.BaseDirectory)))!;
        var campos = NomesDePropriedades(schema).ToList();
        var registro = typeof(ParecerLlm).GetProperties().Select(p => p.Name);

        Assert.NotEmpty(campos);
        Assert.DoesNotContain(campos.Concat(registro), c => operacionais.Any(o => c.Contains(o, StringComparison.OrdinalIgnoreCase)) && c != "VersaoContrato");
    }

    /// <summary>O schema enviado ao provedor é o mesmo contrato validado localmente, sem metadados de registro.</summary>
    [Fact]
    public void ContratoSaida_SchemaDoProvedorEhOContratoPublicado()
    {
        var publicado = JsonNode.Parse(File.ReadAllText(SchemaSaidaParecer.Caminho(AppContext.BaseDirectory)))!.AsObject();
        publicado.Remove("$schema");
        publicado.Remove("$id");

        var provedor = JsonNode.Parse(Pareceres.Schema.ParaProvedor);

        Assert.True(JsonNode.DeepEquals(publicado, provedor));
    }

    /// <summary>P8: o prompt versionado trata dados recebidos como não confiáveis e delimitados.</summary>
    [Fact]
    public void PromptParecer_DelimitaDadosNaoConfiaveis()
    {
        Assert.Contains("<<<DADOS", Pareceres.Prompt.Texto, StringComparison.Ordinal);
        Assert.Contains("DADOS>>>", Pareceres.Prompt.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiAgente_NaoReferenciaApiRag()
    {
        Assert.DoesNotContain(typeof(Cnpj).Assembly.GetReferencedAssemblies(), a => a.Name == "Api.RAG");
    }

    private static IEnumerable<string> NomesDePropriedades(JsonNode? no) => no switch
    {
        JsonObject objeto => objeto.SelectMany(p =>
            (p.Key == "properties" && p.Value is JsonObject propriedades ? propriedades.Select(q => q.Key) : [])
            .Concat(NomesDePropriedades(p.Value))),
        JsonArray lista => lista.SelectMany(NomesDePropriedades),
        _ => [],
    };

    private static bool Proibido(Type tipo) =>
        NamespacesProibidosNoDominio.Any(n => tipo.Namespace?.StartsWith(n, StringComparison.Ordinal) == true);

    private static IEnumerable<Type> TiposReferenciados(Type tipo)
    {
        const BindingFlags Todos = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var diretos = new List<Type?> { tipo.BaseType };
        diretos.AddRange(tipo.GetInterfaces());
        diretos.AddRange(tipo.GetFields(Todos).Select(f => f.FieldType));
        diretos.AddRange(tipo.GetProperties(Todos).Select(p => p.PropertyType));
        foreach (var metodo in tipo.GetMethods(Todos).Cast<MethodBase>().Concat(tipo.GetConstructors(Todos)))
        {
            diretos.AddRange(metodo.GetParameters().Select(p => p.ParameterType));
            if (metodo is MethodInfo info)
            {
                diretos.Add(info.ReturnType);
            }
        }

        return diretos.Where(t => t is not null).SelectMany(t => Expandir(t!));
    }

    private static IEnumerable<Type> Expandir(Type tipo)
    {
        if (tipo.HasElementType)
        {
            return Expandir(tipo.GetElementType()!);
        }

        return tipo.IsGenericType ? [tipo, .. tipo.GetGenericArguments().SelectMany(Expandir)] : [tipo];
    }
}
