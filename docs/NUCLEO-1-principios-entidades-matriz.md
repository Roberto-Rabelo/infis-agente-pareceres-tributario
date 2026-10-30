# NÚCLEO 1 – Princípios, Entidades Compartilhadas e Matriz de Presença

> Este é o núcleo do agente. As obrigações **não são independentes**: elas registram **os mesmos fatos** em níveis diferentes de detalhe. O agente verifica se cada fato aparece **onde deve aparecer**, **com o mesmo valor** (ou com a agregação correta) e **não aparece onde não deve**.
>
> Os arquivos chegam **já validados** (layout/PVA). Aqui só se verifica **coesão entre obrigações**.
>
> Leitura complementar: [NUCLEO-2 – Mapas bidirecionais campo a campo](NUCLEO-2-mapas-bidirecionais.md) · [NUCLEO-3 – Cadeias de valor ponta a ponta](NUCLEO-3-cadeias-de-valor.md)

---

## 1. Como as obrigações se complementam

O mesmo fato econômico percorre **cinco camadas**. Cada camada resume a anterior:

```
CAMADA 0 – Evidência externa      DIMP (instituição de pagamento declara o que o contribuinte RECEBEU)
                                       │ deve ser explicado por ▼
CAMADA 1 – Documento               NF-e / NFC-e / NFS-e   (1 fato = 1 documento, item a item)
                                       │ é escriturado em ▼ (documento a documento)
CAMADA 2 – Escrituração fiscal     EFD ICMS/IPI (por estabelecimento)   EFD-Contribuições (centralizada)
                                       │ C100/C170/C190 → E110/E520        A100/C100/C180/F → M200/M600
                                       │ é contabilizado em ▼ (lançamento → saldo)
CAMADA 3 – Contabilidade           ECD (I200/I250 lançamentos → I155/I355 saldos → J100/J150)
                                       │ é recuperada por hash em ▼
CAMADA 4 – Apuração IRPJ/CSLL      ECF (Bloco C/K = ECD; L/P = demonstrações; M = e-Lalur; N/P = tributos)
```

**Consequências para o agente:**
1. **Descendo as camadas** (documento → EFD → ECD → ECF), cada fato deve **aparecer** na camada seguinte, de forma individual ou agregada. Se não aparece, é **omissão**.
2. **Subindo as camadas** (ECF → ECD → EFD → documento), cada valor deve ter **origem** na camada anterior. Se não tem, é **valor sem lastro** (crédito, dedução ou receita sem documento).
3. **Entre obrigações da mesma camada** (EFD ICMS/IPI × EFD-Contribuições), o mesmo documento deve ter **os mesmos atributos** (chave, data, valor, participante, item).
4. **Exclusividades legítimas:** alguns fatos existem só em uma obrigação, por definição oficial. O agente **não** pode apontá-los como omissão. Estão listados em cada mapa do NÚCLEO-2.

---

## 2. Tipos de relação (vocabulário do motor de regras)

Toda regra do núcleo usa um destes tipos. O tipo define o algoritmo do motor (.NET), não o texto do parecer.

| Código | Tipo | Definição | Algoritmo |
|---|---|---|---|
| **PRE→** | Presença direta | Se o fato existe em A, deve existir em B | `A.except(B, chave)` → vazio |
| **←PRE** | Presença reversa (lastro) | Se o fato existe em B, deve existir origem em A | `B.except(A, chave)` → vazio |
| **IDT** | Identidade de atributo | Mesmo fato, mesmo valor de atributo | `A.campo == B.campo` (por chave) |
| **AGR** | Agregação | Soma de N registros de A = 1 valor de B | `Σ A.campo (grupo) == B.campo` |
| **LIM** | Limite | Valor de B não pode exceder o de A | `B.campo <= A.campo` |
| **CNT** | Continuidade temporal | Saldo final de um período = saldo inicial do seguinte | `A(t).final == A(t+1).inicial` |
| **PAR** | Coerência de parâmetro | Indicadores de regime/qualificação compatíveis | Tabela de compatibilidade |
| **EXC** | Exclusividade legítima | Fato que, por definição oficial, só existe em uma obrigação | Lista de exclusão aplicada **antes** de PRE→/←PRE |

> **Ordem de execução obrigatória:** `PAR` → `EXC` (filtros) → `PRE→` / `←PRE` → `IDT` → `AGR` / `LIM` → `CNT`. Sem `PAR` e `EXC` antes, o motor gera falsas omissões.

---

## 3. Entidades compartilhadas (dados mestres)

São as **chaves de junção** entre as obrigações. Se as entidades não casam, nenhum cruzamento de valor é confiável. Por isso, as divergências de entidade vêm **antes** das divergências de valor no parecer.

### 3.1 Contribuinte / estabelecimento

| Atributo | NF-e | NFS-e | DIMP | EFD ICMS/IPI | EFD-Contrib. | ECD | ECF |
|---|---|---|---|---|---|---|---|
| CNPJ | `emit/CNPJ` ou `dest/CNPJ` | `prest/CNPJ` ou `toma/CNPJ` | CNPJ/CPF do beneficiário | `0000.CNPJ` (estabelecimento) | `0000.CNPJ` (matriz) + `0140.CNPJ` (cada estab.) | `0000.CNPJ` | `0000.CNPJ` |
| Inscrição estadual | `emit/IE`, `dest/IE` | — | conforme leiaute | `0000.IE` | `0140.IE` | `0000.IE` | — |
| Inscrição municipal | — | `prest/IM` | — | `0000.IM` | `0140.IM` | `0000.IM` | — |
| UF / município | `enderEmit/UF`, `cMun` | `cLocEmi`, endereço | UF do arquivo | `0000.UF`, `COD_MUN` | `0140.UF`, `COD_MUN` | `0000.UF`, `COD_MUN` | `0030` |
| Regime | `emit/CRT` | `opSimpNac`, regime especial | — | `0000.IND_PERFIL`, `IND_ATIV` | `0110` | `0000.COD_PLAN_REF` | `0010.FORMA_TRIB` |

**Regras de entidade (todas são condição para as demais):**

| ID | Tipo | Regra | Severidade |
|---|---|---|---|
| ENT-001 | PRE→ | Todo CNPJ de estabelecimento com EFD ICMS/IPI no mês ⇒ consta do `0140` da EFD-Contribuições do mesmo mês. | ESCRITURACAO_GRAVE |
| ENT-002 | ←PRE | Todo estabelecimento do `0140` com movimento de mercadorias (Bloco C) e inscrição estadual ⇒ deve existir EFD ICMS/IPI do estabelecimento no mês (salvo dispensa da UF). | ESCRITURACAO_GRAVE |
| ENT-003 | PRE→ | Todo CNPJ emitente de NF-e de saída (`emit/CNPJ` = estabelecimento do grupo) ⇒ tem EFD ICMS/IPI no mês de emissão (se contribuinte obrigado). | ESCRITURACAO_GRAVE |
| ENT-004 | IDT | Raiz do CNPJ (8 primeiras posições) igual em ECD, ECF, EFD-Contribuições e em todas as EFD ICMS/IPI do grupo analisado. | ESTRUTURA (pré-condição) |
| ENT-005 | IDT | `IE` do `0000` da EFD ICMS/IPI = `emit/IE` das NF-e próprias do estabelecimento = `0140.IE`. | ALERTA |
| ENT-006 | PAR | Regime coerente em todas: ver §5 (matriz de parâmetros). | ESCRITURACAO_GRAVE |

### 3.2 Participante (cliente/fornecedor/tomador/prestador/instituição de pagamento)

| Obrigação | Onde está | Chave |
|---|---|---|
| NF-e | `emit`, `dest`, `transporta`, `autXML`, `infIntermed`, `card/CNPJ` (credenciadora) | CNPJ/CPF |
| NFS-e | `prest`, `toma`, `interm` | CNPJ/CPF/NIF |
| EFD ICMS/IPI | `0150` (`COD_PART` → `CNPJ`/`CPF`/`IE`/`COD_MUN`) | `COD_PART` (código interno) → CNPJ |
| EFD-Contribuições | `0150` (por estabelecimento, dentro do `0140`); `C191/C195.CNPJ_CPF_PART`; `F600.CNPJ` (fonte pagadora) | CNPJ/CPF |
| ECD | `0150` (participantes com relacionamento – partes relacionadas) e `I250` (sem participante, salvo subcontas) | CNPJ |
| ECF | `Y570.CNPJ_FON` (fonte pagadora), `Y600` (sócios), `X` (operações com exterior) | CNPJ |

> `COD_PART` é **código interno** do contribuinte. O casamento entre obrigações deve ser sempre feito pelo **CNPJ/CPF resolvido via `0150`**, nunca pelo `COD_PART`.

| ID | Tipo | Regra | Severidade |
|---|---|---|---|
| ENT-010 | IDT | Para cada documento casado por chave, o CNPJ/CPF do `0150` apontado por `C100.COD_PART` = `dest/CNPJ` (saída própria) ou `emit/CNPJ` (entrada de terceiros) do XML. | ESCRITURACAO_GRAVE |
| ENT-011 | IDT | Mesmo documento na EFD ICMS/IPI e na EFD-Contribuições ⇒ participante com o mesmo CNPJ/CPF. | ALERTA |
| ENT-012 | IDT | `1601.COD_PART_IP` / `COD_PART_IT` → `0150.CNPJ` = CNPJ do declarante da DIMP (instituição/intermediador). | ESCRITURACAO_GRAVE |

### 3.3 Item / produto / serviço

| Obrigação | Onde está |
|---|---|
| NF-e | `det/prod`: `cProd`, `cEAN`, `xProd`, `NCM`, `EXTIPI`, `CEST`, `uCom`/`uTrib` |
| NFS-e | `cTribNac`, `cTribMun`, `cNBS`, `xDescServ` |
| EFD ICMS/IPI | `0200` (`COD_ITEM`, `DESCR_ITEM`, `COD_BARRA`, `UNID_INV`, `TIPO_ITEM`, `COD_NCM`, `EX_IPI`, `COD_GEN`, `COD_LST`, `CEST`), `0190`, `0220` (fator de conversão) |
| EFD-Contribuições | `0200` (mesmos campos-base), `C180/C190.COD_NCM` |
| ECD / ECF | Não há item. Itens chegam à contabilidade **agregados em contas** (`COD_CTA`). |

| ID | Tipo | Regra | Severidade |
|---|---|---|---|
| ENT-020 | IDT | Saída própria: `0200.COD_ITEM` = `cProd` do XML; `0200.COD_NCM` = `NCM`; `0200.COD_BARRA` = `cEAN` (quando houver); `0200.CEST` = `CEST`. | ALERTA (NCM divergente: ESCRITURACAO_GRAVE quando altera alíquota de IPI/PIS/COFINS monofásico) |
| ENT-021 | IDT | Entrada de terceiros: o `COD_ITEM` é do **informante** (pode diferir de `cProd`). Casar pelo número do item (`C170.NUM_ITEM` = `det/@nItem`) e conferir `COD_NCM` = `NCM` do fornecedor. | ALERTA |
| ENT-022 | IDT | Unidade: se `C170.UNID` ≠ `0200.UNID_INV`, deve existir `0220` com o fator de conversão. Quantidade convertida deve ser coerente com `qCom`/`qTrib`. | ALERTA |
| ENT-023 | IDT | Mesmo `COD_ITEM` na EFD ICMS/IPI e na EFD-Contribuições do mesmo período ⇒ mesmo `COD_NCM` e mesma descrição. | ALERTA |

### 3.4 Conta contábil (a ponte fiscal → contábil)

| Obrigação | Onde está |
|---|---|
| EFD ICMS/IPI | `0500` (`COD_NAT_CC`, `IND_CTA`, `NIVEL`, `COD_CTA`, `NOME_CTA`), referenciada em `C170.COD_CTA`, `H010.COD_CTA`, `0300.COD_CTA` |
| EFD-Contribuições | `0500` (`COD_NAT_CC`, `IND_CTA`, `COD_CTA`, `NOME_CTA`, `COD_CTA_REF`), referenciada em `A170`, `C170`, `C181/C185`, `F100`, `F120/F130`, `F500`, `F525`, `M400/M410`, `M800/M810` |
| ECD | `I050` (`COD_NAT`, `IND_CTA`, `NIVEL`, `COD_CTA`, `COD_CTA_SUP`, `CTA`), `I051` (referencial) |
| ECF | `C050` (recuperada da ECD) ou `J050`; `K155/K355`; `M310` (vínculo e-Lalur ↔ conta) |

| ID | Tipo | Regra | Severidade |
|---|---|---|---|
| ENT-030 | PRE→ | Todo `COD_CTA` do `0500` da EFD ICMS/IPI e da EFD-Contribuições ⇒ existe no `I050` da ECD do mesmo ano com o mesmo código. | ESCRITURACAO_GRAVE |
| ENT-031 | IDT | `0500.COD_NAT_CC` = `I050.COD_NAT` e `0500.IND_CTA` = `I050.IND_CTA` (a conta referenciada nos registros deve ser **analítica**, `A`). | ESCRITURACAO_GRAVE |
| ENT-032 | IDT | Coerência de natureza: conta de receita (`COD_NAT = 04`) em `C181`/`A170`/`F500` de saída; conta de estoque (`01`) em `H010`; conta do ativo (`01`) em `0300`/`F120`/`F130`. | ALERTA |
| ENT-033 | PRE→ | Toda conta de `I050` usada pela ECF (`M310.COD_CTA`) ⇒ existe na ECD e teve movimento/saldo no período. | ALERTA |

> **Hipótese legítima para ENT-030/031:** mudança do plano de contas no ano (`ECD 0000.IND_MUDANC_PC` + `I157`) ou EFD de anos anteriores. Quando houver `I157`, aceitar o código antigo mapeado.

### 3.5 Documento fiscal (a entidade mais cruzada)

| Atributo | NF-e/NFC-e | NFS-e | EFD ICMS/IPI | EFD-Contribuições | ECD |
|---|---|---|---|---|---|
| Chave | `infNFe/@Id` (44) | chave NFS-e | `C100.CHV_NFE` | `C100.CHV_NFE`, `A100.CHV_NFSE` | `I250.HIST` (texto livre – não confiável como chave) |
| Modelo/série/número | `mod`/`serie`/`nNF` | `serie`/`nDPS`, nº NFS-e | `COD_MOD`/`SER`/`NUM_DOC` | idem / `A100.SER`/`NUM_DOC` | — |
| Data | `dhEmi`, `dhSaiEnt` | `dhEmi`, `dCompet` | `DT_DOC`, `DT_E_S` | `DT_DOC`, `DT_E_S` / `A100.DT_DOC`, `DT_EXE_SERV` | `I200.DT_LCTO` |
| Situação | protocolo + eventos | eventos | `COD_SIT` | `COD_SIT` | — |
| Sentido | `tpNF` + papel do CNPJ (`emit`/`dest`) | papel (`prest`/`toma`) | `IND_OPER` + `IND_EMIT` | `IND_OPER` + `IND_EMIT` | — |

**Tabela de correspondência de sentido (obrigatória para o casamento):**

| Situação real | NF-e (`tpNF`, papel) | EFD `IND_OPER` | EFD `IND_EMIT` |
|---|---|---|---|
| Venda/saída emitida pelo contribuinte | `tpNF=1`, contribuinte = `emit` | 1 (saída) | 0 (própria) |
| Entrada com nota própria (ex.: importação, compra de produtor rural PF) | `tpNF=0`, contribuinte = `emit` | 0 (entrada) | 0 (própria) |
| Compra (nota do fornecedor) | `tpNF=1`, contribuinte = `dest` | 0 (entrada) | 1 (terceiros) |
| Devolução recebida emitida pelo cliente contribuinte | `tpNF=1`, `finNFe=4`, contribuinte = `dest` | 0 (entrada) | 1 (terceiros) |
| Nota de entrada emitida por terceiro para o contribuinte (ex.: devolução por fornecedor com nota de entrada dele) | `tpNF=0`, contribuinte = `dest` | 1 (saída do ponto de vista do contribuinte) | 1 (terceiros) |

> Base: definição dos campos `IND_OPER` e `IND_EMIT` do `C100` (Guia Prático EFD ICMS/IPI) e `tpNF` (MOC). A última linha deve ser conferida caso a caso pelo CFOP (o `IND_OPER` segue a movimentação **para o informante**).

### 3.6 Período

| Obrigação | Período | Conversão para cruzamento |
|---|---|---|
| NF-e/NFS-e | Instantâneo (`dhEmi`, `dhSaiEnt`, `dCompet`) | Alocar ao mês pela data relevante (ver NÚCLEO-2) |
| DIMP | Conforme leiaute (período de referência das transações) | Mensal |
| EFD ICMS/IPI / EFD-Contribuições | Mensal (`DT_INI`–`DT_FIN`) | Base |
| ECD | Anual (ou situação especial); saldos em `I150` por período (normalmente mensal) | `I150/I155` permitem **cruzamento mensal** |
| ECF | Anual; trimestral (`FORMA_APUR = T`); estimativas mensais (`N620/N660`) | Agregar meses da EFD por trimestre/ano |

---

## 4. MATRIZ DE PRESENÇA – fato × obrigação

**Leitura:** para cada fato, onde ele **deve** estar (●), onde pode estar conforme condição (◐), onde **não** deve estar (○ = se aparecer é erro), e onde não se aplica (—). A coluna “Exclusividade legítima” evita falsos positivos.

### 4.1 Fatos de mercadorias (NF-e/NFC-e)

| Fato | NF-e | DIMP | EFD ICMS/IPI | EFD-Contrib. | ECD | ECF | Exclusividade legítima / observação |
|---|---|---|---|---|---|---|---|
| Venda de mercadoria (saída própria autorizada) | ● | ◐ se recebida por meio eletrônico | ● `C100` (`IND_OPER=1`,`IND_EMIT=0`) + `C190`; débito em `E110` | ● `C100/C170` ou `C180/C181/C185` (exceto presumido caixa → `F500/F525` quando recebida) | ● receita (`COD_NAT 04`) + clientes/caixa | ● dentro da receita bruta (`L300`/`P150`/`P200`) | — |
| NF-e cancelada (evento 110111) | ● (evento) | ○ (não deveria haver recebimento; se houver, alerta) | ● `C100` com `COD_SIT 02/03`, **sem valores** | ○ (não compõe receita) | ○ | ○ | Única presença válida na EFD ICMS/IPI é como cancelada |
| NF-e denegada | ● | ○ | ● `COD_SIT 04` | ○ | ○ | ○ | — |
| Numeração inutilizada | (protocolo de inutilização) | ○ | ● `COD_SIT 05` | ○ | ○ | ○ | Deixa de existir a partir da vigência da NT 2023.002 v1.02 |
| Transferência entre estabelecimentos | ● | ○ | ● saída no remetente (CFOP 5.15x/6.15x) e entrada no destinatário | ◐ sem receita (CST de não incidência/outras) | ◐ sem receita (contas de estoque entre filiais ou nenhuma movimentação se centralizada) | ○ na receita | **Nunca** compõe receita; deve aparecer **nas duas EFD ICMS/IPI** (remetente e destinatário) |
| Remessa sem receita (conserto, demonstração, comodato, industrialização, armazenagem) e retorno | ● | ○ | ● | ◐ (pode ser omitida se sem repercussão em PIS/COFINS) | ◐ (contas de compensação ou nenhuma) | ○ na receita | Exclusividade EFD ICMS/IPI × EFD-Contrib. |
| Bonificação/brinde | ● | ○ | ● | ◐ | ● (despesa ou redução) | ◐ | Não é receita de venda |
| Compra de mercadoria para revenda / insumo | ● (do fornecedor) | — | ● `C100` (`IND_OPER=0`,`IND_EMIT=1`) | ◐ com crédito: `C100/C170` ou `C190/C191/C195` (CST 50–56); sem crédito: pode ser omitida | ● estoque/custo + fornecedores | ● via custo (`L300`/`P150`) | Presumido (cumulativo) não escritura aquisições na EFD-Contrib. |
| Compra de uso e consumo | ● | — | ● (CFOP 1.556/2.556), **sem crédito de ICMS** | ◐ (em regra sem crédito) | ● despesa | ● | — |
| Compra de ativo imobilizado | ● | — | ● (CFOP 1.551/2.551) + **CIAP** `G125`/`G130` (se houver crédito) | ◐ `F120`/`F130` (crédito sobre ativo) | ● imobilizado | ● | Crédito de ICMS só via CIAP (1/48 – LC 87/96, art. 20, §5º) |
| Devolução de venda (recebida) | ● (`finNFe=4`) | ◐ estorno na DIMP | ● entrada + `C113` | ● dedução/ajuste (CST de crédito de devolução ou ajuste de receita) | ● devoluções (redutora de receita) | ● deduções da receita bruta | — |
| Venda com ICMS-ST (substituto) | ● (`vST`) | ◐ | ● `C100.VL_ICMS_ST` + `E200/E210` UF destino | ● (ST não compõe receita da PJ substituta) | ● ICMS-ST a recolher | ◐ | — |
| Venda a consumidor final não contribuinte de outra UF (DIFAL) | ● `ICMSUFDest` | ◐ | ● `C101` + `E300/E310` | ● | ● | ◐ | — |
| Venda com IPI (industrial) | ● `vIPI` | ◐ | ● `C100.VL_IPI` + `E510/E520` | ● (IPI não compõe receita bruta) | ● IPI a recolher | ● | Só se `IND_ATIV = 0` |
| Venda por intermediador/marketplace | ● `indIntermed=1` + CNPJ do intermediador | ● declarada pelo intermediador | ● + `1601.COD_PART_IT` | ● | ● | ● | — |
| Inventário de 31/12 | — | — | ● `H005/H010` na EFD de **fevereiro** | ◐ `F150` (estoque de abertura – só em transição de regime) | ● saldo de estoque em 31/12 (`I155`) | ● `L100`/`P100` estoque; `Y672.VL_EST` (presumido) | `H010` com `IND_PROP = 2` (de terceiros) **não** está na ECD |
| Recebimento por cartão/PIX | ◐ (`detPag/tPag`) | ● | ● `1601` | ◐ `F525` (presumido caixa) | ● clientes/caixa/bancos | ◐ | — |

### 4.2 Fatos de serviços (NFS-e)

| Fato | NFS-e | DIMP | EFD ICMS/IPI | EFD-Contrib. | ECD | ECF | Observação |
|---|---|---|---|---|---|---|---|
| Serviço prestado (ISS) | ● | ◐ | ○ (exceto DF – Bloco B; e `1601.TOT_ISS` se recebido por meio eletrônico) | ● `A100` (`IND_OPER=1`, `CHV_NFSE`) + `A170`, ou `F500/F525` (caixa), ou `F550` (competência consolidada) | ● receita de serviços | ● receita bruta | — |
| Serviço prestado com retenção federal | ● (`vRetIRRF`, `vRetCSLL`, `vRetCP`, PIS/COFINS retidos) | — | ○ | ● `A100.VL_PIS_RET/VL_COFINS_RET` + `F600` (+ `1300/1700`) | ● tributos a recuperar/compensar | ● `Y570` (IRRF/CSLL retidos), dedução em `N630`/`N670`/`P300`/`P500` | — |
| Serviço tomado com crédito de PIS/COFINS | ● (como tomador) | — | ○ | ● `A100` (`IND_OPER=0`) + `A170` (CST 50–56, `NAT_BC_CRED 03`) | ● custo/despesa | ● | Só não cumulativo |
| NFS-e cancelada / substituída | ● evento | ○ | ○ | ○ (ou `COD_SIT` de cancelado) | ○ | ○ | Substituída não soma com a substituta |
| Serviço de transporte / comunicação | (CT-e / NFCom – fora do escopo) | | ● Bloco D | ● Bloco D | ● | ● | Fora do escopo deste estudo; não confundir com NFS-e |

### 4.3 Fatos sem documento fiscal (existem em escriturações mas não em NF-e/NFS-e)

| Fato | EFD ICMS/IPI | EFD-Contrib. | ECD | ECF | Observação |
|---|---|---|---|---|---|
| Receita financeira | ○ | ● `F100`/`M210` (lucro real – Decreto 8.426/2015) / presumido: conforme regras do regime | ● | ● (fora da receita bruta) | **Exclusiva** das escriturações — não procurar documento fiscal |
| Aluguel recebido/pago, energia, depreciação | ○ | ● `F100`, `F120/F130` (créditos) | ● | ● | Sem NF-e: lastro é a contabilidade |
| Ajustes de apuração (benefícios, estornos) | ● `E111`/`C197` | ● `M110/M220`… | ● | ◐ | Exigem documento de suporte (ato concessivo) |
| Saldo credor de ICMS | ● `E110` | ○ | ● ICMS a recuperar | ● ativo | — |
| Créditos de PIS/COFINS de períodos anteriores | ○ | ● `1100/1500` | ● | ◐ | — |
| Adições/exclusões do e-Lalur | ○ | ○ | ◐ (conta contábil vinculada) | ● `M300/M350` | Exclusiva da ECF |
| Prejuízo fiscal / base negativa | ○ | ○ | ○ | ● `M010/M500` (Parte B) | Exclusiva da ECF |

---

## 5. Matriz de parâmetros (PAR) – compatibilidade de regime entre obrigações

Executar **antes** de qualquer cruzamento de valor. Uma incompatibilidade aqui invalida as conclusões de valor.

| Parâmetro (fonte) | Valor | EFD-Contrib. `0110` esperado | ECD esperado | ECF esperado | EFD ICMS/IPI | Base |
|---|---|---|---|---|---|---|
| ECF `0010.FORMA_TRIB` | 1 (lucro real) | `COD_INC_TRIB` 1 ou 3 (2 só se todas as receitas estiverem nas exceções legais) | **obrigatória**; `COD_PLAN_REF` de lucro real | Blocos L, M, N | conforme ICMS/IPI | Lei 10.637/02 art. 8º; Lei 10.833/03 art. 10; IN RFB 2.003/2021 |
| ECF `0010.FORMA_TRIB` | 5 (presumido) | `COD_INC_TRIB = 2` (cumulativo); `IND_REG_CUM` 1 (caixa) / 2 / 9 (competência) | conforme IN RFB 2.003/2021; se entregue, `COD_PLAN_REF` de presumido; ECF `TIP_ESC_PRE = C` | Bloco P (e Q se Livro Caixa) | conforme ICMS/IPI | idem |
| ECF presumido – regime de reconhecimento da receita (caixa) | caixa | `IND_REG_CUM = 1` (`F500/F510/F525`) | — | indicador de regime de caixa na ECF (ex.: `Y672.IND_REG_APUR` – conferir leiaute) | — | IN SRF 104/1998 (opção única para IRPJ, CSLL, PIS e COFINS) |
| NF-e `emit/CRT` | 3 | obrigatória | conforme forma de tributação | obrigatória | obrigatória (se contribuinte) | IN RFB 1.252/2012; Ajuste SINIEF 02/09 |
| NF-e `emit/CRT` | 1 ou 4 (SN/MEI) | **não** deve existir (dispensada) | dispensada | **não** deve existir | dispensada (em regra) | LC 123/2006; IN RFB 1.252/2012; IN RFB 2.004/2021 |
| EFD ICMS/IPI `0000.IND_ATIV` | 0 (industrial/equiparado) | — | — | — | `E500–E520` com movimento quando houver IPI | Guia EFD |
| ECF `0010.COD_QUALIF_PJ` | 01/02/03 | — | `COD_PLAN_REF` compatível (PJ em geral / financeira / seguradora) | — | — | Manuais ECD/ECF |

**Exemplo de falha PAR → conclusão bloqueada:** a NF-e tem `CRT = 1` (Simples Nacional), mas existem EFD-Contribuições e ECF de lucro presumido no mesmo período. Antes de qualquer cruzamento de valor, o parecer deve apontar **incoerência de enquadramento** (`ESCRITURACAO_GRAVE`).
