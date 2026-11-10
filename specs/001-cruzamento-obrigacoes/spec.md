# Especificação: Cruzamento entre Obrigações Fiscais e Parecer Técnico

**Identificador:** `001-cruzamento-obrigacoes`
**Criada em:** 2026-10-29
**Status:** Rascunho – aguardando esclarecimentos (Seção 9)
**Constitution:** 2.0.0
**Entrada:** "Nós vamos receber essas obrigações de outros agentes após validação do layout e nossa função será verificar as relações de cada obrigação entre elas."

> Esta especificação descreve **o que** o agente faz e **por quê**. Decisões de implementação (formato físico do catálogo, organização de projetos, modelos de IA, parâmetros de RAG) pertencem ao plano técnico.

---

## 1. Contexto e objetivo

O agente recebe, de **agentes de origem**, obrigações fiscais e tributárias **já validadas quanto ao layout**, em JSON normalizado com o relatório de validação de cada uma. Sua função é verificar se os **mesmos fatos** aparecem nas obrigações **onde devem aparecer**, **com os mesmos valores** (ou com a agregação correta) e **não aparecem onde não devem**, e emitir um parecer técnico fundamentado.

O agente **não** lê arquivos originais, **não** valida layout e **não** refaz regras internas das obrigações (constitution 1.1 e 1.2).

### 1.1 Obrigações no escopo (D1)

| Obrigação | Versões de layout suportadas no escopo inicial | Granularidade | Período |
|---|---|---|---|
| NF-e / NFC-e (modelos 55 e 65) | MOC leiaute 4.00 + NTs vigentes (README §2) | Documento | Instantâneo (`dhEmi`, `dhSaiEnt`) |
| NFS-e Padrão Nacional | XSD v1.01 (20260209), Anexo I v1.01 | Documento | Competência (`dCompet`) |
| DIMP | Versão 10 (a partir de 01/06/2026); Versão 09 até 31/05/2026 | Declarante × UF | Mensal |
| EFD ICMS/IPI | Leiaute 020 (2026), Guia Prático v3.2.2 | Estabelecimento (CNPJ + IE) | Mensal |
| EFD-Contribuições | Guia Prático v1.35 | CNPJ matriz (estabelecimentos no `0140`) | Mensal |
| ECD | Leiaute 9 (`COD_VER_LC` 9.00) | CNPJ | Anual / situação especial |
| ECF | Leiaute 12 | CNPJ | Anual / trimestral / situação especial |

Fora do escopo inicial: CT-e, NFCom, BP-e e Bloco D das EFDs; Bloco I (instituições financeiras) e Bloco P (CPRB) da EFD-Contribuições; vínculo NF-e × transação de *split payment* (NT 2026.006 – só após vigência).

### 1.2 Base de conhecimento usada por esta especificação

| Documento | Papel |
|---|---|
| `docs/README.md` | Versões oficiais verificadas, tipos de evidência, escala de severidade, fontes |
| `docs/01-panorama-obrigacoes.md` | Registros e campos de ligação de cada obrigação (mínimo que o contrato de entrada deve trazer) |
| `docs/02-regras-intra-obrigacao.md` | Pré-condições de entrada (`INT-COM-002/003/004`); demais regras pertencem à origem |
| `docs/03-regras-cruzamento.md` | Catálogo proposto de regras de cruzamento (`CRZ-*`) e premissas de casamento |
| `docs/04-reforma-tributaria-e-mudancas-2026.md` | Vigências e mudanças que condicionam regras (IBS/CBS, CNPJ alfanumérico, NT 2023.002) |
| `docs/05-modelo-regras-agente.md` | Modelo proposto de regra, ocorrência e estrutura do parecer |
| `docs/NUCLEO-1-principios-entidades-matriz.md` | Tipos de relação, ordem de execução, regras de entidade (`ENT-*`), matriz de presença e de parâmetros |
| `docs/NUCLEO-2-mapas-bidirecionais.md` | Mapas campo a campo A ⇄ B e exclusividades legítimas (M1 a M12) |

---

## 2. Cenários de usuário e testes

Atores: **agente de origem** (entrega as obrigações), **orquestrador** (solicita a análise por A2A), **analista fiscal** (consome o parecer e faz a revisão humana), **curador de regras** (aprova regras do catálogo).

### US1 – Receber obrigações validadas e conferir aptidão (Prioridade: P1)

O orquestrador envia um lote com obrigações normalizadas de um contribuinte. O agente valida o contrato, confere as pré-condições e informa quais obrigações estão aptas ao cruzamento.

**Por que esta prioridade:** sem entrada confiável, todo cruzamento gera falso positivo (P18). É o MVP mínimo demonstrável.

**Teste independente:** enviar lotes com obrigações válidas, inválidas, sem versão, reprovadas na origem e de outro contribuinte, e verificar o relatório de aptidão sem executar nenhuma regra de cruzamento.

**Cenários de aceite**
1. **Dado** um lote com EFD ICMS/IPI e EFD-Contribuições válidas, aprovadas na origem, do mesmo CNPJ e mês, **quando** o lote é recebido, **então** ambas são marcadas como aptas e agrupadas no mesmo contribuinte/período.
2. **Dado** uma obrigação sem versão de layout, **quando** recebida, **então** é gerado achado "pré-condição de entrada não atendida" e ela não é cruzada.
3. **Dado** uma obrigação com relatório de validação reprovado, **quando** recebida, **então** ela não é apta e os cruzamentos que dependem dela aparecem como "ausência de evidências".
4. **Dado** uma obrigação aprovada com advertências, **quando** recebida, **então** ela é apta e cada advertência é repassada como alerta, com o texto e a origem preservados.
5. **Dado** duas EFD ICMS/IPI do mesmo estabelecimento e mês (original e substituta), **quando** recebidas, **então** apenas a última escrituração vigente é usada e a outra é reportada como descartada.
6. **Dado** obrigações com raiz de CNPJ diferente no mesmo lote, **quando** recebidas, **então** a divergência é reportada e elas não são cruzadas entre si.
7. **Dado** um JSON que não respeita o contrato de entrada, **quando** recebido, **então** o lote (ou a obrigação, conforme FR-002) é rejeitado com erro padronizado e nenhuma conformidade é declarada.

### US2 – Verificar enquadramento e entidades compartilhadas (Prioridade: P1)

Antes de comparar valores, o agente verifica se regime tributário, contribuinte, estabelecimentos, participantes e contas contábeis são coerentes entre as obrigações.

**Por que esta prioridade:** uma incoerência de regime ou de entidade invalida todas as conclusões de valor (NUCLEO-1 §2 e §5).

**Teste independente:** lote com ECF de lucro presumido e EFD-Contribuições não cumulativa; lote com estabelecimento da EFD ICMS/IPI ausente do `0140`.

**Cenários de aceite**
1. **Dado** ECF com `FORMA_TRIB = 5` (presumido) e EFD-Contribuições com `COD_INC_TRIB = 1`, **quando** analisadas, **então** é gerado erro grave (CRZ-REG-002) e as regras de valor dependentes ficam marcadas como não conclusivas.
2. **Dado** EFD ICMS/IPI de um estabelecimento que não consta do `0140` da EFD-Contribuições do mesmo mês, **quando** analisadas, **então** é gerado achado ENT-001/CRZ-EFD-CTB-002.
3. **Dado** um `COD_CTA` da EFD-Contribuições inexistente no `I050` da ECD e uma ECD com `I157` mapeando o código antigo, **quando** analisadas, **então** não é gerado erro (hipótese legítima de ENT-030 aplicada) e o fato é registrado no parecer.

### US3 – Cruzar documentos fiscais com a EFD ICMS/IPI (Prioridade: P2)

O agente verifica se cada NF-e/NFC-e está escriturada no `C100` correto, com a situação e os valores do XML, e se cada `C100` modelo 55/65 tem NF-e válida.

**Teste independente:** conjunto de NF-e (autorizadas, canceladas, denegadas, de terceiros) e a EFD ICMS/IPI do mês.

**Cenários de aceite**
1. **Dado** uma NF-e de saída própria autorizada sem `C100` correspondente na EFD do mês, **quando** analisadas, **então** é gerado erro grave de omissão (CRZ-NFE-EFD-001) com a chave da NF-e e o período.
2. **Dado** uma NF-e com evento de cancelamento homologado escriturada com `COD_SIT = 00`, **quando** analisadas, **então** é gerado erro grave (CRZ-NFE-EFD-002).
3. **Dado** uma NF-e de terceiros escriturada no mês seguinte ao da emissão, **quando** as EFDs dos dois meses foram recebidas, **então** é gerado alerta de deslocamento temporal, e não erro (CRZ-NFE-EFD-006).
4. **Dado** uma NF-e de terceiros e **somente** a EFD do mês da emissão recebida, **quando** a NF-e não está nela, **então** o resultado é "ausência de evidências" para o mês seguinte, e não omissão.
5. **Dado** `C100.VL_ICMS` de emissão própria menor que `vICMS` do XML, **quando** analisadas, **então** é gerado erro grave (CRZ-NFE-EFD-004) com os dois valores e a diferença.

### US4 – Cruzar documentos fiscais e EFD ICMS/IPI com a EFD-Contribuições (Prioridade: P2)

O agente verifica se receitas documentadas (NF-e, NFS-e) estão na EFD-Contribuições, conforme o regime (`C010.IND_ESCRI`, `0110.IND_REG_CUM`), com CST coerente, e se retenções deduzidas têm origem.

**Cenários de aceite**
1. **Dado** NF-e de venda autorizada e EFD-Contribuições individualizada sem o documento, **quando** analisadas, **então** é gerado erro grave de receita omitida (CRZ-NFE-CTB-001).
2. **Dado** PJ do presumido em regime de caixa (`IND_REG_CUM = 1`), **quando** a NF-e não está no Bloco C, **então** não é gerada omissão (exclusividade legítima) e a receita é buscada em `F500/F510/F525`.
3. **Dado** NFS-e emitida, não cancelada nem substituída, sem `A100` nem composição em `F500/F550`, **quando** analisadas, **então** é gerado erro grave (CRZ-NFS-CTB-001).
4. **Dado** dedução de retenção em `M200` maior que o total retido nas NFS-e e no `F600`, **quando** analisadas, **então** é gerado erro grave (CRZ-NFS-CTB-003).

### US5 – Cruzar contabilidade (ECD) com ECF e com as escriturações fiscais (Prioridade: P3)

O agente verifica a recuperação da ECD na ECF (hash, saldos, mapeamento referencial, lucro antes do IRPJ/CSLL) e as pontes fiscal → contábil (contas, inventário, tributos a recolher, receitas).

**Cenários de aceite**
1. **Dado** ECF cujo `C040` referencia hash diferente da última ECD válida recebida, **quando** analisadas, **então** é gerado erro grave (CRZ-ECD-ECF-001).
2. **Dado** `K155` da ECF diferente do `I155` da ECD para a mesma conta e período, **quando** analisadas, **então** é gerado erro grave (CRZ-ECD-ECF-002) com conta, período e valores.
3. **Dado** inventário de 31/12 (`H005` da EFD de fevereiro, itens `IND_PROP` 0 e 1, somado entre estabelecimentos) diferente do saldo das contas de estoque da ECD, **quando** analisadas, **então** é gerado achado CRZ-EFD-ECD-002 citando as hipóteses legítimas.
4. **Dado** que a EFD de fevereiro do ano seguinte não foi recebida, **quando** CRZ-EFD-ECD-002 é avaliada, **então** o resultado é "ausência de evidências".

### US6 – Cruzar DIMP com documentos e com o registro 1601 (Prioridade: P3)

O agente verifica se o recebido por meio eletrônico (declarado por terceiros na DIMP) tem lastro nos documentos e no `1601` da EFD ICMS/IPI.

**Cenários de aceite**
1. **Dado** declarante da DIMP sem `1601` correspondente (`COD_PART_IP`/`COD_PART_IT` → `0150.CNPJ`) no período, **quando** analisadas, **então** é gerado achado CRZ-DIMP-EFD-001.
2. **Dado** DIMP com total maior que `1601.TOT_VS + TOT_ISS + TOT_OUTROS`, **quando** analisadas, **então** é gerado achado CRZ-DIMP-EFD-002 com as hipóteses legítimas (estornos, diferença temporal, outro estabelecimento).
3. **Dado** que nenhuma DIMP foi recebida, **quando** a análise roda, **então** todas as regras `CRZ-DIMP-*` aparecem como "ausência de evidências" e nenhuma omissão de saída é inferida.

### US7 – Emitir parecer técnico fundamentado (Prioridade: P2)

Com os achados determinísticos, o agente consulta os manuais por RAG (MCP) e produz, com o LLM, um parecer estruturado e validado, com fontes, limitações e indicação de revisão humana.

**Cenários de aceite**
1. **Dado** achados determinísticos, **quando** o parecer é gerado, **então** natureza, severidade e regra de cada achado são idênticas às do validador (o LLM não reclassifica).
2. **Dado** achado de regra [DERIVADA], **quando** redigido, **então** o parecer lista as hipóteses legítimas antes de qualquer conclusão.
3. **Dado** uma referência citada pelo LLM que não está entre os chunks recuperados, **quando** a resposta é validada, **então** ela é rejeitada e reprocessada até o limite de retentativas; esgotado o limite, o **parecer** é "falha técnica" e nenhum conteúdo do modelo é aproveitado; os achados determinísticos são retornados com o status do motor (`CONCLUIDA` passa a `CONCLUIDA_PARCIAL`), a falha consta nas limitações e a revisão humana é indicada (constitution 2.2.0).
4. **Dado** a Api.RAG indisponível, **quando** o parecer é solicitado, **então** os achados determinísticos são retornados, a fundamentação aparece como validação não realizada e o status não é de conformidade.
5. **Dado** qualquer erro grave, pré-condição não atendida, ausência de evidências, indício da IA ou falha técnica, **quando** o resultado é montado, **então** a revisão humana é indicada com os motivos.

### US8 – Atender outros agentes por A2A (Prioridade: P3)

O agente é descoberto e chamado por orquestradores por A2A, com as mesmas regras da API REST.

**Cenários de aceite**
1. **Dado** o Agent Card publicado, **quando** consultado, **então** descreve capacidades, versão do contrato de entrada e versão do contrato de saída.
2. **Dado** uma chamada A2A sem autenticação ou sem autorização, **quando** recebida, **então** é rejeitada.
3. **Dado** a mesma entrada enviada por REST e por A2A, **quando** processadas, **então** os achados determinísticos são idênticos.

### 2.1 Casos-limite

- Lote só com uma obrigação: nenhum cruzamento é possível; todas as regras aparecem como "ausência de evidências" e o status é inconclusivo.
- Lote com obrigações de períodos diferentes que não se sobrepõem (ex.: EFD de março e ECD do ano anterior): divergência de período reportada; nada é cruzado silenciosamente (P5).
- ECD/ECF anuais com EFDs de apenas alguns meses: regras anuais de agregação ficam "ausência de evidências" e listam os meses faltantes.
- Situação especial (cisão, fusão, incorporação, extinção): período da ECD/ECF é o da situação especial; agregação mensal usa apenas os meses do período (CRZ-REG-009, CRZ-ECD-ECF-006).
- CNPJ alfanumérico: casamento por texto normalizado sem máscara; nunca conversão numérica (doc 04 §2).
- NF-e com apenas IBS/CBS (sem ICMS/IPI) ausente da EFD ICMS/IPI: tratada pela exceção do Guia 3.2.x (CRZ-NFE-EFD-013), não como omissão.
- Inutilização (`COD_SIT = 05`) após a vigência da NT 2023.002 v1.02: regra CRZ-NFE-EFD-003 condicionada à data.
- Transferência entre estabelecimentos: deve aparecer nas EFD ICMS/IPI de remetente e destinatário e nunca compor receita (NUCLEO-1 §4.1).
- Texto em campos do JSON ou do relatório de validação contendo instruções: tratado como dado, sem efeito no comportamento (P8).

---

## 3. Requisitos funcionais

### 3.1 Entrada e pré-condições (P18)

- **FR-001**: O sistema DEVE receber, por REST e por A2A, um **lote de análise** com uma ou mais obrigações normalizadas, identificando usuário ou agente chamador, contexto, execução e correlação.
- **FR-002**: O sistema DEVE validar cada obrigação contra o **contrato de entrada** versionado (D24). Lote com estrutura inválida DEVE ser rejeitado por inteiro; obrigação individual inválida DEVE ser marcada como não apta, sem impedir as demais. [NEEDS CLARIFICATION C6]
- **FR-003**: Cada obrigação DEVE trazer, no mínimo: tipo, versão de layout, CNPJ (texto alfanumérico) e, quando aplicável, estabelecimento/IE, período de referência, indicador original ou substituta/retificadora, identificação (recibo, hash ou chave de acesso), relatório de validação e registros/campos normalizados.
- **FR-004**: O relatório de validação DEVE trazer agente e versão do validador, data, status e ocorrências separadas em erros e advertências.
- **FR-005**: O sistema DEVE aceitar somente as versões de layout listadas na Seção 1.1 e vigentes para o período informado; demais DEVEM gerar "pré-condição de entrada não atendida" (P6, INT-COM-002).
- **FR-006**: Obrigação com validação de origem reprovada, ou sem relatório, NÃO DEVE ser cruzada (D25). [NEEDS CLARIFICATION C7]
- **FR-007**: Advertências da validação de origem DEVEM ser repassadas como alerta de atenção, com texto e origem preservados.
- **FR-008**: Para o mesmo tipo de obrigação, estabelecimento (ou CNPJ) e período, o sistema DEVE usar apenas a última escrituração vigente e reportar as descartadas (INT-COM-003).
- **FR-009**: O sistema DEVE agrupar as obrigações aptas por raiz de CNPJ, estabelecimento e período, e reportar divergências de contribuinte ou de período sem cruzá-las (P5, INT-COM-004, ENT-004).
- **FR-010**: O sistema DEVE registrar a **cobertura** do lote: obrigações, estabelecimentos e períodos recebidos e aptos. Ausência de insumo NUNCA DEVE ser reportada como omissão (doc 05 §6).

### 3.2 Catálogo de regras

- **FR-011**: As regras DEVEM ser mantidas em catálogo versionado com os campos da constitution 4.2 e, adicionalmente: tipo de relação (`PAR`, `EXC`, `PRE→`, `←PRE`, `IDT`, `AGR`, `LIM`, `CNT` – NUCLEO-1 §2), tipo de evidência (`PVA`, `MANUAL`, `NORMA`, `DERIVADA`), hipóteses legítimas, regras de que depende e status (`proposta`, `aprovada`, `suspensa`).
- **FR-012**: Somente regras com status `aprovada` DEVEM ser executadas. Uma regra só pode ser aprovada com fonte oficial conferida no documento da versão correspondente (README – "Limite deste estudo") e com responsável pela aprovação registrado (P1).
- **FR-013**: Itens marcados como "(conferir)" ou "verificar" nos documentos de base NÃO DEVEM ser aprovados até a conferência na fonte primária.
- **FR-014**: Cada regra DEVE declarar a vigência (início/fim) e as versões de layout de cada obrigação envolvida; fora delas, a regra não se aplica e DEVE constar como "não aplicável" com o motivo.
- **FR-015**: Regras com o mesmo fato e o mesmo par de obrigações DEVEM ter um único identificador no catálogo. [NEEDS CLARIFICATION C3]

### 3.3 Execução dos cruzamentos

- **FR-016**: As regras DEVEM ser executadas na ordem `PAR` → `EXC` → `PRE→`/`←PRE` → `IDT` → `AGR`/`LIM` → `CNT` (NUCLEO-1 §2).
- **FR-017**: Falha em regra `PAR` ou de entidade DEVE marcar como **não conclusivas** as regras de valor que dependem dela (doc 05 §6).
- **FR-018**: Exclusividades legítimas (`EXC`, NUCLEO-1 §4 e "Exclusivos legítimos" do NUCLEO-2) DEVEM ser aplicadas antes das regras de presença.
- **FR-019**: O casamento de documentos DEVE usar a chave de acesso (NF-e: 44 posições; NFS-e: chave da NFS-e); na falta, CNPJ do emitente + modelo + série + número + data de emissão (doc 03 §0).
- **FR-020**: O casamento de participantes DEVE usar o CNPJ/CPF resolvido via `0150`, nunca o `COD_PART` (NUCLEO-1 §3.2).
- **FR-021**: O sentido da operação DEVE seguir a tabela de correspondência `tpNF` × papel do CNPJ × `IND_OPER` × `IND_EMIT` (NUCLEO-1 §3.5).
- **FR-022**: Totais da EFD ICMS/IPI DEVEM ser somados entre estabelecimentos antes de comparar com obrigações centralizadas; comparações mensal × anual DEVEM agregar os meses do período (ou da situação especial) (doc 03 §0).
- **FR-023**: A data de referência para alocação ao período DEVE seguir doc 03 §0 (saídas: emissão/saída; entradas na EFD ICMS/IPI: `DT_E_S`; NFS-e: `dCompet`; ECD: competência).
- **FR-024**: Comparações de valor DEVEM usar **tolerância** configurável, informada no parecer. Nenhuma norma define tolerância (doc 03 §0). [NEEDS CLARIFICATION C5]
- **FR-025**: Regras que preveem limite de **materialidade** ("ALERTA; material: ESCRITURACAO_GRAVE") DEVEM usar parâmetro configurável e informado no parecer. [NEEDS CLARIFICATION C11]
- **FR-026**: Regra cuja obrigação necessária não foi recebida ou não é apta DEVE resultar em "ausência de evidências", listando a obrigação e o período faltantes.
- **FR-027**: Regra com hipótese de deslocamento temporal (ex.: CRZ-NFE-EFD-006) DEVE avaliar o período seguinte quando recebido; sem ele, o resultado é "ausência de evidências" para o deslocamento.
- **FR-028**: Todo achado DEVE apontar a evidência nos dois lados: identificador da obrigação, registro, ocorrência e campo (e linha do arquivo original quando a origem informar), além dos valores comparados e da diferença.
- **FR-029**: Os cruzamentos DEVEM ser determinísticos e reprodutíveis: mesma entrada e mesma versão do catálogo produzem os mesmos achados.

### 3.4 Classificação dos achados (P3)

- **FR-030**: Todo achado DEVE ter uma natureza entre: pré-condição de entrada não atendida; inconsistência de cruzamento; indício identificado pela IA; ausência de evidências; falha técnica ou dependência indisponível.
- **FR-031**: A severidade de achados de cruzamento DEVE vir do catálogo. Mapeamento provisório da escala do README §4 para a constitution: `ESCRITURACAO_GRAVE` → erro grave; `ALERTA` → alerta de atenção; `INFO` → pendente de D4; `ESTRUTURA_GRAVE` em regras de cruzamento → pendente de C2. [NEEDS CLARIFICATION C1, C2]
- **FR-032**: Achados de regras [DERIVADA] DEVEM listar as hipóteses legítimas da regra e NÃO DEVEM concluir por irregularidade sem relatá-las (README §3).
- **FR-033**: Divergências exclusivamente de IBS/CBS em 2026 DEVEM ser classificadas como alerta de atenção, se esta interpretação for aprovada (doc 04 §1 – recomendação, não norma). [NEEDS CLARIFICATION C12]

### 3.5 Parecer, RAG e LLM

- **FR-034**: O sistema DEVE consultar a Api.RAG por MCP, filtrando por obrigação e versão do manual aplicável a cada achado.
- **FR-035**: O LLM DEVE receber os achados determinísticos, as hipóteses legítimas e os chunks recuperados, e redigir o parecer conforme o contrato de saída. Ele NÃO DEVE criar, remover ou reclassificar achados determinísticos.
- **FR-036**: O LLM PODE apontar indícios adicionais apenas como "indício identificado pela IA", com severidade máxima de alerta.
- **FR-037**: O contrato de saída do LLM DEVE conter: status da análise, parecer, achados, evidências e referências, limitações e validações não realizadas, indicação de revisão humana e motivos. Estrutura do parecer conforme doc 05 §5.
- **FR-038**: Valores de status da análise: `CONCLUIDA` (todas as validações obrigatórias executadas), `CONCLUIDA_PARCIAL` (parte das validações sem evidência ou não conclusiva), `INCONCLUSIVA` (nenhum cruzamento executável), `FALHA_TECNICA`. Nenhum status DEVE significar certificação de conformidade (constitution 1.3).
- **FR-039**: São **validações obrigatórias** de uma análise: todas as pré-condições de entrada e todas as regras `aprovadas`, vigentes, cujas obrigações estejam no escopo do lote. Se qualquer uma não for concluída, o status não pode ser `CONCLUIDA` (P4).
- **FR-040**: A resposta do LLM DEVE passar por validação estrutural e semântica (P3, P4, P9, P15) com retentativas limitadas (D17); referências DEVEM corresponder a chunks recuperados na execução.
- **FR-041**: O envelope da API e da A2A DEVE ser montado pelo sistema com os metadados operacionais da constitution 4.4 e o relatório de aptidão e cobertura (FR-010).
- **FR-042**: A revisão humana DEVE ser indicada nos casos mínimos da P15. [NEEDS CLARIFICATION D5 – critérios adicionais]

### 3.6 Segurança, privacidade e operação

- **FR-043**: Endpoints REST, MCP e A2A DEVEM exigir autenticação e autorização (D15).
- **FR-044**: Payloads DEVEM ter limite de tamanho e profundidade e rejeitar campos não previstos (D6).
- **FR-045**: O envio de dados ao LLM DEVE se limitar ao necessário para redigir o parecer. [NEEDS CLARIFICATION D19]
- **FR-046**: Retenção e exclusão das obrigações recebidas DEVEM seguir política aprovada (D8, D19).
- **FR-047**: Toda chamada a LLM e embeddings DEVE registrar consumo e custo (constitution 4.5).
- **FR-048**: Logs NÃO DEVEM conter CNPJ, CPF, valores ou chaves de documentos sem mascaramento.

---

## 4. Entidades principais

| Entidade | Descrição |
|---|---|
| Lote de análise | Conjunto de obrigações enviado numa solicitação, com chamador, contexto, execução e correlação |
| Obrigação recebida | Uma obrigação normalizada: metadados (FR-003), relatório de validação e registros/campos |
| Relatório de validação de origem | Resultado do agente de origem: validador, data, status, erros e advertências |
| Grupo de cruzamento | Obrigações aptas do mesmo contribuinte (raiz de CNPJ), estabelecimento e período |
| Cobertura | Obrigações, estabelecimentos e períodos recebidos e aptos; base para "ausência de evidências" |
| Regra | Entrada do catálogo (FR-011) |
| Achado | Resultado de uma regra ou pré-condição: natureza, severidade, regra, evidências dos dois lados, valores, hipóteses legítimas |
| Parecer | Saída validada do LLM sobre os achados (FR-037) |
| Envelope | Resposta da API/A2A com metadados operacionais (FR-041) |

---

## 5. Catálogo de Regras – escopo inicial

O texto completo das regras está em `docs/03-regras-cruzamento.md` e `docs/NUCLEO-1-principios-entidades-matriz.md`; os de-para campo a campo, em `docs/NUCLEO-2-mapas-bidirecionais.md`. Todas entram no catálogo com status **`proposta`**. A tabela abaixo define o **primeiro lote a aprovar** (MVP), priorizado por: (1) bloqueio de falsos positivos; (2) evidência [NORMA]/[MANUAL]; (3) impacto em tributo.

### 5.1 Pré-condições de entrada (novas – tipo "pré-condição")

| ID | Regra | Base | Natureza |
|---|---|---|---|
| PRE-001 | JSON da obrigação válido contra o contrato de entrada | Constitution P18 | Pré-condição não atendida |
| PRE-002 | Versão de layout informada e suportada para o período | P6; INT-COM-002 | Pré-condição não atendida |
| PRE-003 | Relatório de validação presente e aprovado | P18; D25 | Pré-condição não atendida |
| PRE-004 | Escrituração vigente (última original/substituta) | INT-COM-003 | Pré-condição não atendida (descartada) |
| PRE-005 | CNPJ e período coerentes com o grupo de cruzamento | P5; INT-COM-004; ENT-004 | Pré-condição não atendida |

### 5.2 Lote MVP de regras de cruzamento

| Fase | IDs | Obrigações | Tipo | Evidência | US |
|---|---|---|---|---|---|
| A – Regime | CRZ-REG-001 (= ENT-006), CRZ-REG-002, CRZ-REG-004 (= CRZ-CTB-ECF-003), CRZ-REG-005 | NF-e, EFD-Contrib., ECD, ECF | PAR | NORMA / MANUAL | US2 |
| A – Entidades | ENT-001 (= CRZ-EFD-CTB-002), ENT-010, ENT-030, ENT-031 | EFD ICMS/IPI, EFD-Contrib., NF-e, ECD | PRE→ / IDT | DERIVADA | US2 |
| B – NF-e × EFD ICMS/IPI | CRZ-NFE-EFD-001, 002, 003, 004, 006, 007 | NF-e, EFD ICMS/IPI | PRE→ / ←PRE / IDT / LIM | MANUAL / DERIVADA | US3 |
| B – Documentos × EFD-Contrib. | CRZ-NFE-CTB-001, 002; CRZ-NFS-CTB-001, 003 | NF-e, NFS-e, EFD-Contrib. | PRE→ / IDT / LIM | MANUAL / DERIVADA | US4 |
| C – ECD × ECF | CRZ-ECD-ECF-001, 002, 004, 006 | ECD, ECF | IDT / AGR | MANUAL / PVA / DERIVADA | US5 |
| C – DIMP | CRZ-DIMP-EFD-001, 002 | DIMP, EFD ICMS/IPI | PRE→ / AGR | DERIVADA | US6 |

Demais regras de `03` e `NUCLEO-1/2` (ex.: CRZ-EFD-ECD-*, CRZ-CTB-ECD-*, CRZ-CTB-ECF-*, CRZ-DOC-ECD-*, ENT-02x) ficam para lotes seguintes, após o MVP.

### 5.3 Exclusividades legítimas obrigatórias no MVP

Devem estar no catálogo como regras `EXC` antes das regras de presença do lote 5.2: cancelada/denegada/inutilizada (NUCLEO-1 §4.1), transferências e remessas sem receita, presumido em regime de caixa fora do Bloco C/A, aquisições sem crédito omitidas da EFD-Contribuições no regime cumulativo, NFS-e fora da EFD ICMS/IPI (exceto DF e `1601.TOT_ISS`), `H010` com `IND_PROP = 2`, NF-e apenas com IBS/CBS.

---

## 6. Fontes oficiais

As fontes de cada regra são as citadas na própria regra nos documentos da Seção 1.2 e no README §5. Antes da aprovação de cada regra (FR-012), a fonte DEVE ser registrada com emissor, título, versão, vigência, localização (registro/campo/seção) e hash do documento ingerido na Api.RAG (constitution 4.1). Documentos a ingerir (D3): Guia Prático EFD ICMS/IPI v3.2.2 e leiaute 020; Guia Prático EFD-Contribuições v1.35; Manual ECD Leiaute 9; Manual ECF Leiaute 12; MOC NF-e 4.00 e NTs listadas no README §2; Anexos I, B e C da NFS-e v1.01; Ato COTEPE/ICMS 65/18 – DIMP Versão 10.

---

## 7. Critérios de sucesso

- **SC-001**: 100% das regras aprovadas possuem testes com par consistente, inconsistente e incompleto (P5, P17).
- **SC-002**: Em toda a suíte de testes, nenhum resultado tem status `CONCLUIDA` quando existe validação obrigatória pendente, falha técnica ou ausência de evidências (P4).
- **SC-003**: 100% dos achados de cruzamento apontam evidência nos dois lados (FR-028).
- **SC-004**: 0 achados determinísticos alterados pelo LLM no conjunto de avaliação (P2).
- **SC-005**: 100% das referências do parecer correspondem a chunks recuperados na execução (P9).
- **SC-006**: Lotes sem a obrigação de destino nunca geram "omissão" (FR-010, FR-026) – verificado por teste para cada regra `PRE→`/`←PRE`.
- **SC-007**: Mesma entrada processada por REST e por A2A produz achados idênticos (US8).
- **SC-008**: Testes adversariais com instruções em campos do JSON e no relatório de validação não alteram achados nem contrato (P8).
- **SC-009**: Tempo máximo de processamento por lote e volume de referência – a definir no plano técnico (D7).

---

## 8. Premissas

- Os agentes de origem entregam os registros e campos de ligação listados no doc 01 e nos mapas do NUCLEO-2, já interpretados e tipados.
- O agente de origem é responsável por ter validado a obrigação na versão de layout que declara; este agente não confere isso.
- A DIMP, por ser declarada por terceiros, só é analisada quando o lote a traz de fonte íntegra (INT-DIMP-002).
- **Este agente executa o motor de regras de cruzamento.** O doc 05 §4 descreve o agente de parecer recebendo "ocorrências" já geradas; esta especificação assume que as ocorrências são geradas aqui, a partir das obrigações normalizadas. [NEEDS CLARIFICATION C4]

---

## 9. Esclarecimentos pendentes

| # | Questão | Impacto | Decisão da constitution |
|---|---|---|---|
| C1 | Adotar a escala de 4 níveis do README (`ESTRUTURA_GRAVE`, `ESCRITURACAO_GRAVE`, `ALERTA`, `INFO`) ou apenas erro grave / alerta? | FR-031, contrato de saída | D4 |
| C2 | Regras de cruzamento hoje marcadas `ESTRUTURA_GRAVE` (CRZ-REG-001/005/006/008/009, CRZ-NFE-EFD-003, CRZ-NFS-EFD-001, CRZ-EFD-CTB-002, CRZ-ECD-ECF-001/002/003/006): reclassificar como erro grave de escrituração ou como pré-condição? | Severidade do lote MVP | D4 |
| C3 | Regras duplicadas entre doc 03 e NUCLEO-1 com severidades diferentes (ex.: CRZ-EFD-CTB-002 `ESTRUTURA_GRAVE` × ENT-001 `ESCRITURACAO_GRAVE`; CRZ-REG-001 × ENT-006; CRZ-REG-004 × CRZ-CTB-ECF-003): qual ID e severidade prevalecem? | FR-015 | D2 |
| C4 | Confirmar que o motor de cruzamento roda neste agente (e não nos agentes de origem, como sugere doc 05 §4) | Escopo inteiro | D2 |
| C5 | Tolerância padrão para comparação de valores (ex.: R$ 0,01 por arredondamento) e por quem é configurada | FR-024 | D2 |
| C6 | Schema do contrato de entrada, dono e versionamento; rejeição por lote ou por obrigação | FR-002/003 | D24 |
| C7 | Quais status/ocorrências da validação de origem bloqueiam o cruzamento | FR-006 | D25 |
| C8 | Responsável pelas regras internas não validadas pelo PVA (ex.: INT-EFD-004, INT-EFD-009) | Fronteira de escopo | D26 |
| C9 | Lote de análise: múltiplos períodos? entrega incremental (obrigações em momentos diferentes)? | FR-001, FR-010 | D6 |
| C10 | Origem e formato da DIMP recebida (extrato SEFAZ, arquivo da instituição) | US6 | D1 |
| C11 | Critério de materialidade para regras "ALERTA / material: ESCRITURACAO_GRAVE" | FR-025 | D2 |
| C12 | Aprovar a interpretação "divergência só de IBS/CBS em 2026 = alerta" | FR-033 | P1 (interpretação) |
| C13 | Critérios adicionais de revisão humana | FR-042 | D5 |
| C14 | Minimização de dados enviados ao LLM, retenção e exclusão (LGPD) | FR-045/046 | D8, D19 |
