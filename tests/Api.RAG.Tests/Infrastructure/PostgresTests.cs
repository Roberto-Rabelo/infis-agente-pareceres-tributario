using Api.RAG.Domain;
using Api.RAG.Infrastructure.Postgres;
using Api.RAG.Tests.Fixtures;
using Npgsql;
using Pgvector.Npgsql;
using Testcontainers.PostgreSql;

namespace Api.RAG.Tests.Infrastructure;

/// <summary>
/// PostgreSQL + PgVector real via Testcontainers. Sem Docker disponível os testes são marcados como ignorados
/// (não como aprovados), conforme a skill testes-agentes-ia.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string? ConnectionString { get; private set; }

    public string? MotivoIndisponivel { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("pgvector/pgvector:pg17").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            await MigradorPostgres.AplicarAsync(ConnectionString, Ambiente.Dimensao, CancellationToken.None);
        }
#pragma warning disable CA1031 // Qualquer falha do Docker torna o teste ignorável.
        catch (Exception erro)
#pragma warning restore CA1031
        {
            MotivoIndisponivel = $"Docker/PostgreSQL indisponível: {erro.GetType().Name}";
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public NpgsqlDataSource Fonte()
    {
        var construtor = new NpgsqlDataSourceBuilder(ConnectionString);
        construtor.UseVector();
        return construtor.Build();
    }
}

public class PostgresTests(PostgresFixture banco) : IClassFixture<PostgresFixture>
{
    private void ExigirBanco() => Skip.If(banco.ConnectionString is null, banco.MotivoIndisponivel);

    [SkippableFact]
    public async Task Migracao_EhIdempotente()
    {
        ExigirBanco();

        await MigradorPostgres.AplicarAsync(banco.ConnectionString!, Ambiente.Dimensao, CancellationToken.None);
    }

    [SkippableFact]
    public async Task Migracao_RecusaDimensaoDiferenteDaExistente()
    {
        ExigirBanco();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MigradorPostgres.AplicarAsync(banco.ConnectionString!, Ambiente.Dimensao * 2, CancellationToken.None));
    }

    [SkippableFact]
    public async Task IngestaoEConsulta_PorRegraESemantica()
    {
        ExigirBanco();
        await using var fonte = banco.Fonte();
        var repositorio = new RepositorioPostgres(fonte);
        var ambiente = new Ambiente(repositorio, repositorio);

        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(chave: "pg/regras"), CancellationToken.None);
        var porRegra = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-NFE-EFD-002"]), CancellationToken.None);
        var semantica = await ambiente.Consulta.ConsultarAsync(
            Ambiente.Pergunta("evento de cancelamento homologado COD_SIT", obrigacoes: ["EFD_ICMS_IPI"], limite: 2), CancellationToken.None);

        Assert.Equal("CRZ-NFE-EFD-002", porRegra.Trechos[0].Trecho.RegraPrincipal);
        Assert.Contains(porRegra.Trechos, t => t.Documento.Chave == "pg/regras");
        Assert.Equal("CRZ-NFE-EFD-002", semantica.Trechos[0].Trecho.RegraPrincipal);
        Assert.NotNull(semantica.Trechos[0].Similaridade);
    }

    [SkippableFact]
    public async Task NovaVersao_SubstituiAAnteriorNaMesmaTransacao()
    {
        ExigirBanco();
        await using var fonte = banco.Fonte();
        var repositorio = new RepositorioPostgres(fonte);
        var ambiente = new Ambiente(repositorio, repositorio);

        var v1 = await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(chave: "pg/versoes"), CancellationToken.None);
        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(chave: "pg/versoes", versao: "v2", conteudo: Documentos.Cruzamentos + "\nRevisão."), CancellationToken.None);

        var documentos = (await repositorio.ListarAsync(CancellationToken.None)).Where(d => d.Chave == "pg/versoes").ToList();
        Assert.Equal(StatusDocumento.Substituido, documentos.Single(d => d.Id == v1.Documento.Id).Status);
        Assert.Equal("v2", documentos.Single(d => d.Status == StatusDocumento.Ativo).Versao);
        var consulta = await ambiente.Consulta.ConsultarAsync(Ambiente.Pergunta(regraIds: ["CRZ-ECD-ECF-001"]), CancellationToken.None);
        Assert.DoesNotContain(consulta.Trechos, t => t.Documento.Id == v1.Documento.Id);
    }

    [SkippableFact]
    public async Task Consumo_PersistidoComTokensDesconhecidosComoNulo()
    {
        ExigirBanco();
        await using var fonte = banco.Fonte();
        var repositorio = new RepositorioPostgres(fonte);
        var ambiente = new Ambiente(repositorio, repositorio);
        ambiente.Gerador.InformarUso = false;

        await ambiente.Ingestao.IngerirAsync(Ambiente.Comando(chave: "pg/consumo"), CancellationToken.None);

        await using var comando = fonte.CreateCommand("SELECT count(*) FROM rag_consumo_embeddings WHERE correlacao_id = 'corr-teste' AND tokens_entrada IS NULL");
        Assert.True((long)(await comando.ExecuteScalarAsync())! > 0);
    }
}
