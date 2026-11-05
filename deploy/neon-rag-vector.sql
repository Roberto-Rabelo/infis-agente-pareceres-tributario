-- Banco ConnectionStrings__Rag. Preferir a migração da Api.RAG (MigradorPostgres).
-- Executar no SQL Editor do Neon só se for validar a extensão antes do primeiro boot.

CREATE EXTENSION IF NOT EXISTS vector;
SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';
