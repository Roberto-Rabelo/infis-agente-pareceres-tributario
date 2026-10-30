# ADR D11 – Modelo de embeddings, dimensão, métrica e estratégia de busca

- **Status:** Proposto (aguarda aprovação; constitution P7, Seção 9)
- **Data:** 2026-10-29
- **Tarefas:** T063, T064

## Contexto

A base de conhecimento de cruzamentos (docs/03, NUCLEO-1, NUCLEO-2 etc.) precisa ser recuperável pelo LLM, que analisa as inconsistências encontradas pelo motor determinístico. A P7 exige que modelo, dimensão e estratégia de busca sejam compatíveis entre si, estejam documentados e prevejam reindexação.

## Decisão proposta

| Item | Valor | Onde |
|---|---|---|
| Provedor / modelo | OpenAI `text-embedding-3-small` | `Embeddings:Provedor`, `Embeddings:Modelo` |
| Dimensão | 1536 (dimensão nativa do modelo) | `Embeddings:Dimensao` → coluna `vector(1536)` |
| Métrica | Distância de cosseno (`<=>`); similaridade = 1 − distância | `RepositorioPostgres` |
| Busca | **Híbrida em duas etapas:** (1) exata por identificador de regra (`regra_ids && @ids`, índice GIN), determinística; (2) vetorial pela pergunta, completando o limite sem repetir trechos | `ServicoConsulta` |
| Índice vetorial | Nenhum (varredura exata). A base tem poucas centenas de trechos; um índice ANN (HNSW/IVFFlat) só entra se o volume justificar, por novo ADR | `MigradorPostgres` |
| Isolamento de modelo | Toda consulta filtra `modelo_embedding` = modelo configurado; a migração falha se a dimensão do banco divergir da configuração | `MigradorPostgres`, `FiltroBusca` |

**Reindexação:** trocar o modelo exige reingestão de todos os documentos. Até a reingestão, documentos do modelo antigo ficam invisíveis à consulta (nunca misturados).

## Alternativas

- **Somente busca vetorial:** rejeitada. O achado já traz o identificador da regra, e a busca exata é determinística e auditável (P2, P5).
- **`text-embedding-3-large` (3072):** custo maior sem avaliação que o justifique (P16).
- **Modelo local:** exigiria infraestrutura adicional; pode ser reavaliado.

## Consequências

- Chave da OpenAI via variável de ambiente `OpenAI__ApiKey` (nunca no appsettings). Sem chave, ingestão e busca semântica falham como erro técnico; a busca por regra continua disponível.
- Consumo de embeddings registrado por chamada em `rag_consumo_embeddings` (tokens desconhecidos = NULL; custo NULL até existir tabela de preços versionada, D18).
- **Pendência:** a constitution 4.6 atribui consumo e custos à Api.Agente. O consumo da *consulta* volta na resposta MCP (`consumo`) para a Api.Agente registrar (T074). O consumo da *ingestão* não tem execução da Api.Agente associada e fica na Api.RAG. Essa divisão precisa ser aprovada.
