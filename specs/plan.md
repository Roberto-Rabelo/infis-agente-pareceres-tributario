# Plano de Implementação — Agente Pareceres Tributário

> Subordinado a [`constitution.md`](constitution.md), [`requirements.md`](requirements.md) e [`architecture.md`](architecture.md). As tarefas detalhadas estão em [`tasks.md`](tasks.md).

| Campo | Valor |
|---|---|
| Responsável | Roberto |
| Versão | 0.1.0 (rascunho) |
| Etapa atual | **Fase 0 — Especificação** (sem código) |

---

## 1. Estratégia

Entrega incremental em fases. Cada fase termina com um **portão de qualidade** (gate) que precisa ser aprovado pelo responsável antes da fase seguinte. Segurança, isolamento e auditoria são construídos **desde a Fase 1**, não adicionados ao final.

## 2. Fases

### Fase 0 — Especificação (atual)
**Objetivo:** estabelecer a base SDD.
- Constituição, requisitos, arquitetura, plano, tarefas e `AGENTS.md`.
- Levantamento das questões em aberto (fontes, contrato SOFIA, IdP, LLM, retenção).

**Gate 0:** documentos revisados e aprovados por Roberto; questões em aberto com responsável e prazo.

### Fase 1 — Fundação segura
**Objetivo:** esqueleto executável com segurança, isolamento e auditoria, sem IA.
- Estrutura de repositório, ambiente, CI, lint, testes.
- Contratos (JSON Schema) de requisição/resposta v1 e manifesto.
- API `/v1/tasks`, `/v1/manifest`, `/health`, `/ready`.
- Autenticação, RBAC e escopo de tenant.
- Gravador de auditoria append-only com encadeamento por hash.
- Logs estruturados com `correlation_id` e `tenant_id`.

**Gate 1:** testes de isolamento entre tenants passando; requisição sem `tenant_id` rejeitada; toda requisição auditada.

### Fase 2 — Base de conhecimento normativa
**Objetivo:** pesquisa normativa confiável.
- Definição e ingestão das fontes autorizadas (com vigência e data de consulta).
- Índice de busca (vetorial + lexical) com coleção pública separada das coleções por tenant.
- Pesquisador Normativo e Validador de Citações.

**Gate 2:** recuperação avaliada em conjunto de consultas de teste; validador rejeita 100% das citações inexistentes no conjunto de teste.

### Fase 3 — Geração de minutas (MVP)
**Objetivo:** primeira minuta ponta a ponta.
- Adaptador LLM e prompts versionados.
- Analisador de lacunas (perguntas de esclarecimento).
- Gerador de minuta na estrutura padrão INFIS.
- Ciclo de vida, versões e aprovação humana.
- Golden set inicial (≥ 20 consultas revisadas por especialistas).

**Gate 3 (MVP):** critérios de aceite da seção 4 de `requirements.md` atendidos; zero citações inválidas no golden set; piloto interno aprovado.

### Fase 4 — Integração com SOFIA
**Objetivo:** operar como agente do ecossistema.
- Camada anticorrupção para o contrato oficial SOFIA.
- Execução assíncrona e eventos de domínio.
- Testes de contrato com SOFIA (consumer-driven contracts).

**Gate 4:** testes de contrato verdes em homologação com SOFIA; propagação de `correlation_id` verificada ponta a ponta.

### Fase 5 — Produção e evolução
- Exportação DOCX, templates por tipo de consulta, diff de versões.
- Métricas de custo por tenant, dashboards e alertas.
- Revisão de segurança e LGPD antes do go-live.
- Rotina de reavaliação do golden set a cada mudança de modelo/prompt.

**Gate 5:** revisão de segurança aprovada; política de retenção implementada; runbook operacional publicado.

## 3. Dependências e riscos

| Risco / Dependência | Impacto | Mitigação |
|---|---|---|
| Contrato SOFIA indefinido | Retrabalho na integração | Envelope próprio + camada anticorrupção (ADR-002) |
| Licenciamento de bases normativas | Bloqueia Fase 2 | Iniciar com fontes públicas oficiais; decidir licenças na Fase 0 |
| Alucinação de citações | Risco jurídico e reputacional | Validador de Citações obrigatório + golden set + revisão humana |
| Vazamento entre tenants | Violação de sigilo/LGPD | Filtro no repositório, namespaces, testes automatizados de isolamento |
| Desatualização normativa | Parecer incorreto | Vigência registrada; data-base no parecer; rotina de atualização |
| Custo de LLM | Viabilidade | Métricas por tenant; cache de pesquisa normativa (nunca de dados de cliente entre tenants) |

## 4. Definição de pronto (DoD) — vale para toda tarefa

- [ ] Requisito(s) referenciado(s) (`RF-xxx` / `RNF-xxx`).
- [ ] Testes automatizados cobrindo o comportamento.
- [ ] Nenhum segredo em código, logs ou prompts.
- [ ] Acesso a dados filtrado por `tenant_id`.
- [ ] Eventos relevantes auditados.
- [ ] Especificação atualizada se o comportamento mudou.
- [ ] Revisão de código aprovada com checklist da constituição.
