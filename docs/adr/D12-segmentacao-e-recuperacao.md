# ADR D12 – Segmentação, topK e limiar de similaridade

- **Status:** Proposto, com **parâmetros provisórios** (P7: não podem ser fixados sem avaliação; T065 pendente)
- **Data:** 2026-10-29

## Critérios de segmentação (determinísticos)

1. **Linha de tabela = 1 trecho**, com o cabeçalho da tabela repetido. Nas tabelas de regras (docs/03, docs/02, NUCLEO-1, NUCLEO-2) cada linha é uma regra (`CRZ-*`, `INT-*`, `ENT-*`, `M1-P01`...), que assim é recuperada inteira, com condição, hipóteses legítimas e severidade.
2. **Parágrafos, listas e citações** são agrupados por seção até `Segmentacao:MaximoCaracteres`, sem sobreposição. Um bloco maior que o limite é dividido por linha.
3. **Blocos de código** não são quebrados em linhas em branco.
4. **Mudança de título** encerra o trecho. O caminho de títulos vira `secao` (localização, P7), junto com `linha_inicio` e `linha_fim`.
5. **Metadados por trecho:** `regra_ids` (todos os IDs citados), `regra_principal` (ID da primeira célula da linha), `obrigacoes` (derivadas dos segmentos do ID, como `CRZ-NFE-EFD` → NFE, NFCE, EFD_ICMS_IPI, e dos nomes no título da seção). Trecho sem obrigação identificada herda as do documento.
6. O texto vetorizado é `título do documento + seção + trecho`; o texto armazenado é só o trecho.

## Deduplicação e versões

| Situação | Resultado |
|---|---|
| Mesma chave + versão + hash | Idempotente (`JA_EXISTENTE`), sem nova chamada ao provedor |
| Mesma chave + versão, hash diferente | 409: é preciso publicar nova versão |
| Nova versão da mesma chave | Grava a nova e marca a anterior `SUBSTITUIDO` na mesma transação. A anterior continua listável (constitution 4.1), mas não é recuperada |

## Parâmetros provisórios (sem avaliação)

| Parâmetro | Valor | Configuração |
|---|---|---|
| Tamanho máximo de trecho de texto | 1500 caracteres | `Segmentacao:MaximoCaracteres` |
| topK padrão / máximo | 8 / 30 | `Consulta:LimitePadrao`, `Consulta:LimiteMaximo` |
| Trechos por consulta da Api.Agente | 20 | `BaseConhecimento:LimitePorConsulta` |
| Limiar de similaridade | **nenhum** (nulo) | `Consulta:LimiarSimilaridade` |
| Sobreposição | nenhuma | — |

Antes de aprovar, é obrigatório o conjunto de avaliação versionado (T065: perguntas × trechos esperados, por exemplo "NF-e cancelada escriturada como regular" → `CRZ-NFE-EFD-002`, `M1-R02`), com os resultados registrados no plano.
