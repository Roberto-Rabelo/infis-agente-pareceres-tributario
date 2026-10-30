# 03 – Regras de Cruzamento entre Obrigações

Núcleo do agente de parecer. Cada regra indica **origem → destino**, a **condição**, as **hipóteses legítimas de divergência** (que o parecer deve citar antes de concluir) e a **severidade**.

Convenção de IDs: `CRZ-<ORIGEM>-<DESTINO>-<nnn>`.

> **Atenção:** quase todas as regras de cruzamento são **[DERIVADA]** – os manuais definem cada lado, mas a regra “cruzada” é consequência lógica dessas definições. As exceções que tornam a divergência legítima estão listadas em cada regra e devem ser avaliadas antes de qualquer conclusão.

---

## 0. Premissas de casamento (matching)

| Premissa | Regra |
|---|---|
| Chave primária de documento | NF-e/NFC-e: chave de 44 posições (`CHV_NFE` na EFD ICMS/IPI e EFD-Contribuições). NFS-e: chave da NFS-e (`CHV_NFSE` no `A100` da EFD-Contribuições). |
| Chave secundária (quando não há chave) | CNPJ emitente + modelo + série + número + data de emissão. |
| Granularidade | EFD ICMS/IPI é **por estabelecimento** (CNPJ completo + IE); EFD-Contribuições, ECD e ECF são **centralizadas** (CNPJ base/matriz). Totais da EFD ICMS/IPI devem ser **somados entre estabelecimentos** antes de comparar com as demais. |
| Período | EFD ICMS/IPI e EFD-Contribuições: mensal. ECD/ECF: anual (ou situação especial). Ao comparar mensal × anual, agregar os 12 meses (ou os meses do período de situação especial). |
| Data de referência | Saídas: data de emissão/saída. Entradas na EFD ICMS/IPI: escrituração pela **data de entrada** (`DT_E_S`). NFS-e: competência (`dCompet`). Contabilidade: regime de competência (ECD). |
| Tolerância | Nenhuma norma define tolerância para estes cruzamentos. A tolerância deve ser **parâmetro configurável** e explicitada no parecer. |

---

## 1. Enquadramento e regime (aplicável a todas)

| ID | Origem → Destino | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|---|
| CRZ-REG-001 | NF-e (`emit/CRT`) → existência de EFD ICMS/IPI, EFD-Contribuições | `CRT = 3` (regime normal) ⇒ espera-se EFD-Contribuições (IN RFB 1.252/2012) e, se contribuinte de ICMS/IPI, EFD ICMS/IPI (Ajuste SINIEF 02/09). `CRT = 1/4` (Simples/MEI) ⇒ em regra dispensado de ambas. | [NORMA] | Exclusão do Simples no curso do período; exigência específica de UF; inatividade | ESTRUTURA_GRAVE (obrigação ausente) |
| CRZ-REG-002 | ECF `0010.FORMA_TRIB` → EFD-Contribuições `0110.COD_INC_TRIB` | Lucro presumido ou arbitrado ⇒ PIS/COFINS **cumulativo** (`COD_INC_TRIB = 2`) – Lei 10.637/2002, art. 8º, II; Lei 10.833/2003, art. 10, II. | [NORMA] | Nenhuma para a regra geral | ESCRITURACAO_GRAVE |
| CRZ-REG-003 | ECF `FORMA_TRIB` (real) → EFD-Contribuições `COD_INC_TRIB` | Lucro real ⇒ em regra não cumulativo (`1`) ou misto (`3`). `COD_INC_TRIB = 2` exige que **todas** as receitas estejam nas exceções do art. 8º da Lei 10.637/02 e art. 10 da Lei 10.833/03. | [NORMA] | Atividades listadas nas exceções legais | ALERTA |
| CRZ-REG-004 | ECF (presumido – regime de reconhecimento de receitas) → EFD-Contribuições `0110.IND_REG_CUM` | Opção pelo regime de **caixa** no presumido deve ser a mesma para IRPJ, CSLL, PIS e COFINS (`IND_REG_CUM = 1`, registros `F500/F510/F525`). | [NORMA] (IN SRF 104/1998 – conferir dispositivo consolidado na IN RFB 1.700/2017) | Nenhuma | ESCRITURACAO_GRAVE |
| CRZ-REG-005 | ECF `FORMA_TRIB`/ECD | Lucro real ⇒ ECD obrigatória (IN RFB 2.003/2021) e recuperada no Bloco C da ECF. Presumido com ECD ⇒ ECF deve usar `TIP_ESC_PRE = C`; sem ECD e com Livro Caixa ⇒ `L` + Bloco Q. | [NORMA]/[MANUAL] | Presumido não obrigado à ECD | ESTRUTURA_GRAVE |
| CRZ-REG-006 | EFD ICMS/IPI `0000` (CNPJ/IE/UF) → NF-e `emit` | CNPJ, IE e UF do estabelecimento na EFD devem coincidir com os do emitente das NF-e de emissão própria daquele estabelecimento. | [DERIVADA] | Alteração cadastral no mês | ESTRUTURA_GRAVE |
| CRZ-REG-007 | EFD ICMS/IPI `0000.IND_ATIV` → NF-e emitidas | `IND_ATIV = 1` (não industrial) com NF-e de saída com IPI destacado como contribuinte. | [DERIVADA] | Equiparação a industrial (RIPI) mal informada | ALERTA |
| CRZ-REG-008 | ECD `0000.COD_PLAN_REF` → ECF `0010` (`FORMA_TRIB`, `COD_QUALIF_PJ`) | O plano referencial usado no mapeamento da ECD deve ser compatível com a forma de tributação e a qualificação da PJ na ECF. | [MANUAL] | — | ESTRUTURA_GRAVE |
| CRZ-REG-009 | Períodos / situação especial | `IND_SIT_ESP`/`DT_SIT_ESP` (cisão, fusão, incorporação, extinção) devem ser coerentes entre ECD e ECF e com o encerramento das EFDs mensais. | [MANUAL] | — | ESTRUTURA_GRAVE |

---

## 2. NF-e/NFC-e → EFD ICMS/IPI

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-NFE-EFD-001 | Toda NF-e/NFC-e **de emissão própria autorizada** no período deve estar no `C100` com `IND_EMIT = 0`, `CHV_NFE` igual e `COD_SIT` = `00` (ou `01` extemporâneo, `06/07` complementar, `08` regime especial). | [MANUAL] Guia – C100 | NF-e cancelada (ver 002) | ESCRITURACAO_GRAVE (omissão de saída/entrada) |
| CRZ-NFE-EFD-002 | NF-e com evento de **cancelamento** homologado deve estar com `COD_SIT = 02` (ou `03` se extemporâneo), sem valores. Escriturada como regular ⇒ débito/crédito indevido. Escriturada como cancelada sem evento ⇒ **omissão de débito**. | [MANUAL] | — | ESCRITURACAO_GRAVE |
| CRZ-NFE-EFD-003 | NF-e **denegada** ⇒ `COD_SIT = 04`. Numeração **inutilizada** ⇒ `COD_SIT = 05`. | [MANUAL] | NT 2023.002 v1.02 (27/10/2026) elimina os serviços de inutilização – a partir da vigência dessa NT, não haverá novas inutilizações | ESTRUTURA_GRAVE |
| CRZ-NFE-EFD-004 | Valores do `C100` = totais do XML: `VL_DOC`↔`vNF`, `VL_BC_ICMS`↔`vBC`, `VL_ICMS`↔`vICMS`, `VL_BC_ICMS_ST`↔`vBCST`, `VL_ICMS_ST`↔`vST`, `VL_IPI`↔`vIPI`, `VL_PIS`↔`vPIS`, `VL_COFINS`↔`vCOFINS`; `DT_DOC`↔`dhEmi`; `NUM_DOC`/`SER`/`COD_MOD` ↔ `nNF`/`serie`/`mod`. Para emissão própria a igualdade é exigida. | [DERIVADA] (definição dos campos) | Entradas de terceiros: o adquirente pode registrar base/ICMS **creditado** diferente do destacado (ex.: uso e consumo sem crédito) – ver 006 | Emissão própria: ESCRITURACAO_GRAVE (débito menor) |
| CRZ-NFE-EFD-005 | Agregação por CST/CFOP/alíquota dos itens do XML (saídas próprias) = `C190` do documento. | [DERIVADA] | — | ESCRITURACAO_GRAVE |
| CRZ-NFE-EFD-006 | NF-e de **terceiros** em que o contribuinte é destinatário (`dest` = CNPJ do estabelecimento), autorizada e não cancelada, deve estar escriturada com `IND_EMIT = 1`, no período da entrada (`DT_E_S`). | [MANUAL] | Mercadoria ainda não recebida (escrituração no mês seguinte); evento 210220/210240 (desconhecimento/operação não realizada) | Ausente no período e no seguinte: ESCRITURACAO_GRAVE; apenas deslocamento: ALERTA |
| CRZ-NFE-EFD-007 | Crédito de ICMS escriturado na entrada (`C190.VL_ICMS` de entradas) **maior** que o ICMS destacado no XML. | [DERIVADA] (LC 87/96 – crédito limitado ao imposto cobrado) | Ajuste de crédito por documento (`C197`) com fundamento | ESCRITURACAO_GRAVE |
| CRZ-NFE-EFD-008 | CFOP de entrada escriturado deve ser o correspondente ao CFOP de saída do fornecedor (5→1, 6→2, 7→3) e à destinação real (revenda, industrialização, uso e consumo, ativo). | [MANUAL] Guia (CFOP de entrada é do informante) / Convênio s/nº 1970 – Tabela CFOP | Destinação distinta da presumida | ALERTA |
| CRZ-NFE-EFD-009 | `NCM` dos itens do XML ↔ `0200.COD_NCM` do item escriturado. | [DERIVADA] | Itens de terceiros com código próprio do informante (`0200` do informante) – conferir pela correlação item/fornecedor | ALERTA |
| CRZ-NFE-EFD-010 | NF-e com `ICMSUFDest` (DIFAL consumidor final não contribuinte) ⇒ `C101` no documento e apuração `E300/E310` para a UF de destino. | [MANUAL] | — | ESCRITURACAO_GRAVE |
| CRZ-NFE-EFD-011 | NF-e com ICMS-ST retido (`vST > 0`) emitida pelo substituto ⇒ apuração `E200/E210` para a UF do destinatário. | [MANUAL] | — | ESCRITURACAO_GRAVE |
| CRZ-NFE-EFD-012 | NF-e com `finNFe = 4` (devolução) ⇒ `C113` com o documento referenciado e CFOP de devolução. | [MANUAL] | — | ALERTA |
| CRZ-NFE-EFD-013 | NF-e apenas com IBS/CBS (sem ICMS/IPI) – observar a exceção ao `C100` introduzida no Guia Prático 3.2.x para documentos que contenham apenas os tributos da Reforma Tributária do Consumo. | [MANUAL] (conferir texto da exceção na versão do Guia aplicável ao período) | — | INFO |

---

## 3. NF-e/NFC-e → EFD-Contribuições

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-NFE-CTB-001 | Toda NF-e de saída de emissão própria, autorizada e não cancelada, que represente receita deve estar na EFD-Contribuições: individualizada (`C100/C170`, `C010.IND_ESCRI = 2`) ou consolidada (`C180/C181/C185`, `IND_ESCRI = 1`). | [MANUAL] Guia | PJ do presumido em **regime de caixa** (`F500/F510` – documento não é escriturado no Bloco C) | ESCRITURACAO_GRAVE (receita omitida) |
| CRZ-NFE-CTB-002 | CST PIS/COFINS dos itens do XML de emissão própria ↔ CST escriturado (`C170`/`C181`/`C185`). | [DERIVADA] | Correção posterior documentada | ESCRITURACAO_GRAVE se reduz contribuição |
| CRZ-NFE-CTB-003 | NF-e de aquisição com crédito (CST 50–56) deve ter documento correspondente autorizado e não cancelado, emitido para o CNPJ da PJ. | [DERIVADA] | — | ESCRITURACAO_GRAVE |
| CRZ-NFE-CTB-004 | Base de crédito de PIS/COFINS na aquisição **não** deve incluir o ICMS incidente na operação de aquisição (Leis 10.637/02 e 10.833/03, art. 3º, §2º, III, incluído pela Lei 14.592/2023, conversão da MP 1.159/2023). | [NORMA] | Aquisições anteriores à vigência (MP 1.159/2023 – efeitos a partir de 01/05/2023); decisão judicial própria do contribuinte (tema com contestação doutrinária) | ESCRITURACAO_GRAVE |
| CRZ-NFE-CTB-005 | Exclusão do ICMS da base de débito de PIS/COFINS (STF, RE 574.706 – Tema 69): verificar coerência entre o critério adotado e a base escriturada. | [NORMA] | Critério de ICMS destacado | ALERTA |
| CRZ-NFE-CTB-006 | Totais por chave entre EFD ICMS/IPI e EFD-Contribuições (quando ambas individualizadas): mesmo `VL_DOC` e mesmos documentos. | [DERIVADA] | Documentos sem repercussão em PIS/COFINS (ex.: remessas) podem não constar | ALERTA |

---

## 4. NFS-e → EFD-Contribuições / ECF

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-NFS-CTB-001 | Toda NFS-e emitida (prestador = CNPJ da PJ), não cancelada/substituída, deve gerar receita no `A100` (com `CHV_NFSE`), ou compor `F500`/`F550` (presumido caixa/competência consolidada). | [MANUAL] Guia – Bloco A / F | Regime de caixa: receita só no recebimento | ESCRITURACAO_GRAVE |
| CRZ-NFS-CTB-002 | CST PIS/COFINS da NFS-e (`tribFed/piscofins/CST`) ↔ `A170.CST_PIS/CST_COFINS`. | [DERIVADA] | — | ESCRITURACAO_GRAVE se reduz contribuição |
| CRZ-NFS-CTB-003 | Retenções de PIS/COFINS sofridas (NFS-e emitida com retenção) ↔ `F600` e `1300/1700`; dedução no `M200/M600` não pode exceder o retido. | [DERIVADA]/[MANUAL] | Retenção na NFS-e não efetivada pelo tomador | ESCRITURACAO_GRAVE (dedução a maior) |
| CRZ-NFS-CTB-004 | NFS-e **tomada** (PJ é tomador) com crédito (CST 50–56, `NAT_BC_CRED 03` serviços como insumo) ↔ `A100` com `IND_OPER = 0`. | [MANUAL] | — | ALERTA (crédito sem documento: ESCRITURACAO_GRAVE) |
| CRZ-NFS-ECF-001 | IRRF e CSLL retidos nas NFS-e emitidas (`vRetIRRF`, `vRetCSLL`) ↔ `Y570` da ECF (demonstrativo do IR e CSLL retidos na fonte). | [DERIVADA] | Retenção destacada mas não efetuada | ALERTA |
| CRZ-NFS-EFD-001 | Contribuinte do DF: NFS-e/ISS deve constar do **Bloco B** da EFD ICMS/IPI. Demais UFs: não deve haver Bloco B com movimento. | [MANUAL] Guia – Bloco B | — | ESTRUTURA_GRAVE |

---

## 5. DIMP → NF-e/NFC-e/NFS-e → EFD ICMS/IPI (registro 1601)

A DIMP é declarada pelas instituições de pagamento e intermediadores (Convênio ICMS 134/2016). Do lado do contribuinte, a contrapartida declaratória é o **registro 1601** da EFD ICMS/IPI; do lado documental, os grupos `pag/detPag` (`tPag`, `card`) e `infIntermed` da NF-e/NFC-e.

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-DIMP-EFD-001 | Para cada instituição de pagamento/intermediador da DIMP (CNPJ do declarante) e período, deve existir `1601` com `COD_PART_IP`/`COD_PART_IT` correspondente (participante no `0150`). | [DERIVADA] | Instituição sem transações no estabelecimento/UF | ESCRITURACAO_GRAVE |
| CRZ-DIMP-EFD-002 | Valor total das transações da DIMP (por declarante, por estabelecimento/UF, por período) ↔ `1601.TOT_VS + TOT_ISS + TOT_OUTROS`. | [DERIVADA] | Estornos/chargebacks; diferença entre data da transação e data do documento; vendas de outro estabelecimento | DIMP > 1601: ESCRITURACAO_GRAVE (indício de omissão de saídas); diferença pequena/temporal: ALERTA |
| CRZ-DIMP-NFE-001 | Valor recebido por meio eletrônico (DIMP) ↔ soma de `vPag` das NF-e/NFC-e/NFS-e emitidas no período com `tPag` eletrônico (03, 04, 17, 20 etc.). | [DERIVADA] | Recebimentos sem fato gerador (adiantamentos, transferências entre contas próprias, empréstimos); operações fora do campo do ICMS (devem estar em `TOT_ISS`/`TOT_OUTROS`) | ALERTA (material: ESCRITURACAO_GRAVE) |
| CRZ-DIMP-NFE-002 | NF-e/NFC-e com `indIntermed = 1` (operação em plataforma de terceiro) ⇒ CNPJ do intermediador informado e correspondência com declarante intermediador na DIMP. | [MANUAL] NT 2020.006 / [DERIVADA] | — | ALERTA |
| CRZ-DIMP-ECD-001 | Total anual da DIMP ↔ receita bruta contábil (ECD) e receita na ECF. | [DERIVADA] | As mesmas da linha CRZ-DIMP-NFE-001 | ALERTA |

---

## 6. EFD ICMS/IPI ↔ EFD-Contribuições

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-EFD-CTB-001 | Soma das EFD ICMS/IPI de todos os estabelecimentos (`0140` da EFD-Contribuições) – total de saídas com CFOP de **venda/prestação** (receita) ↔ receita de vendas de mercadorias na EFD-Contribuições (`C180/C181`, `C100/C170`, `M210.VL_REC_BRT` + `M400`). | [DERIVADA] | CFOPs sem receita (remessas, transferências, bonificações, devoluções, retorno); receitas financeiras e outras só na EFD-Contribuições; regime de caixa | ESCRITURACAO_GRAVE se receita na EFD ICMS/IPI > EFD-Contribuições sem explicação |
| CRZ-EFD-CTB-002 | Todo estabelecimento com EFD ICMS/IPI deve constar no `0140` da EFD-Contribuições (mesmo período). | [DERIVADA] | — | ESTRUTURA_GRAVE |
| CRZ-EFD-CTB-003 | Mesmos itens (`0200`), mesmas NCM e mesmos participantes (`0150`) para os mesmos documentos. | [DERIVADA] | — | ALERTA |
| CRZ-EFD-CTB-004 | Entradas com crédito de PIS/COFINS (CST 50–56) ↔ documento escriturado como entrada na EFD ICMS/IPI. | [DERIVADA] | Serviços (NFS-e) e energia/aluguel não constam do bloco C da EFD ICMS/IPI | ALERTA |
| CRZ-EFD-CTB-005 | Créditos sobre ativo imobilizado: aquisições no CIAP (`0300`/`G125`) ↔ `F120/F130` (encargos de depreciação ou valor de aquisição). | [DERIVADA] | Bens sem direito a crédito de PIS/COFINS | ALERTA |

---

## 7. EFD ICMS/IPI ↔ ECD

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-EFD-ECD-001 | `COD_CTA` informado na EFD ICMS/IPI (ex.: `C170`, `H010`, `0300`) deve existir na ECD (`I050`) como **conta analítica** (`IND_CTA = A`). | [MANUAL] (campo definido como conta analítica contábil) | Mudança de plano de contas (`IND_MUDANC_PC`/`I157`) | ALERTA |
| CRZ-EFD-ECD-002 | Inventário de 31/12 (`H005.VL_INV` da EFD de fevereiro, somente itens `IND_PROP` 0 e 1 – de propriedade do informante), **somado entre estabelecimentos** ↔ saldo das contas de estoque da ECD em 31/12 (`I155.VL_SLD_FIN`). | [DERIVADA] | Itens de propriedade de terceiros (`IND_PROP = 2`) não compõem o ativo; estoques em trânsito; critérios de avaliação (custo médio x PEPS) idênticos em ambos | ESCRITURACAO_GRAVE |
| CRZ-EFD-ECD-003 | ICMS a recolher apurado no ano (soma de `E110.VL_ICMS_RECOLHER` + ST `E210` + DIFAL `E310`) ↔ movimento credor das contas de ICMS a recolher na ECD. | [DERIVADA] | Saldo credor/devedor transferido; parcelamentos | ALERTA |
| CRZ-EFD-ECD-004 | Saldo credor de ICMS a transportar (`E110.VL_SLD_CREDOR_TRANSPORTAR` de dezembro) ↔ saldo da conta de ICMS a recuperar na ECD. | [DERIVADA] | Créditos reconhecidos contabilmente de forma diferente (CIAP em conta própria) | ALERTA |
| CRZ-EFD-ECD-005 | Receita bruta de vendas (saídas com CFOP de venda, todas as EFDs do ano) ↔ contas de receita bruta (`COD_NAT = 04`) da ECD (via `I355`). | [DERIVADA] | Competência x data de saída; vendas para entrega futura; receitas de serviços | ALERTA (material: ESCRITURACAO_GRAVE) |

---

## 8. EFD-Contribuições ↔ ECD

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-CTB-ECD-001 | Contas do `0500` (EFD-Contribuições) devem existir na ECD (`I050`) com mesmo código e natureza; contas referenciadas devem ser analíticas. | [DERIVADA] (0500 é o plano de contas contábil) | Mudança de plano de contas | ALERTA |
| CRZ-CTB-ECD-002 | Soma anual das receitas na EFD-Contribuições (tributadas + `M400/M800`) ↔ receitas contábeis (`I355`). | [DERIVADA] | Regime de caixa (presumido); receitas não operacionais; reversões | ALERTA (material: ESCRITURACAO_GRAVE) |
| CRZ-CTB-ECD-003 | Créditos sobre depreciação (`F120`) ↔ encargos de depreciação contabilizados no período. | [DERIVADA] | Bens sem direito a crédito; opção pelo valor de aquisição (`F130`) | ALERTA |
| CRZ-CTB-ECD-004 | PIS/COFINS a recolher apurados (`M200/M600`) ↔ movimento credor das contas de PIS/COFINS a recolher. | [DERIVADA] | — | ALERTA |

---

## 9. ECD ↔ ECF

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-ECD-ECF-001 | O hash da ECD recuperada na ECF (`C040`) deve ser o da **última** ECD válida do período. Se a ECD foi substituída após a entrega da ECF, a ECF deve ser retificada. | [MANUAL] Manual ECF – Bloco C | — | ESTRUTURA_GRAVE |
| CRZ-ECD-ECF-002 | Saldos `K155`/`K355` da ECF = `I155`/`I355` da ECD para as mesmas contas e períodos. | [PVA] (recuperação automática) / [MANUAL] | — | ESTRUTURA_GRAVE |
| CRZ-ECD-ECF-003 | Mapeamento referencial (`I051` ECD → `K156/K356` ECF) coerente: balanço e DRE referenciais (`L100`/`L300` ou `P100`/`P150`) derivam desses saldos. | [MANUAL] | — | ESTRUTURA_GRAVE |
| CRZ-ECD-ECF-004 | Lucro líquido antes do IRPJ/CSLL na DRE da ECD (`J150`) ↔ ponto de partida da Parte A do e-Lalur/e-Lacs (`M300`/`M350`) e DRE referencial (`L300`). | [DERIVADA] | — | ESCRITURACAO_GRAVE |
| CRZ-ECD-ECF-005 | Ajustes de adição/exclusão vinculados a conta contábil (`M310/M312`) devem referir contas existentes e movimentadas na ECD. | [MANUAL] | — | ALERTA |
| CRZ-ECD-ECF-006 | Situação especial e período (`DT_INI`/`DT_FIN`, `IND_SIT_ESP`) iguais nas duas escriturações. | [MANUAL] | — | ESTRUTURA_GRAVE |

---

## 10. EFD-Contribuições ↔ ECF

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-CTB-ECF-001 | **Lucro presumido**: receitas por atividade usadas na base do IRPJ/CSLL (`P200`/`P400`, somatório dos trimestres) ↔ receitas na EFD-Contribuições (`F500/F510` caixa ou `F550/F560` competência ou blocos A/C) dos meses do trimestre. | [DERIVADA] | Receitas não sujeitas a PIS/COFINS mas computadas na base presumida (e vice-versa), ganhos de capital, receitas financeiras | ESCRITURACAO_GRAVE se receita PIS/COFINS > receita base IRPJ |
| CRZ-CTB-ECF-002 | **Lucro real**: receita bruta da DRE referencial (`L300`) ↔ `M210.VL_REC_BRT` + receitas de `M400` (agregado anual). | [DERIVADA] | Receitas financeiras (tributadas na EFD-Contribuições, mas fora da receita bruta – Decreto 8.426/2015); deduções; vendas canceladas | ALERTA |
| CRZ-CTB-ECF-003 | Regime de caixa do presumido (indicador de reconhecimento de receitas na ECF) ↔ `IND_REG_CUM = 1`. | [NORMA] (ver CRZ-REG-004) | — | ESCRITURACAO_GRAVE |
| CRZ-CTB-ECF-004 | Retenções na fonte (CSRF) por órgãos/PJ tomadoras: PIS/COFINS em `F600`, IRRF/CSLL em `Y570` – os documentos de origem (NFS-e) são os mesmos. | [DERIVADA] | — | ALERTA |
| CRZ-CTB-ECF-005 | Receita de vendas por estabelecimento/atividade (`Y540` ECF) ↔ receitas agregadas por estabelecimento na EFD-Contribuições (`0140`/`C010`/`A010`). | [DERIVADA] | — | ALERTA |

---

## 11. NF-e / NFS-e ↔ ECD (documento × contabilidade)

| ID | Condição | Evidência | Hipóteses legítimas | Severidade |
|---|---|---|---|---|
| CRZ-DOC-ECD-001 | Total de documentos de saída que representam receita (NF-e com CFOP de venda + NFS-e) no ano ↔ receita bruta contábil. | [DERIVADA] | Competência; entrega futura; vendas canceladas fora do período | ALERTA (material: ESCRITURACAO_GRAVE) |
| CRZ-DOC-ECD-002 | NF-e de aquisição de bens de valor relevante para ativo (CFOP 1551/2551/3551) ↔ incremento de imobilizado na ECD. | [DERIVADA] | Arrendamento, consórcio, bens de terceiros | ALERTA |
| CRZ-DOC-ECD-003 | Lançamentos contábeis de receita sem documento fiscal correspondente (quando o histórico `I250.HIST` identifica documento) ou documentos sem lançamento. | [DERIVADA] | Escrituração resumida (`IND_ESC = R`) com livros auxiliares | ALERTA |

---

## 12. Matriz de relacionamento (resumo)

| ↓ Origem / Destino → | NF-e | NFS-e | DIMP | EFD ICMS/IPI | EFD-Contrib. | ECD | ECF |
|---|---|---|---|---|---|---|---|
| **NF-e** | — | — | CRZ-DIMP-NFE-* | CRZ-NFE-EFD-* | CRZ-NFE-CTB-* | CRZ-DOC-ECD-* | via ECD |
| **NFS-e** | — | — | CRZ-DIMP-NFE-001 | CRZ-NFS-EFD-001 (DF) | CRZ-NFS-CTB-* | CRZ-DOC-ECD-001 | CRZ-NFS-ECF-001 |
| **DIMP** | | | — | CRZ-DIMP-EFD-* | — | CRZ-DIMP-ECD-001 | (via ECD) |
| **EFD ICMS/IPI** | | | | — | CRZ-EFD-CTB-* | CRZ-EFD-ECD-* | CRZ-REG-* |
| **EFD-Contrib.** | | | | | — | CRZ-CTB-ECD-* | CRZ-CTB-ECF-*, CRZ-REG-002/003/004 |
| **ECD** | | | | | | — | CRZ-ECD-ECF-* |
