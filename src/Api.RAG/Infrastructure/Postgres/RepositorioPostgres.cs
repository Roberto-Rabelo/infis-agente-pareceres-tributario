using Api.RAG.Application;
using Api.RAG.Domain;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

namespace Api.RAG.Infrastructure.Postgres;

public sealed class RepositorioPostgres(NpgsqlDataSource fonte) : IRepositorioConhecimento, IRegistroConsumo
{
    private const string ColunasDocumento = """
        d.id, d.chave, d.titulo, d.tipo, d.emissor, d.versao, d.data_publicacao, d.vigencia_inicio, d.vigencia_fim,
        d.origem, d.hash_conteudo, d.obrigacoes, d.status, d.modelo_embedding, d.dimensao_embedding, d.ingerido_em, d.substituido_em
        """;

    private const string ColunasTrecho = "t.id, t.ordem, t.secao, t.linha_inicio, t.linha_fim, t.texto, t.hash_texto, t.regra_ids, t.regra_principal, t.obrigacoes";

    /// <summary>Somente documentos ativos, do modelo de embeddings atual, do tipo e vigência pedidos.</summary>
    private const string Filtro = """
        d.status = 'ATIVO'
        AND d.modelo_embedding = @modelo
        AND (cardinality(@tipos) = 0 OR d.tipo = ANY(@tipos))
        AND (cardinality(@obrigacoes) = 0 OR t.obrigacoes && @obrigacoes OR cardinality(t.obrigacoes) = 0)
        AND (@data_referencia::date IS NULL
             OR ((d.vigencia_inicio IS NULL OR d.vigencia_inicio <= @data_referencia::date)
                 AND (d.vigencia_fim IS NULL OR d.vigencia_fim >= @data_referencia::date)))
        """;

    public async Task<Documento?> ObterPorChaveVersaoAsync(string chave, string versao, CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand($"SELECT {ColunasDocumento} FROM rag_documentos d WHERE d.chave = @chave AND d.versao = @versao");
        comando.Parameters.AddWithValue("chave", chave);
        comando.Parameters.AddWithValue("versao", versao);
        return await LerDocumentoAsync(comando, cancelamento).ConfigureAwait(false);
    }

    public async Task<Documento?> ObterAtivoAsync(string chave, CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand($"SELECT {ColunasDocumento} FROM rag_documentos d WHERE d.chave = @chave AND d.status = 'ATIVO'");
        comando.Parameters.AddWithValue("chave", chave);
        return await LerDocumentoAsync(comando, cancelamento).ConfigureAwait(false);
    }

    public async Task SalvarAsync(Documento documento, IReadOnlyList<TrechoIndexado> trechos, Guid? substituirId, CancellationToken cancelamento)
    {
        await using var conexao = await fonte.OpenConnectionAsync(cancelamento).ConfigureAwait(false);
        await using var transacao = await conexao.BeginTransactionAsync(cancelamento).ConfigureAwait(false);

        if (substituirId is { } anterior)
        {
            await using var substituir = new NpgsqlCommand(
                "UPDATE rag_documentos SET status = 'SUBSTITUIDO', substituido_em = @em WHERE id = @id AND status = 'ATIVO'", conexao, transacao);
            substituir.Parameters.AddWithValue("id", anterior);
            substituir.Parameters.AddWithValue("em", documento.IngeridoEm);
            await substituir.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
        }

        await using (var inserir = new NpgsqlCommand(
            """
            INSERT INTO rag_documentos (id, chave, titulo, tipo, emissor, versao, data_publicacao, vigencia_inicio, vigencia_fim,
                origem, hash_conteudo, obrigacoes, status, modelo_embedding, dimensao_embedding, ingerido_em)
            VALUES (@id, @chave, @titulo, @tipo, @emissor, @versao, @data_publicacao, @vigencia_inicio, @vigencia_fim,
                @origem, @hash, @obrigacoes, 'ATIVO', @modelo, @dimensao, @ingerido_em)
            """, conexao, transacao))
        {
            inserir.Parameters.AddWithValue("id", documento.Id);
            inserir.Parameters.AddWithValue("chave", documento.Chave);
            inserir.Parameters.AddWithValue("titulo", documento.Titulo);
            inserir.Parameters.AddWithValue("tipo", CodigosTipoDocumento.De(documento.Tipo));
            inserir.Parameters.AddWithValue("emissor", documento.Emissor);
            inserir.Parameters.AddWithValue("versao", documento.Versao);
            AdicionarData(inserir, "data_publicacao", documento.DataPublicacao);
            AdicionarData(inserir, "vigencia_inicio", documento.VigenciaInicio);
            AdicionarData(inserir, "vigencia_fim", documento.VigenciaFim);
            inserir.Parameters.AddWithValue("origem", documento.Origem);
            inserir.Parameters.AddWithValue("hash", documento.HashConteudo);
            inserir.Parameters.AddWithValue("obrigacoes", documento.Obrigacoes.ToArray());
            inserir.Parameters.AddWithValue("modelo", documento.ModeloEmbedding);
            inserir.Parameters.AddWithValue("dimensao", documento.DimensaoEmbedding);
            inserir.Parameters.AddWithValue("ingerido_em", documento.IngeridoEm);
            await inserir.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
        }

        var lote = new NpgsqlBatch(conexao, transacao);
        await using (lote.ConfigureAwait(false))
        {
            foreach (var indexado in trechos)
            {
                var t = indexado.Trecho;
                var comando = new NpgsqlBatchCommand(
                    """
                    INSERT INTO rag_trechos (id, documento_id, ordem, secao, linha_inicio, linha_fim, texto, hash_texto, regra_ids, regra_principal, obrigacoes, embedding)
                    VALUES (@id, @documento_id, @ordem, @secao, @linha_inicio, @linha_fim, @texto, @hash, @regra_ids, @regra_principal, @obrigacoes, @embedding)
                    """);
                comando.Parameters.AddWithValue("id", indexado.Id);
                comando.Parameters.AddWithValue("documento_id", documento.Id);
                comando.Parameters.AddWithValue("ordem", t.Ordem);
                comando.Parameters.AddWithValue("secao", t.Secao);
                comando.Parameters.AddWithValue("linha_inicio", t.LinhaInicio);
                comando.Parameters.AddWithValue("linha_fim", t.LinhaFim);
                comando.Parameters.AddWithValue("texto", t.Texto);
                comando.Parameters.AddWithValue("hash", t.HashTexto);
                comando.Parameters.AddWithValue("regra_ids", t.RegraIds.ToArray());
                comando.Parameters.Add(new NpgsqlParameter("regra_principal", NpgsqlDbType.Text) { Value = (object?)t.RegraPrincipal ?? DBNull.Value });
                comando.Parameters.AddWithValue("obrigacoes", t.Obrigacoes.ToArray());
                comando.Parameters.AddWithValue("embedding", new Vector(indexado.Embedding));
                lote.BatchCommands.Add(comando);
            }

            await lote.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
        }

        await transacao.CommitAsync(cancelamento).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Documento>> ListarAsync(CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand($"SELECT {ColunasDocumento} FROM rag_documentos d ORDER BY d.chave, d.ingerido_em DESC");
        await using var leitor = await comando.ExecuteReaderAsync(cancelamento).ConfigureAwait(false);
        var documentos = new List<Documento>();
        while (await leitor.ReadAsync(cancelamento).ConfigureAwait(false))
        {
            documentos.Add(LerDocumento(leitor, 0));
        }

        return documentos;
    }

    public async Task<IReadOnlyList<TrechoEncontrado>> BuscarPorRegrasAsync(
        IReadOnlyList<string> regraIds, FiltroBusca filtro, int limite, CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand($"""
            SELECT {ColunasDocumento}, {ColunasTrecho}, NULL::double precision
            FROM rag_trechos t JOIN rag_documentos d ON d.id = t.documento_id
            WHERE t.regra_ids && @regra_ids AND {Filtro}
            ORDER BY (t.regra_principal = ANY(@regra_ids)) DESC NULLS LAST, d.chave, t.ordem
            LIMIT @limite
            """);
        comando.Parameters.AddWithValue("regra_ids", regraIds.ToArray());
        return await BuscarAsync(comando, filtro, limite, MotivoRecuperacao.Regra, cancelamento).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TrechoEncontrado>> BuscarSemanticaAsync(
        float[] vetor, FiltroBusca filtro, int limite, CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand($"""
            SELECT {ColunasDocumento}, {ColunasTrecho}, 1 - (t.embedding <=> @vetor)
            FROM rag_trechos t JOIN rag_documentos d ON d.id = t.documento_id
            WHERE {Filtro}
            ORDER BY t.embedding <=> @vetor, t.id
            LIMIT @limite
            """);
        comando.Parameters.AddWithValue("vetor", new Vector(vetor));
        return await BuscarAsync(comando, filtro, limite, MotivoRecuperacao.Semantica, cancelamento).ConfigureAwait(false);
    }

    public async Task RegistrarAsync(RegistroConsumoEmbeddings registro, CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand("""
            INSERT INTO rag_consumo_embeddings (id, correlacao_id, servico, provedor, modelo, operacao, inicio, fim, duracao_ms,
                status, quantidade_textos, tokens_entrada, codigo_erro)
            VALUES (@id, @correlacao_id, @servico, @provedor, @modelo, @operacao, @inicio, @fim, @duracao_ms,
                @status, @quantidade_textos, @tokens_entrada, @codigo_erro)
            """);
        comando.Parameters.AddWithValue("id", registro.Id);
        comando.Parameters.Add(new NpgsqlParameter("correlacao_id", NpgsqlDbType.Text) { Value = (object?)registro.CorrelacaoId ?? DBNull.Value });
        comando.Parameters.AddWithValue("servico", registro.Servico);
        comando.Parameters.AddWithValue("provedor", registro.Provedor);
        comando.Parameters.AddWithValue("modelo", registro.Modelo);
        comando.Parameters.AddWithValue("operacao", registro.Operacao);
        comando.Parameters.AddWithValue("inicio", registro.Inicio);
        comando.Parameters.AddWithValue("fim", registro.Fim);
        comando.Parameters.AddWithValue("duracao_ms", registro.DuracaoMs);
        comando.Parameters.AddWithValue("status", registro.Status);
        comando.Parameters.AddWithValue("quantidade_textos", registro.QuantidadeTextos);
        comando.Parameters.Add(new NpgsqlParameter("tokens_entrada", NpgsqlDbType.Bigint) { Value = (object?)registro.TokensEntrada ?? DBNull.Value });
        comando.Parameters.Add(new NpgsqlParameter("codigo_erro", NpgsqlDbType.Text) { Value = (object?)registro.CodigoErro ?? DBNull.Value });
        await comando.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<TrechoEncontrado>> BuscarAsync(
        NpgsqlCommand comando, FiltroBusca filtro, int limite, MotivoRecuperacao motivo, CancellationToken cancelamento)
    {
        comando.Parameters.AddWithValue("modelo", filtro.ModeloEmbedding);
        comando.Parameters.AddWithValue("tipos", filtro.TiposDocumento.Select(CodigosTipoDocumento.De).ToArray());
        comando.Parameters.AddWithValue("obrigacoes", filtro.Obrigacoes.ToArray());
        AdicionarData(comando, "data_referencia", filtro.DataReferencia);
        comando.Parameters.AddWithValue("limite", limite);

        await using var leitor = await comando.ExecuteReaderAsync(cancelamento).ConfigureAwait(false);
        var encontrados = new List<TrechoEncontrado>();
        while (await leitor.ReadAsync(cancelamento).ConfigureAwait(false))
        {
            var documento = LerDocumento(leitor, 0);
            const int t = 17;
            var trecho = new Trecho(
                leitor.GetInt32(t + 1),
                leitor.GetString(t + 2),
                leitor.GetInt32(t + 3),
                leitor.GetInt32(t + 4),
                leitor.GetString(t + 5),
                leitor.GetString(t + 6),
                leitor.GetFieldValue<string[]>(t + 7),
                leitor.IsDBNull(t + 8) ? null : leitor.GetString(t + 8),
                leitor.GetFieldValue<string[]>(t + 9));
            var similaridade = leitor.IsDBNull(t + 10) ? (double?)null : leitor.GetDouble(t + 10);
            encontrados.Add(new TrechoEncontrado(leitor.GetGuid(t), documento, trecho, motivo, similaridade));
        }

        return encontrados;
    }

    private static async Task<Documento?> LerDocumentoAsync(NpgsqlCommand comando, CancellationToken cancelamento)
    {
        await using var leitor = await comando.ExecuteReaderAsync(cancelamento).ConfigureAwait(false);
        return await leitor.ReadAsync(cancelamento).ConfigureAwait(false) ? LerDocumento(leitor, 0) : null;
    }

    private static Documento LerDocumento(NpgsqlDataReader l, int i)
    {
        _ = CodigosTipoDocumento.TentarDe(l.GetString(i + 3), out var tipo);
        return new Documento(
            l.GetGuid(i),
            l.GetString(i + 1),
            l.GetString(i + 2),
            tipo,
            l.GetString(i + 4),
            l.GetString(i + 5),
            l.IsDBNull(i + 6) ? null : l.GetFieldValue<DateOnly>(i + 6),
            l.IsDBNull(i + 7) ? null : l.GetFieldValue<DateOnly>(i + 7),
            l.IsDBNull(i + 8) ? null : l.GetFieldValue<DateOnly>(i + 8),
            l.GetString(i + 9),
            l.GetString(i + 10),
            l.GetFieldValue<string[]>(i + 11),
            l.GetString(i + 12) == "ATIVO" ? StatusDocumento.Ativo : StatusDocumento.Substituido,
            l.GetString(i + 13),
            l.GetInt32(i + 14),
            l.GetFieldValue<DateTimeOffset>(i + 15),
            l.IsDBNull(i + 16) ? null : l.GetFieldValue<DateTimeOffset>(i + 16));
    }

    private static void AdicionarData(NpgsqlCommand comando, string nome, DateOnly? valor) =>
        comando.Parameters.Add(new NpgsqlParameter(nome, NpgsqlDbType.Date) { Value = valor is { } v ? v : DBNull.Value });
}
