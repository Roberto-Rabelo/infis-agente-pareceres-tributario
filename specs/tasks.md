# Tarefas — Agente Pareceres Tributário

> Derivadas de [`plan.md`](plan.md). Toda tarefa referencia requisitos de [`requirements.md`](requirements.md). Nenhum código deve ser escrito sem uma tarefa correspondente aqui (constituição P7).

| Campo | Valor |
|---|---|
| Responsável | Roberto |
| Versão | 0.2.0 (rascunho) |
| Stack | .NET 10 · ASP.NET Core · Clean Architecture · DDD · Semantic Kernel · OpenAI · pgvector · MongoDB · xUnit (ver [`architecture.md`](architecture.md)) |

**Status:** `[ ]` pendente · `[~]` em andamento · `[x]` concluída · `[!]` bloqueada

---

## Fase 0 — Especificação

- [x] **T-000** Criar estrutura SDD inicial (`specs/*`, `AGENTS.md`).
- [ ] **T-001** Revisar e aprovar `constitution.md`. — *Owner: Roberto*
- [ ] **T-002** Revisar e aprovar `requirements.md`.
- [ ] **T-003** Revisar e aprovar `architecture.md` (ADR-002 a ADR-011).
- [ ] **T-004** Definir fontes normativas e jurisprudenciais autorizadas e seu licenciamento. — RF-010
- [ ] **T-005** Obter ou co-definir com o time de SOFIA o contrato de mensagens. — RF-040, RNF-042
- [ ] **T-006** Definir provedor de identidade (IdP) e modelo de papéis. — RNF-001, RNF-002
- [ ] **T-007** Definir política de retenção de dados e de auditoria com jurídico/compliance. — RNF-007, RNF-023
- [ ] **T-008** Definir modelos OpenAI (chat e embeddings) e validar com compliance a proteção de dados (retenção, *Zero Data Retention*, residência) ou a alternativa Azure OpenAI. — RNF-007, RNF-041
- [ ] **T-009** Criar template padrão de parecer INFIS e 3 exemplos sintéticos em `samples/`. — RF-020

## Fase 1 — Fundação segura

- [ ] **T-100** Inicializar repositório (git, `.gitignore` para .NET, `appsettings*.json` apenas com placeholders, sem segredos).
- [ ] **T-101** Criar a solution `Infis.PareceresTributario.sln` com os projetos Domain, Application, Infrastructure, Contracts e Api (.NET 10) e as referências conforme a regra de dependência da Clean Architecture.
- [ ] **T-102** Configurar padrões de build: `Directory.Build.props` (nullable, *warnings as errors*, analisadores), `.editorconfig`, `dotnet format` e gerenciamento central de pacotes (`Directory.Packages.props`).
- [ ] **T-103** Criar os projetos de teste xUnit (Domain, Application, Infrastructure, Api, Architecture, Evaluation).
- [ ] **T-104** Configurar CI (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, varredura de segredos e de pacotes vulneráveis). — RNF-003
- [ ] **T-105** Testes de arquitetura (NetArchTest) garantindo a regra de dependência entre camadas. — RNF-040
- [ ] **T-106** Escrever JSON Schemas do envelope v1 em `contracts/` e os DTOs equivalentes no projeto `Contracts`. — RF-002, RF-040, RNF-042
- [ ] **T-107** Modelar o Domain: agregado `Parecer`, objetos de valor, máquina de estados e eventos de domínio, com testes unitários. — RF-030, RF-031
- [ ] **T-108** Implementar `ManifestController` (`/v1/manifest`). — RF-041
- [ ] **T-109** Implementar `/health` e `/ready` com health checks de PostgreSQL e MongoDB. — RF-044
- [ ] **T-110** Implementar `TasksController` (`/v1/tasks`) com validação (FluentValidation + schema) e erros em Problem Details (RFC 9457). — RF-001, RF-002
- [ ] **T-111** Configurar documento OpenAPI (`Microsoft.AspNetCore.OpenApi`) e Swagger UI apenas em dev/hml, com autenticação. — ADR-010
- [ ] **T-112** Implementar autenticação JWT Bearer (token de serviço + usuário OIDC). — RNF-001
- [ ] **T-113** Implementar políticas de autorização por papel e middleware de escopo de tenant e `IContextoExecucao`. — RNF-002, RNF-010
- [ ] **T-114** Implementar persistência EF Core + PostgreSQL com *global query filter* por `tenant_id` e Row-Level Security. — RNF-010, RNF-013
- [ ] **T-115** Implementar auditoria append-only no PostgreSQL (usuário só com `INSERT`, *trigger* contra `UPDATE`/`DELETE`, encadeamento por hash). — RF-050, RF-052
- [ ] **T-116** Implementar logs estruturados (`ILogger` JSON) e OpenTelemetry com `correlation_id`, `tenant_id` e mascaramento de dados pessoais. — RF-042, RNF-006, RNF-020
- [ ] **T-117** Integrar cofre de segredos (`dotnet user-secrets` apenas em dev). — RNF-003
- [ ] **T-118** Testes de isolamento entre tenants com Testcontainers (PostgreSQL e MongoDB). — RNF-010, RNF-011
- [ ] **T-119** Dockerfile e `docker compose` local (API, PostgreSQL + pgvector, MongoDB). — RNF-014

## Fase 2 — Base de conhecimento normativa

- [ ] **T-200** Pipeline de ingestão de fontes com metadados de vigência e data de consulta e embeddings OpenAI versionados. — RF-011, RF-012
- [ ] **T-201** Configurar pgvector: tabela `normas_publicas` (somente leitura em runtime) e `documentos_cliente` (RLS por tenant), índice HNSW e full-text em português. — RNF-011, RNF-012
- [ ] **T-202** Implementar `IPesquisaNormativa` com busca híbrida (vetorial + full-text, *reciprocal rank fusion*) e `PesquisaNormativaPlugin` no Semantic Kernel. — RF-010, RF-013
- [ ] **T-203** Implementar `IValidadorCitacoes` e `ValidacaoCitacaoPlugin`. — RF-021, RNF-032
- [ ] **T-204** Conjunto de avaliação de recuperação.
- [ ] **T-205** Extração de texto de anexos (PDF, DOCX, TXT) com sanitização. — RF-004, RNF-005

## Fase 3 — Geração de minutas (MVP)

- [ ] **T-300** Configurar Semantic Kernel com conector OpenAI (chat completion e embeddings), kernel *scoped* por requisição, timeouts, retries e limite de tokens. — RNF-041, RNF-013
- [ ] **T-301** Estrutura de prompts versionados em `prompts/`, com saída estruturada em JSON e DTOs tipados. — RNF-040, RF-051
- [ ] **T-302** Filtros do Semantic Kernel: renderização de prompt (conteúdo não confiável, mascaramento), invocação de função (auditoria, verificação de tenant) e validação de saída. — RNF-005, RNF-006, RF-050
- [ ] **T-303** Implementar `IAnalisadorLacunas` e o caso de uso de esclarecimento. — RF-003
- [ ] **T-304** Implementar `IGeradorMinuta` na estrutura padrão INFIS com marcação de minuta. — RF-020, RF-022, RF-023
- [ ] **T-305** Implementar `IMemoriaConversacional` no MongoDB (coleção `sessoes_conversa`, filtro por tenant, somente com `sessao_id` explícito, índice TTL, resumo de histórico). — RNF-013, RNF-007
- [ ] **T-306** Casos de uso de ciclo de vida e versionamento do parecer. — RF-030, RF-032
- [ ] **T-307** Fluxo de decisão humana (aprovar/rejeitar) com auditoria. — RF-031, RF-053
- [ ] **T-308** Golden set inicial (≥ 20 consultas revisadas por especialistas) no projeto `Evaluation.Tests`. — RNF-031, RNF-032
- [ ] **T-309** Exportação Markdown. — RF-024
- [ ] **T-310** Validação dos critérios de aceite do MVP.

## Fase 4 — Integração com SOFIA

- [ ] **T-400** Camada anticorrupção para o contrato oficial SOFIA. — RF-040
- [ ] **T-401** Execução assíncrona e consulta de status. — RF-043
- [ ] **T-402** Publicação de eventos de domínio com padrão *outbox* (tabela `outbox_eventos`). — RF-046
- [ ] **T-403** Testes de contrato com SOFIA. — RNF-042
- [ ] **T-404** Rastreamento distribuído ponta a ponta com SOFIA (OpenTelemetry). — RNF-022
- [ ] **T-405** Reavaliar Semantic Kernel × Microsoft Agent Framework (ADR-006).

## Fase 5 — Produção e evolução

- [ ] **T-500** Exportação DOCX. — RF-024
- [ ] **T-501** Templates por tipo de consulta. — RF-025
- [ ] **T-502** Diff entre versões. — RF-033
- [ ] **T-503** Métricas de custo e consumo por tenant. — RNF-021
- [ ] **T-504** Revisão de segurança e LGPD. — RNF-004, RNF-007
- [ ] **T-505** Runbook operacional e plano de resposta a incidentes.
