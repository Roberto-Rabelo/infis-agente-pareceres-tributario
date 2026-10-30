# 01 – Panorama das Obrigações

Para cada obrigação: natureza, base normativa, quem produz o dado, unidade/período e os registros/campos que servem de **chave de ligação** com as demais. Campos citados conforme os leiautes listados no [README](README.md#2-versões-oficiais-vigentes-verificadas-fonte-primária).

> Este agente não lê os arquivos originais: os registros e campos abaixo são os que ele espera receber no **JSON normalizado** enviado pelos agentes de origem (contrato de entrada – P18/D24 da constitution). Os campos de ligação listados aqui são o mínimo que esse contrato precisa trazer para que os cruzamentos sejam possíveis.

---

## 1. Visão de dependências

```
                 Documentos fiscais (fato gerador)                    Meios de pagamento
      ┌──────────────────────┬───────────────────────┐                ┌────────────────┐
      │ NF-e / NFC-e (55/65) │ NFS-e (Padrão Nac.)   │                │ DIMP (inst. de │
      │ ICMS, IPI, PIS/COF,  │ ISS, PIS/COF, retenç. │                │ pagamento e    │
      │ IBS/CBS/IS (RTC)     │ IBS/CBS (RTC)         │                │ intermediador) │
      └──────────┬───────────┴───────────┬───────────┘                └───────┬────────┘
                 │                       │                                    │
       ┌─────────▼─────────┐   ┌─────────▼──────────┐                          │
       │ EFD ICMS/IPI      │   │ EFD-Contribuições  │◄── NF-e (bloco C)        │
       │ (mensal, por      │   │ (mensal, por CNPJ  │◄── NFS-e (bloco A)       │
       │  estabelecimento) │   │  matriz)           │                          │
       │ Reg. 1601 ◄───────┼───┼────────────────────┼──────────────────────────┘
       └─────────┬─────────┘   └─────────┬──────────┘
                 │ COD_CTA / saldos       │ COD_CTA (0500) / receitas
                 ▼                        ▼
           ┌──────────────────────────────────────┐
           │ ECD (anual – contabilidade)          │
           └─────────────────┬────────────────────┘
                             │ recuperada por hash (Bloco C da ECF)
                             ▼
           ┌──────────────────────────────────────┐
           │ ECF (anual – IRPJ/CSLL, e-Lalur/Lacs)│
           └──────────────────────────────────────┘
```

---

## 2. NF-e / NFC-e (modelos 55 e 65)

| Item | Conteúdo |
|---|---|
| Base | Ajuste SINIEF 07/2005 (NF-e); Manual de Orientação do Contribuinte (MOC) – leiaute 4.00; Notas Técnicas do Portal Nacional |
| Quem produz | Emitente; autorizada pela SEFAZ da UF (ou SVRS/SVAN/contingência) |
| Natureza | Documento fiscal eletrônico (XML assinado + protocolo de autorização) |
| Chave | Chave de acesso de 44 posições (`infNFe/@Id`) |

**Campos de ligação mais relevantes (grupos do leiaute):**

| Grupo | Campos | Liga com |
|---|---|---|
| `ide` | `mod`, `serie`, `nNF`, `dhEmi`, `dhSaiEnt`, `tpNF` (0 entrada / 1 saída), `idDest`, `finNFe` (1 normal, 2 complementar, 3 ajuste, 4 devolução), `NFref` | EFD ICMS/IPI C100 (`COD_MOD`, `SER`, `NUM_DOC`, `DT_DOC`, `DT_E_S`, `IND_OPER`), C113 (documento referenciado) |
| `emit` / `dest` | CNPJ/CPF, `IE`, `CRT` (1 SN, 2 SN excesso sublimite, 3 regime normal, 4 MEI), endereço/UF | EFD 0000/0150; EFD-Contribuições 0150; enquadramento (SN dispensado de EFD) |
| `det/prod` | `cProd`, `NCM`, `CEST`, `CFOP`, `uCom`/`uTrib`, `qCom`, `vProd`, `cBenef` | EFD 0200 (`COD_NCM`), 0190/0220, C170, C190 (`CFOP`) |
| `det/imposto` | ICMS (`CST`/`CSOSN`, `vBC`, `pICMS`, `vICMS`, ST, FCP, DIFAL `ICMSUFDest`), IPI, PIS, COFINS (`CST`), grupo IBS/CBS/IS (NT 2025.002) | EFD C170/C190; EFD-Contribuições C170/C181/C185; E300 (DIFAL) |
| `total/ICMSTot` | `vBC`, `vICMS`, `vBCST`, `vST`, `vProd`, `vFrete`, `vDesc`, `vIPI`, `vPIS`, `vCOFINS`, `vNF` | EFD C100 (`VL_DOC`, `VL_BC_ICMS`, `VL_ICMS`, `VL_ICMS_ST`, `VL_IPI`, `VL_PIS`, `VL_COFINS`) |
| `pag/detPag` | `tPag` (ex.: 03 cartão de crédito, 04 cartão de débito, 17 PIX dinâmico, 20 PIX estático, 90 sem pagamento, 99 outros), `vPag`, grupo `card` (CNPJ da credenciadora, `tBand`, `cAut`) | DIMP; EFD ICMS/IPI registro 1601 |
| `infIntermed` | `indIntermed` (no `ide`) e CNPJ do intermediador | DIMP (intermediadores – Conv. ICMS 134/16); EFD 1601 (`COD_PART_IT`) |
| Eventos | Cancelamento (110111), Carta de Correção (110110), Manifestação do Destinatário (210200 confirmação, 210210 ciência, 210220 desconhecimento, 210240 operação não realizada) | EFD C100 `COD_SIT`; escrituração de entradas |

---

## 3. NFS-e – Padrão Nacional

| Item | Conteúdo |
|---|---|
| Base | LC 116/2003 (ISS); Sistema Nacional NFS-e (Ambiente de Dados Nacional – ADN, Emissor Público Nacional); LC 214/2025 (adoção do padrão nacional e campos IBS/CBS); Anexo I – leiaute DPS/NFS-e v1.01 |
| Quem produz | Prestador emite a **DPS** (Declaração de Prestação de Serviço); o sistema gera a NFS-e |
| Chave | Chave de acesso da NFS-e (identificador `infNFSe/@Id`) e identificador da DPS |

**Campos de ligação (Anexo I, v1.01):**

| Grupo DPS | Campos | Liga com |
|---|---|---|
| Identificação | `dhEmi`, `dCompet` (competência), `serie`, `nDPS`, `cLocEmi` | EFD-Contribuições A100 (`DT_DOC`, `DT_EXE_SERV`), ECD (competência da receita) |
| Prestador/Tomador/Intermediário | CNPJ/CPF, inscrição municipal, regime (`opSimpNac`, regime especial) | EFD-Contribuições 0150/A100; ECF (receita por atividade) |
| Serviço | `cTribNac` (código de tributação nacional – item da lista LC 116), `cTribMun`, `cNBS`, local da prestação | ECF Y540 (CNAE), regras de incidência ISS |
| Valores/Tributos | `vServ`, descontos, `tribMun` (`tribISSQN`, `tpRetISSQN`, `pAliq`), `tribFed` (PIS/COFINS: `CST`, base, alíquotas, valores, `tpRetPisCofins`; retenções `vRetCP`, `vRetIRRF`, `vRetCSLL`), grupo IBS/CBS (RTC) | EFD-Contribuições A100/A170 (`CST_PIS`, `CST_COFINS`, `VL_PIS`, `VL_COFINS`, `VL_PIS_RET`, `VL_COFINS_RET`, `VL_ISS`), F600 (retenções), ECD |
| Eventos | Cancelamento, substituição | EFD-Contribuições A100 `COD_SIT` |

> A NFS-e **não** é escriturada na EFD ICMS/IPI, exceto: (a) **Bloco B** – exclusivo de contribuintes do **Distrito Federal** (ISS na EFD) [MANUAL – Guia Prático EFD ICMS/IPI, Bloco B]; (b) valor de ISS em vendas com pagamento eletrônico no registro **1601** (`TOT_ISS`).

---

## 4. DIMP – Declaração de Informações de Meios de Pagamento

| Item | Conteúdo |
|---|---|
| Base | Convênio ICMS 134/2016; Ato COTEPE/ICMS 65/2018 (especificação técnica) – **Versão 10 a partir de 01/06/2026** (redação do Ato COTEPE/ICMS 124/25) |
| Quem produz | **Instituições e intermediadores financeiros e de pagamento** (integrantes ou não do SPB) e **intermediadores de serviços e de negócios** (marketplaces) – **não** o contribuinte vendedor |
| Escopo | Transações com cartões de débito, crédito, de loja (*private label*), transferência de recursos, transações do Sistema de Pagamentos Instantâneos (PIX) e demais instrumentos eletrônicos, e transações comerciais/serviços intermediadas, realizadas por PJ (CNPJ) ou PF (CPF), **ainda que não inscritas no cadastro de ICMS** (ementa do Ato COTEPE/ICMS 65/18) |
| Destino | SEFAZ de cada UF (arquivo por UF) |

**Consequência para o agente:** a DIMP é um dado **de terceiro** sobre o contribuinte analisado. Só será insumo se o fluxo de agentes fornecer o extrato da DIMP (ex.: obtido pelo contribuinte junto à SEFAZ ou pela própria instituição). O cruzamento é **receita recebida por meio eletrônico x documentos fiscais emitidos x escrituração**. O lado declaratório do próprio contribuinte é o **registro 1601 da EFD ICMS/IPI** (operações com instrumentos de pagamento eletrônico: participante instituição de pagamento `COD_PART_IP`, intermediador `COD_PART_IT`, `TOT_VS` – vendas/prestações no campo do ICMS, `TOT_ISS`, `TOT_OUTROS`).

---

## 5. EFD ICMS/IPI (SPED Fiscal)

| Item | Conteúdo |
|---|---|
| Base | Convênio ICMS 143/2006; Ajuste SINIEF 02/2009; Ato COTEPE/ICMS 44/2018 (manual de orientação do leiaute); Ato COTEPE/ICMS 79/2025 (leiaute versão 020 – 2026); Guia Prático v3.2.2 |
| Periodicidade | Mensal, **por estabelecimento** (CNPJ + IE) |
| Obrigados | Contribuintes do ICMS e/ou IPI conforme legislação; optantes do Simples Nacional, em regra, dispensados (Ajuste SINIEF 02/09 – verificar exigência específica da UF) |

**Blocos e registros-chave:**

| Bloco | Registros | Função |
|---|---|---|
| 0 | `0000` (`COD_VER`, `COD_FIN` 0 original / 1 substituta, `DT_INI`, `DT_FIN`, `CNPJ`, `UF`, `IE`, `IND_PERFIL` A/B/C, `IND_ATIV` 0 industrial ou equiparado / 1 outros), `0150` participantes, `0190` unidades, `0200` itens (`TIPO_ITEM`, `COD_NCM`), `0220` fatores de conversão, `0300/0305` bens do CIAP, `0500` plano de contas (para `COD_CTA`) | Cadastro |
| C | `C100` documento (NF-e 55, NFC-e 65 etc.: `IND_OPER`, `IND_EMIT`, `COD_SIT`, `CHV_NFE`, `DT_DOC`, `DT_E_S`, valores), `C101` (DIFAL/FCP), `C113` (documento referenciado), `C170` itens, `C190` analítico por CST_ICMS/CFOP/alíquota, `C197` ajustes por documento | Mercadorias |
| D | CT-e, BP-e, comunicação/telecom (`D100`, `D190`, `D500`, `D590` …) | Transporte/comunicação |
| E | `E100/E110` apuração ICMS próprio, `E111/E116` ajustes e obrigações a recolher, `E200/E210` ST por UF, `E300/E310` DIFAL/FCP (EC 87/2015), `E500/E510/E520` IPI | Apuração |
| G | `G110/G125` CIAP | Crédito do ativo |
| H | `H005` (`DT_INV`, `VL_INV`, `MOT_INV`), `H010` (item, quantidade, valor, `IND_PROP`, `COD_CTA`) | Inventário |
| K | `K200` estoque escriturado, `K230/K235` produção, etc. | Controle da produção e do estoque (conforme cronograma de obrigatoriedade) |
| 1 | `1010` (indicadores de obrigatoriedade dos registros do bloco 1), `1601` (instrumentos de pagamento eletrônico) | Informações complementares |
| B | ISS – **somente Distrito Federal** | ISS |

---

## 6. EFD-Contribuições

| Item | Conteúdo |
|---|---|
| Base | IN RFB 1.252/2012; Guia Prático EFD-Contribuições v1.35 |
| Periodicidade | Mensal, **centralizada no CNPJ da matriz** (estabelecimentos em `0140`) |
| Obrigados | PJ de direito privado em geral (lucro real e presumido/arbitrado); dispensadas, entre outras, as optantes do Simples Nacional (IN RFB 1.252/2012) |

**Registros-chave:**

| Bloco | Registros | Função |
|---|---|---|
| 0 | `0000` (`TIPO_ESCRIT` 0 original / 1 retificadora, `NUM_REC_ANTERIOR`, `IND_NAT_PJ`, `IND_ATIV`), `0110` (`COD_INC_TRIB` 1 não cumulativo / 2 cumulativo / 3 ambos; `IND_APRO_CRED` 1 apropriação direta / 2 rateio proporcional; `COD_TIPO_CONT`; `IND_REG_CUM` 1 caixa / 2 competência consolidada / 9 competência detalhada), `0111` (receita bruta para rateio), `0140` estabelecimentos, `0150`, `0200`, `0500` plano de contas | Regime e cadastros |
| A | `A100` documento de serviço (inclui `CHV_NFSE`), `A170` itens (`CST_PIS`, `CST_COFINS`, `NAT_BC_CRED`) | Serviços sujeitos ao ISS |
| C | `C010` (`IND_ESCRI`: consolidado C180/C190 ou individualizado C100/C170), `C100/C170`, `C180/C181/C185` (consolidação de vendas por NF-e), `C190/C191/C195` (consolidação de aquisições/devoluções por NF-e), NFC-e (`C400`… / `C860`… conforme modelo) | Mercadorias |
| D | Transporte e comunicação | |
| F | `F100` demais operações, `F120/F130` créditos sobre ativo imobilizado, `F150` estoque de abertura, `F200` atividade imobiliária, `F500/F510` (lucro presumido – **regime de caixa**), `F525` composição da receita (caixa), `F550/F560` (lucro presumido – competência consolidada), `F600` contribuição retida na fonte, `F700` deduções, `F800` créditos de eventos de incorporação/fusão/cisão | Demais operações |
| I | Instituições financeiras e assemelhadas | |
| M | `M100/M105` créditos PIS (`NAT_BC_CRED`), `M200/M210` contribuição PIS apurada, `M400/M410` receitas isentas/não alcançadas/alíquota zero/suspensão (PIS), `M500…M810` (COFINS, equivalentes) | Apuração |
| P | CPRB | Contribuição previdenciária sobre receita |
| 1 | `1100/1500` controle de créditos fiscais (PIS/COFINS) de períodos anteriores, `1300/1700` controle de retenções | Controle |

**Tabela de CST PIS/COFINS (Guia Prático):** saídas `01`–`09` e `49`; entradas com crédito `50`–`56`, crédito presumido `60`–`66`, `67`; sem crédito `70`–`75`; `98`/`99` outras.

---

## 7. ECD – Escrituração Contábil Digital

| Item | Conteúdo |
|---|---|
| Base | Decreto 6.022/2007; IN RFB 2.003/2021; Manual de Orientação do Leiaute 9 (ADE Cofis nº 1/2026) |
| Periodicidade | Anual (ou período de situação especial) |
| Obrigados | Conforme IN RFB 2.003/2021 (ex.: lucro real; lucro presumido nas hipóteses da IN; imunes/isentas nas hipóteses da IN); dispensadas, entre outras, optantes do Simples Nacional |

**Registros-chave:**

| Registro | Campos | Uso |
|---|---|---|
| `0000` | `DT_INI`, `DT_FIN`, `CNPJ`, `IND_SIT_ESP`, `IND_SIT_INI_PER`, `IND_FIN_ESC` (0 original / 1 substituta), `COD_HASH_SUB`, `IND_GRANDE_PORTE`, `TIP_ECD` (0 não participante de SCP / 1 sócia ostensiva / 2 SCP), `COD_SCP`, `IDENT_MF`, `IND_ESC_CONS`, `IND_CENTRALIZADA`, `IND_MUDANC_PC`, `COD_PLAN_REF` | Identificação e plano referencial |
| `I010` | `IND_ESC` (G, R, A, B, Z), `COD_VER_LC` (9.00) | Forma de escrituração |
| `I050` | `COD_NAT` (01 ativo, 02 passivo, 03 PL, 04 resultado, 05 compensação, 09 outras), `IND_CTA` (S sintética / A analítica), `NIVEL`, `COD_CTA`, `COD_CTA_SUP` | Plano de contas |
| `I051` | Mapeamento para o plano referencial | Ponte para a ECF |
| `I150/I155` | Saldos periódicos: `VL_SLD_INI`/`IND_DC_INI`, `VL_DEB`, `VL_CRED`, `VL_SLD_FIN`/`IND_DC_FIN` | Base para ECF (K155) |
| `I200/I250` | Lançamento (`NUM_LCTO`, `DT_LCTO`, `VL_LCTO`, `IND_LCTO` N normal / E encerramento / X extemporâneo) e partidas (`COD_CTA`, `VL_DC`, `IND_DC`, `HIST`) | Diário |
| `I350/I355` | Saldos das contas de resultado antes do encerramento | Base para ECF (K355) |
| `J100/J150/J210/J800` | Balanço, DRE, DLPA/DMPL, outras demonstrações/notas | Demonstrações |
| `J900/J930/J932` | Termo de encerramento, signatários (contabilista e responsável), auditor | Autenticação |

---

## 8. ECF – Escrituração Contábil Fiscal

| Item | Conteúdo |
|---|---|
| Base | IN RFB 2.004/2021; Manual de Orientação do **Leiaute 12** (anexo ao ADE Cofis nº 2/2026) |
| Periodicidade | Anual |
| Obrigados | Todas as PJ, inclusive imunes e isentas, exceto, entre outras, optantes do Simples Nacional, órgãos públicos e PJ inativas (IN RFB 2.004/2021) |

**Registros-chave:**

| Bloco/Registro | Campos | Uso |
|---|---|---|
| `0000` | `COD_VER`, `CNPJ`, `IND_SIT_INI_PER`, `SIT_ESPECIAL`, `DT_INI`, `DT_FIN`, `RETIFICADORA`, `NUM_REC`, `TIP_ECF`, `COD_SCP`; Leiaute 12: indicação de **CEBAS** para entidades de assistência social, saúde ou educação | Identificação |
| `0010` | `FORMA_TRIB` (1 real, 2 real/arbitrado, 3 presumido/real, 4 presumido/real/arbitrado, 5 presumido, 6 arbitrado, 7 presumido/arbitrado, 8 imune, 9 isenta), `FORMA_APUR` (T/A), `COD_QUALIF_PJ`, `FORMA_TRIB_PER` (por trimestre), `TIP_ESC_PRE` (C contábil / L livro caixa), `TIP_ENT`, `APUR_CSLL`, indicador de regime de reconhecimento de receitas (caixa/competência) para presumido | Parâmetros de tributação |
| Bloco C | `C040` (identificação e **hash** da ECD recuperada), `C050/C051/C155/C355` | Dados recuperados da ECD |
| Bloco E | Saldos da ECF anterior e da Parte B | Continuidade |
| Bloco J/K | `J050/J051` plano de contas e mapeamento; `K155/K156`, `K355/K356` saldos contábeis e referenciais | Balanço e resultado |
| Bloco L | `L100` balanço referencial, `L200`, `L210`, `L300` DRE (lucro real) | Lucro real |
| Bloco M | e-Lalur/e-Lacs: `M010` contas da Parte B, `M300/M350` Parte A, `M305/M310/M312` vínculo com contas contábeis, `M410`, `M500` controle de saldos da Parte B | Ajustes fiscais |
| Bloco N | `N500`…`N670` cálculo IRPJ/CSLL lucro real, `N620/N660` estimativas mensais | Apuração |
| Bloco P | `P100` balanço, `P150` DRE, `P200` base IRPJ, `P300` IRPJ, `P400` base CSLL, `P500` CSLL | Lucro presumido |
| Bloco Q | Livro Caixa (presumido sem escrituração contábil) | |
| Bloco T / U | Arbitrado / Imunes e isentas | |
| Bloco X / Y | Informações econômicas e gerais (ex.: `Y540` receita por CNAE, `Y570` IRRF/CSLL retidos, `Y600` sócios, `Y671/Y672` outras informações lucro real/presumido) | |
