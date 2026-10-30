using System.Text.Json;
using Api.Agente.Application.Parecer;
using Api.Agente.Domain;
using Api.Agente.Tests.Fixtures;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Xunit.Abstractions;

namespace Api.Agente.Tests.Avaliacao;

/// <summary>
/// T075 (P17). Na CI: consistência do conjunto e execução do avaliador com provedor roteirizado.
/// Com o modelo real: ignorado sem chave; gera custo no provedor.
/// </summary>
public class AvaliacaoParecerTests(ITestOutputHelper saida)
{
    [Fact]
    public void Conjunto_RegrasDosCenariosExistemNaBaseDeCruzamentos()
    {
        var conjunto = AvaliadorParecer.Carregar();

        Assert.Equal("1.0.0", conjunto.Versao);
        Assert.Equal(conjunto.Cenarios.Count, conjunto.Cenarios.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(conjunto.Cenarios.SelectMany(c => c.Achados), a => Assert.NotNull(AvaliadorParecer.LinhaDaRegra(a.RegraId)));
        Assert.Contains(conjunto.Cenarios, c => c.Injecao);
        Assert.Contains(conjunto.Cenarios.SelectMany(c => c.Achados), a => a.Severidade is null);
    }

    [Fact]
    public void LinhaDaRegra_LeHipotesesDaTabela()
    {
        Assert.Equal(["NF-e cancelada (ver 002)"], AvaliadorParecer.LinhaDaRegra("CRZ-NFE-EFD-001")!.Value.Hipoteses);
        Assert.Empty(AvaliadorParecer.LinhaDaRegra("CRZ-NFE-EFD-002")!.Value.Hipoteses);
        Assert.Null(AvaliadorParecer.LinhaDaRegra("CRZ-NFE-EFD-999"));
    }

    [Fact]
    public async Task Avaliador_ComProvedorRoteirizado_AceitaTodosSemAlterarAchados()
    {
        var relatorio = await AvaliadorParecer.ExecutarAsync(
            AvaliadorParecer.Carregar(),
            analise => new ChatFalso().Responder(Pareceres.ParecerValido(analise)),
            new OpcoesParecer { Habilitado = true, Modelo = "modelo-teste", EsperaRetentativaMs = 0 },
            CancellationToken.None);

        Assert.Equal(1, relatorio.TaxaAceite);
        Assert.Equal(1, relatorio.TaxaAceitePrimeiraTentativa);
        Assert.Equal(0, relatorio.AchadosAlterados);
        Assert.NotNull(relatorio.TokensEntrada);
    }

    /// <summary>Exige OPENAI_API_KEY e PARECER_AVALIACAO_MODELO. O relatório deve ser registrado no plano (constitution 6.2, item 12).</summary>
    [SkippableFact]
    public async Task Avaliacao_ComModeloReal()
    {
        var chave = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var modelo = Environment.GetEnvironmentVariable("PARECER_AVALIACAO_MODELO");
        Skip.If(string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(modelo), "Avaliação real ignorada: defina OPENAI_API_KEY e PARECER_AVALIACAO_MODELO.");

        var provedor = new OpenAIChatCompletionService(modelo!, chave!);
        var relatorio = await AvaliadorParecer.ExecutarAsync(
            AvaliadorParecer.Carregar(), _ => provedor, new OpcoesParecer { Habilitado = true, Modelo = modelo! }, CancellationToken.None);

        var diretorio = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "avaliacao")).FullName;
        var arquivo = Path.Combine(diretorio, $"parecer-{relatorio.VersaoConjunto}-{relatorio.ExecutadoEm:yyyyMMddHHmmss}.json");
        await File.WriteAllTextAsync(arquivo, JsonSerializer.Serialize(relatorio, AvaliadorParecer.Json));
        saida.WriteLine($"aceite={relatorio.TaxaAceite:P0} primeira={relatorio.TaxaAceitePrimeiraTentativa:P0} alterados={relatorio.AchadosAlterados}");
        saida.WriteLine($"Relatório: {arquivo}");

        Assert.Equal(0, relatorio.AchadosAlterados);
        Assert.DoesNotContain(relatorio.Cenarios, c => c.Aceito && c.StatusFinal == StatusAnalise.FalhaTecnica);
    }
}
