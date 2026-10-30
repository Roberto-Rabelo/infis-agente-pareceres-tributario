using System.Globalization;
using Npgsql;

namespace Api.RAG.Infrastructure.Postgres;

/// <summary>
/// Migração idempotente do schema do RAG. A dimensão da coluna vetorial vem da configuração (ADR D11);
/// se o banco já tiver outra dimensão, a inicialização falha: trocar de modelo exige reindexação (P7).
/// Busca exata (sem índice ANN) enquanto o volume da base não justificar outra estratégia.
/// </summary>
public static class MigradorPostgres
{
    public static async Task AplicarAsync(string connectionString, int dimensao, CancellationToken cancelamento)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimensao, 1);
        await using var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync(cancelamento).ConfigureAwait(false);

        await using (var comando = new NpgsqlCommand(Script(dimensao), conexao))
        {
            await comando.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
        }

        await using var verificacao = new NpgsqlCommand(
            "SELECT atttypmod FROM pg_attribute WHERE attrelid = 'rag_trechos'::regclass AND attname = 'embedding'", conexao);
        var existente = Convert.ToInt32(await verificacao.ExecuteScalarAsync(cancelamento).ConfigureAwait(false), CultureInfo.InvariantCulture);
        if (existente != dimensao)
        {
            throw new InvalidOperationException(
                $"A coluna vetorial tem dimensão {existente} e a configuração exige {dimensao}. Reindexe a base antes de trocar o modelo de embeddings.");
        }
    }

    private static string Script(int dimensao) => $"""
        CREATE EXTENSION IF NOT EXISTS vector;

        CREATE TABLE IF NOT EXISTS rag_documentos (
            id                 uuid PRIMARY KEY,
            chave              text NOT NULL,
            titulo             text NOT NULL,
            tipo               text NOT NULL,
            emissor            text NOT NULL,
            versao             text NOT NULL,
            data_publicacao    date NULL,
            vigencia_inicio    date NULL,
            vigencia_fim       date NULL,
            origem             text NOT NULL,
            hash_conteudo      char(64) NOT NULL,
            obrigacoes         text[] NOT NULL,
            status             text NOT NULL,
            modelo_embedding   text NOT NULL,
            dimensao_embedding integer NOT NULL,
            ingerido_em        timestamptz NOT NULL,
            substituido_em     timestamptz NULL,
            CONSTRAINT uq_rag_documentos_chave_versao UNIQUE (chave, versao)
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ux_rag_documentos_chave_ativo ON rag_documentos (chave) WHERE status = 'ATIVO';

        CREATE TABLE IF NOT EXISTS rag_trechos (
            id             uuid PRIMARY KEY,
            documento_id   uuid NOT NULL REFERENCES rag_documentos (id),
            ordem          integer NOT NULL,
            secao          text NOT NULL,
            linha_inicio   integer NOT NULL,
            linha_fim      integer NOT NULL,
            texto          text NOT NULL,
            hash_texto     char(64) NOT NULL,
            regra_ids      text[] NOT NULL,
            regra_principal text NULL,
            obrigacoes     text[] NOT NULL,
            embedding      vector({dimensao.ToString(CultureInfo.InvariantCulture)}) NOT NULL,
            CONSTRAINT uq_rag_trechos_documento_ordem UNIQUE (documento_id, ordem)
        );

        CREATE INDEX IF NOT EXISTS ix_rag_trechos_regra_ids ON rag_trechos USING gin (regra_ids);
        CREATE INDEX IF NOT EXISTS ix_rag_trechos_documento ON rag_trechos (documento_id);

        CREATE TABLE IF NOT EXISTS rag_consumo_embeddings (
            id                uuid PRIMARY KEY,
            correlacao_id     text NULL,
            servico           text NOT NULL,
            provedor          text NOT NULL,
            modelo            text NOT NULL,
            operacao          text NOT NULL,
            inicio            timestamptz NOT NULL,
            fim               timestamptz NOT NULL,
            duracao_ms        bigint NOT NULL,
            status            text NOT NULL,
            quantidade_textos integer NOT NULL,
            tokens_entrada    bigint NULL,
            codigo_erro       text NULL,
            custo_estimado    numeric(18, 8) NULL,
            tabela_precos     text NULL
        );
        """;
}
