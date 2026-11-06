-- Banco ConnectionStrings__Agente (consumo do parecer). A Api.Agente aplica o mesmo DDL se Postgres__AplicarMigracoes=true.
-- Não contém pgvector: embeddings ficam no banco da Api.RAG.

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
