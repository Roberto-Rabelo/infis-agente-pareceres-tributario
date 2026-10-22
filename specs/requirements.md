# Requisitos — Agente Pareceres Tributário

> Subordinado a [`constitution.md`](constitution.md). Cada requisito tem identificador estável e deve ser rastreável até tarefas (`specs/tasks.md`) e testes.

| Campo | Valor |
|---|---|
| Responsável | Roberto |
| Versão | 0.1.0 (rascunho) |
| Status | Em elaboração |

**Prioridade (MoSCoW):** `M` = Must · `S` = Should · `C` = Could · `W` = Won't (nesta versão)

---

## 1. Contexto e escopo

### 1.1 Problema
A elaboração de pareceres tributários exige pesquisa extensa em legislação federal, estadual e municipal, soluções de consulta, jurisprudência (CARF, tribunais) e doutrina, consumindo grande parte do tempo do consultor em tarefas de levantamento e estruturação.

### 1.2 Objetivo
Reduzir o tempo de elaboração de minutas de pareceres, mantendo a qualidade técnica, a fundamentação rastreável e o controle humano sobre o resultado final.

### 1.3 Atores
| Ator | Descrição |
|---|---|
| Consultor tributário | Usuário principal; solicita, revisa e aprova minutas |
| Revisor / sócio | Aprova pareceres de maior risco |
| SOFIA | Orquestrador central que roteia tarefas ao agente (futuro) |
| Outros agentes INFIS | Consumidores/produtores de contexto via SOFIA |
| Administrador | Gerencia configuração, fontes e acessos |

### 1.4 Fora de escopo (v1)
- Emissão/assinatura de pareceres.
- Integração direta com portais de órgãos públicos (e-CAC, SEFAZ etc.).
- Cálculo de tributos e geração de guias.
- Interface web própria (o agente expõe API; UI fica a cargo de SOFIA ou ferramenta interna).

---

## 2. Requisitos funcionais

### 2.1 Recepção da consulta
| ID | Requisito | Prioridade |
|---|---|---|
| RF-001 | O agente deve receber uma consulta tributária estruturada contendo: cliente (tenant), pergunta/fato, tributo(s) envolvido(s), esfera (federal/estadual/municipal), UF/município quando aplicável, período de referência e documentos anexos opcionais. | M |
| RF-002 | O agente deve validar a consulta contra o schema de entrada e rejeitar requisições inválidas com erro estruturado. | M |
| RF-003 | O agente deve identificar informações faltantes essenciais e retornar uma lista de perguntas de esclarecimento, em vez de presumir fatos. | M |
| RF-004 | O agente deve aceitar documentos anexos (PDF, DOCX, TXT) e extrair seu texto para análise. | S |

### 2.2 Pesquisa e fundamentação
| ID | Requisito | Prioridade |
|---|---|---|
| RF-010 | O agente deve pesquisar fontes normativas em base de conhecimento autorizada (legislação, INs, soluções de consulta, jurisprudência). | M |
| RF-011 | Cada fonte utilizada deve ser registrada com: identificação, dispositivo citado, trecho relevante, data de vigência e data de consulta. | M |
| RF-012 | O agente deve sinalizar normas revogadas, alteradas ou com vigência incerta. | M |
| RF-013 | O agente deve identificar e apresentar entendimentos divergentes (ex.: posição do Fisco × jurisprudência). | S |
| RF-014 | O agente deve permitir restringir a pesquisa a fontes específicas indicadas pelo consultor. | C |

### 2.3 Geração da minuta
| ID | Requisito | Prioridade |
|---|---|---|
| RF-020 | O agente deve gerar minuta de parecer na estrutura padrão INFIS: Ementa, Consulta, Fatos, Legislação Aplicável, Análise, Conclusão, Ressalvas e Referências. | M |
| RF-021 | Toda afirmação na Análise e na Conclusão deve estar vinculada a ao menos uma referência listada. | M |
| RF-022 | A minuta deve conter: grau de confiança (alto/médio/baixo), nível de risco tributário e data-base da análise. | M |
| RF-023 | A minuta deve ser marcada visivelmente como **"MINUTA — PENDENTE DE REVISÃO HUMANA"** até aprovação. | M |
| RF-024 | O agente deve exportar a minuta em Markdown e DOCX. | S |
| RF-025 | O agente deve suportar templates de parecer configuráveis por tipo de consulta. | C |

### 2.4 Revisão e ciclo de vida
| ID | Requisito | Prioridade |
|---|---|---|
| RF-030 | O agente deve manter estados do parecer: `RASCUNHO`, `AGUARDANDO_ESCLARECIMENTO`, `EM_REVISAO`, `APROVADO`, `REJEITADO`, `ARQUIVADO`. | M |
| RF-031 | Somente um usuário humano autorizado pode mover um parecer para `APROVADO`. | M |
| RF-032 | O agente deve aceitar comentários de revisão e gerar nova versão da minuta, preservando versões anteriores. | S |
| RF-033 | O agente deve apresentar diff entre versões da minuta. | C |

### 2.5 Integração com SOFIA e multiagente
| ID | Requisito | Prioridade |
|---|---|---|
| RF-040 | O agente deve expor um endpoint de tarefa compatível com o contrato SOFIA (envelope de mensagem versionado). | M |
| RF-041 | O agente deve publicar um *manifesto de capacidades* (nome, versão, habilidades, schemas de entrada/saída, limites). | M |
| RF-042 | O agente deve propagar o `correlation_id` e o `tenant_id` recebidos em todos os logs, eventos e respostas. | M |
| RF-043 | O agente deve suportar execução assíncrona (aceite imediato + notificação/consulta de status). | S |
| RF-044 | O agente deve expor endpoint de saúde (`health`) e prontidão (`ready`). | M |
| RF-045 | O agente deve poder ser invocado de forma autônoma (sem SOFIA) usando o mesmo contrato. | M |
| RF-046 | O agente deve emitir eventos de domínio (ex.: `parecer.minuta_gerada`, `parecer.aprovado`) para consumo por outros agentes via SOFIA. | S |

### 2.6 Auditoria
| ID | Requisito | Prioridade |
|---|---|---|
| RF-050 | Toda requisição deve gerar registro de auditoria append-only conforme P4 da constituição. | M |
| RF-051 | O registro deve incluir versão do modelo de IA, versão do prompt/template e lista de fontes consultadas. | M |
| RF-052 | Deve ser possível consultar a trilha completa de auditoria de um parecer por `parecer_id` ou `correlation_id`. | M |
| RF-053 | Ações humanas (aprovação, rejeição, edição) devem ser registradas com identidade do usuário e data/hora. | M |

---

## 3. Requisitos não funcionais

### 3.1 Segurança
| ID | Requisito | Prioridade |
|---|---|---|
| RNF-001 | Autenticação obrigatória em todos os endpoints (token de serviço para SOFIA; identidade de usuário para humanos). | M |
| RNF-002 | Autorização baseada em papéis (RBAC) e escopo de tenant. | M |
| RNF-003 | Segredos armazenados em cofre; nunca em código, variáveis versionadas, logs ou prompts. | M |
| RNF-004 | Criptografia em trânsito (TLS 1.2+) e em repouso para dados de clientes. | M |
| RNF-005 | Conteúdo de documentos e mensagens de outros agentes tratado como dado não confiável; mitigação de *prompt injection*. | M |
| RNF-006 | Mascaramento de dados pessoais (CPF, CNPJ quando aplicável, nomes) em logs técnicos. | M |
| RNF-007 | Conformidade com LGPD: base legal, minimização, retenção definida e descarte. | M |

### 3.2 Isolamento
| ID | Requisito | Prioridade |
|---|---|---|
| RNF-010 | Todas as consultas a armazenamento devem ser filtradas por `tenant_id`; ausência de `tenant_id` resulta em rejeição. | M |
| RNF-011 | Índices de busca/vetoriais com dados de clientes devem ser segregados por tenant (namespace ou coleção). | M |
| RNF-012 | Base normativa pública (legislação) é compartilhada e somente leitura; dados de clientes nunca são gravados nela. | M |
| RNF-013 | O agente não mantém memória de conversa entre tenants nem entre requisições sem identificador de sessão explícito. | M |
| RNF-014 | O agente roda em processo/contêiner próprio, com credenciais próprias. | M |

### 3.3 Auditoria e observabilidade
| ID | Requisito | Prioridade |
|---|---|---|
| RNF-020 | Logs estruturados (JSON) com `correlation_id`, `tenant_id`, `agent_id`, nível e timestamp UTC. | M |
| RNF-021 | Métricas: latência, taxa de erro, tokens consumidos, custo estimado por requisição e por tenant. | S |
| RNF-022 | Rastreamento distribuído compatível com OpenTelemetry. | S |
| RNF-023 | Retenção de auditoria mínima definida pela política INFIS (padrão proposto: 5 anos, alinhado a prazos decadenciais). | M |

### 3.4 Qualidade, desempenho e disponibilidade
| ID | Requisito | Prioridade |
|---|---|---|
| RNF-030 | Minuta padrão gerada em até 3 minutos (p95) para consultas sem anexos extensos. | S |
| RNF-031 | Conjunto de avaliação (golden set) de consultas com respostas revisadas por especialistas, executado a cada mudança de prompt ou modelo. | M |
| RNF-032 | Taxa de citações inválidas/inexistentes (alucinação de fonte) = 0 no golden set. | M |
| RNF-033 | Disponibilidade alvo de 99% em horário comercial. | C |

### 3.5 Manutenibilidade
| ID | Requisito | Prioridade |
|---|---|---|
| RNF-040 | Prompts e templates versionados no repositório, separados do código. | M |
| RNF-041 | Provedor de LLM abstraído por interface, permitindo troca sem alterar a lógica de domínio. | S |
| RNF-042 | Contratos de integração versionados com SemVer. | M |

---

## 4. Critérios de aceite (v1 / MVP)

1. Dada uma consulta válida, o agente retorna minuta com todas as seções de RF-020 e todas as afirmações referenciadas (RF-021).
2. Dada uma consulta incompleta, o agente retorna perguntas de esclarecimento e estado `AGUARDANDO_ESCLARECIMENTO` (RF-003).
3. Uma requisição sem `tenant_id` é rejeitada (RNF-010).
4. Uma requisição de tenant A não consegue recuperar nenhum dado de tenant B (teste de isolamento automatizado).
5. Toda requisição gera registro de auditoria consultável (RF-050, RF-052).
6. Nenhum parecer chega a `APROVADO` sem ação humana registrada (RF-031, RF-053).
7. O endpoint de tarefa aceita o envelope SOFIA e devolve resposta no contrato definido (RF-040).

## 5. Questões em aberto

- [ ] Quais bases normativas e de jurisprudência serão licenciadas/autorizadas?
- [ ] Qual é o contrato oficial de mensagens do SOFIA (ainda em definição)?
- [ ] Qual provedor de identidade (IdP) será usado pela INFIS?
- [ ] Política oficial de retenção de dados e auditoria.
- [ ] Modelo(s) de LLM aprovados e região de processamento dos dados.
