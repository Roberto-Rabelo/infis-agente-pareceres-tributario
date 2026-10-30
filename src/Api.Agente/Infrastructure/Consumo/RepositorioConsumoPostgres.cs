using Api.Agente.Application.Parecer;
using Npgsql;
using NpgsqlTypes;

namespace Api.Agente.Infrastructure.Consumo;

/// <summary>Consumo e custos da Api.Agente (constitution 4.5/4.6). Uma linha por chamada; desconhecido = NULL.</summary>
public sealed class RepositorioConsumoPostgres(NpgsqlDataSource fonte) : IRegistroConsumoLlm
{
    public const string Migracao = """
        CREATE TABLE IF NOT EXISTS agente_consumo_llm (
            id              uuid PRIMARY KEY,
            execucao_id     text NOT NULL,
            correlacao_id   text NOT NULL,
            servico         text NOT NULL,
            provedor        text NOT NULL,
            modelo          text NOT NULL,
            operacao        text NOT NULL,
            tentativa       integer NOT NULL,
            inicio          timestamptz NOT NULL,
            fim             timestamptz NOT NULL,
            duracao_ms      bigint NOT NULL,
            status          text NOT NULL,
            resultado       text NULL,
            tokens_entrada  bigint NULL,
            tokens_saida    bigint NULL,
            codigo_erro     text NULL,
            custo_estimado  numeric(18, 8) NULL,
            tabela_precos   text NULL
        );
        CREATE INDEX IF NOT EXISTS ix_agente_consumo_llm_execucao ON agente_consumo_llm (execucao_id);
        CREATE INDEX IF NOT EXISTS ix_agente_consumo_llm_inicio ON agente_consumo_llm (inicio);
        """;

    public static async Task AplicarMigracaoAsync(NpgsqlDataSource fonte, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        await using var comando = fonte.CreateCommand(Migracao);
        await comando.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
    }

    public async Task RegistrarAsync(RegistroConsumoLlm registro, CancellationToken cancelamento)
    {
        await using var comando = fonte.CreateCommand("""
            INSERT INTO agente_consumo_llm (id, execucao_id, correlacao_id, servico, provedor, modelo, operacao, tentativa, inicio, fim,
                duracao_ms, status, resultado, tokens_entrada, tokens_saida, codigo_erro, custo_estimado, tabela_precos)
            VALUES (@id, @execucao_id, @correlacao_id, @servico, @provedor, @modelo, @operacao, @tentativa, @inicio, @fim,
                @duracao_ms, @status, @resultado, @tokens_entrada, @tokens_saida, @codigo_erro, @custo_estimado, @tabela_precos)
            ON CONFLICT (id) DO NOTHING
            """);
        comando.Parameters.AddWithValue("id", registro.Id);
        comando.Parameters.AddWithValue("execucao_id", registro.ExecucaoId);
        comando.Parameters.AddWithValue("correlacao_id", registro.CorrelacaoId);
        comando.Parameters.AddWithValue("servico", registro.Servico);
        comando.Parameters.AddWithValue("provedor", registro.Provedor);
        comando.Parameters.AddWithValue("modelo", registro.Modelo);
        comando.Parameters.AddWithValue("operacao", registro.Operacao);
        comando.Parameters.AddWithValue("tentativa", registro.Tentativa);
        comando.Parameters.AddWithValue("inicio", registro.Inicio);
        comando.Parameters.AddWithValue("fim", registro.Fim);
        comando.Parameters.AddWithValue("duracao_ms", registro.DuracaoMs);
        comando.Parameters.AddWithValue("status", registro.Status);
        Anulavel(comando, "resultado", NpgsqlDbType.Text, registro.Resultado);
        Anulavel(comando, "tokens_entrada", NpgsqlDbType.Bigint, registro.TokensEntrada);
        Anulavel(comando, "tokens_saida", NpgsqlDbType.Bigint, registro.TokensSaida);
        Anulavel(comando, "codigo_erro", NpgsqlDbType.Text, registro.CodigoErro);
        Anulavel(comando, "custo_estimado", NpgsqlDbType.Numeric, registro.CustoEstimado);
        Anulavel(comando, "tabela_precos", NpgsqlDbType.Text, registro.TabelaPrecos);
        await comando.ExecuteNonQueryAsync(cancelamento).ConfigureAwait(false);
    }

    private static void Anulavel(NpgsqlCommand comando, string nome, NpgsqlDbType tipo, object? valor) =>
        comando.Parameters.Add(new NpgsqlParameter(nome, tipo) { Value = valor ?? DBNull.Value });
}

/// <summary>Sem banco configurado e com o parecer desabilitado, não há chamadas a registrar.</summary>
public sealed class RegistroConsumoNaoConfigurado : IRegistroConsumoLlm
{
    public Task RegistrarAsync(RegistroConsumoLlm registro, CancellationToken cancelamento) =>
        throw new InvalidOperationException("Registro de consumo não configurado.");
}
