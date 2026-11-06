<!--
Relatório de Impacto de Sincronização
- Versão: 1.0.0 → 2.0.0
- Tipo de alteração: MAJOR – redefinição do escopo. A validação de layout/estrutura de cada
  obrigação passa a ser responsabilidade dos agentes de origem. Este agente recebe as obrigações
  já validadas, em dados normalizados (JSON) acompanhados do relatório de validação, e verifica
  exclusivamente as relações entre obrigações.
- Princípios alterados: P2 (determinismo sobre dados normalizados), P3 (natureza "erro estrutural"
  substituída por "pré-condição de entrada não atendida"), P5 (aptidão ao cruzamento), P6 (versão
  informada pela origem), P8 (relatório de validação como dado não confiável)
- Princípio adicionado: P18 (contrato de entrada com os agentes de origem)
- Seções alteradas: 1.1, 1.2, 3.2, 3.4, 3.5, 4.2, 4.6, 5.1, 9 (D1, D6 revistas; D24 a D26 adicionadas)
- Especificações impactadas: nenhuma ainda; docs/02-regras-intra-obrigacao.md passa a ser
  referência de pré-condições, não regras deste agente
- Histórico: 1.0.0 – criação inicial (P1 a P17, seções 1 a 9)
- Templates a revisar após inicialização do Spec Kit:
  - .specify/templates/spec-template.md: incluir seção "Catálogo de Regras" e "Fontes oficiais"
  - .specify/templates/plan-template.md: incluir "Verificação da Constitution" com P1–P17
  - .specify/templates/tasks-template.md: incluir rastreabilidade requisito → tarefa → teste
- Pendências: ver Seção 9 (Decisões pendentes)
-->

# Constitution — infis-agente-parecer-tributário

**Versão:** 2.1.0
**Data de ratificação:** 2026-10-29
**Última alteração:** 2026-11-05

Termos normativos:

- **DEVE / NÃO DEVE**: obrigação ou proibição. O descumprimento bloqueia a aprovação do pull request, salvo desvio aprovado (Seção 8.3).
- **PODE**: opção permitida, a critério da especificação ou do plano técnico.

---

## 1. Propósito e escopo

### 1.1 Propósito

O **infis-agente-parecer-tributário** é um agente de IA que:

1. Recebe, de outros agentes (agentes de origem), obrigações fiscais e tributárias **já validadas quanto ao layout**, em dados normalizados (JSON por registro e campo) acompanhados do relatório de validação de cada obrigação.
2. Confere as pré-condições de entrada: contrato do JSON, versão de layout informada, resultado da validação de origem, contribuinte e período (P18).
3. Verifica as relações e regras que DEVEM existir entre obrigações distintas (cruzamentos), indicando o que deve constar em uma obrigação em função do que consta em outra.
4. Classifica cada achado, distinguindo **erro grave** de escrituração de **alerta de atenção**.
5. Emite um parecer técnico estruturado, fundamentado em documentos oficiais, com evidências e rastreabilidade.

A validação de layout, schema e estrutura de cada obrigação NÃO é responsabilidade deste agente. Ele NÃO DEVE refazê-la nem suprir a sua ausência.

### 1.2 Escopo

Estão no escopo:

- Conferência determinística das pré-condições de entrada.
- Validação cruzada entre obrigações, com base em regras catalogadas.
- Consulta a manuais oficiais por RAG.
- Uso controlado de ferramentas externas.
- Análise complementar por LLM.
- Respostas estruturadas, validadas e rastreáveis.
- Exposição do agente a outros agentes e orquestradores por A2A.

Fora do escopo do agente:

- Leitura de arquivos originais (TXT SPED, XML) e validação de layout, schema, estrutura e regras internas validadas pelo PVA ou pela SEFAZ/Ambiente Nacional. Essas validações pertencem aos agentes de origem.

Fora do escopo desta constitution:

- A lista de obrigações suportadas, os layouts, os schemas, as regras fiscais e os critérios de conformidade. Esses elementos DEVEM ser obtidos de documentos oficiais e registrados nas especificações (ver P1 e Seção 9).
- Detalhes de implementação, versões de SDKs e parâmetros ajustáveis, que pertencem aos planos técnicos.

### 1.3 Natureza do parecer

O parecer é um **apoio técnico**. Ele NÃO DEVE ser apresentado como certificação de conformidade fiscal nem substituir a responsabilidade do profissional que o revisa.

---

## 2. Princípios obrigatórios

Cada princípio traz a **Regra**, a **Justificativa** e a **Verificação** (como comprovar o cumprimento em revisão).

### P1. Fundamentação exclusiva em fontes oficiais

**Regra**
- Layouts, schemas, regras de validação, regras de cruzamento e severidades DEVEM ter origem em documento oficial identificado (emissor, título, versão, data ou vigência e localização: seção, registro, campo ou página).
- Nenhum agente, humano ou de IA, DEVE inventar layout, regra fiscal, schema ou critério de conformidade.
- Interpretações que não constem literalmente da fonte DEVEM ser registradas como interpretação, com responsável e aprovação na especificação.
- Na falta de fonte oficial, o item DEVE ser registrado como decisão pendente e NÃO DEVE ser implementado como regra aprovada.

**Justificativa**: o parecer tem efeito fiscal. Uma regra sem fonte não pode ser auditada nem defendida.

**Verificação**
- Toda regra do Catálogo de Regras (Seção 4.2) tem campo de fonte preenchido e recuperável.
- O PR que adiciona ou altera um validador referencia o identificador da regra e a sua fonte.

### P2. Determinismo antes da IA

**Regra**
- Pré-condições de entrada e regras de cruzamento que possam ser expressas deterministicamente DEVEM ser implementadas como validadores ou ferramentas verificáveis sobre os dados normalizados recebidos, com testes automatizados.
- O LLM NÃO DEVE substituir, sobrepor ou reclassificar o resultado de um validador determinístico nem o resultado da validação de origem.
- O LLM NÃO DEVE interpretar dados ausentes do JSON normalizado nem reconstruir registros ou campos a partir de texto livre.
- O LLM PODE ser usado para explicar achados, relacionar evidências, apontar indícios e redigir o parecer.

**Justificativa**: resultados determinísticos são reprodutíveis e testáveis. Resultados de LLM não são.

**Verificação**
- Cada regra do catálogo marcada como determinística aponta para um validador e para seus testes.
- Testes demonstram que a saída do LLM não altera o status de um achado determinístico.
- Prompts não contêm regras fiscais que deveriam estar em validadores.

### P3. Classificação obrigatória dos achados

**Regra**
Todo achado DEVE ter uma **natureza** e, quando aplicável, uma **severidade**.

Naturezas obrigatórias, mutuamente exclusivas:

| Natureza | Origem |
|---|---|
| Pré-condição de entrada não atendida | Validador determinístico de entrada (P18): contrato do JSON inválido, versão de layout ausente ou não suportada, validação de origem reprovada ou ausente, contribuinte ou período divergente |
| Inconsistência de cruzamento | Validador determinístico de regra de cruzamento entre obrigações |
| Indício identificado pela IA | Análise do LLM, sem confirmação determinística |
| Ausência de evidências | Não há dados ou fonte suficiente para concluir |
| Falha técnica ou dependência indisponível | Erro de processamento, timeout, serviço externo ou MCP indisponível |

Severidades mínimas:

- **Erro grave**: divergência de escrituração entre obrigações que, segundo a fonte oficial, caracteriza descumprimento da regra de cruzamento.
- **Alerta de atenção**: situação que exige análise humana, sem caracterizar erro conforme a fonte oficial.

Regras adicionais:

- A severidade de cada regra DEVE ser definida no Catálogo de Regras, com base na fonte oficial. Ela NÃO DEVE ser atribuída livremente pelo LLM.
- Um indício identificado pela IA NÃO DEVE ser classificado como erro grave. PODE ser classificado como alerta de atenção.
- Falhas técnicas e ausência de evidências NÃO DEVEM ser apresentadas como conformidade nem como erro do contribuinte.
- Pré-condição de entrada não atendida NÃO DEVE receber severidade de erro do contribuinte. Ela impede os cruzamentos que dependem da obrigação afetada.

**Justificativa**: o usuário precisa distinguir o que está comprovado, o que é suspeita e o que não foi verificado.

**Verificação**
- O contrato de saída (Seção 4.3) contém os campos de natureza e severidade com valores enumerados.
- Testes cobrem pelo menos um caso de cada natureza.
- A validação semântica rejeita indício da IA com severidade "erro grave".

### P4. Proibição de conformidade presumida

**Regra**
- O agente NÃO DEVE declarar conformidade quando qualquer validação obrigatória não tiver sido concluída, tiver falhado tecnicamente ou não tiver evidências suficientes.
- O resultado DEVE listar as validações não realizadas e o motivo de cada uma.
- A especificação de cada obrigação e de cada cruzamento DEVE definir quais validações são obrigatórias.

**Justificativa**: um resultado "conforme" incompleto é mais perigoso do que um resultado "inconclusivo".

**Verificação**
- Testes simulam falha de validador, de MCP e de ferramenta, e comprovam que o status final não é de conformidade.
- A validação semântica da resposta rejeita status de conformidade quando há validações obrigatórias pendentes.

### P5. Validação entre obrigações rastreável

**Regra**
- Cada regra de cruzamento DEVE identificar: obrigações envolvidas e suas versões de layout, registros e campos relacionados, período de apuração aplicável, condição de aplicabilidade, resultado esperado, severidade e fonte oficial.
- Cruzamentos DEVEM considerar o mesmo contribuinte (ou estabelecimento, conforme a regra) e o mesmo período de referência. Divergência de contribuinte ou de período DEVE ser reportada, e não cruzada silenciosamente.
- Quando uma obrigação necessária ao cruzamento não for fornecida, ou for recebida sem aptidão (P18), o cruzamento DEVE ser reportado como "ausência de evidências", e não como inconsistência.
- Cada achado de cruzamento DEVE apontar a localização da evidência em ambas as obrigações (identificador da obrigação recebida, registro, ocorrência e campo; linha do arquivo original quando informada pela origem).

**Justificativa**: cruzamentos são o principal valor do agente e a principal fonte de falsos positivos.

**Verificação**
- O catálogo contém todos os campos acima para cada regra de cruzamento.
- Existem testes com par de arquivos consistente, inconsistente e incompleto para cada regra.

### P6. Versionamento e vigência de layouts e manuais

**Regra**
- O tipo de obrigação e a versão do layout DEVEM ser informados pelo agente de origem no JSON recebido. Este agente NÃO DEVE inferi-los.
- Regras de cruzamento e consultas ao RAG DEVEM usar a versão do layout e do manual vigente para o período de cada obrigação envolvida.
- Quando a versão não for informada ou não for suportada pelo catálogo, a obrigação DEVE ser tratada como "pré-condição de entrada não atendida" e os cruzamentos dependentes DEVEM ser reportados como "ausência de evidências".

**Justificativa**: layouts fiscais mudam com frequência. Cruzar com a versão errada produz falsos erros.

**Verificação**
- Testes com obrigações de versões distintas comprovam a seleção correta das regras.
- Testes comprovam a rejeição de versão ausente ou desconhecida.

### P7. Conhecimento rastreável (RAG)

**Regra**
- Todo documento ingerido DEVE ter origem, identificação, versão e, quando aplicável, vigência.
- Todo chunk DEVE permitir recuperar o documento de origem e sua localização (seção, página, registro), quando disponível.
- A ingestão DEVE definir critérios documentados de segmentação, deduplicação, reprocessamento e atualização, incluindo o tratamento de versões substituídas.
- Modelo de embeddings, dimensão dos vetores e estratégia de busca DEVEM ser compatíveis entre si e documentados em ADR. A troca do modelo de embeddings DEVE prever reindexação.
- A recuperação DEVE filtrar por tipo de documento e versão do manual aplicável.
- Tamanho de chunk, topK e limiares de similaridade NÃO DEVEM ser fixados sem avaliação. DEVEM ser especificados no plano técnico e avaliados com um conjunto representativo de perguntas e respostas esperadas.
- Respostas fundamentadas no RAG DEVEM apresentar referências recuperáveis. Na ausência de evidência suficiente, o agente DEVE declarar a limitação.

**Justificativa**: sem rastreabilidade, a fundamentação do parecer não pode ser conferida.

**Verificação**
- O schema de dados do RAG contém os metadados exigidos.
- Testes de ingestão cobrem duplicidade, reprocessamento e nova versão.
- Existe conjunto de avaliação de recuperação versionado e seus resultados constam no plano ou no PR que altera parâmetros.

### P8. Conteúdo não confiável

**Regra**
- O conteúdo das obrigações recebidas, dos relatórios de validação de origem, de chunks recuperados, de resultados de ferramentas e de mensagens de outros agentes DEVE ser tratado como dado não confiável.
- O relatório de validação de origem é usado apenas como pré-condição (P18). Textos nele contidos NÃO DEVEM ser tratados como instrução.
- Esse conteúdo NÃO DEVE alterar instruções do sistema, regras, autorizações, contrato de saída ou seleção de ferramentas autorizadas.
- Prompts DEVEM delimitar claramente instruções e dados.

**Justificativa**: arquivos e documentos podem conter texto que tenta manipular o modelo (prompt injection).

**Verificação**
- Testes adversariais com arquivos e chunks contendo instruções maliciosas comprovam que o comportamento e o contrato não mudam.

### P9. Saída estruturada contratual e validada

**Regra**
- A resposta do LLM DEVE seguir um contrato JSON explícito e versionado (Seção 4.3).
- DEVE ser usado o mecanismo de saída estruturada compatível com o modelo configurado.
- Toda resposta DEVE passar por validação estrutural (schema) e semântica (regras de coerência, como P3 e P4) antes de ser aceita.
- Respostas inválidas DEVEM seguir política de retentativas limitada. Esgotadas as tentativas, o resultado DEVE ser retornado como falha técnica, sem aproveitar conteúdo inválido.
- O LLM NÃO DEVE fabricar evidências, resultados de ferramentas, achados de validadores ou referências. Toda referência citada DEVE corresponder a um chunk ou resultado efetivamente recuperado na execução.

**Justificativa**: o parecer é consumido por pessoas e por outros sistemas. Uma resposta fora do contrato ou com referência inventada compromete ambos.

**Verificação**
- Existe schema versionado do contrato e testes de validação com respostas válidas e inválidas.
- A validação semântica confere se cada referência citada existe no conjunto recuperado na execução.
- Testes cobrem o esgotamento de retentativas.

### P10. Metadados operacionais preenchidos pelo sistema

**Regra**
- O JSON produzido pelo LLM e o envelope retornado pela API DEVEM ser contratos distintos.
- Identificadores de execução e correlação, versão do contrato, consumo de tokens, custos, tempos, status técnico e resultados de validadores DEVEM ser preenchidos pelo sistema e NÃO DEVEM ser gerados pelo modelo.

**Justificativa**: o modelo não conhece esses valores e os inventaria.

**Verificação**
- O schema da saída do LLM não contém campos operacionais.
- O envelope da API é montado no código da aplicação, com testes.

### P11. Ferramentas controladas

**Regra**
Cada ferramenta (função do Semantic Kernel, ferramenta MCP ou serviço externo) DEVE possuir:

- Nome e responsabilidade únicos e claros.
- Contrato de entrada e saída.
- Validação de argumentos.
- Regras de autenticação e autorização.
- Timeout e tratamento de falhas.
- Política de retentativa e idempotência, quando aplicável.
- Testes e registro de execução.

Além disso:

- A seleção de uma ferramenta pelo LLM NÃO DEVE conceder autorização para executá-la. A autorização DEVE ser verificada pelo sistema, com base no usuário, no contexto e na política da ferramenta.
- Ferramentas externas só DEVEM ser executadas quando previstas na especificação e autorizadas.
- Falhas de ferramentas DEVEM resultar em achado de natureza "falha técnica", nunca em resultado presumido.

**Justificativa**: function calling dá ao modelo capacidade de ação. Essa capacidade precisa de limites explícitos.

**Verificação**
- Existe registro (catálogo) das ferramentas com os itens acima.
- Testes comprovam rejeição de argumentos inválidos, de chamada não autorizada e tratamento de timeout.

### P12. Fronteiras de serviço e propriedade de dados

**Regra**
- A comunicação funcional entre Api.Agente e Api.RAG DEVE ocorrer por MCP.
- Cada serviço DEVE ser proprietário dos seus dados. A Api.Agente NÃO DEVE acessar diretamente tabelas, vetores ou armazenamento interno da Api.RAG.
- Cada serviço DEVE separar endpoints, aplicação, domínio e infraestrutura. O domínio NÃO DEVE depender de infraestrutura, frameworks de IA ou provedores externos.

**Justificativa**: preserva a evolução independente dos serviços e mantém as regras fiscais testáveis sem dependências externas.

**Verificação**
- A Api.Agente não possui string de conexão nem referência a repositórios do banco da Api.RAG.
- Testes de arquitetura (ou revisão de referências entre projetos) comprovam o sentido das dependências.

### P13. Observabilidade, consumo e custos

**Regra**
- Toda execução DEVE ter identificador de execução e de correlação propagados entre Api.Agente, Api.RAG, ferramentas e chamadas ao provedor.
- Toda chamada ao provedor de LLM ou de embeddings, incluindo retentativas e chamadas adicionais, DEVE registrar consumo conforme a Seção 4.5.
- Logs NÃO DEVEM conter dados sensíveis sem mascaramento (ver Seção 5.1).

**Justificativa**: custo e comportamento do agente só são controláveis se forem mensuráveis.

**Verificação**
- Testes de integração comprovam o registro de consumo, inclusive em retentativas.
- Uma execução pode ser reconstruída a partir do identificador de correlação.

### P14. Memória conversacional isolada e governada

**Regra**
- A memória DEVE ser isolada por usuário e por conversa, e por organização quando aplicável.
- DEVEM existir políticas de retenção, expiração e exclusão.
- O histórico enviado ao modelo DEVE ser limitado conforme parâmetro definido no plano técnico.
- O histórico conversacional NÃO DEVE ser usado como fonte de conhecimento oficial nem como evidência no parecer.
- A tecnologia (MongoDB ou Redis) DEVE ser escolhida por ADR antes da implementação.

**Justificativa**: evita vazamento entre usuários e evita que conversas contaminem a fundamentação do parecer.

**Verificação**
- Testes comprovam que um usuário não acessa a memória de outro.
- Testes comprovam expiração e exclusão.
- O ADR da tecnologia existe e está aprovado antes do primeiro PR de memória.

### P15. Revisão humana

**Regra**
- O resultado DEVE indicar se requer revisão humana e por quê.
- A revisão humana DEVE ser indicada, no mínimo, quando houver erro grave, indício da IA, pré-condição de entrada não atendida, ausência de evidências, falha técnica ou validação obrigatória não realizada. Critérios adicionais DEVEM ser definidos na especificação.

**Justificativa**: o parecer apoia uma decisão profissional e não a substitui.

**Verificação**
- A validação semântica rejeita resultado sem indicação de revisão humana quando algum dos critérios mínimos estiver presente.

### P16. Simplicidade e complexidade justificada

**Regra**
- A solução DEVE conter apenas os serviços Api.Agente e Api.RAG, além de projetos de teste.
- Projetos, bibliotecas, serviços ou dependências adicionais DEVEM ser justificados no plano técnico ou em ADR, indicando o problema resolvido e a alternativa mais simples descartada.
- Abstrações NÃO DEVEM ser criadas para necessidades hipotéticas.

**Justificativa**: reduz custo de manutenção em um domínio que já é complexo.

**Verificação**
- A seção "Verificação da Constitution" do plano lista cada projeto e dependência adicional com justificativa.

### P17. Testabilidade e critérios de aceite

**Regra**
- Cada requisito DEVE ter critérios de aceite verificáveis.
- Cada regra do catálogo DEVE ter testes com casos positivo e negativo, usando arquivos de exemplo representativos e sem dados reais não anonimizados.
- Comportamentos que dependem do LLM DEVEM ser avaliados com conjunto de avaliação versionado. Os critérios de aprovação DEVEM ser definidos no plano técnico.
- Testes automatizados NÃO DEVEM depender de chamadas reais ao provedor de LLM. Avaliações com o modelo real DEVEM ser executadas separadamente.

**Justificativa**: sem testes, as regras fiscais regridem a cada mudança de layout ou de modelo.

**Verificação**
- A matriz de rastreabilidade (Seção 6.2) liga requisito, regra, tarefa e teste.
- A pipeline de CI executa os testes sem acesso ao provedor de LLM.

### P18. Contrato de entrada com os agentes de origem

**Regra**
- As obrigações DEVEM ser recebidas em JSON normalizado, conforme contrato explícito e versionado (versionamento semântico), acordado com os agentes de origem (D24).
- Cada obrigação recebida DEVE conter, no mínimo: tipo de obrigação, versão do layout, contribuinte (CNPJ e, quando aplicável, estabelecimento/IE), período de referência, indicação de original ou substituta/retificadora, identificação da escrituração ou documento (recibo, hash ou chave de acesso), relatório de validação de origem e os registros e campos normalizados.
- O relatório de validação de origem DEVE informar, no mínimo: agente e versão do validador que o produziu, data da validação, status (aprovado ou reprovado) e as ocorrências, distinguindo erros de advertências.
- Uma obrigação só é **apta ao cruzamento** quando o JSON for válido contra o contrato, a versão de layout for suportada e a validação de origem estiver aprovada, conforme critério definido na especificação (D25).
- Uma obrigação não apta DEVE gerar achado de natureza "pré-condição de entrada não atendida". Os cruzamentos que dependem dela DEVEM ser reportados como "ausência de evidências".
- Advertências da validação de origem PODEM ser repassadas ao parecer como alerta de atenção, preservando o texto e a origem, sem reclassificação pelo LLM.
- Campos de data, valor, CNPJ e códigos DEVEM ter tipos e formatos definidos no contrato. CNPJ DEVE ser tratado como texto alfanumérico.

**Justificativa**: o agente depende integralmente da qualidade da entrada. Sem contrato e sem critério de aptidão, um erro de layout não detectado na origem viraria um falso erro de cruzamento.

**Verificação**
- Existe schema versionado do contrato de entrada e testes com entradas válidas, inválidas, sem versão, com validação reprovada e sem relatório.
- Testes comprovam que obrigação não apta não gera inconsistência de cruzamento nem conformidade.

---

## 3. Restrições arquiteturais

### 3.1 Plataforma

- A solução DEVE se chamar **infis-agente-parecer-tributário** e usar **.NET 10** com **ASP.NET Core**.
- Versões de SDKs e bibliotecas DEVEM ser definidas no plano técnico, não nesta constitution.

### 3.2 Api.Agente

Responsabilidades:

- Disponibilizar API REST e servidor A2A para execução do agente.
- Receber obrigações normalizadas, com relatórios de validação de origem, e solicitações de análise (P18).
- Orquestrar o fluxo de validação (Seção 3.4).
- Integrar-se à OpenAI.
- Consultar a Api.RAG como cliente MCP.
- Usar Semantic Kernel para integração com o modelo e execução de ferramentas por function calling.
- Gerenciar memória conversacional (P14).
- Registrar consumo de tokens e custos em PostgreSQL (Seção 4.5).
- Validar a resposta estruturada do LLM (P9).
- Expor o agente por servidor A2A, com descrição de capacidades (Agent Card) versionada.

A Api.Agente NÃO DEVE armazenar nem consultar vetores de conhecimento.

### 3.3 Api.RAG

Responsabilidades:

- Disponibilizar API REST para administração da base de conhecimento.
- Disponibilizar endpoint POST de ingestão de documentos.
- Processar documentos, gerar chunks e embeddings.
- Armazenar e consultar vetores em PostgreSQL com PgVector.
- Expor ferramentas de recuperação de conhecimento por servidor MCP.

A Api.RAG NÃO DEVE gerar o parecer nem executar validação de obrigações.

### 3.4 Fluxo obrigatório de validação

Toda análise DEVE seguir, em ordem:

1. Receber as obrigações normalizadas e identificar usuário ou agente chamador, contexto, execução e correlação.
2. Validar tamanho do payload e o JSON contra o contrato de entrada (P18).
3. Conferir as pré-condições de cada obrigação: versão de layout suportada, validação de origem aprovada, escrituração vigente (original ou última substituta/retificadora).
4. Agrupar as obrigações aptas por contribuinte (ou estabelecimento) e período, reportando divergências (P5).
5. Executar as regras de cruzamento aplicáveis às obrigações aptas e reportar como "ausência de evidências" as regras cujas obrigações faltam ou não são aptas.
6. Consultar por MCP os chunks do manual correspondente.
7. Executar ferramentas externas, quando necessário e autorizado.
8. Produzir a análise com o LLM.
9. Validar a saída estruturada.
10. Retornar o resultado com evidências e rastreabilidade.

Regras do fluxo:

- A falha de uma etapa DEVE ser registrada no resultado. As etapas seguintes PODEM ser executadas apenas se não dependerem da etapa que falhou, e o resultado final DEVE refletir P4.
- O LLM NÃO DEVE ser chamado para preencher etapas determinísticas que falharam.

### 3.5 Integrações

- MCP é o único meio de comunicação funcional entre Api.Agente e Api.RAG (P12).
- A2A é a interface para outros agentes e orquestradores. Este agente é **servidor** A2A (não cliente/orquestrador).
- Perfil A2A: **v0.3.x JSON-RPC 2.0**, método `message/send`. Descoberta em `GET /.well-known/agent-card.json`. O campo `url` do card é o endpoint RPC (não o caminho do card).
- Mensagens A2A DEVEM passar pelas mesmas autorizações JWT, validações e contrato de lote (`lote.v1`) da API REST. O TextPart PODE ser o JSON do lote ou o envelope SOFIA v1 (`SolicitacaoAnaliseSofia`) com o lote em `documentos[].texto`.
- CORS aplica-se somente a origens de navegador configuradas; chamadas backend-backend da SOFIA NÃO dependem de CORS.

---

## 4. Contratos e governança dos dados

### 4.1 Fontes oficiais

- Todo documento oficial usado DEVE ser registrado com: emissor, título, versão, data de publicação, vigência (quando aplicável), origem (URL ou referência) e hash do arquivo ingerido.
- Documentos substituídos DEVEM permanecer identificáveis enquanto houver períodos de apuração que dependam deles.

### 4.2 Catálogo de Regras

As regras de validação DEVEM ser mantidas em um catálogo versionado, registrado nas especificações, com no mínimo:

- Identificador único e estável.
- Tipo: pré-condição de entrada ou cruzamento.
- Obrigações e versões de layout envolvidas.
- Registros e campos envolvidos.
- Condição de aplicabilidade e vigência.
- Descrição do resultado esperado.
- Natureza do achado gerado e severidade (P3).
- Fonte oficial e localização (P1).
- Implementação: determinística (validador) ou assistida por IA, com justificativa quando não for determinística.
- Testes associados.

O formato físico do catálogo é decisão do plano técnico.

### 4.3 Contrato da resposta do LLM

O contrato DEVE ser explícito, versionado (versionamento semântico) e conter, conforme a especificação:

- Status da análise.
- Resultado da análise (parecer).
- Achados, com natureza, severidade, regra relacionada (quando houver), descrição e localização.
- Evidências e referências, vinculadas a chunks ou resultados efetivamente recuperados.
- Limitações e validações não realizadas.
- Indicação de revisão humana e motivos.

O contrato NÃO DEVE conter campos operacionais (P10).

### 4.4 Envelope da API

O envelope retornado pela API e por A2A DEVE conter, preenchidos pelo sistema:

- Identificador da execução e de correlação.
- Versão do contrato e do envelope.
- Status técnico do processamento.
- Resultados dos validadores determinísticos.
- Resposta validada do LLM, quando houver.
- Consumo, custos e tempos, ou referência a eles.

Mudanças incompatíveis em contratos DEVEM incrementar a versão MAJOR e prever período de convivência, definido no plano.

### 4.5 Consumo e custos

O consumo DEVE ser persistido em PostgreSQL, registrando por chamada, quando disponibilizado pelo provedor:

- Execução e correlação.
- Provedor e modelo.
- Tokens de entrada e de saída.
- Consumo de embeddings, quando aplicável.
- Data, duração e status.
- Estimativa de custo e referência da tabela de preços utilizada.

Regras:

- Chamadas adicionais e retentativas DEVEM ser registradas.
- Valores estimados DEVEM ser distinguidos de valores informados pelo provedor.
- Consumo desconhecido NÃO DEVE ser registrado como zero. DEVE ser registrado como desconhecido.
- A tabela de preços DEVE ser versionada e configurável, nunca fixa no código.

### 4.6 Propriedade dos dados

| Dado | Proprietário | Armazenamento |
|---|---|---|
| Documentos oficiais, chunks e vetores | Api.RAG | PostgreSQL + PgVector |
| Consumo e custos | Api.Agente | PostgreSQL |
| Memória conversacional | Api.Agente | MongoDB ou Redis (ADR pendente) |
| Obrigações normalizadas e relatórios de validação recebidos | Api.Agente | Decisão pendente |

---

## 5. Segurança e qualidade

### 5.1 Segurança e privacidade

- Endpoints REST, MCP e o RPC A2A DEVEM exigir autenticação e autorização, exceto endpoints de saúde explicitamente declarados e o Agent Card `GET /.well-known/agent-card.json` (descoberta A2A v0.3.x).
- Segredos (chaves da OpenAI, strings de conexão) NÃO DEVEM estar no código-fonte nem em arquivos versionados.
- Obrigações recebidas contêm dados fiscais e pessoais. O tratamento DEVE seguir a LGPD, com política de retenção e exclusão definida na especificação.
- Dados sensíveis NÃO DEVEM ser registrados em logs sem mascaramento.
- O envio de dados ao provedor de LLM DEVE se limitar ao necessário para a análise. A política de minimização DEVE ser definida na especificação.
- Payloads recebidos DEVEM ter limites de tamanho e profundidade, validação contra o contrato de entrada e rejeição de campos não previstos ou tipos inválidos.
- O endpoint de ingestão da Api.RAG DEVE ser restrito a perfis administrativos.

### 5.2 Qualidade

- Configuração do modelo e dos parâmetros de execução DEVE ser externa ao código.
- Chamadas externas DEVEM ter timeout explícito.
- Erros DEVEM ser retornados em formato padronizado, sem expor detalhes internos.
- Código DEVE compilar sem avisos tratados como erro conforme definido no plano, e passar por análise estática configurada na solução.
- Prompts DEVEM ser versionados junto ao código e alterados por PR.

---

## 6. Fluxo de desenvolvimento SDD

### 6.1 Etapas

constitution → especificação → esclarecimentos → plano técnico → tarefas → implementação → validação

- **Especificação**: o que e por quê. DEVE conter obrigações envolvidas, fontes oficiais, regras do catálogo, critérios de aceite e itens marcados como pendentes. NÃO DEVE conter decisões de implementação.
- **Esclarecimentos**: toda ambiguidade ou decisão pendente que afete o escopo DEVE ser resolvida ou explicitamente adiada antes do plano.
- **Plano técnico**: como. DEVE conter a seção "Verificação da Constitution", indicando o atendimento a P1–P17 e os desvios propostos.
- **Tarefas**: DEVEM referenciar os requisitos e as regras que implementam, e os testes que as comprovam.
- **Implementação**: PRs DEVEM referenciar tarefas e especificação.
- **Validação**: critérios de aceite comprovados por testes ou avaliação registrada.

### 6.2 Rastreabilidade

DEVE existir rastreabilidade entre requisito → regra do catálogo → tarefa → teste. Um requisito sem teste associado NÃO DEVE ser considerado concluído.

### 6.3 ADRs

Decisões arquiteturais relevantes DEVEM ser registradas em ADR com: contexto, opções consideradas, decisão, consequências, data e status. São obrigatórias, no mínimo, as decisões pendentes da Seção 9 marcadas como "ADR".

---

## 7. Definition of Done

Um item só está concluído quando:

1. Atende aos critérios de aceite da especificação.
2. Regras implementadas constam no Catálogo de Regras com fonte oficial (P1).
3. Validações determinísticas possuem testes com casos positivo e negativo (P2, P17).
4. Achados seguem natureza e severidade previstas (P3).
5. Há testes de falha comprovando que não há conformidade presumida (P4).
6. Contratos alterados estão versionados e validados (P9, P10).
7. Ferramentas novas atendem a P11, com testes de autorização e timeout.
8. Consumo e custos são registrados para novas chamadas a provedores (P13).
9. Não há segredos, dados reais não anonimizados ou dados sensíveis em logs.
10. Fronteiras de serviço e de camadas foram respeitadas (P12).
11. Testes automatizados passam na CI sem dependência do provedor de LLM.
12. Mudanças em prompts, parâmetros de RAG ou modelo têm resultado do conjunto de avaliação registrado.
13. ADRs e documentação afetados estão atualizados.
14. O PR foi revisado com o checklist de princípios desta constitution.

---

## 8. Governança e alterações

### 8.1 Precedência

Esta constitution prevalece sobre especificações, planos, tarefas e práticas. Conflitos DEVEM ser resolvidos alterando o artefato inferior ou registrando desvio.

### 8.2 Alterações

- Alterações DEVEM ser feitas por PR, com versão, data e avaliação de impacto nas especificações, planos e templates existentes.
- Versionamento semântico:
  - **MAJOR**: remoção ou redefinição incompatível de princípio ou restrição.
  - **MINOR**: novo princípio, seção ou ampliação relevante de regra.
  - **PATCH**: correções de redação sem mudança de significado.

### 8.3 Desvios

Desvios DEVEM ser registrados com: princípio afetado, justificativa, impacto, responsável, aprovação e, quando temporários, prazo de revisão. Desvios sem aprovação bloqueiam o merge.

### 8.4 Revisão de conformidade

- Todo PR DEVE ser revisado quanto ao atendimento dos princípios aplicáveis.
- Esta constitution DEVE ser reavaliada quando houver mudança relevante de layout oficial, de provedor de LLM ou de protocolo (MCP, A2A).

---

## 9. Decisões pendentes

Nenhum item abaixo é requisito aprovado. Cada um DEVE ser resolvido na etapa indicada antes de ser usado.

| # | Decisão | Etapa | Forma |
|---|---|---|---|
| D1 | Lista de obrigações fiscais e tributárias no escopo inicial, suas versões de layout e os pares de obrigações a cruzar | Especificação | Especificação |
| D2 | Matriz de cruzamentos entre obrigações e catálogo inicial de regras, com fontes oficiais | Especificação / Esclarecimentos | Catálogo de Regras |
| D3 | Documentos oficiais a ingerir (manuais, guias práticos, schemas, notas técnicas) e responsável pela curadoria | Especificação | Especificação |
| D4 | Critérios de severidade além de "erro grave" e "alerta de atenção" (por exemplo, nível informativo) | Esclarecimentos | Especificação |
| D5 | Critérios adicionais de revisão humana | Especificação | Especificação |
| D6 | Tamanho máximo do payload, envio de múltiplas obrigações e períodos por execução, e entrega incremental (obrigações chegando em momentos diferentes) | Especificação | Especificação |
| D7 | Processamento síncrono ou assíncrono para arquivos grandes | Plano técnico | ADR |
| D8 | Armazenamento e retenção dos arquivos recebidos | Plano técnico | ADR |
| D9 | MongoDB ou Redis para memória conversacional | Plano técnico | ADR |
| D10 | Modelo da OpenAI, parâmetros de execução e mecanismo de saída estruturada | Plano técnico | ADR |
| D11 | Modelo de embeddings, dimensão, métrica de distância e estratégia de busca (vetorial ou híbrida) | Plano técnico | ADR |
| D12 | Tamanho de chunk, sobreposição, topK, limiares e conjunto de avaliação de recuperação | Plano técnico | Plano + avaliação |
| D13 | Transporte e autenticação do MCP entre Api.Agente e Api.RAG | Plano técnico | ADR |
| D14 | Resolvido em `docs/adr/D14-a2a.md` (A2A v0.3.x JSON-RPC, Agent Card anônimo, JWT no RPC) | — | ADR |
| D15 | Provedor de identidade, modelo de autorização e suporte a múltiplas organizações | Plano técnico | ADR |
| D16 | Ferramentas externas permitidas e suas políticas | Especificação | Especificação + catálogo de ferramentas |
| D17 | Política de retentativas (quantidade, intervalo) para LLM e ferramentas | Plano técnico | Plano |
| D18 | Tabela de preços, moeda e forma de atualização | Plano técnico | Plano |
| D19 | Políticas de retenção, minimização e exclusão conforme LGPD | Especificação | Especificação com aprovação de compliance |
| D20 | Organização física das camadas (projetos separados ou pastas) e eventuais bibliotecas compartilhadas de contratos | Plano técnico | ADR |
| D21 | Formato físico do Catálogo de Regras | Plano técnico | Plano |
| D22 | Ferramentas de testes, avaliação de LLM e critérios mínimos de aprovação | Plano técnico | Plano |
| D23 | Observabilidade (logs, métricas, rastreamento) e ambiente de implantação | Plano técnico | ADR |
| D24 | Contrato de entrada (schema JSON normalizado por obrigação e do relatório de validação), acordado com os responsáveis pelos agentes de origem | Especificação | Especificação + schema versionado |
| D25 | Critério de aptidão ao cruzamento: quais status e ocorrências da validação de origem bloqueiam o cruzamento e quais são repassados como alerta | Especificação / Esclarecimentos | Especificação |
| D26 | Responsabilidade pelas regras internas que não são validadas pelo PVA/SEFAZ (ex.: continuidade de saldos entre períodos, inventário de fevereiro): agentes de origem ou este agente | Esclarecimentos | Especificação |
