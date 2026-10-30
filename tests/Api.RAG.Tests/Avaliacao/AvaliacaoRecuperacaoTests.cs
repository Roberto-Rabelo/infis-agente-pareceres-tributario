using System.Globalization;
using System.Text.Json;
using Api.RAG.Domain;
using Api.RAG.Tests.Fixtures;
using Microsoft.Extensions.AI;
using OpenAI.Embeddings;
using Xunit.Abstractions;

namespace Api.RAG.Tests.Avaliacao;

/// <summary>
/// T065 (P7, D12). A CI só verifica a consistência do conjunto e o funcionamento do avaliador com embeddings falsos;
/// a medição de qualidade usa o provedor real e fica ignorada sem chave (P17).
/// </summary>
public class AvaliacaoRecuperacaoTests(ITestOutputHelper saida)
{
    private const int MaximoCaracteresAtual = 1500;
    private const int TopKAtual = 8;

    [Fact]
    public void Conjunto_RegrasEsperadasExistemComoTrechoPrincipalNaBase()
    {
        var conjunto = AvaliadorRecuperacao.Carregar();
        var principais = AvaliadorRecuperacao.Documentos()
            .SelectMany(d => SegmentadorMarkdown.Segmentar(d.Conteudo, MaximoCaracteresAtual))
            .Select(t => t.RegraPrincipal)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var ausentes = conjunto.Casos
            .SelectMany(c => c.Esperados.Concat(c.Relacionados).Where(id => !principais.Contains(id)).Select(id => $"{c.Id}: {id}"))
            .ToList();

        Assert.Empty(ausentes);
    }

    [Fact]
    public void Conjunto_BemFormado()
    {
        var conjunto = AvaliadorRecuperacao.Carregar();

        Assert.Equal("1.0.0", conjunto.Versao);
        Assert.NotEmpty(conjunto.Casos);
        Assert.Equal(conjunto.Casos.Count, conjunto.Casos.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(conjunto.Casos, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Pergunta));
            Assert.NotEmpty(c.Esperados);
            Assert.All(c.Esperados.Concat(c.Relacionados), id => Assert.True(ReferenciasRegra.EhIdValido(id), id));
            Assert.All(c.Obrigacoes, o => Assert.Contains(o, CodigosObrigacao.Todos));
            Assert.Empty(c.Esperados.Intersect(c.Relacionados));
        });
    }

    /// <summary>Sem o identificador na pergunta, a avaliação mede a busca semântica e não a busca exata por regra.</summary>
    [Fact]
    public void Conjunto_PerguntasNaoCitamIdentificadores() =>
        Assert.All(AvaliadorRecuperacao.Carregar().Casos, c => Assert.Empty(ReferenciasRegra.Extrair(c.Pergunta)));

    /// <summary>Exercita o avaliador ponta a ponta; o número obtido com embeddings falsos não é evidência de qualidade.</summary>
    [Fact]
    public async Task Avaliador_ComEmbeddingsFalsos_ProduzRelatorioCompleto()
    {
        var conjunto = AvaliadorRecuperacao.Carregar();

        var relatorio = await AvaliadorRecuperacao.ExecutarAsync(
            conjunto, new GeradorFalso(Ambiente.Dimensao), new ConfiguracaoAvaliacao("Falso", Ambiente.Modelo, Ambiente.Dimensao, MaximoCaracteresAtual, TopKAtual), CancellationToken.None);

        Assert.Equal(conjunto.Casos.Count, relatorio.Casos.Count);
        Assert.True(relatorio.QuantidadeTrechos > 0);
        Assert.All(relatorio.Casos, c => Assert.InRange(c.Recuperados.Count, 1, TopKAtual));
        Assert.InRange(relatorio.AcertoEmK, 0, 1);
        Assert.InRange(relatorio.Mrr, 0, relatorio.AcertoEmK);
        Assert.NotNull(relatorio.TokensEmbeddings);
    }

    /// <summary>
    /// Medição real: exige OPENAI_API_KEY, RAG_AVALIACAO_MODELO e RAG_AVALIACAO_DIMENSAO (opcionais: RAG_AVALIACAO_MAXIMO_CARACTERES,
    /// RAG_AVALIACAO_TOPK). Gera custo no provedor. O relatório deve ser registrado no plano antes de aprovar o ADR D12.
    /// </summary>
    [SkippableFact]
    public async Task Avaliacao_ComProvedorReal()
    {
        var chave = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var modelo = Environment.GetEnvironmentVariable("RAG_AVALIACAO_MODELO");
        var dimensao = Environment.GetEnvironmentVariable("RAG_AVALIACAO_DIMENSAO");
        Skip.If(
            string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(modelo) || string.IsNullOrWhiteSpace(dimensao),
            "Avaliação real ignorada: defina OPENAI_API_KEY, RAG_AVALIACAO_MODELO e RAG_AVALIACAO_DIMENSAO.");

        var configuracao = new ConfiguracaoAvaliacao(
            "OpenAI",
            modelo!,
            int.Parse(dimensao!, CultureInfo.InvariantCulture),
            Inteiro("RAG_AVALIACAO_MAXIMO_CARACTERES") ?? MaximoCaracteresAtual,
            Inteiro("RAG_AVALIACAO_TOPK") ?? TopKAtual);
        using var gerador = new EmbeddingClient(modelo, chave).AsIEmbeddingGenerator(configuracao.Dimensao);

        var relatorio = await AvaliadorRecuperacao.ExecutarAsync(AvaliadorRecuperacao.Carregar(), gerador, configuracao, CancellationToken.None);

        var diretorio = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "avaliacao")).FullName;
        var arquivo = Path.Combine(diretorio, $"recuperacao-{relatorio.VersaoConjunto}-{relatorio.ExecutadoEm:yyyyMMddHHmmss}.json");
        await File.WriteAllTextAsync(arquivo, JsonSerializer.Serialize(relatorio, AvaliadorRecuperacao.Json));
        saida.WriteLine($"acerto@{configuracao.TopK}={relatorio.AcertoEmK:P1} revocacao={relatorio.RevocacaoMedia:P1} mrr={relatorio.Mrr:F3} tokens={relatorio.TokensEmbeddings?.ToString(CultureInfo.InvariantCulture) ?? "desconhecido"}");
        saida.WriteLine($"Relatório: {arquivo}");
        Assert.Equal(AvaliadorRecuperacao.Carregar().Casos.Count, relatorio.Casos.Count);
    }

    private static int? Inteiro(string variavel) =>
        int.TryParse(Environment.GetEnvironmentVariable(variavel), NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) ? valor : null;
}
