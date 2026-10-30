using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.RAG.Application;
using Api.RAG.Domain;
using Api.RAG.Tests.Fixtures;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Api.RAG.Tests.Avaliacao;

public sealed record CasoAvaliacao(string Id, string Pergunta, IReadOnlyList<string> Obrigacoes, IReadOnlyList<string> Esperados, IReadOnlyList<string> Relacionados);

public sealed record ConjuntoAvaliacao(string Versao, string Descricao, string Base, IReadOnlyList<CasoAvaliacao> Casos);

public sealed record ConfiguracaoAvaliacao(string Provedor, string Modelo, int Dimensao, int MaximoCaracteres, int TopK);

public sealed record ResultadoCaso(string Id, int? PosicaoPrimeiroEsperado, double Revocacao, IReadOnlyList<string> RelacionadosRecuperados, IReadOnlyList<string?> Recuperados);

/// <summary>Métricas por regra principal do trecho: acerto@k, revocação@k dos esperados e MRR. Tokens nulos = desconhecidos.</summary>
public sealed record RelatorioAvaliacao(
    string VersaoConjunto,
    ConfiguracaoAvaliacao Configuracao,
    DateTimeOffset ExecutadoEm,
    int QuantidadeTrechos,
    double AcertoEmK,
    double RevocacaoMedia,
    double Mrr,
    long? TokensEmbeddings,
    IReadOnlyList<ResultadoCaso> Casos);

/// <summary>Executa o conjunto de avaliação de recuperação (T065) sobre o repositório em memória.</summary>
public static class AvaliadorRecuperacao
{
    private static readonly string[] TodasObrigacoes = [.. CodigosObrigacao.Todos.Order(StringComparer.Ordinal)];

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string DiretorioDocs => Path.Combine(AppContext.BaseDirectory, "docs");

    public static ConjuntoAvaliacao Carregar(string versao = "v1") =>
        JsonSerializer.Deserialize<ConjuntoAvaliacao>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "eval", "recuperacao", $"{versao}.json")), Json)!;

    public static IEnumerable<(string Nome, string Conteudo)> Documentos() =>
        Directory.GetFiles(DiretorioDocs, "*.md")
            .Order(StringComparer.Ordinal)
            .Select(caminho => (Path.GetFileName(caminho), File.ReadAllText(caminho, Encoding.UTF8)));

    public static async Task<RelatorioAvaliacao> ExecutarAsync(
        ConjuntoAvaliacao conjunto, IEmbeddingGenerator<string, Embedding<float>> gerador, ConfiguracaoAvaliacao configuracao, CancellationToken cancelamento)
    {
        var memoria = new RepositorioEmMemoria();
        var opcoesEmbeddings = Options.Create(new OpcoesEmbeddings
        {
            Provedor = configuracao.Provedor,
            Modelo = configuracao.Modelo,
            Dimensao = configuracao.Dimensao,
            TimeoutSegundos = 120,
        });
        var provedor = new ProvedorEmbeddings(gerador, opcoesEmbeddings, memoria, TimeProvider.System, NullLogger<ProvedorEmbeddings>.Instance);
        var ingestao = new ServicoIngestao(memoria, provedor, Options.Create(new OpcoesSegmentacao { MaximoCaracteres = configuracao.MaximoCaracteres }), TimeProvider.System);
        var consulta = new ServicoConsulta(memoria, provedor, opcoesEmbeddings, Options.Create(new OpcoesConsulta()));

        foreach (var (nome, conteudo) in Documentos())
        {
            var versao = "sha256-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(conteudo)))[..16];
            await ingestao.IngerirAsync(
                Ambiente.Comando(conteudo, "docs/" + nome.ToLowerInvariant(), versao, origem: "docs/" + nome, obrigacoes: TodasObrigacoes),
                cancelamento);
        }

        var casos = new List<ResultadoCaso>();
        foreach (var caso in conjunto.Casos)
        {
            var resultado = await consulta.ConsultarAsync(
                new ConsultaConhecimento(caso.Pergunta, [], caso.Obrigacoes, [], null, configuracao.TopK, "avaliacao-" + caso.Id), cancelamento);
            var principais = resultado.Trechos.Select(t => t.Trecho.RegraPrincipal).ToList();
            var posicao = principais.FindIndex(p => p is not null && caso.Esperados.Contains(p));
            casos.Add(new ResultadoCaso(
                caso.Id,
                posicao < 0 ? null : posicao + 1,
                caso.Esperados.Count(principais.Contains) / (double)caso.Esperados.Count,
                [.. caso.Relacionados.Where(principais.Contains)],
                principais));
        }

        var tokens = memoria.Consumo.Aggregate<RegistroConsumoEmbeddings, long?>(0, (total, r) => total is null || r.TokensEntrada is null ? null : total + r.TokensEntrada);
        return new RelatorioAvaliacao(
            conjunto.Versao,
            configuracao,
            DateTimeOffset.UtcNow,
            memoria.QuantidadeTrechos,
            casos.Count(c => c.PosicaoPrimeiroEsperado is not null) / (double)casos.Count,
            casos.Average(c => c.Revocacao),
            casos.Average(c => c.PosicaoPrimeiroEsperado is { } p ? 1.0 / p : 0),
            tokens,
            casos);
    }
}
