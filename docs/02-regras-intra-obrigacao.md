# 02 – Regras Internas de Cada Obrigação

> **Escopo (constitution 2.0.0):** a validação de layout e das regras internas de cada obrigação é responsabilidade dos **agentes de origem**. Este agente recebe as obrigações já validadas, em JSON normalizado com o relatório de validação, e **não** executa as regras deste documento. Elas servem para:
> 1. **Pré-condições de entrada** – as regras `INT-COM-002`, `INT-COM-003` e `INT-COM-004` são conferidas por este agente a partir dos metadados recebidos (versão, escrituração vigente, CNPJ/período). As demais `INT-COM` são esperadas como já validadas na origem.
> 2. **Referência para o contrato de entrada** – indicam o que o relatório de validação de origem deve cobrir (D24/D25).
> 3. **Pendência D26** – regras internas que **não** são validadas pelo PVA/SEFAZ (marcadas [MANUAL], [NORMA] ou [DERIVADA], ex.: `INT-EFD-004`, `INT-EFD-009`, `INT-CTB-006`, `INT-ECF-003`) ainda não têm responsável definido.
>
> Obrigação reprovada na origem é tratada como "pré-condição de entrada não atendida" e os cruzamentos que dependem dela, como "ausência de evidências".

Regras **dentro** de um mesmo arquivo/documento. Como os documentos chegam já validados, falhas em regras **[PVA]** indicam: arquivo diferente do transmitido, versão de leiaute incorreta, ou transmissão não comprovada.

Convenção de IDs: `INT-<OBRIGAÇÃO>-<nnn>`.

> Observação geral sobre os PVAs do SPED: os programas validadores classificam ocorrências em **erros** (impedem a transmissão/assinatura) e **advertências/avisos** (não impedem). Advertências presentes no relatório de validação recebido devem ser repassadas ao parecer como `ALERTA`, preservando o texto original do PVA.

---

## 1. Pré-condições comuns (todas as escriturações SPED)

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-COM-001 | O arquivo deve possuir recibo de transmissão (ou número de recibo informado) e o hash/recibo deve corresponder ao arquivo analisado. Sem recibo, não é escrituração entregue. | [NORMA] (escriturações são consideradas entregues com a transmissão e recibo) | ESTRUTURA_GRAVE |
| INT-COM-002 | A versão de leiaute declarada no arquivo deve ser a vigente para o período (`0000.COD_VER` EFD ICMS/IPI = `020` para 2026; `I010.COD_VER_LC` = `9.00` na ECD; `0000.COD_VER` ECF compatível com o Leiaute 12 para AC 2025). | [PVA] | ESTRUTURA_GRAVE |
| INT-COM-003 | Para um mesmo período/estabelecimento só deve ser considerada a **última** escrituração válida (original ou substituta/retificadora). O agente deve rejeitar análise sobre arquivo substituído. | [MANUAL] (`COD_FIN`, `TIPO_ESCRIT`/`NUM_REC_ANTERIOR`, `IND_FIN_ESC`/`COD_HASH_SUB`, `RETIFICADORA`/`NUM_REC`) | ESTRUTURA_GRAVE |
| INT-COM-004 | CNPJ e período do registro `0000` devem coincidir com o contribuinte e o período sob análise. | [MANUAL] | ESTRUTURA_GRAVE |
| INT-COM-005 | Todo código referenciado (participante, item, unidade, conta contábil) deve existir no respectivo cadastro do Bloco 0 (ou I050 na ECD). | [PVA] | ESTRUTURA_GRAVE |
| INT-COM-006 | CNPJ deve ser tratado como **alfanumérico** (NT Conjunta 2025.001; ADE Cofis 1/2026 para a ECD; NT 2026.004 NF-e). Validação de DV pelo algoritmo novo; nunca converter para número. | [MANUAL] | ESTRUTURA_GRAVE se o parser rejeitar CNPJ válido |

---

## 2. NF-e / NFC-e

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-NFE-001 | Documento só tem validade jurídica com **protocolo de autorização** (`protNFe` com `cStat` de autorização). XML sem protocolo ou com protocolo de denegação/rejeição não é documento válido para escrituração como operação regular. | [NORMA] Ajuste SINIEF 07/05 | ESTRUTURA_GRAVE |
| INT-NFE-002 | Assinatura digital deve ser válida e o certificado deve pertencer ao CNPJ (raiz) do emitente. | [PVA] SEFAZ | ESTRUTURA_GRAVE |
| INT-NFE-003 | Chave de acesso deve ser coerente com `cUF`, AAMM de emissão, CNPJ do emitente, `mod`, `serie`, `nNF`, `tpEmis`, `cNF` e DV. | [PVA] | ESTRUTURA_GRAVE |
| INT-NFE-004 | Totais (`ICMSTot`) devem corresponder à soma dos itens (`vProd`, `vBC`, `vICMS`, `vST`, `vIPI`, `vPIS`, `vCOFINS`…) e `vNF` deve observar a fórmula do MOC. | [PVA] regras de validação do MOC | ESTRUTURA_GRAVE |
| INT-NFE-005 | Verificar existência de evento de **cancelamento** homologado antes de considerar a NF-e como operação efetiva. | [MANUAL] MOC – eventos | ESCRITURACAO_GRAVE se escriturada como regular |
| INT-NFE-006 | Carta de Correção (110110) **não** pode corrigir valores do imposto, base de cálculo, alíquota, diferença de preço, quantidade, valor da operação, dados cadastrais que mudem o remetente/destinatário, nem data de emissão/saída (art. 7º, §1º-A do Convênio SINIEF s/nº de 1970). CC-e que pretenda esses ajustes é ineficaz. | [NORMA] | ALERTA |
| INT-NFE-007 | `CRT` do emitente deve ser coerente com o grupo de ICMS usado (`CSOSN` para CRT 1/4; `CST` para CRT 3; CRT 2 – observar regra do MOC). | [PVA] | ESTRUTURA_GRAVE |
| INT-NFE-008 | `finNFe = 4` (devolução) e `finNFe = 2` (complementar) exigem documento referenciado (`NFref`). | [PVA] | ESTRUTURA_GRAVE |
| INT-NFE-009 | Grupo IBS/CBS/IS (NT 2025.002 v1.52) – observar cronograma de obrigatoriedade definido na própria NT (regra UB12-10). Ausência do grupo antes da obrigatoriedade em produção = `INFO`; após = `ESTRUTURA_GRAVE`. | [MANUAL] NT 2025.002 | Conforme vigência |
| INT-NFE-010 | Manifestação do destinatário: NF-e recebida com evento **210220 (desconhecimento)** ou **210240 (operação não realizada)** não deve ser tratada como entrada efetiva sem justificativa. | [MANUAL] NT 2020.001 | ALERTA |

---

## 3. NFS-e (Padrão Nacional)

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-NFS-001 | XML deve validar contra o XSD v1.01 (20260209) ou o XSD vigente na data da emissão. | [PVA] ADN | ESTRUTURA_GRAVE |
| INT-NFS-002 | `cTribNac` deve existir no Anexo B (Lista de Serviço Nacional/NBS v1.01) vigente na competência. | [PVA] | ESTRUTURA_GRAVE |
| INT-NFS-003 | Verificar eventos de cancelamento e de substituição antes de considerar o documento como receita efetiva; NFS-e substituída não deve ser somada junto com a substituta. | [MANUAL] Anexo II (eventos) | ESCRITURACAO_GRAVE se somadas |
| INT-NFS-004 | Retenção de ISS (`tpRetISSQN`) e retenções federais (`vRetIRRF`, `vRetCSLL`, `vRetCP`, PIS/COFINS retidos) devem ser coerentes com o tomador (ex.: retenção atribuída a tomador PF sem previsão legal). | [MANUAL]/[NORMA] | ALERTA |
| INT-NFS-005 | Diferença entre `dhEmi` e `dCompet` – a competência é a data do fato gerador; a escrituração da receita segue a competência (exceto regime de caixa – ver cruzamentos). | [MANUAL] Anexo I | INFO |

---

## 4. DIMP

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-DIMP-001 | Arquivo deve estar na **Versão 10** para períodos a partir de 01/06/2026 (Versão 09 até 31/05/2026). | [NORMA] Ato COTEPE/ICMS 65/18 (Ato 124/25) | ESTRUTURA_GRAVE |
| INT-DIMP-002 | O agente só usa DIMP de fonte íntegra (arquivo/extrato oficial). Valores informados “de memória” ou planilhas sem origem devem ser marcados como não verificáveis. | Governança | ALERTA |

> Os registros internos da DIMP devem ser codificados a partir do PDF da Versão 10 publicado no site do CONFAZ (chave MD5 `1c6d50499b76f664163eb0d07ae79ab3`, conforme Ato COTEPE/ICMS 65/18). Este estudo não detalha registros da DIMP para não reproduzir informação não conferida no PDF.

---

## 5. EFD ICMS/IPI

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-EFD-001 | `C190`: soma de `VL_BC_ICMS`, `VL_ICMS`, `VL_BC_ICMS_ST`, `VL_ICMS_ST`, `VL_IPI` dos analíticos = campos correspondentes do `C100`. | [PVA] | ESTRUTURA_GRAVE |
| INT-EFD-002 | `E110.VL_TOT_DEBITOS` = soma do ICMS dos registros analíticos de **saídas** (C190, D190, D590 etc., CFOP iniciados em 5, 6, 7) do período; `E110.VL_TOT_CREDITOS` = soma do ICMS dos analíticos de **entradas** (CFOP 1, 2, 3). | [PVA] | ESTRUTURA_GRAVE |
| INT-EFD-003 | Soma de `E116.VL_OR` = `E110.VL_ICMS_RECOLHER` + `E110.DEB_ESP`. | [PVA] | ESTRUTURA_GRAVE |
| INT-EFD-004 | `E110.VL_SLD_CREDOR_ANT` do período deve ser igual ao `E110.VL_SLD_CREDOR_TRANSPORTAR` do período anterior. Divergência sem ajuste documentado. | [DERIVADA] (definição dos campos) | ESCRITURACAO_GRAVE |
| INT-EFD-005 | Documentos com `COD_SIT` 02/03 (cancelado), 04 (denegado), 05 (inutilizado) informam apenas os campos de identificação; não podem ter valores nem registros filhos. | [PVA]/[MANUAL] Guia – C100 | ESTRUTURA_GRAVE |
| INT-EFD-006 | Não pode haver duas ocorrências de `C100` com a mesma `CHV_NFE` no mesmo arquivo (salvo exceções do Guia). | [PVA] | ESTRUTURA_GRAVE |
| INT-EFD-007 | `IND_ATIV = 0` (industrial ou equiparado) → apuração de IPI (`E500/E510/E520`) esperada quando houver operações com IPI. | [MANUAL] | ALERTA |
| INT-EFD-008 | `H005.VL_INV` = soma de `H010.VL_ITEM`. | [PVA] | ESTRUTURA_GRAVE |
| INT-EFD-009 | Inventário de 31/12 deve ser informado no Bloco H da EFD de **fevereiro** do ano seguinte (com `MOT_INV = 01`), salvo hipótese diversa do Guia ou da legislação da UF. Ausência = omissão de inventário. | [MANUAL] Guia – Bloco H | ESCRITURACAO_GRAVE |
| INT-EFD-010 | Bloco K exigido conforme cronograma de obrigatoriedade (atividade/CNAE/faturamento) definido no Ajuste SINIEF 02/09 e Guia. | [NORMA] | ESTRUTURA_GRAVE se obrigatório e ausente |
| INT-EFD-011 | Registro `1601` exigido quando houver operações com instrumentos de pagamento eletrônico, conforme indicador correspondente do `1010`. Indicador “N” com recebimentos eletrônicos conhecidos = inconsistência. | [MANUAL] Guia – Bloco 1 | ESCRITURACAO_GRAVE |
| INT-EFD-012 | Crédito de ICMS de entradas em C190/C170 incompatível com o CFOP de uso e consumo/ativo (ativo deve ir ao CIAP – Bloco G, em 1/48 avos – LC 87/96, art. 20, §5º). | [NORMA] | ESCRITURACAO_GRAVE |
| INT-EFD-013 | `E300/E310` (DIFAL/FCP – EC 87/2015) exigidos quando houver operações interestaduais a consumidor final não contribuinte com ICMS DIFAL destacado (`C101`). | [MANUAL] | ESCRITURACAO_GRAVE |

---

## 6. EFD-Contribuições

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-CTB-001 | `0110.COD_INC_TRIB = 2` (exclusivamente cumulativo) não admite CSTs de crédito (`50`–`66`) nem registros `M100/M500` de crédito básico. | [MANUAL] | ESTRUTURA_GRAVE |
| INT-CTB-002 | `0110.IND_APRO_CRED = 2` (rateio proporcional) exige `0111` com receita bruta segregada. | [PVA] | ESTRUTURA_GRAVE |
| INT-CTB-003 | `0110.IND_REG_CUM = 1` (regime de caixa) → receitas no `F500/F510` e composição no `F525`; não usar C180/A100 para apuração. Regime de caixa só é aplicável a PJ do lucro presumido. | [MANUAL]/[NORMA] | ESTRUTURA_GRAVE |
| INT-CTB-004 | `M200` (PIS) e `M600` (COFINS) = consolidação dos `M210`/`M610`; contribuição a recolher = contribuição apurada – créditos descontados – retenções/deduções. | [PVA] | ESTRUTURA_GRAVE |
| INT-CTB-005 | Receitas com CST `04`, `05`, `06`, `07`, `08`, `09` devem estar detalhadas em `M400/M410` (PIS) e `M800/M810` (COFINS). | [PVA]/[MANUAL] | ESTRUTURA_GRAVE |
| INT-CTB-006 | Créditos utilizados de períodos anteriores devem estar controlados em `1100` (PIS) e `1500` (COFINS), com saldo coerente com o período anterior. | [MANUAL] | ESCRITURACAO_GRAVE |
| INT-CTB-007 | Retenções na fonte deduzidas (`M200`/`M600`) devem ter origem em `F600` e controle em `1300`/`1700`. | [MANUAL] | ESCRITURACAO_GRAVE |
| INT-CTB-008 | `COD_CTA` deve ser informado nos registros que o exigem, referenciando conta do `0500`. | [MANUAL] | ESTRUTURA_GRAVE (quando exigido) |
| INT-CTB-009 | `NAT_BC_CRED` deve ser coerente com a natureza da aquisição (ex.: `01` aquisição de bens para revenda, `02` bens utilizados como insumo, `03` serviços utilizados como insumo, `09`/`10`/`11` ativo imobilizado …). | [MANUAL] Tabela 4.3.7 | ALERTA |

---

## 7. ECD

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-ECD-001 | Em cada lançamento (`I200`), soma das partidas a débito = soma das partidas a crédito = `VL_LCTO`. | [PVA] | ESTRUTURA_GRAVE |
| INT-ECD-002 | `I155.VL_SLD_FIN` = `VL_SLD_INI` ± `VL_DEB` ∓ `VL_CRED` (considerando `IND_DC`). | [PVA] | ESTRUTURA_GRAVE |
| INT-ECD-003 | Movimentos de débito e crédito no `I155` = soma das partidas `I250` da conta no período. | [PVA] | ESTRUTURA_GRAVE |
| INT-ECD-004 | Saldo inicial de um período = saldo final do período anterior (entre `I150` consecutivos no arquivo). | [PVA] | ESTRUTURA_GRAVE |
| INT-ECD-005 | Balancete equilibrado: soma dos saldos devedores = soma dos saldos credores, por período. | [PVA] | ESTRUTURA_GRAVE |
| INT-ECD-006 | Balanço (`J100`): Ativo = Passivo + Patrimônio Líquido. | [PVA] | ESTRUTURA_GRAVE |
| INT-ECD-007 | Contas de resultado (`COD_NAT = 04`) devem estar zeradas após o lançamento de encerramento (`IND_LCTO = E`); saldos antes do encerramento em `I350/I355`. | [MANUAL] | ESTRUTURA_GRAVE |
| INT-ECD-008 | Toda conta analítica de natureza 01–04 utilizada deve possuir mapeamento referencial (`I051`) quando exigido pelo manual (necessário à recuperação pela ECF). | [MANUAL] | ESTRUTURA_GRAVE |
| INT-ECD-009 | `J930` deve conter contabilista habilitado (CRC) e responsável legal; `J932`/auditor independente quando exigido (`IND_GRANDE_PORTE`, regulação). | [MANUAL] | ESTRUTURA_GRAVE |
| INT-ECD-010 | Lançamentos extemporâneos (`IND_LCTO = X`) devem ser destacados no parecer, pois alteram base de períodos anteriores. | [MANUAL] | ALERTA |
| INT-ECD-011 | ECD substituta (`IND_FIN_ESC = 1`) exige `COD_HASH_SUB` e Termo de Verificação para fins de substituição (`J801`) conforme manual. | [MANUAL] | ESTRUTURA_GRAVE |

---

## 8. ECF

| ID | Regra | Evidência | Severidade |
|---|---|---|---|
| INT-ECF-001 | PJ do lucro real (e demais obrigadas à ECD) deve ter Bloco C com a ECD recuperada (`C040` com hash). | [PVA]/[MANUAL] | ESTRUTURA_GRAVE |
| INT-ECF-002 | `FORMA_TRIB` e `FORMA_TRIB_PER` coerentes entre si e com os blocos preenchidos (L/M/N para real; P para presumido; T arbitrado; U imunes/isentas). | [PVA] | ESTRUTURA_GRAVE |
| INT-ECF-003 | Saldos das contas da Parte B (`M500`) devem partir dos saldos finais da ECF anterior (Bloco E – `E020`). | [MANUAL] | ESCRITURACAO_GRAVE |
| INT-ECF-004 | Ajustes da Parte A do e-Lalur/e-Lacs (`M300/M350`) que dependam de conta contábil devem estar vinculados (`M310/M312`) e coerentes com os saldos recuperados da ECD. | [MANUAL] | ALERTA |
| INT-ECF-005 | Compensação de prejuízo fiscal / base negativa limitada a 30% do lucro real/base ajustada (Lei 9.065/95, art. 15; Lei 8.981/95, arts. 42 e 58). | [NORMA]/[PVA] | ESCRITURACAO_GRAVE |
| INT-ECF-006 | Entidades de assistência social, saúde ou educação devem informar a existência de CEBAS e o certificado (`0000`, Leiaute 12). | [MANUAL] Leiaute 12 | ESTRUTURA_GRAVE |
| INT-ECF-007 | Estimativas mensais (`N620/N660`) pagas/compensadas devem ser deduzidas do imposto anual (`N630/N670`). | [PVA]/[MANUAL] | ESCRITURACAO_GRAVE |
