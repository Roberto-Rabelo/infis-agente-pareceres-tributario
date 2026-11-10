# Plano Técnico: Cruzamento entre Obrigações Fiscais e Parecer Técnico

**Especificação:** [spec.md](spec.md) · **Tarefas:** [tasks.md](tasks.md)
**Constitution:** 2.0.0
**Status:** **Parcial – incremento 1 (Fases 1 a 3 das tarefas)**. As decisões abaixo marcadas como *Proposta* aguardam aprovação. As decisões de RAG, LLM, MCP, A2A, memória e identidade (D7, D9–D15, D17, D18, D23) continuam pendentes e bloqueiam as Fases 7 e 10.

---

## 1. Escopo do incremento 1

| Incluído | Excluído (aguarda esclarecimentos/ADRs) |
|---|---|
| Solução .NET 10, projetos `Api.Agente`, `Api.RAG` (esqueleto) e testes | Qualquer regra fiscal de cruzamento (C1, C2, C3, C5, C11, C12 pendentes; nenhuma regra `aprovada`) |
| Domínio: obrigação, período, CNPJ alfanumérico, achado, regra, catálogo | RAG, embeddings, MCP, LLM, Semantic Kernel |
| Catálogo de regras versionado (formato JSON) e carregador com validação | A2A e Agent Card |
| Motor de regras (ordem, vigência, dependências, ausência de evidências) | Memória conversacional, PostgreSQL de consumo |
| Contrato de entrada v1 (proposta) e pré-condições PRE-001 a PRE-005 | |
| Endpoint REST `POST /api/v1/analises` com autenticação | |

## 2. Decisões do incremento

| Decisão | Escolha | Alternativa mais simples descartada | Status |
|---|---|---|---|
| D20 – Camadas | Pastas `Endpoints/`, `Application/`, `Domain/`, `Infrastructure/` dentro de cada serviço; dependência verificada por teste de arquitetura | Projetos separados por camada: mais projetos sem ganho agora (P16) | Proposta |
| D21 – Catálogo | **Substituída pela spec 004 (D401/D402):** o motor lê `docs/regras/*.json` + `aprovacoes.json`; `catalog/versoes-layout.json` continua. Original: arquivo JSON versionado em `catalog/regras.json` + `catalog/versoes-layout.json`, copiados para a saída do `Api.Agente`; validadores determinísticos em código, ligados à regra pelo `id` | Regras em código: impede versionar regra como dado (doc 05 §1) | Proposta |
| D22 – Testes | xUnit; `Microsoft.AspNetCore.Mvc.Testing` para testes de API; `JsonSchema.Net` (só no projeto de testes) para provar que as fixtures respeitam o schema publicado do contrato | Validar schema à mão: não prova a aderência ao contrato publicado | Proposta |
| D24 – Contrato de entrada | `contracts/entrada/lote.v1.schema.json` (JSON Schema 2020-12). Valores de campos de registros como texto em formato canônico (datas ISO 8601, decimais com ponto) | Schema tipado por obrigação: depende de acordo com os agentes de origem | **Proposta – acordar com os agentes de origem (T004)** |
| D25 – Aptidão | Apta = contrato válido + versão suportada e vigente + relatório presente com `status = APROVADO` e **sem** ocorrência do tipo `ERRO`. Advertências são repassadas no relatório de aptidão como alerta | Aceitar `APROVADO` com erros: contradiz P18 | **Proposta – confirmar (T004)** |
| D15 – Autenticação | JWT Bearer configurado por `Autenticacao:Authority`/`Audience`; sem configuração, todos os endpoints de análise recusam (falha fechada) | API key: não identifica usuário/agente (P14, P15) | Provisória até o ADR D15 |
| API REST (2026-10-30) | Controllers ASP.NET Core (`[ApiController]`, rotas por atributo) em `Endpoints/` de cada serviço; documentação OpenAPI com `Swashbuckle.AspNetCore` (gerador + Swagger UI), alimentada pelos comentários XML. As ações leem o corpo bruto: a desserialização estrita, o limite de tamanho e o erro padronizado continuam com o contrato de entrada, e o schema do corpo é declarado por `[CorpoJson]`. Swagger desligado por padrão (`Swagger:Habilitado`), ligado só em `appsettings.Development.json` e **nunca servido em Production**, porque o documento é anônimo (5.1). `/health` passa a usar `MapHealthChecks`. O `/mcp` da Api.RAG continua mapeado pelo SDK do MCP (não é REST) | Minimal API: dispersa rotas, metadados e documentação em `Program.cs`. `Microsoft.AspNetCore.OpenApi` sozinho: gera o documento, mas não tem interface para teste manual | Proposta |
| C5 – Tolerância | **Movida para `PARAM:TOL_VALOR_DOCUMENTO` em `docs/regras/parametros.json` (spec 004, D403), mesmo valor provisório.** Original: parâmetro `Cruzamento:Tolerancia`, padrão **0,00** (igualdade exata, pois nenhuma norma define tolerância) | — | Provisória até decisão C5 |
| Pré-condições PRE-001 a PRE-005 | `aprovada` no catálogo com fonte = constitution 2.0.0 (P5, P6, P18), docs/02 `INT-COM-002/003/004` | — | **Confirmar com o responsável** (são regras de contrato, não fiscais) |

### 2.1 Regras de determinação de escrituração vigente (PRE-004)

Para o mesmo tipo, CNPJ completo e período: monta-se a cadeia `identificacao.reciboAnterior → identificacao.recibo`. A obrigação que não é referenciada por nenhuma outra é a vigente; as demais são descartadas. Se houver mais de uma candidata (cadeia ambígua), **todas** ficam não aptas. Para documentos (NF-e, NFC-e, NFS-e), duplicidade de `chaveAcesso` mantém a primeira e descarta as demais.

### 2.2 Grupo de cruzamento (PRE-005)

O lote declara `contribuinte.cnpj` (raiz de 8 posições ou CNPJ completo) e `periodo`. Obrigação com raiz diferente ou período sem sobreposição com o do lote não é apta.

### 2.3 Versões de layout suportadas

`catalog/versoes-layout.json` lista versões por obrigação com vigência e campo `conferido`. Somente entradas com `conferido = true` são suportadas. Itens sem o valor literal do campo de versão confirmado nos documentos (EFD-Contribuições, ECF, DIMP) ficam `conferido = false` até a curadoria (T011) — obrigações desses tipos serão não aptas até lá.

## 3. Verificação da Constitution

| Princípio | Atendimento no incremento 1 |
|---|---|
| P1 | Carregador rejeita regra sem fonte e regra `aprovada` com fonte não conferida ou com "(conferir)" |
| P2 | Pré-condições e motor determinísticos, testados; LLM ausente |
| P3 | `Natureza` com os 5 valores; severidade vem do catálogo |
| P4 | `StatusAnalise` nunca é `Concluida` com regra obrigatória pendente; sem regras de cruzamento aprovadas, status `Inconclusiva` |
| P5 | Achados com localização; regras sem obrigação apta viram ausência de evidências |
| P6 | PRE-002 contra `versoes-layout.json` com vigência |
| P7, P9, P11, P13 (consumo), P14 | Não aplicáveis neste incremento (sem RAG/LLM/ferramentas/memória) |
| P8 | Desserialização estrita (campos desconhecidos rejeitados); nenhum conteúdo de entrada vira instrução; teste adversarial |
| P10 | Envelope montado pelo sistema |
| P12 | Domínio sem dependência de ASP.NET/infraestrutura (teste de arquitetura); `Api.Agente` não referencia `Api.RAG` |
| P13 | Execução e correlação (`X-Correlation-Id`) no envelope e nos logs; mascaramento de CNPJ |
| P15 | Indicação de revisão humana quando houver pré-condição não atendida ou ausência de evidências |
| P16 | Dois serviços + dois projetos de teste. Dependências: `JwtBearer` (autenticação), `Swashbuckle.AspNetCore` 10.3.1 (documentação OpenAPI, nos dois serviços; ver §2 "API REST"), `Mvc.Testing` e `JsonSchema.Net` (testes) |
| P17 | Testes positivos e negativos; CI sem LLM |
| P18 | Contrato v1, aptidão e testes com entradas válidas, inválidas, sem versão, reprovadas e sem relatório |

**Desvio registrado:** tarefas e implementação começaram com o plano parcial (constitution 6.1). Impacto limitado ao incremento 1, que não contém regra fiscal. Responsável pela aprovação: *a definir*. Revisão: ao concluir a Fase 0.

## 4. Parâmetros configuráveis (`appsettings.json`)

| Chave | Padrão | Uso |
|---|---|---|
| `Entrada:TamanhoMaximoBytes` | 52 428 800 (50 MB) | FR-044 |
| `Entrada:ProfundidadeMaximaJson` | 64 | FR-044 |
| `Entrada:MaximoObrigacoesPorLote` | 500 | FR-044 |
| `PARAM:TOL_VALOR_DOCUMENTO` (`docs/regras/parametros.json`) | 0.00 (provisório) | FR-024; spec 004 D403 |
| `Catalogo:Caminho` / `Catalogo:CaminhoRegras` | `catalog` / `regras` | D21; spec 004 D401 |
| `Autenticacao:Authority` / `Audience` | vazio (falha fechada) | FR-043 |
| `Swagger:Habilitado` | `false` (`true` em Development; ignorado em Production) | Documentação OpenAPI em `/swagger`, nos dois serviços |

## 5. Incremento 2 – Base de cruzamentos em embeddings (Fase 7, parcial)

**Objetivo:** vetorizar o conhecimento de comparação entre obrigações (`docs/03`, `NUCLEO-1/2/3`, `01`, `02`, `04`, `05`, `README`) na Api.RAG (PostgreSQL + PgVector) e expô-lo por MCP. Assim o LLM da Api.Agente, ou outro agente autorizado, analisa cada inconsistência junto com a regra, as hipóteses legítimas e o de-para que a originaram.

**Fluxo:** motor determinístico → achados (CRZ/ENT) → `ServicoFundamentacao` → MCP `consultar_base_conhecimento` (busca exata por `regraIds` + filtros de data/vigência) → `resultado.fundamentacao[]` e `resultado.limitacoes[]` no envelope. O LLM (T071–T073) ainda não foi integrado: o envelope já entrega achados e trechos rastreáveis, prontos para ele.

**Decisões:** ADR D11 (embeddings/busca), D12 (segmentação/parâmetros provisórios) e D13 (MCP), em `docs/adr/`, todos propostos. Novas dependências (P16): `ModelContextProtocol(.AspNetCore)` 2.3.0, `Npgsql` 10.0.3, `Pgvector` 0.3.2, `Microsoft.Extensions.AI.OpenAI` 10.11.0; nos testes, `Testcontainers.PostgreSql` 4.16.0 e `Xunit.SkippableFact`.

**Natureza das fontes (P1):** `docs/` é base de estudo interna e é ingerida como `BASE_REGRAS_INTERNA`. Ela orienta a análise, mas não substitui a fonte oficial citada em cada trecho. Os manuais oficiais (`MANUAL_OFICIAL`) dependem da curadoria T011.

| Chave (Api.RAG) | Padrão | Uso |
|---|---|---|
| `ConnectionStrings:Rag` | vazio → base indisponível | variável `ConnectionStrings__Rag` |
| `OpenAI:ApiKey` | vazio → provedor indisponível | variável `OpenAI__ApiKey` |
| `Embeddings:Modelo` / `Dimensao` | `text-embedding-3-small` / 1536 | D11 |
| `Segmentacao:MaximoCaracteres` | 1500 | D12 (provisório) |
| `Consulta:LimitePadrao` / `LimiteMaximo` / `LimiarSimilaridade` | 8 / 30 / nulo | D12 (provisório) |
| `Ingestao:TamanhoMaximoBytes` | 5 242 880 | 5.1 |

| Chave (Api.Agente) | Padrão | Uso |
|---|---|---|
| `BaseConhecimento:Endpoint` | vazio → limitação registrada | URL `/mcp` da Api.RAG |
| `BaseConhecimento:TokenAcesso` | vazio | variável `BaseConhecimento__TokenAcesso` (papel `rag.consulta`) |
| `BaseConhecimento:TimeoutSegundos` / `LimitePorConsulta` / `RegrasPorConsulta` | 10 / 20 / 20 | D13, D12 |

**Pendências:** resultado real de T065 (conjunto pronto; ver §6) antes de aprovar D12; divisão do registro de consumo entre Api.RAG e Api.Agente (D11, constitution 4.6); emissão de tokens de serviço (D15); execução real com PostgreSQL e chave OpenAI.

## 6. Incremento 3 – Parecer por LLM e avaliações (Fase 7)

**Fluxo:** análise determinística → fundamentação (MCP) → `ServicoParecer` (Semantic Kernel + OpenAI, saída estruturada estrita) → `ValidadorParecer` (schema + semântica) → envelope 1.1.0 com `parecer`, `trechosCitados`, `consumo`, `versaoContratoParecer` e `versaoPrompt`. ADRs D10 (modelo e saída estruturada) e D17 (retentativas e consumo), ambos propostos. Novas dependências (P16): `Microsoft.SemanticKernel` 1.81.0, `ModelContextProtocol` 2.3.0, `Npgsql` 10.0.3; nos testes, `Xunit.SkippableFact`.

| Chave (Api.Agente) | Padrão | Uso |
|---|---|---|
| `Parecer:Habilitado` | `false` → só resultado determinístico + limitação | D10 |
| `Parecer:Modelo` | `gpt-4o-mini` (indicado em 2026-10-29; aprovação após T075) | D10 |
| `Parecer:TimeoutSegundos` / `RetentativasTransitorias` / `RetentativasSaidaInvalida` / `EsperaRetentativaMs` | 60 / 2 / 2 / 500 | D17 |
| `Parecer:MaximoChamadasFerramenta` / `FerramentasAutorizadas` | 3 / `base_conhecimento-consultar` | P11 |
| `Parecer:Temperatura` / `MaximoTokensSaida` | nulo (padrão do modelo) | D10 |
| `Custos:Versao` / `Moeda` / `Modelos` | vazio → custo nulo | 4.5, D17 |
| `ConnectionStrings:Agente` | vazio → consumo não persistido (obrigatório com parecer habilitado) | T074 |
| `OpenAI:ApiKey` | vazio | variável `OpenAI__ApiKey` |

### Avaliação de recuperação (T065, D12)

- **Conjunto:** `eval/recuperacao/v1.json` com 18 perguntas sem identificador de regra × regras esperadas (36 IDs, todos verificados como trecho principal da base na CI).
- **Métricas:** acerto@k (algum esperado entre os k trechos), revocação@k dos esperados e MRR, contadas pela regra principal do trecho, mais os tokens de embeddings.
- **Execução real:** `AvaliacaoRecuperacaoTests.Avaliacao_ComProvedorReal`, com `OPENAI_API_KEY`, `RAG_AVALIACAO_MODELO` e `RAG_AVALIACAO_DIMENSAO`; opcionais `RAG_AVALIACAO_MAXIMO_CARACTERES` e `RAG_AVALIACAO_TOPK`. O relatório JSON é gravado em `bin/.../avaliacao/`.
- **Critérios propostos (a aprovar):** acerto@8 ≥ 0,90; revocação@8 ≥ 0,80; MRR ≥ 0,60. `LimiarSimilaridade` continua nulo até haver distribuição de similaridades medida.
- **Resultado 2026-10-29 (conjunto 1.0.0):** `text-embedding-3-small`, 1536 dimensões, trechos de até 1500 caracteres, k = 8, 631 trechos, 92 227 tokens de embeddings. **acerto@8 = 0,778; revocação@8 = 0,639; MRR = 0,539. Abaixo dos três critérios propostos: D12 não pode ser aprovado.**
  - Sem nenhum esperado no top 8: R002 (NF-e própria ausente na EFD), R005 (DIFAL), R015 (presumido × não cumulativo), R017 (raiz do CNPJ).
  - Esperado fora do topo: R006, R008, R012 e R018 (posições 5 a 7).
  - Impacto: a fundamentação dos achados usa busca **exata** por `regraIds` e não depende desse resultado. A lacuna afeta as perguntas livres feitas pelo LLM pela ferramenta.
  - Próximos experimentos (dependem de aprovação; P7): incluir o título da seção e o ID da regra no texto vetorizado, avaliar trechos menores, k maior ou busca híbrida (texto + vetor).

### Avaliação do parecer (T075, D10)

- **Conjunto:** `eval/parecer/v1.json` com 5 cenários: achado grave com hipótese, hipótese longa, dois achados com cobertura parcial, trecho com injeção de instrução e ausência de evidências sem severidade. Trecho e hipóteses vêm da linha da regra em `docs/03`.
- **Execução real:** `AvaliacaoParecerTests.Avaliacao_ComModeloReal`, com `OPENAI_API_KEY` e `PARECER_AVALIACAO_MODELO`.
- **Critérios propostos (a aprovar):** 0 achados determinísticos alterados (SC-004, obrigatório); aceite ≥ 0,80 dos cenários; aceite na 1ª tentativa ≥ 0,60; cenário de injeção aceito sem reclassificação ou terminando em `FALHA_TECNICA` (nunca conformidade).
- **Resultado 2026-10-29 (conjunto 1.0.0, prompt 1.0.0):** `gpt-4o-mini`. Aceite = 1,00 (5/5), aceite na 1ª tentativa = 1,00, **0 achados alterados (SC-004)**. O cenário de injeção (P004) foi aceito sem reclassificação. Nenhuma chamada de ferramenta; 11 774 tokens de entrada e 2 018 de saída. O custo não foi calculado porque não há tabela de preços aprovada. Atende aos critérios propostos. Limitação: 5 cenários e uma única execução, sem medir variação entre execuções nem a qualidade textual do parecer (que exige revisão humana).

## 7. Incremento 4 – Primeiras regras de cruzamento NF-e × EFD ICMS/IPI (2026-10-30)

**Objetivo:** executar de fato o cruzamento, a fundamentação por RAG e o parecer sobre achados reais.

| Decisão | Escolha |
|---|---|
| Identificadores | IDs do NUCLEO-2 (`M1-*`), aceitos pelo `ValidadorCatalogo`. Os `CRZ-*` de `docs/03` são a versão anterior; a equivalência continua pendente (T002) |
| Aprovação | Roberto, com `docs/NUCLEO-2` como fonte conferida do projeto. Os manuais oficiais citados nas regras seguem sem curadoria (T011) |
| Validadores | `Domain/Motor/ValidadoresNfeEfd.cs`, uma função por regra, sem acesso a infraestrutura |
| Evento da NF-e | Grupo `evento` em `registros`, com `tpEvento` (110111 = cancelamento); a acordar com os agentes de origem (T004) |
| Ausência parcial | Validador que devolve ausência de evidências deixa a regra em `AusenciaEvidencias`, mesmo com inconsistência (P4) |
| Tolerância | `PARAM:TOL_VALOR_DOCUMENTO` = 0,00 (provisório, C5), lido de `docs/regras/parametros.json` (spec 004) |
| Fatos no parecer | Hipóteses, escopo e limitações vêm do `ConsolidadorParecer`, não do modelo. `HIPOTESE_OMITIDA` deixa de existir: a cópia literal fazia análises válidas terminarem em `FALHA_TECNICA` |
| Validação do texto | `AFIRMACAO_PROIBIDA` (conformidade, cobertura total, consequência jurídica) e `ACHADO_SEM_REFERENCIA` provocam retentativa. A lista de termos não detecta toda afirmação errada: a revisão humana continua obrigatória |
| Prompt | `prompts/parecer/v1.1.0.md`; a versão 1.0.0 fica no repositório para reprodutibilidade (P9) |

**Limitações conhecidas:**

- `PAR-001`, `ENT-003` e os filtros `EXC` não estão no catálogo: a dependência de M1-P01 foi retirada e os filtros EXC-003 e EXC-005 estão embutidos no universo das regras. EXC-007 (NF-e só com IBS/CBS) não é aplicado.
- NFC-e (modelo 65) e o C190 não são cruzados nesta entrega.
- Sem M1-P05/P06: dependem de `PARAM:JANELA_MESES_ENTRADA`, sem valor. NF-e de terceiros ausente da EFD não gera achado.
- Eventos de denegação e inutilização (M1-P03/P04) não têm contrato definido.
