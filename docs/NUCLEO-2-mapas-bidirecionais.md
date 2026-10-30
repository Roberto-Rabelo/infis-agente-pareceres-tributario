# NÚCLEO 2 – Mapas Bidirecionais (campo a campo)

Para cada par de obrigações:
- **A → B**: o que existe em A e **deve** existir em B (omissão se faltar).
- **B → A**: o que existe em B e **deve** ter origem em A (valor sem lastro se faltar).
- **De-para**: campo a campo, com o tipo de relação (`IDT`, `AGR`, `LIM`, `CNT` – ver [NÚCLEO-1 §2](NUCLEO-1-principios-entidades-matriz.md#2-tipos-de-relação-vocabulário-do-motor-de-regras)).
- **Exclusivo legítimo**: o que pode existir só em um lado, por definição oficial. É filtro (`EXC`) aplicado antes da checagem de presença.

Evidência: salvo indicação, o de-para é **[DERIVADA]** da definição oficial dos campos nos leiautes vigentes ([README §2](README.md#2-versões-oficiais-vigentes-verificadas-fonte-primária)). Os nomes de campos devem ser conferidos no PDF da versão do período antes de codificar.

---

## M1. NF-e/NFC-e ⇄ EFD ICMS/IPI

### M1.1 A → B (NF-e → EFD): toda NF-e deve estar escriturada

| ID | Universo de NF-e | Deve existir na EFD do estabelecimento | Período de alocação | Severidade se ausente |
|---|---|---|---|---|
| M1-P01 | Emitidas pelo estabelecimento (`emit/CNPJ` = `0000.CNPJ`), autorizadas, sem cancelamento | `C100` com `CHV_NFE`, `IND_EMIT=0`, `COD_SIT ∈ {00,01,06,07,08}` | Mês de `dhEmi` (saída) | ESCRITURACAO_GRAVE (omissão de saída → débito omitido) |
| M1-P02 | Emitidas pelo estabelecimento e **canceladas** | `C100` com `COD_SIT ∈ {02,03}` | Mês de `dhEmi` | ALERTA (sem impacto de imposto, mas é exigência do Guia) |
| M1-P03 | Emitidas pelo estabelecimento e **denegadas** | `C100` com `COD_SIT = 04` | Mês de `dhEmi` | ALERTA |
| M1-P04 | Faixas inutilizadas | `C100` com `COD_SIT = 05` | Mês da inutilização | ALERTA |
| M1-P05 | Destinadas ao estabelecimento (`dest/CNPJ` = `0000.CNPJ`), autorizadas, sem cancelamento, **sem** evento 210220/210240 | `C100` com `IND_EMIT=1`, `IND_OPER=0`, mesma `CHV_NFE` | Mês da **entrada** (`DT_E_S`) – pode ser posterior a `dhEmi` | Ausente no mês e na janela seguinte: ESCRITURACAO_GRAVE; apenas deslocada: ALERTA. A **janela de meses** é parâmetro do projeto (não há prazo fixado no Guia para este cruzamento) |
| M1-P06 | Destinadas ao estabelecimento com evento **210200 (Confirmação da Operação)** | `C100` de entrada | idem | ESCRITURACAO_GRAVE (o próprio contribuinte confirmou a operação) |

> **Fundamento de M1-P05/P06:** o Guia Prático determina a escrituração das entradas, e a Manifestação do Destinatário (NT 2020.001) registra a confirmação. Uma entrada confirmada e não escriturada indica omissão de entrada. Aqui não se trata de crédito: a omissão de entrada é indício de pagamento com recursos não contabilizados, tema da camada contábil.

### M1.2 B → A (EFD → NF-e): todo C100 modelo 55/65 deve ter NF-e válida

| ID | Registro na EFD | Exigência no universo de NF-e | Severidade |
|---|---|---|---|
| M1-R01 | `C100` `COD_MOD ∈ {55,65}`, `COD_SIT ∈ {00,01,06,07,08}` | XML autorizado com a mesma chave, **sem** evento de cancelamento | ESCRITURACAO_GRAVE (documento inexistente/cancelado escriturado como regular – gera crédito sem lastro na entrada) |
| M1-R02 | `C100` `COD_SIT ∈ {02,03}` | Deve existir evento de cancelamento homologado | ESCRITURACAO_GRAVE (saída escriturada como cancelada sem estar cancelada → **débito omitido**) |
| M1-R03 | `C100` `IND_EMIT=0` | `emit/CNPJ` do XML = `0000.CNPJ` | ESCRITURACAO_GRAVE |
| M1-R04 | `C100` `IND_EMIT=1`, `IND_OPER=0` | `dest/CNPJ` do XML = `0000.CNPJ` (a nota é **para** o estabelecimento) | ESCRITURACAO_GRAVE (crédito de nota destinada a terceiro) |
| M1-R05 | `C113` (documento referenciado) | Documento referenciado existe e é coerente com `NFref` | ALERTA |
| M1-R06 | `G130.CHV_NFE_CTA` (CIAP) | NF-e de aquisição do bem existe, autorizada, destinada ao estabelecimento | ESCRITURACAO_GRAVE (crédito CIAP sem lastro) |

### M1.3 De-para – cabeçalho (`C100` ↔ XML)

| EFD `C100` | NF-e | Relação | Regra / exceção | Severidade |
|---|---|---|---|---|
| `CHV_NFE` | `infNFe/@Id` (sem prefixo "NFe") | IDT | Chave de junção | — |
| M1-I01: `COD_MOD` / `SER` / `NUM_DOC` | `ide/mod` / `ide/serie` / `ide/nNF` | IDT | — | ESCRITURACAO_GRAVE |
| M1-I01: `DT_DOC` | `ide/dhEmi` (data) | IDT | — | ALERTA |
| `DT_E_S` | `ide/dhSaiEnt` (saída própria) / data efetiva de entrada (terceiros) | IDT (própria) / `DT_E_S ≥ dhEmi` (terceiros) | Entrada não pode ser anterior à emissão | ALERTA |
| `COD_PART` → `0150.CNPJ/CPF` | `dest` (saída própria) / `emit` (entrada terceiros) | IDT | — | ESCRITURACAO_GRAVE |
| M1-I03: `VL_DOC` | `total/ICMSTot/vNF` | IDT | — | ESCRITURACAO_GRAVE |
| `VL_MERC` | `vProd` | IDT | — | ALERTA |
| `VL_DESC` | `vDesc` | IDT | — | ALERTA |
| `VL_FRT` / `VL_SEG` / `VL_OUT_DA` | `vFrete` / `vSeg` / `vOutro` | IDT | — | ALERTA |
| `IND_FRT` | `transp/modFrete` | IDT | Códigos alinhados (0,1,2,3,4,9) | INFO |
| `IND_PGTO` | `pag/detPag/indPag` | IDT | — | INFO |
| M1-I05 (própria) e M1-I06 (terceiros): `VL_BC_ICMS` / `VL_ICMS` | `vBC` / `vICMS` | **Própria: IDT.** Terceiros: `LIM` (EFD ≤ XML) | Na entrada, o informante escritura o **crédito a que tem direito** (pode ser menor ou zero, ex.: uso e consumo) | Própria a menor: ESCRITURACAO_GRAVE; terceiros acima do XML: ESCRITURACAO_GRAVE |
| `VL_BC_ICMS_ST` / `VL_ICMS_ST` | `vBCST` / `vST` | IDT (própria) | — | ESCRITURACAO_GRAVE |
| `VL_IPI` | `vIPI` | IDT (própria); `LIM` (terceiros) | — | ESCRITURACAO_GRAVE |
| `VL_PIS` / `VL_COFINS` | `vPIS` / `vCOFINS` | IDT (própria) | — | ALERTA (o PIS/COFINS é apurado na EFD-Contrib.) |
| `C101.VL_FCP_UF_DEST` / `VL_ICMS_UF_DEST` / `VL_ICMS_UF_REM` | `vFCPUFDest` / `vICMSUFDest` / `vICMSUFRemet` | IDT | Só existe `C101` se o XML tiver `ICMSUFDest` | ESCRITURACAO_GRAVE |
| `COD_SIT` | protocolo + eventos | regra | 00 autorizada; 02 cancelada; 04 denegada; 06 `finNFe=2` complementar | ESCRITURACAO_GRAVE |

### M1.4 De-para – itens e analítico

| EFD | NF-e | Relação | Regra / exceção |
|---|---|---|---|
| `C170` (quando informado) `NUM_ITEM` | `det/@nItem` | IDT | Saída própria por NF-e: o Guia **dispensa** o `C170` (salvo exceções do próprio Guia). Entradas: `C170` exigido. |
| `C170.QTD` / `UNID` | `qCom`/`uCom` (ou `qTrib`/`uTrib`) | IDT ou via `0220` | Unidade do informante pode diferir – exige fator `0220` |
| `C170.VL_ITEM` | `vProd` do item | IDT | — |
| `C170.CFOP` | `prod/CFOP` | Própria: IDT. Terceiros: **conversão** 5→1, 6→2, 7→3, ajustada à destinação | ALERTA |
| `C170.CST_ICMS` (3 dígitos) | `orig` + `CST` (ou `CSOSN`) | Própria: IDT. Terceiros: CST do ponto de vista do informante | ALERTA |
| `C170.VL_BC_ICMS`/`ALIQ_ICMS`/`VL_ICMS` | `vBC`/`pICMS`/`vICMS` do item | Própria: IDT; Terceiros: LIM | — |
| `C170.CST_IPI`/`VL_IPI` | `IPI/CST`, `vIPI` | Própria: IDT | — |
| `C170.CST_PIS`/`CST_COFINS` | `PIS/CST`, `COFINS/CST` | Própria: IDT | — |
| `C190` (CST_ICMS, CFOP, ALIQ_ICMS) | Itens agrupados por `orig+CST`, `CFOP`, `pICMS` | **AGR**: `Σ vProd+vFrete+vSeg+vOutro−vDesc+vST+vIPI` (por grupo) = `VL_OPR`; `Σ vBC` = `VL_BC_ICMS`; `Σ vICMS` = `VL_ICMS`; `Σ vBCST`/`vST` = `VL_BC_ICMS_ST`/`VL_ICMS_ST`; `Σ vIPI` = `VL_IPI` | Saída própria: ESCRITURACAO_GRAVE se divergir. Composição de `VL_OPR`: conferir definição no Guia vigente |
| `C197` (ajuste por documento) | `cBenef` do item (benefício fiscal) | Presença condicional | Se a UF exige `cBenef` e há benefício com reflexo na apuração, espera-se ajuste correspondente (`C197`/`E111`) conforme tabela 5.3 da UF | ALERTA |
| `0200` do item | `prod` | ver ENT-020/021 | — |

### M1.5 Exclusivos legítimos

| Só na NF-e | Só na EFD ICMS/IPI |
|---|---|
| NF-e emitida por **outro** estabelecimento do mesmo CNPJ base (vai na EFD **daquele** estabelecimento) | Documentos não eletrônicos ou outros modelos (01, 1B, 04, CT-e no Bloco D, NF3e, NFCom…) |
| NF-e destinada ao contribuinte com evento 210220/210240 (desconhecimento/operação não realizada) | Ajustes de apuração (`E111`, `E220`), saldo credor anterior, CIAP (`G110`), inventário (`H`), Bloco K |
| NF-e só com IBS/CBS, sem ICMS/IPI – conforme exceção ao `C100` do Guia 3.2.x (conferir texto) | Registros `1601` (pagamentos eletrônicos) e demais do Bloco 1 |
| NF-e com entrada ainda não ocorrida (`DT_E_S` em mês posterior) | Notas de entrada emitidas pelo próprio estabelecimento sem XML de terceiro (importação, produtor rural) – elas existem como NF-e **própria** |

---

## M2. NF-e/NFC-e ⇄ EFD-Contribuições

### M2.1 A → B

| ID | Universo de NF-e | Deve existir na EFD-Contrib. | Severidade se ausente |
|---|---|---|---|
| M2-P01 | Saídas próprias com **receita** (CFOP de venda/prestação), autorizadas, não canceladas, de **qualquer** estabelecimento do `0140` | Lucro real ou presumido por competência: `C100/C170` (`C010.IND_ESCRI = 2`) com a chave, **ou** `C180/C181/C185` (`IND_ESCRI = 1`) agregando o item no período. Presumido por caixa (`IND_REG_CUM=1`): receita em `F500/F510` **quando recebida**, detalhe em `F525` | ESCRITURACAO_GRAVE (receita omitida) |
| M2-P02 | Aquisições com direito a crédito (não cumulativo: `COD_INC_TRIB` 1 ou 3) | `C100/C170` com CST 50–56/60–66 **ou** `C190/C191/C195` | INFO (não aproveitar crédito é faculdade, não erro) |
| M2-P03 | Devoluções de venda recebidas | `C100/C170` (entrada com CST de crédito sobre devolução) ou `C190/C191/C195`, ou ajuste de redução da receita, conforme Guia | ALERTA |

### M2.2 B → A

| ID | Registro na EFD-Contrib. | Exigência | Severidade |
|---|---|---|---|
| M2-R01 | `C100` com `CHV_NFE` e `IND_OPER=0` com crédito (CST 50–66 em `C170`) | NF-e autorizada, não cancelada, `dest/CNPJ` = estabelecimento do `0140` | ESCRITURACAO_GRAVE (crédito sem lastro) |
| M2-R02 | `C191/C195` (consolidado de aquisições) | `Σ` das NF-e de entrada do item/fornecedor (`CNPJ_CPF_PART`) no período ≥ `VL_ITEM` consolidado | ESCRITURACAO_GRAVE se exceder (LIM) |
| M2-R03 | `C181/C185` (consolidado de vendas) por item/CST/CFOP | `Σ vProd` das NF-e de saída do item no período (`DT_DOC_INI`–`DT_DOC_FIN`) = `VL_ITEM` (AGR) | ESCRITURACAO_GRAVE se a NF-e for maior (receita a menor) |
| M2-R04 | Crédito sobre aquisição com base que **inclui** o ICMS da operação | Base de crédito = valor do item **sem** o ICMS incidente na aquisição (Leis 10.637/02 e 10.833/03, art. 3º, §2º, III – Lei 14.592/2023) | ESCRITURACAO_GRAVE |

### M2.3 De-para

| EFD-Contrib. | NF-e | Relação | Observação |
|---|---|---|---|
| `C100.CHV_NFE`, `VL_DOC`, `DT_DOC` | `@Id`, `vNF`, `dhEmi` | IDT | — |
| `C170.CST_PIS`/`CST_COFINS` (saída própria) | `det/imposto/PIS/CST`, `COFINS/CST` | IDT | Divergência que reduz contribuição: ESCRITURACAO_GRAVE |
| `C170.VL_BC_PIS`/`ALIQ_PIS`/`VL_PIS` (saída própria) | `vBC`/`pPIS`/`vPIS` | IDT | Exclusão do ICMS da base de débito (STF Tema 69) pode gerar base menor que a do XML: **ALERTA** com hipótese legítima |
| `C181.CST_PIS`, `CFOP`, `VL_ITEM` | itens agrupados | AGR | — |
| `C180.COD_NCM` / `C190.COD_NCM` | `NCM` | IDT | Relevante para monofásicos/alíquota zero (CST 04/06) |
| `0200` | `prod` | IDT | ver ENT-023 |
| CST 04 (monofásico revenda), 06 (alíquota zero) na saída | NCM do item na lista legal correspondente | PAR | ALERTA (verificação de enquadramento por NCM depende de tabela legal externa) |

### M2.4 Exclusivos legítimos

| Só na NF-e | Só na EFD-Contrib. |
|---|---|
| Remessas, transferências, retornos, bonificações sem receita (o Guia admite não escriturar documentos sem repercussão nas contribuições) | Receitas sem documento fiscal (`F100`: financeiras, aluguéis, etc.) |
| Aquisições sem crédito (cumulativo, ou opção de não creditar) | Créditos de depreciação (`F120`), ativo (`F130`), estoque de abertura (`F150`) |
| No presumido por caixa: a NF-e vendida e ainda **não recebida** | Retenções (`F600`), CPRB (Bloco P), controle de créditos (`1100/1500`) |

---

## M3. EFD ICMS/IPI ⇄ EFD-Contribuições (mesmos documentos, visões diferentes)

### M3.1 A → B / B → A

| ID | Regra | Tipo | Severidade |
|---|---|---|---|
| M3-01 | Para cada `C100` com chave presente nas duas (quando EFD-Contrib. individualizada), atributos idênticos: `DT_DOC`, `VL_DOC`, participante (CNPJ), `COD_SIT`, `IND_OPER`, `IND_EMIT`. | IDT | ESCRITURACAO_GRAVE (valor) / ALERTA (data) |
| M3-02 | Documento cancelado (`COD_SIT 02/03`) na EFD ICMS/IPI **não** pode estar como regular na EFD-Contrib. (e vice-versa). | IDT | ESCRITURACAO_GRAVE |
| M3-03 | Saídas da EFD ICMS/IPI com CFOP de **receita** ⇒ chave presente na EFD-Contrib. (individualizada) ou valor contido no consolidado `C180/C181` do item. | PRE→ | ESCRITURACAO_GRAVE |
| M3-04 | Entradas com crédito na EFD-Contrib. ⇒ mesma chave escriturada como entrada (`IND_OPER=0`) na EFD ICMS/IPI de algum estabelecimento do `0140` no mesmo mês ou em mês anterior próximo. | ←PRE | ALERTA (crédito de PIS/COFINS de mercadoria que não entrou no estoque fiscal) |
| M3-05 | Mesmo `COD_ITEM` ⇒ mesmo `COD_NCM`, `TIPO_ITEM` e descrição. | IDT | ALERTA |
| M3-06 | **AGR mensal de receita**: `Σ (C190.VL_OPR de saídas com CFOP de receita, todos os estabelecimentos)` − ICMS-ST − IPI (que não compõem a receita) ≈ `Σ receitas de vendas de mercadorias na EFD-Contrib.` (C170/C181 + parcelas de M210/M400). | AGR | Diferença material não explicada: ESCRITURACAO_GRAVE; pequena: ALERTA |
| M3-07 | CIAP (`G125` entrada de bem, `0300`) ⇔ `F120`/`F130` do mesmo bem (`IDENT_BEM_IMOB`, `COD_CTA`). | PRE→ (condicional) | ALERTA |

> **Lista de CFOP de receita:** o motor precisa de uma **tabela parametrizável CFOP → gera receita (S/N)** construída a partir da Tabela de CFOP oficial (Convênio s/nº de 1970, Anexo). Não há na norma uma lista oficial "CFOP de receita". Essa tabela é decisão técnica do projeto e deve ser documentada e revisada por contador.

### M3.2 Exclusivos legítimos

| Só na EFD ICMS/IPI | Só na EFD-Contrib. |
|---|---|
| Documentos sem repercussão em PIS/COFINS (remessas, transferências, retornos) | Serviços (Bloco A – NFS-e), Bloco F, Bloco I |
| Inventário (H), produção (K), CIAP (G) | Contribuição retida, CPRB, créditos extemporâneos |
| Estabelecimento com IE mas não incluído no período de apuração de contribuições (não deveria ocorrer – ver ENT-001) | Estabelecimento sem IE (só serviços) no `0140` |

---

## M4. NFS-e ⇄ EFD-Contribuições

### M4.1 A → B

| ID | Universo NFS-e | Deve existir | Severidade |
|---|---|---|---|
| M4-P01 | Emitidas (`prest/CNPJ` = estabelecimento do `0140`), não canceladas/substituídas | `A100` com `IND_OPER=1`, `IND_EMIT=0`, `CHV_NFSE` (competência). Presumido caixa: `F500/F525` no recebimento. Presumido competência consolidada: `F550` | ESCRITURACAO_GRAVE |
| M4-P02 | Tomadas (`toma/CNPJ` = estabelecimento), não cumulativo, serviço como insumo | `A100` `IND_OPER=0` com crédito (opcional) | INFO |
| M4-P03 | Emitidas com PIS/COFINS retidos | `A100.VL_PIS_RET`/`VL_COFINS_RET` **e** `F600` (`CNPJ` = tomador) | ESCRITURACAO_GRAVE se a retenção foi deduzida sem `F600` |

### M4.2 B → A

| ID | Registro | Exigência | Severidade |
|---|---|---|---|
| M4-R01 | `A100` com `CHV_NFSE` | NFS-e existente, não cancelada/substituída, CNPJ correspondente ao papel | ESCRITURACAO_GRAVE |
| M4-R02 | `A170` com crédito (CST 50–56) | NFS-e tomada existente (`toma/CNPJ` = estabelecimento) | ESCRITURACAO_GRAVE |
| M4-R03 | `F600` (retenção sofrida) – fonte pagadora `CNPJ` | NFS-e emitida para esse tomador no período, com retenção destacada; `Σ VL_RET_PIS/COFINS ≤ Σ` retenções nas NFS-e | LIM – ESCRITURACAO_GRAVE |

### M4.3 De-para

| EFD-Contrib. `A100`/`A170` | NFS-e (DPS/NFS-e – Anexo I v1.01) | Relação |
|---|---|---|
| `CHV_NFSE` | chave da NFS-e | IDT |
| `NUM_DOC` / `SER` | número da NFS-e / série da DPS | IDT |
| `DT_DOC` / `DT_EXE_SERV` | `dhEmi` / `dCompet` | IDT |
| `COD_PART` → CNPJ | `toma` (prestado) / `prest` (tomado) | IDT |
| `VL_DOC` | valor do serviço (`vServ`) | IDT |
| `VL_DESC` | descontos (incondicionados) | IDT |
| `VL_ISS` | ISS apurado (`vISSQN`) | IDT |
| `VL_PIS` / `VL_COFINS` | `tribFed/piscofins` – valores | IDT |
| `VL_PIS_RET` / `VL_COFINS_RET` | PIS/COFINS retidos (`tpRetPisCofins`) | IDT |
| `A170.CST_PIS` / `CST_COFINS` | `tribFed/piscofins/CST` | IDT (prestado) |

### M4.4 Exclusivos legítimos
- **Só na NFS-e:** NFS-e de estabelecimento de terceiro (CNPJ fora do `0140`); NFS-e cancelada/substituída; no presumido por caixa, NFS-e ainda não recebida.
- **Só na EFD-Contrib. (Bloco A):** documentos de serviço de municípios não integrados ao padrão nacional, ou emitidos antes da adesão (NFS-e municipal sem chave nacional). O `A100` existe sem chave nacional. É **ALERTA de cobertura**, não omissão.

---

## M5. NFS-e ⇄ EFD ICMS/IPI

| ID | Regra | Severidade |
|---|---|---|
| M5-01 | Estabelecimento do **DF**: NFS-e/ISS deve estar no **Bloco B** da EFD ICMS/IPI. | ESCRITURACAO_GRAVE |
| M5-02 | Fora do DF: Bloco B com movimento ⇒ incoerência. | ALERTA |
| M5-03 | NFS-e recebida por meio eletrônico ⇒ valor compõe `1601.TOT_ISS` da EFD ICMS/IPI do estabelecimento. | ALERTA |

---

## M6. DIMP ⇄ (NF-e, NFC-e, NFS-e, EFD ICMS/IPI 1601, EFD-Contrib. F525, ECD)

A DIMP é **evidência externa**. Ela não é "escriturada" pelo contribuinte, mas tudo o que ela registra como recebido precisa ter **explicação** nas outras obrigações.

### M6.1 DIMP → demais (o recebido deve ter lastro)

| ID | Valor DIMP (por declarante, estabelecimento/UF, mês) | Deve corresponder a | Relação | Severidade |
|---|---|---|---|---|
| M6-01 | Total transacionado por instituição de pagamento X | `1601` com `COD_PART_IP` → CNPJ de X: `TOT_VS + TOT_ISS + TOT_OUTROS` | IDT (ou AGR por estabelecimento) | DIMP > 1601: ESCRITURACAO_GRAVE |
| M6-02 | Total transacionado por intermediador Y | `1601` com `COD_PART_IT` → CNPJ de Y | IDT | ESCRITURACAO_GRAVE |
| M6-03 | Total transacionado no mês | `Σ detPag.vPag` das NF-e/NFC-e com `tPag` eletrônico + NFS-e pagas eletronicamente | AGR com tolerância | DIMP > documentos: ALERTA (material: ESCRITURACAO_GRAVE – indício de saída sem documento) |
| M6-04 | Total anual | Receita bruta contábil (ECD) e da ECF | LIM (DIMP ≤ receita + recebimentos sem fato gerador explicados) | ALERTA |
| M6-05 | Presumido em regime de caixa | `F525` com indicação de recebimento via administradora de cartão/instituição (conferir códigos do campo `IND_REC` no Guia) | AGR | ALERTA |

### M6.2 Demais → DIMP (o documentado como eletrônico deve aparecer)

| ID | Origem | Expectativa | Severidade |
|---|---|---|---|
| M6-10 | NF-e/NFC-e com `detPag/card/CNPJ` (credenciadora) | Credenciadora presente na DIMP como declarante | ALERTA |
| M6-11 | NF-e com `indIntermed = 1` e CNPJ do intermediador | Intermediador presente na DIMP | ALERTA |
| M6-12 | `1601` com `COD_PART_IP` X | X presente na DIMP do período | ALERTA |

### M6.3 Exclusivos legítimos (explicam DIMP > documentos)
Transferências entre contas do próprio titular, empréstimos, aportes, adiantamentos de clientes (fato gerador futuro), estornos/chargebacks, recebimentos de vendas de **outro** estabelecimento ou UF, recebimentos de períodos anteriores (vendas a prazo). Todos devem ser **justificados**. O agente lista, não presume.

---

## M7. EFD ICMS/IPI ⇄ ECD

| ID | EFD ICMS/IPI (agregada no ano, todos os estabelecimentos) | ECD | Relação | Severidade |
|---|---|---|---|---|
| M7-01 | `0500.COD_CTA` e `COD_CTA` usados em `C170`, `H010`, `0300` | `I050` analítica com mesmo código e natureza | PRE→ / IDT | ESCRITURACAO_GRAVE |
| M7-02 | `H005.VL_INV` (EFD de fevereiro/ano+1, `MOT_INV=01`), somando `H010` com `IND_PROP ∈ {0,1}` de **todos** os estabelecimentos | `Σ I155.VL_SLD_FIN` (31/12) das contas de estoque referenciadas em `H010.COD_CTA` | IDT | ESCRITURACAO_GRAVE |
| M7-03 | `H010.VL_ITEM_IR` (valor do item para efeitos do Imposto de Renda, quando informado) | Saldo contábil da conta de estoque | IDT (preferencial sobre `VL_ITEM`) | ESCRITURACAO_GRAVE |
| M7-04 | `H010` agrupado por `COD_CTA` | `I155.VL_SLD_FIN` de cada `COD_CTA` | AGR | ALERTA |
| M7-05 | `Σ E110.VL_ICMS_RECOLHER + DEB_ESP` (ano) + `E210` (ST) + `E310` (DIFAL/FCP) | Movimento credor (`I155.VL_CRED`) das contas de ICMS a recolher | AGR | ALERTA |
| M7-06 | `E110.VL_SLD_CREDOR_TRANSPORTAR` de dezembro (todos os estabelecimentos) | Saldo de ICMS a recuperar (`I155`, 31/12) | IDT | ALERTA |
| M7-07 | `Σ E520` IPI a recolher (ano) | Movimento credor de IPI a recolher | AGR | ALERTA |
| M7-08 | Saídas com CFOP de receita (ano), líquidas de IPI e ICMS-ST | Receita bruta de vendas (`I355`, contas de receita) | AGR | ALERTA; material: ESCRITURACAO_GRAVE |
| M7-09 | Saldo CIAP (`G110`) / bens em `0300` | Contas do imobilizado | ALERTA | ALERTA |
| M7-10 | `K200` de 31/12 (quantidades) | `H010` (quantidades) do inventário de 31/12 do mesmo item e posse | IDT [DERIVADA – ambos representam estoque na mesma data] | ALERTA |

**Exclusivos legítimos:** a ECD contém fatos sem documento fiscal (folha, financeiro, provisões). A EFD contém documentos sem reflexo contábil de receita (remessas, transferências, mercadorias de terceiros – `IND_PROP=2`).

---

## M8. EFD-Contribuições ⇄ ECD

| ID | EFD-Contrib. | ECD | Relação | Severidade |
|---|---|---|---|---|
| M8-01 | `0500` (`COD_CTA`, `COD_NAT_CC`, `IND_CTA`) | `I050` | PRE→ / IDT | ESCRITURACAO_GRAVE |
| M8-02 | Receitas por `COD_CTA` (`C181`, `A170`, `F100`, `F500`, `F550`, `M400/M410`, `M800/M810`) – soma anual por conta | Movimento credor líquido das contas de receita (`I155`/`I355`) | AGR | Receita na ECD > EFD-Contrib.: ESCRITURACAO_GRAVE (receita fora da apuração); inverso: ALERTA |
| M8-03 | `M400/M410` (receitas isentas/não tributadas/alíquota zero) com `COD_CTA` | Contas de receita correspondentes existem e movimentaram | PRE→ | ALERTA |
| M8-04 | `F120` (encargos de depreciação) | Depreciação contabilizada no período das contas dos bens | LIM (crédito sobre depreciação ≤ depreciação contábil do bem) | ESCRITURACAO_GRAVE |
| M8-05 | `M200.VL_TOT_CONT_REC` + `M600` (por mês) | Movimento credor das contas de PIS/COFINS a recolher (mensal via `I155`) | AGR | ALERTA |
| M8-06 | `F600` (retenções sofridas) | Contas de tributos retidos a compensar (ativo) | AGR | ALERTA |
| M8-07 | Créditos não cumulativos (`M100/M500`) | Conta de PIS/COFINS a recuperar | AGR | ALERTA |

---

## M9. ECD ⇄ ECF (a relação mais forte – grande parte é recuperação automática)

### M9.1 Recuperação e identidade

| ID | ECD | ECF | Relação | Severidade |
|---|---|---|---|---|
| M9-01 | Hash da **última** ECD válida do período (original ou substituta) | `C040.HASH_ECD` | IDT | ESCRITURACAO_GRAVE (ECF calculada sobre contabilidade que não é a vigente → ECF deve ser retificada) |
| M9-02 | `0000.DT_INI/DT_FIN`, `IND_SIT_ESP` | `0000` da ECF / `C040` | IDT | ESCRITURACAO_GRAVE |
| M9-03 | `I050` / `I051` | `C050` / `C051` | IDT | ESCRITURACAO_GRAVE |
| M9-04 | `I155` (todos os períodos) | `C155` → `K155` | IDT | ESCRITURACAO_GRAVE |
| M9-05 | `I355` | `C355` → `K355` | IDT | ESCRITURACAO_GRAVE |
| M9-06 | Mapeamento `I051` | `K156` / `K356` (saldos por conta referencial) | AGR por conta referencial | ESCRITURACAO_GRAVE |

### M9.2 Demonstrações e resultado

| ID | ECD | ECF | Relação | Severidade |
|---|---|---|---|---|
| M9-10 | `J100` (balanço) | `L100` (real) / `P100` (presumido) – saldos referenciais | AGR (via mapeamento) | ESCRITURACAO_GRAVE |
| M9-11 | `J150` (DRE) – resultado antes do IRPJ/CSLL | `L300` (real) / `P150` (presumido) | IDT | ESCRITURACAO_GRAVE |
| M9-12 | Lucro líquido antes do IRPJ/CSLL | Ponto de partida do e-Lalur (`M300`) e e-Lacs (`M350`) | IDT | ESCRITURACAO_GRAVE |
| M9-13 | `I200.NUM_LCTO` e contas | `M310.COD_CTA` (+ `M312.NUM_LCTO`) – adição/exclusão vinculada a conta/lançamento contábil | PRE← (o lançamento referenciado deve existir na ECD) | ESCRITURACAO_GRAVE |
| M9-14 | Saldo da conta contábil vinculada ao ajuste | Valor do ajuste (`M310.VALOR`) | LIM (ajuste vinculado ≤ movimento/saldo da conta, salvo justificativa) | ALERTA |
| M9-15 | Contas de IRPJ/CSLL (despesa e passivo) | `N630`/`N670` (real) ou `P300`/`P500` (presumido) | IDT | ALERTA |
| M9-16 | Estoque final (`I155`) | `L100`/`P100` (estoque) e `L210` (custos, quando aplicável) | IDT | ESCRITURACAO_GRAVE |

### M9.3 Continuidade (ano a ano)

| ID | Regra | Relação | Severidade |
|---|---|---|---|
| M9-20 | `I155.VL_SLD_INI` do 1º período do ano = `I155.VL_SLD_FIN` do último período da ECD anterior (contas patrimoniais). | CNT | ESCRITURACAO_GRAVE |
| M9-21 | ECF `E010/E015` (saldos finais recuperados da ECF anterior) = saldos iniciais do ano. | CNT | ESCRITURACAO_GRAVE |
| M9-22 | ECF `M010`/`M500.SD_INI_LAL` = `SD_FIM_LAL` da ECF anterior (`E020`) por conta da Parte B. | CNT | ESCRITURACAO_GRAVE (prejuízo fiscal/base negativa “criados” sem origem) |

**Exclusivos legítimos:** a ECF tem e-Lalur/e-Lacs (`M`), cálculo (`N`/`P`), informações econômicas e gerais (`X`/`Y`), que não estão na ECD. A ECD tem lançamentos (`I200/I250`), que a ECF não recupera.

---

## M10. EFD-Contribuições ⇄ ECF

| ID | EFD-Contrib. (agregado do período) | ECF | Relação | Severidade |
|---|---|---|---|---|
| M10-01 | `0110.COD_INC_TRIB`, `IND_REG_CUM` | `0010.FORMA_TRIB`, regime de caixa do presumido | PAR (ver NÚCLEO-1 §5) – no catálogo JSON implementada como `PAR-003`, `PAR-004` e `PAR-005` | ESCRITURACAO_GRAVE |
| M10-02 | **Presumido**: receitas do trimestre (`F500/F550`, `A100`, `C180`, `M400`) | `P200` (receita bruta por atividade/percentual) e `P400` | AGR por trimestre | Receita PIS/COFINS > base presumida: ESCRITURACAO_GRAVE |
| M10-03 | **Real**: `Σ M210.VL_REC_BRT` (ano) + receitas `M400` | Receita bruta em `L300` | AGR | ALERTA (receitas financeiras e outras compõem a EFD-Contrib. mas não a receita bruta) |
| M10-04 | `F600` (PIS/COFINS retidos), fonte pagadora `CNPJ` | `Y570.CNPJ_FON` (mesma fonte pagadora com IRRF/CSLL retidos) | PRE→ (mesmo tomador retentor) | ALERTA |
| M10-05 | Receita por estabelecimento (`0140` + blocos A/C/F) | `Y540` (`CNPJ_ESTAB`, `VL_REC_ESTAB`, CNAE) | AGR por estabelecimento | ALERTA |
| M10-06 | PIS/COFINS como dedução da receita bruta (real) | Deduções na DRE `L300` | IDT aproximada | ALERTA |

---

## M11. EFD ICMS/IPI ⇄ ECF

| ID | EFD ICMS/IPI | ECF | Relação | Severidade |
|---|---|---|---|---|
| M11-01 | Inventário 31/12 (`H005`, `IND_PROP 0/1`, todos os estabelecimentos) | Estoque no `L100`/`P100`; presumido: `Y672.VL_EST` (conferir campo) | IDT | ESCRITURACAO_GRAVE |
| M11-02 | Compras de mercadorias no ano (entradas com CFOP de compra) | Presumido: `Y672.VL_COMPRA_MERC` (conferir campo) | AGR | ALERTA |
| M11-03 | Aquisições de ativo (CFOP 1.551/2.551/3.551) | Presumido: `Y672.VL_COMPRA_ATIVO` (conferir campo) | AGR | ALERTA |
| M11-04 | `IND_ATIV = 0` (industrial) | ECF: atividade econômica/CNAE coerente; `L210` (custos de produção) quando aplicável | PAR | ALERTA |

---

## M12. NF-e / NFS-e ⇄ ECD / ECF (documento direto à contabilidade)

| ID | Documentos | ECD / ECF | Relação | Severidade |
|---|---|---|---|---|
| M12-01 | `Σ` NF-e de saída com CFOP de receita + NFS-e emitidas (ano, líquido de canceladas) | Receita bruta (`I355` contas de receita / `L300` / `P150`) | AGR | ALERTA; material: ESCRITURACAO_GRAVE |
| M12-02 | NF-e de aquisição de ativo (CFOP x.551) | Adições ao imobilizado (`I155.VL_DEB` das contas de imobilizado) | AGR (LIM) | ALERTA |
| M12-03 | NFS-e emitidas com IRRF/CSLL retidos (`vRetIRRF`, `vRetCSLL`) por tomador | ECF `Y570` (`CNPJ_FON` = tomador; `IR_RET`, `CSLL_RET`) | AGR por tomador | ALERTA |
| M12-04 | NFS-e com retenção previdenciária (`vRetCP`) | Conta de INSS retido a compensar (ECD) | AGR | ALERTA |
| M12-05 | NF-e de entrada com evento 210200 (confirmação) | Contas de fornecedores/estoque/custo (ECD) movimentadas | AGR (presença) | ALERTA |
