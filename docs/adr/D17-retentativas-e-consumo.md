# ADR D17 – Retentativas do LLM e persistência do consumo

- **Status:** Proposto
- **Data:** 2026-10-29

## Decisão proposta

### Retentativas

| Situação | Tratamento | Configuração |
|---|---|---|
| Timeout, HTTP 408, 429, 5xx ou falha sem status | Nova chamada com espera exponencial (500 ms, 1 s, …) | `Parecer:RetentativasTransitorias` = 2, `EsperaRetentativaMs` = 500, `TimeoutSegundos` = 60 |
| HTTP 4xx não transitório (ex.: 401, 400) ou falha inesperada do conector | Sem retentativa | — |
| Resposta fora do contrato ou reprovada na validação semântica | Nova tentativa com os erros do validador devolvidos ao modelo | `Parecer:RetentativasSaidaInvalida` = 2 |
| Cancelamento pelo chamador | Propagado, sem registro de falha técnica | — |

Esgotadas as tentativas, a análise fica `FALHA_TECNICA` e ganha uma limitação com os códigos de erro, o motivo de revisão humana "Há falha técnica." e `parecer = null`. **Nenhum conteúdo da resposta inválida é aproveitado** (P9, US7 cenário 3). Os achados determinísticos são preservados.

### Consumo (constitution 4.5, FR-047)

- Uma linha por chamada ao provedor, **inclusive retentativas e falhas**: `operacao` `PARECER`, `tentativa` sequencial, `status` `SUCESSO`/`FALHA`, `resultado` `RESPOSTA_VALIDA`/`RESPOSTA_INVALIDA`/`CHAMADA_FERRAMENTA`, `codigoErro` (`TIMEOUT`, `HTTP_503`…).
- O consumo de embeddings das consultas feitas pela ferramenta volta na resposta MCP e é registrado pela Api.Agente como `EMBEDDING_CONSULTA_RAG`. O consumo da ingestão fica na Api.RAG (pendência de D11).
- Tokens desconhecidos ficam **nulos, nunca zero**. O custo só é estimado quando `Custos:Versao` está preenchida, a tabela tem o modelo e os tokens são conhecidos. Do contrário, `custoEstimado` e `tabelaPrecos` ficam nulos. A tabela de preços é configuração externa e versionada, e **nenhum preço é definido no código**.
- Persistência em PostgreSQL (`agente_consumo_llm`, `INSERT … ON CONFLICT DO NOTHING`). Para não perder o resultado operacional, a gravação é **best effort**: uma falha do banco é registrada em log (só o tipo da exceção) e a resposta continua. O envelope sempre devolve `consumo[]`.

## Pendências para aprovação

1. Confirmar se a perda eventual de registro de consumo é aceitável ou se a falha de gravação deve virar `FALHA_TECNICA`, ou ir para uma fila de reprocessamento.
2. Tabela de preços oficial (versão, moeda, fonte) e responsável por mantê-la.
3. Valores de timeout e retentativas, revistos após T075.
