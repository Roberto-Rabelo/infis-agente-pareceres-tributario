# Tarefas: Cruzamento entre Obrigações Fiscais e Parecer Técnico

**Especificação:** [spec.md](spec.md)
**Constitution:** 2.0.0
**Status:** Rascunho

> **Ordem SDD (constitution 6.1):** as tarefas pressupõem o plano técnico. Como ele ainda não existe, a **Fase 0 é bloqueante**: nenhuma tarefa das fases 1 em diante começa antes de T001–T012 concluídas. Caminhos de arquivo são **provisórios** até o ADR D20 (organização das camadas).

## Andamento

**Incremento 1 (2026-10-29):** Fases 1, 2 e 3 implementadas com plano parcial (desvio registrado em `plan.md` §3). `[~]` = parcial. Pendente no incremento: T015 (pipeline de CI, depende de onde o repositório será hospedado). Testes: `dotnet test infis-agente-parecer-tributario.slnx`.

## Convenções

- `[P]` – pode rodar em paralelo (arquivos diferentes, sem dependência).
- `[USn]` – história de usuário da spec. `FR-xxx` / regra – rastreabilidade (constitution 6.2).
- **Testes primeiro:** em cada história, as tarefas de teste vêm antes da implementação e DEVEM falhar antes dela.
- **Regra de cruzamento = 3 testes mínimos** (P5/P17): par consistente, par inconsistente, par incompleto (obrigação de destino ausente ou não apta → "ausência de evidências"). Dados de teste anonimizados.
- **Toda regra** só é implementada após a entrada no catálogo com status `aprovada` e fonte conferida (FR-012).

Estrutura provisória:

```
src/Api.Agente/            (Endpoints, Application, Domain, Infrastructure)
src/Api.RAG/               (Endpoints, Application, Domain, Infrastructure)
catalog/                   (catálogo de regras versionado – formato D21)
contracts/                 (schemas de entrada, saída do LLM e envelope)
tests/Api.Agente.Tests/    tests/Api.RAG.Tests/    tests/fixtures/
docs/adr/
```

---

## Fase 0 – Esclarecimentos, plano e ADRs (bloqueante)

- [ ] T001 Resolver C1/C2 (escala e reclassificação de `ESTRUTURA_GRAVE` em cruzamentos) e registrar na spec §9 → FR-031
- [ ] T002 Resolver C3 (IDs duplicados doc 03 × NUCLEO-1) e publicar tabela de equivalência única → FR-015
- [ ] T003 Confirmar C4 (motor roda neste agente) e corrigir `docs/05-modelo-regras-agente.md` §4 conforme a decisão
- [ ] T004 Definir com os responsáveis pelos agentes de origem o contrato de entrada v1 (C6/D24) e o critério de aptidão (C7/D25)
- [ ] T005 [P] Decidir C5 (tolerância), C11 (materialidade) e C12 (IBS/CBS 2026) com responsável fiscal → FR-024/025/033
- [ ] T006 [P] Decidir C8/D26, C9/D6, C10, C13/D5 e C14/D8/D19 (LGPD, com compliance)
- [~] T007 (parcial: plan.md do incremento 1) Escrever `specs/001-cruzamento-obrigacoes/plan.md` com a seção "Verificação da Constitution" (P1–P18) e justificativa de cada projeto/dependência (P16)
- [~] T008 (parcial: D11 e D12 propostos) [P] ADRs: D7 (síncrono/assíncrono), D10 (modelo OpenAI e saída estruturada), D11/D12 (embeddings, busca, chunk, topK) em `docs/adr/`
- [~] T009 (parcial: D13 proposto) [P] ADRs: D13 (MCP), D14 (A2A/Agent Card), D15 (identidade/autorização), D9 (memória)
- [~] T010 (parcial: D20, D21, D22 propostos no plan.md) [P] ADRs/plano: D20 (camadas), D21 (formato do catálogo), D22 (testes/avaliação), D23 (observabilidade), D17 (retentativas), D18 (preços)
- [ ] T011 Curadoria das fontes (D3): listar documentos oficiais da spec §6 com emissor, versão, vigência, URL e responsável
- [ ] T012 Atualizar os caminhos provisórios deste arquivo conforme o ADR D20

**Checkpoint:** spec sem `[NEEDS CLARIFICATION]` bloqueante; plano aprovado.

---

## Fase 1 – Setup

- [x] T013 Criar a solução `infis-agente-parecer-tributário` (.NET 10, ASP.NET Core) com `Api.Agente`, `Api.RAG` e projetos de teste
- [x] T014 [P] Configurar análise estática, avisos como erro (conforme plano) e formatação
- [ ] T015 [P] Pipeline de CI executando testes **sem** acesso ao provedor de LLM (P17)
- [x] T016 [P] Configuração externa (modelo, parâmetros, tolerância, materialidade, tabela de preços) sem segredos versionados (5.1, 5.2)
- [x] T017 [P] Teste de arquitetura: domínio sem dependência de infraestrutura/IA; Api.Agente sem acesso ao banco da Api.RAG (P12)

---

## Fase 2 – Fundação (bloqueia todas as histórias)

- [x] T018 Tipos de domínio: `Obrigacao`, `Natureza`, `Severidade`, `Evidencia`, `TipoRelacao`, `StatusRegra`, `StatusAnalise` → FR-011, FR-030, FR-038
- [x] T019 [P] Testes do tipo CNPJ alfanumérico (texto normalizado, DV do novo padrão, nunca numérico) → doc 04 §2
- [x] T020 [P] Testes de período: mês, ano, trimestre, situação especial; agregação mensal → anual → FR-022/023
- [x] T021 Implementar CNPJ e período (T019, T020)
- [x] T022 Schema do catálogo de regras (campos de 4.2 + FR-011) em `contracts/`
- [x] T023 [P] Testes do carregador do catálogo: rejeita regra sem fonte, sem vigência, sem severidade, ou `aprovada` com item "(conferir)" → FR-012/013/014
- [x] T024 Implementar carregador e validação do catálogo (T023)
- [x] T025 Motor de regras: ordem `PAR → EXC → PRE→/←PRE → IDT → AGR/LIM → CNT`, dependências e marcação "não conclusiva" → FR-016/017
- [x] T026 [P] Testes do motor: ordem de execução, regra dependente não conclusiva, regra fora de vigência "não aplicável", determinismo (mesma entrada → mesmos achados) → FR-014/017/029
- [x] T027 Modelo de `Achado` com evidência nos dois lados, valores, diferença e hipóteses legítimas → FR-028/032
- [x] T028 [P] Utilitários de casamento: chave de acesso, chave secundária, participante via `0150`, tabela de sentido `tpNF`×`IND_OPER`×`IND_EMIT`, com testes → FR-019/020/021
- [x] T029 [P] Comparador de valores com tolerância e materialidade parametrizadas e informadas no achado, com testes → FR-024/025
- [x] T030 Envelope da API (metadados operacionais pelo sistema) e erros padronizados, com testes → FR-041, P10
- [x] T031 [P] Identificador de execução/correlação propagado e logs com mascaramento de CNPJ/CPF/valores/chaves → P13, FR-048

**Checkpoint:** motor executa um catálogo de teste vazio/fictício com todas as garantias de ordem, vigência e determinismo.

---

## Fase 3 – US1: Receber obrigações e conferir aptidão (P1) 🎯 MVP

**Objetivo:** relatório de aptidão e cobertura sem nenhum cruzamento.

### Testes
- [x] T032 [P] [US1] Fixtures do contrato de entrada v1: válida, JSON inválido, sem versão, versão não suportada, sem relatório, reprovada, aprovada com advertências, original + substituta, CNPJ divergente, períodos sem sobreposição
- [x] T033 [P] [US1] Testes PRE-001 (contrato), PRE-002 (versão/vigência), PRE-003 (relatório) → FR-002/004/005/006
- [x] T034 [P] [US1] Testes PRE-004 (escrituração vigente) e PRE-005 (grupo CNPJ/período) → FR-008/009
- [x] T035 [P] [US1] Testes de cobertura e de advertências repassadas como alerta com texto preservado → FR-007/010
- [x] T036 [P] [US1] Testes de limite de payload, profundidade e campos não previstos → FR-044
- [x] T037 [P] [US1] Teste adversarial: instruções em campos e no relatório não alteram resultado → P8, SC-008

### Implementação
- [x] T038 (proposta, aguarda T004) [US1] Schema do contrato de entrada v1 em `contracts/entrada/` (resultado de T004) → FR-003
- [x] T039 [US1] Validador de contrato e pré-condições PRE-001 a PRE-005 (catálogo + validadores)
- [x] T040 [US1] Agrupador de cruzamento e relatório de cobertura → FR-009/010
- [x] T041 [US1] Endpoint REST de análise (recebe lote, retorna envelope com aptidão e cobertura) → FR-001
- [x] T042 [US1] Autenticação/autorização do endpoint e teste de rejeição → FR-043

**Checkpoint:** US1 demonstrável isoladamente; status `INCONCLUSIVA` quando nenhum cruzamento é executável.

---

## Fase 4 – US2: Enquadramento e entidades (P1)

Para cada regra: (a) aprovar no catálogo com fonte conferida; (b) 3 testes; (c) validador.

- [ ] T043 [US2] Aprovar no catálogo CRZ-REG-001 (= ENT-006), CRZ-REG-002, CRZ-REG-004 (= CRZ-CTB-ECF-003), CRZ-REG-005 com fontes da Lei 10.637/02, Lei 10.833/03, IN RFB 1.252/2012, IN RFB 2.003/2021, IN SRF 104/1998 (conferir dispositivo vigente)
- [ ] T044 [P] [US2] Testes das regras PAR de T043 e do bloqueio de regras de valor dependentes → FR-017
- [ ] T045 [US2] Implementar validadores PAR de T043
- [ ] T046 [US2] Aprovar ENT-001 (= CRZ-EFD-CTB-002), ENT-010, ENT-030, ENT-031 com fontes (Guias EFD ICMS/IPI e EFD-Contribuições, Manual ECD)
- [ ] T047 [P] [US2] Testes ENT-001, ENT-010, ENT-030/031 – incluindo hipótese legítima de mudança de plano de contas (`IND_MUDANC_PC`/`I157`)
- [ ] T048 [US2] Implementar validadores de entidade de T046

---

## Fase 5 – US3: NF-e/NFC-e × EFD ICMS/IPI (P2)

- [ ] T049 [US3] Aprovar exclusividades `EXC` de NF-e (cancelada/denegada/inutilizada, transferências, remessas, NF-e só IBS/CBS – conferir exceção do Guia 3.2.x) → spec §5.3
- [ ] T050 [US3] Aprovar CRZ-NFE-EFD-001, 002, 003 (condicionada à NT 2023.002), 004, 006, 007 com fontes (Guia Prático v3.2.2 – C100/C190; MOC; LC 87/96)
- [ ] T051 [P] [US3] Testes CRZ-NFE-EFD-001/002/003 (presença e situação) – 3 casos cada
- [ ] T052 [P] [US3] Testes CRZ-NFE-EFD-004 (IDT por campo do de-para NUCLEO-2 M1.3) e 007 (LIM crédito ≤ destacado, com `C197`)
- [ ] T053 [P] [US3] Testes CRZ-NFE-EFD-006: mês seguinte recebido (alerta), não recebido (ausência de evidências), ausente nos dois (erro grave) → FR-027
- [ ] T054 [US3] Implementar validadores EXC de T049
- [ ] T055 [US3] Implementar validadores de T050

---

## Fase 6 – US4: Documentos e EFD ICMS/IPI × EFD-Contribuições (P2)

- [ ] T056 [US4] Aprovar exclusividades: presumido em regime de caixa fora do Bloco C/A; aquisições sem crédito no cumulativo; remessas sem repercussão em PIS/COFINS
- [ ] T057 [US4] Aprovar CRZ-NFE-CTB-001, 002 e CRZ-NFS-CTB-001, 003 com fontes (Guia EFD-Contribuições v1.35 – Blocos A, C, F, M; Anexo I NFS-e)
- [ ] T058 [P] [US4] Testes CRZ-NFE-CTB-001 nas três formas de escrituração (`IND_ESCRI` 1/2, `IND_REG_CUM = 1`)
- [ ] T059 [P] [US4] Testes CRZ-NFE-CTB-002 e CRZ-NFS-CTB-001 (incluindo NFS-e substituída não somada à substituta)
- [ ] T060 [P] [US4] Testes CRZ-NFS-CTB-003 (LIM dedução ≤ retido em NFS-e/`F600`)
- [ ] T061 [US4] Implementar validadores de T056 e T057

---

## Fase 7 – US7: Parecer técnico (P2)

Pode começar em paralelo à Fase 5 após a Fase 3.

> **Incremento 2 (2026-10-29):** base de regras de cruzamento (`docs/*.md`) vetorizada na Api.RAG e recuperável por MCP para o LLM analisar as inconsistências. ADRs D11, D12 e D13 em `docs/adr/` (propostos; parâmetros de D12 provisórios até T065). Testes: `tests/Api.RAG.Tests` (Domain, Application, Endpoints, Infrastructure) e `tests/Api.Agente.Tests/Application/FundamentacaoTests.cs`. Testes PostgreSQL/PgVector (Testcontainers) ficam ignorados sem Docker.
>
> **Incremento 3 (2026-10-29):** parecer por LLM (Semantic Kernel + OpenAI, saída estruturada estrita, ferramenta MCP controlada, validação semântica, retentativas, consumo). ADRs D10 e D17 propostos; `Parecer:Habilitado = false` por padrão. Avaliações T065/T075 versionadas em `eval/`, com execução real ignorada sem chave.

### Api.RAG
- [x] T062 [P] [US7] Testes de ingestão: metadados obrigatórios, duplicidade, reprocessamento, nova versão e versão substituída → P7
- [x] T063 [US7] Endpoint POST de ingestão restrito a perfil administrativo; segmentação, embeddings e PgVector conforme ADR D11/D12
- [x] T064 [US7] Ferramenta MCP de recuperação com filtro por obrigação e versão do manual, timeout e autorização → FR-034, P11
- [~] T065 [P] [US7] Conjunto de avaliação de recuperação versionado (perguntas × chunks esperados) e registro do resultado → P7, D12 (conjunto `eval/recuperacao/v1.json`, avaliador e teste de consistência na CI; executado em 2026-10-29: acerto@8 0,778, abaixo do critério proposto; ver plan.md §6)
- [~] T066 [US7] Ingerir os documentos oficiais da curadoria T011 (parcial: `scripts/ingerir-base-regras.ps1` ingere a base interna `docs/*.md` como `BASE_REGRAS_INTERNA`; manuais oficiais aguardam T011; não executado: sem PostgreSQL/chave OpenAI no ambiente)

### Api.Agente
- [x] T067 [P] [US7] Schema versionado do contrato de saída do LLM em `contracts/saida/` (sem campos operacionais) → FR-037, P10 (`parecer.v1.schema.json`; teste `ContratoSaida_SemCamposOperacionais`)
- [x] T068 [P] [US7] Testes de validação semântica: LLM não altera achado determinístico; indício da IA nunca erro grave; referência inexistente rejeitada; `CONCLUIDA` proibido com validação pendente; revisão humana obrigatória nos casos da P15 → FR-035/036/038/039/040/042 (`ValidadorParecerTests`; o status é sempre o do sistema, `STATUS_DIVERGENTE`)
- [x] T069 [P] [US7] Testes de falha: Api.RAG indisponível, timeout do LLM, retentativas esgotadas → `FALHA_TECNICA` ou `CONCLUIDA_PARCIAL`, nunca conformidade → P4 (`ServicoParecerTests`, `FundamentacaoTests`)
- [x] T070 [US7] Cliente MCP da Api.RAG com timeout e tratamento de falha como achado "falha técnica" (desvio a aprovar: a falha é registrada em `limitacoes`, rebaixa `CONCLUIDA` → `CONCLUIDA_PARCIAL` e exige revisão humana, mas não vira achado, porque os achados se referem às obrigações)
- [x] T071 [US7] Prompt versionado (instruções separadas dos dados; achados e hipóteses como dado) → P8, 5.2 (`prompts/parecer/v1.0.0.md`)
- [~] T072 [US7] Integração Semantic Kernel + OpenAI com saída estruturada (ADR D10) e retentativas (D17) (implementada e testada com provedor roteirizado; desabilitada por padrão; modelo não escolhido; sem chamada real neste ambiente)
- [x] T073 [US7] Validador estrutural e semântico da resposta e montagem do envelope final → FR-040/041 (envelope 1.1.0)
- [~] T074 [P] [US7] Registro de consumo e custo em PostgreSQL, inclusive retentativas e consumo desconhecido ≠ zero → FR-047, 4.5 (tabela `agente_consumo_llm`; testado com dublê; sem teste contra PostgreSQL real; tabela de preços pendente)
- [~] T075 [P] [US7] Conjunto de avaliação do parecer com o modelo real, executado fora da CI → P17 (`eval/parecer/v1.json` e `AvaliacaoParecerTests`; executado em 2026-10-29 com `gpt-4o-mini`: 5/5 aceitos, 0 achados alterados; critérios ainda não aprovados)

---

## Fase 8 – US5: ECD × ECF e pontes fiscal → contábil (P3)

- [ ] T076 [US5] Aprovar CRZ-ECD-ECF-001, 002, 004, 006 com fontes (Manual ECF Leiaute 12 – Bloco C/K/L/M; Manual ECD Leiaute 9)
- [ ] T077 [P] [US5] Testes CRZ-ECD-ECF-001 (hash da última ECD) e 002 (`K155/K355` × `I155/I355`)
- [ ] T078 [P] [US5] Testes CRZ-ECD-ECF-004 e 006 (lucro antes do IRPJ/CSLL; período e situação especial)
- [ ] T079 [US5] Implementar validadores de T076
- [ ] T080 [US5] Lote seguinte: aprovar e implementar CRZ-EFD-ECD-002 (inventário de fevereiro × estoque) com testes de ausência da EFD de fevereiro

---

## Fase 9 – US6: DIMP (P3)

- [ ] T081 [US6] Aprovar CRZ-DIMP-EFD-001 e 002 após C10 (origem da DIMP) e conferência do PDF da DIMP Versão 10
- [ ] T082 [P] [US6] Testes CRZ-DIMP-EFD-001/002, incluindo hipóteses legítimas (estornos, temporal, outro estabelecimento) e lote sem DIMP
- [ ] T083 [US6] Implementar validadores de T081

---

## Fase 10 – US8: A2A (P3)

- [ ] T084 [P] [US8] Testes: Agent Card com versões dos contratos; chamada sem autenticação rejeitada; REST × A2A com achados idênticos → SC-007
- [ ] T085 [US8] Servidor A2A reutilizando o mesmo pipeline e autorizações da API REST (ADR D14)
- [ ] T086 [US8] Publicar Agent Card versionado

---

## Fase 11 – Acabamento e transversais

- [ ] T087 [P] Memória conversacional isolada por usuário/conversa com retenção e exclusão, com testes (P14, ADR D9)
- [ ] T088 [P] Política LGPD: minimização de dados ao LLM, retenção e exclusão das obrigações recebidas, com testes → FR-045/046
- [ ] T089 [P] Matriz de rastreabilidade requisito → regra → tarefa → teste em `specs/001-cruzamento-obrigacoes/rastreabilidade.md` → 6.2
- [ ] T090 [P] Atualizar `docs/README.md` e docs 02/03/05 com IDs e severidades finais decididos na Fase 0
- [ ] T091 Revisão final com o checklist da constitution (Definition of Done, Seção 7)
- [x] T092 [P] API REST com controllers e documentação OpenAPI/Swagger nos dois serviços, desligada fora de Development e nunca servida em Production → 5.1, P16 (`plan.md` §2 "API REST"; `DocumentacaoApiTests` nos dois projetos de teste)

---

## Dependências

- Fase 0 → Fase 1 → Fase 2 → Fase 3 (US1).
- US2 depende de US1. US3, US4 e US5 dependem de US2 (regras PAR/entidades bloqueiam valores). US6 depende de US2 e de C10.
- US7 depende de US1 (pode evoluir em paralelo a US3–US6; cada nova regra é automaticamente coberta pelo parecer).
- US8 depende de US1 e US7.

## Estratégia de entrega

1. **MVP 1:** Fases 0–3 (aptidão e cobertura via REST).
2. **MVP 2:** + Fase 4 (US2) + Fase 7 (US7) → primeiro parecer com regras de regime e entidades.
3. Incrementos: US3 → US4 → US5 → US6 → US8, cada um com seu lote de regras aprovado no catálogo.
