# Catálogo de Regras de Coesão (JSON)

Versão do catálogo: **1.0.0** · Gerado a partir de [NUCLEO-1](../NUCLEO-1-principios-entidades-matriz.md), [NUCLEO-2](../NUCLEO-2-mapas-bidirecionais.md) e [NUCLEO-3](../NUCLEO-3-cadeias-de-valor.md).

O catálogo é **dado versionado**. O motor de regras (.NET) interpreta os arquivos; o agente LLM recebe as ocorrências e redige o parecer. Nenhuma regra deve ser criada pelo LLM.

## Arquivos

| Arquivo | Conteúdo | Regras |
|---|---|---|
| [schema/regra.schema.json](schema/regra.schema.json) | JSON Schema (draft 2020-12) dos arquivos de regras | — |
| [fontes.json](fontes.json) | Catálogo de fontes oficiais referenciadas no campo `fontes` (com indicação de verificação em fonte primária) | — |
| [parametros.json](parametros.json) | Parâmetros `PARAM:*` (tolerâncias, janela, materialidade, tabela CFOP, contas por função) – **todos sem valor**: decisão do projeto | — |
| [01-parametros-regime-entidades-exclusoes.json](01-parametros-regime-entidades-exclusoes.json) | `PAR-*` (regime), `EXC-*` (filtros de exclusividade legítima), `ENT-*` (entidades) | 39 |
| [02-documentos-x-escrituracoes.json](02-documentos-x-escrituracoes.json) | M1 (NF-e × EFD ICMS/IPI), M2 (NF-e × EFD-Contribuições), M4 (NFS-e × EFD-Contribuições), M5 (NFS-e × EFD ICMS/IPI) | 51 |
| [03-escrituracoes-entre-si.json](03-escrituracoes-entre-si.json) | M3, M7, M8, M10, M11 | 33 |
| [04-contabilidade-x-ecf.json](04-contabilidade-x-ecf.json) | M9 (ECD × ECF), M12 (documentos × ECD/ECF) | 21 |
| [05-dimp.json](05-dimp.json) | M6 (DIMP × documentos, 1601, F525, ECD) | 8 |
| [06-continuidade-mensal.json](06-continuidade-mensal.json) | `CNT-*` – saldo final do mês t = saldo inicial do mês t+1 | 6 |
| [cadeias.json](cadeias.json) | Ordem de execução, cadeias C1–C10 com pontos de controle, política de quebra e regras externas (executadas pelos agentes de origem) | — |

**Total: 158 regras.** Distribuição: IDT 47 · PRE_DIRETA 30 · AGR 24 · PAR 14 · EXC 13 · LIM 11 · PRE_REVERSA 10 · CNT 9. Severidade: ESCRITURACAO_GRAVE 82 · ALERTA 60 · INFO 15 · ESTRUTURA_GRAVE 1. Evidência: DERIVADA 110 · MANUAL 38 · NORMA 8 · PVA 2.

## Correspondência com o núcleo (.md)

- Os IDs `M*-P*` (presença direta), `M*-R*` (presença reversa) e `M*-nn` são os mesmos do NÚCLEO-2.
- `M1-I*`, `M2-I*` e `M4-I01` detalham as tabelas de **de-para** do NÚCLEO-2 (M1.3, M1.4, M2.3, M4.3), uma regra por grupo de campos.
- `PAR-*` implementa NÚCLEO-1 §5 (inclui M10-01). `EXC-*` implementa as colunas de exclusividade legítima. `CNT-*` implementa os pontos de continuidade do NÚCLEO-3.
- `ENT-*` são os mesmos IDs do NÚCLEO-1 §3.

## Como o motor deve consumir

1. Carregar `fontes.json`, `parametros.json` e todos os `0*.json`; validar contra o schema; **recusar** a execução se algum parâmetro usado estiver `null`.
2. Selecionar regras com `vigenciaInicio <= período <= vigenciaFim` e cujo `aplicavelSe` seja verdadeiro.
3. Executar na ordem de `cadeias.json > ordemExecucao`:
   - **EXC** primeiro: monta os conjuntos de exclusão referenciados em `filtrosExclusao`.
   - Uma regra cujo item de `dependeDe` falhou vira **NAO_CONCLUSIVO**.
   - Regra que não pôde rodar por falta de insumo vira **SEM_EVIDENCIA**, nunca erro.
4. Aplicar a `tolerancia`. Se a condição de `severidadeAtenuada.quando` for atendida, usar a severidade atenuada.
5. Consolidar por cadeia: quebra em um CP marca os CPs seguintes como não conclusivos (`politicaQuebra`).
6. Entregar ao LLM, para cada ocorrência: `mensagemParecer` preenchida, valores dos dois lados, `hipotesesLegitimas` e `fontes` resolvidas.

## Pendências antes de produção

| Pendência | Onde | Responsável |
|---|---|---|
| Definir valores de todos os parâmetros (tolerâncias, janela de entrada, materialidade) | `parametros.json` | Contador + arquitetura |
| Construir a tabela CFOP → natureza/gera receita | `PARAM:TABELA_CFOP_RECEITA` | Contador |
| Mapeamento de contas por função por contribuinte (estoque, ICMS a recolher etc.) | `PARAM:TABELA_CONTAS_POR_FUNCAO` | Agente de origem da ECD (via I051) + revisão |
| **34 regras com `conferirCampo: true`**: confirmar nome/código de campo no PDF oficial da versão (ex.: `Y672`, `F525.IND_REC`, `M312`, campos da DIMP v10, nomes da NFS-e) | regras marcadas | Time técnico |
| Fontes com `verificadoEmFontePrimaria: false` (leis, IN, convênios): conferir texto vigente | `fontes.json` | Jurídico/contábil |

## Validação estrutural

O catálogo foi validado com um script de checagem (schema, IDs únicos, fontes e parâmetros existentes, `dependeDe`/`filtrosExclusao`/cadeias com referências válidas, coerência entre cadeia declarada e CP). Resultado da versão 1.0.0: **0 erros, 0 avisos**. Recomenda-se portar essa validação para um teste automatizado do projeto .NET (ex.: um teste que carrega o catálogo e verifica a integridade referencial).
