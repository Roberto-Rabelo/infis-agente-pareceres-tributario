# Especificação 004: Motor com o catálogo completo (158 regras)

**Base:** [spec 001](../001-cruzamento-obrigacoes/spec.md) (US2 a US6) · **Plano:** [plan.md](plan.md) · **Tarefas:** [tasks.md](tasks.md)
**Constitution:** 2.0.0 · **Status:** Fase 1 implementada (10/11/2026); fases 2 a 8 propostas

## 1. Problema

O projeto adotou um catálogo de **158 regras de coesão** ([docs/regras](../../docs/regras/README.md), versão 1.0.0). O motor em produção executa **15**: as pré-condições PRE-001 a PRE-005 e 10 regras M1 (NF-e × EFD ICMS/IPI). As outras **148 não rodam**, por três motivos:

1. **Catálogos separados.** O motor carrega `catalog/regras.json` (formato D21), que tem só as 15. O catálogo de 158 está em `docs/regras/*.json`, em outro formato, e nenhum código o lê.
2. **Um validador escrito à mão por regra.** Cada regra aprovada precisa de um `ValidadorRegra` em C# ([ValidadoresNfeEfd.cs](../../src/Api.Agente/Domain/Motor/ValidadoresNfeEfd.cs)). Nesse ritmo, as 148 viram milhares de linhas sem reaproveitamento.
3. **Decisões em aberto.** Os 10 parâmetros de [parametros.json](../../docs/regras/parametros.json) estão sem valor, e o README do catálogo manda **recusar** a execução nesse caso. Das 32 fontes de [fontes.json](../../docs/regras/fontes.json), 19 não foram verificadas na fonte primária. Pela constitution, sem fonte oficial a regra não pode ser aprovada.

## 2. Objetivo

Executar as 158 regras sem reescrever o motor e sem relaxar a governança. O motor passa a ler o catálogo completo. As regras são executadas por **executores genéricos**, um por tipo de relação e operador; validador à mão fica só para o que não cabe neles. Cada regra só roda depois de **aprovada**, com fonte, parâmetros definidos e testes.

## 3. Inventário

| Bloco (ordem de `cadeias.json`) | Regras | Já executam | Obrigações | Operadores |
|---|---:|---:|---|---|
| 1. PAR – compatibilidade de regime | 10 | 0 | ECF, EFD-Contrib., ECD, NF-e | COMPATIVEL, EXISTE, NAO_EXISTE, IGUAL |
| 2. EXC – exclusividades legítimas | 13 | 0 | NF-e, NFS-e, EFD-Contrib. | FILTRA |
| 3. ENT – entidades compartilhadas | 16 | 0 | EFD ICMS/IPI, EFD-Contrib., ECD, NF-e, ECF, DIMP | EXISTE, IGUAL |
| 4. M1 – NF-e × EFD ICMS/IPI | 29 | **10** | NF-e, EFD ICMS/IPI | EXISTE, IGUAL, MENOR_OU_IGUAL, SOMA_IGUAL |
| 4. M2 – NF-e × EFD-Contribuições | 12 | 0 | NF-e, EFD-Contrib. | idem |
| 4. M4 – NFS-e × EFD-Contribuições | 7 | 0 | NFS-e, EFD-Contrib. | idem |
| 4. M5 – NFS-e × EFD ICMS/IPI | 3 | 0 | NFS-e, EFD ICMS/IPI | EXISTE, SOMA_IGUAL |
| 5. M3, M7, M8, M10, M11 – escriturações entre si | 33 | 0 | EFD ICMS/IPI, EFD-Contrib., ECD, ECF | SOMA_IGUAL, IGUAL, EXISTE |
| 6. CNT – continuidade mensal | 6 | 0 | EFD ICMS/IPI, EFD-Contrib. | SALDO_FINAL_IGUAL_INICIAL |
| 7. M9, M12 – contabilidade × ECF | 21 | 0 | ECD, ECF, NF-e, NFS-e | IGUAL, SOMA_IGUAL |
| 8. M6 – DIMP | 8 | 0 | DIMP, NF-e, EFD ICMS/IPI, ECD | SOMA_IGUAL, IGUAL |
| **Total** | **158** | **10** | | |

**Por tipo de relação × operador:**

| Relação e operador | Regras |
|---|---:|
| IDT IGUAL | 44 |
| PRE_DIRETA / PRE_REVERSA EXISTE | 40 |
| AGR SOMA_IGUAL | 23 |
| EXC FILTRA | 13 |
| PAR (vários operadores) | 14 |
| LIM MENOR/MAIOR_OU_IGUAL | 11 |
| CNT | 9 |
| IDT COMPATIVEL | 3 |
| AGR MENOR_OU_IGUAL | 1 |

Pela relação e pelo operador, cerca de 118 regras caberiam em quatro executores genéricos. A **medição real da Fase 1** foi menor, porque boa parte das regras descreve elemento, filtro ou chave em texto livre (ver §8).

## 4. Requisitos

- **FR-401** O motor DEVE carregar o catálogo completo a partir de uma única fonte versionada, com as 158 regras e o status de cada uma (`PROPOSTA`, `APROVADA`, `SUSPENSA`).
- **FR-402** Regra `PROPOSTA` NÃO DEVE executar. Ela aparece na cobertura do parecer como "não executada: regra não aprovada".
- **FR-403** Regra `APROVADA` DEVE ter: fonte conferida ou decisão do projeto registrada; todos os `PARAM:*` usados com valor; ao menos um teste positivo e um negativo (constitution §5); e `aprovadoPor`.
- **FR-404** O motor DEVE recusar iniciar quando uma regra aprovada usa parâmetro sem valor ou não tem executor (hoje isso só aparece como falha técnica na hora da análise).
- **FR-405** Executores genéricos DEVEM interpretar `origem`, `destino`, `chaveJuncao`, `operador`, `tolerancia`, `filtrosExclusao`, `aplicavelSe` e `dependeDe` da regra, sem código específico por regra.
- **FR-406** As 10 regras M1 que já rodam DEVEM produzir os mesmos achados depois da migração para executores genéricos (teste de regressão nos 10 cenários de `exemplos/cenarios`).
- **FR-407** A ordem de execução DEVE seguir `cadeias.json > ordemExecucao`: EXC antes das presenças; dependência que falhou deixa a regra `NAO_CONCLUSIVA`; falta de insumo gera `AUSENCIA_EVIDENCIAS`, nunca erro.
- **FR-408** O resultado DEVE consolidar os pontos de controle das cadeias C1 a C10. Uma quebra num ponto deixa os seguintes não conclusivos (`politicaQuebra`).
- **FR-409** Regras externas (`INT-*`, executadas pelos agentes de origem) NÃO DEVEM ser reexecutadas. O motor lê o resultado delas em `validacaoOrigem.ocorrencias`.
- **FR-410** O parecer e o `ResultadoAnaliseSofia` DEVEM informar a cobertura por bloco: regras aprovadas, executadas, sem evidência e não aplicáveis.
- **FR-411** IDs: o mapeamento entre os IDs antigos da spec 001 (`CRZ-*`) e os novos (`M*`, `PAR`, `ENT`...) DEVE ser publicado num só lugar (T002 da spec 001).

## 5. Fora do escopo

- Criar regras novas ou mudar o texto fiscal das 158. Mudança de conteúdo passa pelo responsável fiscal.
- Validação intraobrigação: é responsabilidade dos agentes de origem.
- Extrair XML/SPED: o agente continua recebendo o `lote.v1` já estruturado.

## 6. Decisões pendentes (bloqueiam aprovação, não a construção)

| ID | Decisão | Quem decide | Bloqueia |
|---|---|---|---|
| DP-1 | Valores dos 10 parâmetros (`TOL_VALOR_DOCUMENTO`, `TOL_AGREGADO_PERCENTUAL`, `TOL_AGREGADO_ABSOLUTO`, `JANELA_MESES_ENTRADA`, `MATERIALIDADE_DIMP`, `MATERIALIDADE_RECEITA`, `TABELA_CFOP_RECEITA`, `TABELA_CONTAS_POR_FUNCAO`, `DATA_VIGENCIA_NT_2023_002`, `DATA_OBRIGATORIEDADE_UB12_10`) e se as tolerâncias percentual e absoluta se combinam pela maior ou pela menor | Contador responsável | Aprovação de M1 a M12 com tolerância ou materialidade |
| DP-2 | Curadoria das 19 fontes não verificadas na fonte primária (T011 da spec 001) | Responsável pela curadoria | Aprovação das regras que dependem dessas fontes |
| DP-3 | Aprovação regra a regra (`aprovadoPor`), sobretudo as 110 de evidência `DERIVADA` (inferidas pelo projeto, não pela norma) | Contador responsável | Execução de cada regra |
| DP-4 | CNT e o lote mensal: a continuidade compara o mês t com o t+1, e o lote tem um só período | Projeto + agentes de origem | Bloco 6 |
| DP-5 | M9/M12 (ECD e ECF anuais) cruzados com lotes mensais: período do lote e regra de sobreposição | Projeto | Bloco 7 |
| DP-6 | Equivalência de IDs `CRZ-*` × novos (T002 da spec 001) | Projeto | FR-411 |

## 7. Critérios de sucesso

1. O catálogo carregado tem 158 regras, com status e com governança verificada por teste (FR-401, FR-403, FR-404).
2. Os 10 cenários atuais dão os mesmos achados depois da migração para executores genéricos (FR-406).
3. Cada bloco tem cenários sintéticos positivos e negativos por regra (um "consistente" e um com a divergência).
4. Com todas as regras aprovadas, um lote completo e consistente (8 tipos de obrigação) dá zero inconsistências e cobertura 100% das regras aplicáveis.
5. O parecer mostra a cobertura por bloco (FR-410).

## 8. Situação após a Fase 1 (medida em 10/11/2026)

| Item | Resultado |
|---|---|
| Regras carregadas pelo motor | 158 de coesão + 5 pré-condições, todas de `docs/regras` |
| Aprovadas (executam) | 10 M1, iguais às de antes (regressão achado a achado nos 10 cenários: sem diferença) |
| Executor | 7 dedicados (M1-P01, P02, R01–R04, I01) e 3 genéricos (M1-I03, I05, I06) |
| Propostas que o executor genérico já roda, se aprovadas | 4 (M1-I02, I07, I09, I10) |
| Propostas sem executor | 144 |

**Por que as 144 ainda não têm executor:** agregação SOMA_IGUAL sem executor (23, Fase 4); chave composta (CNPJ+ANO, COD_CTA+ANO, CNPJ+MÊS etc., ~40); elemento em texto livre (21); EXC com condição em texto (13); PAR com validador dedicado (14, Fase 2); continuidade (8, Fase 5); condição de aplicabilidade ou presença em texto (8). Cada regra registra o motivo, que o motor expõe em `MotorRegras.Executores`.

**Pendências de conteúdo encontradas no catálogo** (bloqueiam só a aprovação):
- 17 regras com severidade `INFO`, sem correspondência em ERRO_GRAVE/ALERTA (decisão T001 da spec 001).
- ENT-032 (PAR) depende de ENT-030 (PRE_DIRETA) e M9-16 (IDT) depende de M9-06 (AGR): ordem contrária ao NUCLEO-1 §2.
- M2-I05 cita a obrigação `EXTERNO`, que não vem no lote.
- M1-P01 depende de PAR-001 e ENT-003 no catálogo; a aprovação de 30/10/2026 rodava sem elas. A dispensa ficou registrada em `aprovacoes.json` com motivo, para rever em T437.
- `PARAM:TOL_VALOR_DOCUMENTO` recebeu 0,00, que é a mesma tolerância provisória que o motor já usava (C5). Os outros 9 parâmetros continuam sem valor.

## 9. Decisões

- **10/11/2026 – Lote parcial (Roberto).** O lote pode trazer todas as obrigações ou só parte delas, e cada regra roda com o que estiver apto. Regra que não roda é sinalizada (`NaoAplicavel`, `AusenciaEvidencias`, `NaoConclusiva` ou `FalhaTecnica`) e nunca interrompe a análise. Quando falta um lado do cruzamento, seja porque a obrigação não foi enviada, seja porque não estava apta, a regra conta como pendente e a análise termina como `CONCLUIDA_PARCIAL`, com revisão humana (P4). Coberto por `CatalogoCompletoTests.LoteMisto_RodaOQueEstaAptoESinalizaORestanteSemQuebrar` e pelos testes `LoteParcial_*`.
