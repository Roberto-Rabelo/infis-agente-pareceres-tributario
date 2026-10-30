# NÚCLEO 3 – Cadeias de Valor Ponta a Ponta (rastreabilidade)

Cada cadeia acompanha **um valor** desde a origem (documento ou evidência externa) até o destino final (tributo apurado, saldo contábil, base de IRPJ/CSLL). Os pontos de controle **CP-n** são as conferências que o agente executa, em ordem. Uma quebra em CP-n faz com que os pontos seguintes sejam reportados como **"não conclusivos"**, não como erro independente. Assim se evita multiplicar o mesmo problema no parecer.

Notação: `→` fluxo; `=` identidade; `Σ` agregação; `≤` limite.

---

## C1. Cadeia da RECEITA (a mais importante)

```
DIMP (recebido)                       ─┐
                                        ├─ CP-0: DIMP ≤ documentos + justificativas
NF-e/NFC-e saída (receita) + NFS-e     ─┘
   │ CP-1: cada documento → EFD ICMS/IPI C100 (mercadorias) / EFD-Contrib. A100 (serviços)
   ▼
EFD ICMS/IPI  C190 (CFOP receita)  ── CP-2 ──►  EFD-Contribuições  C170/C181 | A170 | F500/F550
   │                                                 │ CP-3: Σ receitas → M210.VL_REC_BRT + M400
   │                                                 ▼
   └────────────── CP-4 ──────────────►  ECD  contas de receita (I155/I355, COD_NAT 04)
                                                     │ CP-5: recuperação (hash)
                                                     ▼
                                             ECF  K355 → L300 / P150 → (presumido) P200/P400
                                                     │ CP-6: receita base IRPJ/CSLL
                                                     ▼
                                             IRPJ/CSLL (N/P)
```

| CP | Verificação | Mapa | Severidade em quebra |
|---|---|---|---|
| CP-0 | DIMP (mês, por estabelecimento/UF) ≤ Σ documentos com pagamento eletrônico + recebimentos justificados; DIMP = `1601` | M6-01..03 | ESCRITURACAO_GRAVE |
| CP-1 | Todo documento de receita está escriturado | M1-P01, M4-P01, M2-P01 | ESCRITURACAO_GRAVE |
| CP-2 | Receita de mercadorias na EFD ICMS/IPI (todos os estabelecimentos) ≈ receita de mercadorias na EFD-Contrib. | M3-06 | ESCRITURACAO_GRAVE (se EFD ICMS/IPI > EFD-Contrib.) |
| CP-3 | Receitas detalhadas = totalizadores de apuração `M210`/`M400` (já garantido pelo PVA – só conferir) | — | (pré-condição) |
| CP-4 | Σ anual das receitas da EFD-Contrib. por `COD_CTA` ≈ movimento das contas de receita da ECD | M8-02, M7-08, M12-01 | ECD > EFD-Contrib.: ESCRITURACAO_GRAVE |
| CP-5 | ECF recupera a ECD vigente | M9-01, M9-05 | ESCRITURACAO_GRAVE |
| CP-6 | Presumido: receita da EFD-Contrib. do trimestre ≤ receita base `P200`/`P400`. Real: receita bruta `L300` ≈ `M210.VL_REC_BRT` | M10-02, M10-03 | ESCRITURACAO_GRAVE (presumido) / ALERTA (real) |

**Divergências legítimas a perguntar antes de concluir:** regime de caixa (presumido), entrega futura, vendas canceladas fora do período, receitas financeiras (só EFD-Contrib./ECD), transferências entre filiais (nunca são receita), ICMS-ST e IPI (não compõem receita bruta).

---

## C2. Cadeia do ICMS

```
NF-e (vICMS, vST, vICMSUFDest, vFCP…)
   │ CP-1: C100/C170 = XML (própria: IDT; entrada: LIM)
   ▼
EFD ICMS/IPI  C190 (débitos: CFOP 5/6/7; créditos: CFOP 1/2/3)  + C197 (ajustes por documento)
   │ CP-2: E110.VL_TOT_DEBITOS / VL_TOT_CREDITOS = Σ C190 (PVA)
   ▼
E110 → E111 (ajustes) → VL_ICMS_RECOLHER → E116 (obrigações a recolher)
      → VL_SLD_CREDOR_TRANSPORTAR → (mês seguinte) VL_SLD_CREDOR_ANT       CP-3 (CNT)
   │ CP-4
   ▼
ECD  ICMS a recolher (passivo) / ICMS a recuperar (ativo) / ICMS sobre vendas (dedução da receita)
   │ CP-5
   ▼
ECF  dedução da receita bruta (L300/P150)
```

| CP | Verificação | Mapa | Severidade |
|---|---|---|---|
| CP-1 | ICMS destacado na NF-e de saída própria = ICMS escriturado (C100/C190) | M1.3, M1.4 | ESCRITURACAO_GRAVE |
| CP-1b | Crédito na entrada ≤ ICMS destacado; uso e consumo sem crédito; ativo apenas via CIAP | M1.3 (LIM), INT-EFD-012 | ESCRITURACAO_GRAVE |
| CP-2 | Totalizadores da apuração (PVA) | — | pré-condição |
| CP-3 | Saldo credor a transportar (mês t) = saldo credor anterior (mês t+1), por estabelecimento | CNT | ESCRITURACAO_GRAVE |
| CP-3b | ST: `C100.VL_ICMS_ST` (saídas do substituto) ⇒ `E210` da UF do destinatário | CRZ-NFE-EFD-011 | ESCRITURACAO_GRAVE |
| CP-3c | DIFAL: `C101` ⇒ `E310` da UF de destino | M1.3 | ESCRITURACAO_GRAVE |
| CP-4 | Σ ICMS a recolher (ano) ≈ movimento credor do passivo de ICMS; saldo credor de dezembro ≈ ativo ICMS a recuperar | M7-05, M7-06 | ALERTA |
| CP-5 | ICMS sobre vendas na DRE (dedução) coerente com débitos do ano | M9-11 | ALERTA |

---

## C3. Cadeia do IPI (somente `IND_ATIV = 0`)

| CP | Verificação | Severidade |
|---|---|---|
| CP-1 | `vIPI` da NF-e própria = `C100.VL_IPI` / `C170.VL_IPI` | ESCRITURACAO_GRAVE |
| CP-2 | Σ IPI das saídas = débitos do `E520` (via `E510` por CFOP/CST_IPI) | (PVA – pré-condição) |
| CP-3 | `E520.VL_SD_IPI` (saldo credor) mês t = `VL_SD_ANT_IPI` mês t+1 | ESCRITURACAO_GRAVE |
| CP-4 | IPI a recolher (ano) ≈ movimento do passivo de IPI na ECD (M7-07) | ALERTA |
| CP-5 | IPI fora da receita bruta (ECF `L300`/`P150` não inclui IPI na receita) | ALERTA |

---

## C4. Cadeia do PIS/COFINS – DÉBITO

```
NF-e / NFS-e (CST, vBC, vPIS, vCOFINS)
   │ CP-1: CST e base escriturados = documento (saída própria)
   ▼
EFD-Contrib.  C170/C181/C185 | A170 | F100 | F500/F550   (CST 01,02,03,05 tributadas; 04,06,07,08,09 → M400/M800)
   │ CP-2 (PVA): M210/M610 por COD_CONT → M200/M600
   ▼
M200.VL_CONT_NC_REC / VL_CONT_CUM_REC  − retenções (F600) − outras deduções
   │ CP-3: deduções com lastro (F600 ↔ NFS-e com retenção ↔ 1300/1700)
   ▼
ECD  PIS/COFINS a recolher; PIS/COFINS sobre receitas (dedução)            CP-4
   ▼
ECF  deduções da receita bruta                                             CP-5
```

| CP | Verificação | Mapa | Severidade |
|---|---|---|---|
| CP-1 | CST/base/valor da saída própria = documento | M2.3, M4.3 | ESCRITURACAO_GRAVE (se reduz contribuição) |
| CP-1b | Receitas com CST 04/06/07/08/09 detalhadas em `M400/M410` com `COD_CTA` existente na ECD | M8-03 | ALERTA |
| CP-3 | `Σ F600.VL_RET_PIS/COFINS` ≤ retenções destacadas nas NFS-e emitidas para cada fonte pagadora; dedução no `M200/M600` ≤ saldo disponível em `1300/1700` | M4-R03 | ESCRITURACAO_GRAVE |
| CP-4 | Contribuição a recolher (mês) ≈ passivo de PIS/COFINS na ECD | M8-05 | ALERTA |
| CP-5 | Coerência com deduções na DRE da ECF | M10-06 | ALERTA |

---

## C5. Cadeia do PIS/COFINS – CRÉDITO (somente não cumulativo)

```
NF-e entrada / NFS-e tomada / contabilidade (depreciação, aluguel, energia)
   │ CP-1: documento existe, é destinado à PJ e não está cancelado
   ▼
EFD-Contrib.  C170/C191/C195 | A170 | F100 | F120/F130 | F150   (CST 50–56, 60–66; NAT_BC_CRED)
   │ CP-2: base sem ICMS da aquisição (Lei 14.592/2023)
   ▼
M100/M105 (PIS) – M500/M505 (COFINS) → crédito descontado no mês → saldo → 1100/1500 (CNT)
   │ CP-4
   ▼
ECD  PIS/COFINS a recuperar; custo/despesa líquidos de crédito
```

| CP | Verificação | Mapa | Severidade |
|---|---|---|---|
| CP-1 | Todo crédito documental tem documento válido destinado à PJ | M2-R01, M4-R02 | ESCRITURACAO_GRAVE |
| CP-1b | Entrada de mercadoria com crédito de PIS/COFINS ⇒ entrada na EFD ICMS/IPI | M3-04 | ALERTA |
| CP-2 | Base de crédito sem ICMS incidente na aquisição | M2-R04 | ESCRITURACAO_GRAVE |
| CP-3 | Crédito sobre depreciação (`F120`) ≤ depreciação contabilizada do bem | M8-04 | ESCRITURACAO_GRAVE |
| CP-3b | `F120/F130` ⇔ bem existente (CIAP `0300` / imobilizado na ECD) | M3-07, M12-02 | ALERTA |
| CP-4 | Saldo de créditos `1100/1500` mês t → mês t+1 (CNT) e ≈ ativo PIS/COFINS a recuperar na ECD | M8-07 | ESCRITURACAO_GRAVE (CNT) / ALERTA (ECD) |
| CP-5 | `COD_INC_TRIB = 2` (cumulativo) ⇒ **nenhum** crédito básico | PAR | ESCRITURACAO_GRAVE |

---

## C6. Cadeia do ESTOQUE

```
NF-e entradas (compras) − NF-e saídas (vendas, CMV)
   ▼
EFD ICMS/IPI  K200 (estoque mensal escriturado – se obrigado ao Bloco K)
              H005/H010 (inventário 31/12, informado em fevereiro)
   │ CP-2: H010 (IND_PROP 0/1) Σ todos os estabelecimentos
   ▼
ECD  contas de estoque (I155 em 31/12)              CP-3
   ▼
ECF  L100/P100 estoque; Y672.VL_EST (presumido)   CP-4
   ▼
EFD-Contrib. F150 (estoque de abertura – somente na mudança para não cumulativo)   CP-5
```

| CP | Verificação | Mapa | Severidade |
|---|---|---|---|
| CP-1 | Inventário de 31/12 presente na EFD de fevereiro de cada estabelecimento com estoque | INT-EFD-009 | ESCRITURACAO_GRAVE |
| CP-2 | `K200` (31/12) = `H010` (31/12) em quantidade, por item e posse | M7-10 | ALERTA |
| CP-3 | Σ `H010` (`IND_PROP` 0/1, `VL_ITEM_IR` quando houver) = Σ saldo das contas de estoque na ECD | M7-02, M7-03 | ESCRITURACAO_GRAVE |
| CP-4 | Estoque ECD = estoque ECF | M9-16, M11-01 | ESCRITURACAO_GRAVE |
| CP-5 | `F150` (se houver) coerente com o inventário de abertura | — | ALERTA |

**Exclusivos legítimos:** `IND_PROP = 2` (estoque de terceiros em poder do informante) aparece só na EFD; mercadorias em trânsito e importações em andamento podem estar só na ECD (contas específicas).

---

## C7. Cadeia do ATIVO IMOBILIZADO

| CP | Verificação | Mapa | Severidade |
|---|---|---|---|
| CP-1 | NF-e de aquisição de ativo (CFOP x.551) existe e está escriturada na EFD ICMS/IPI | M1-P05 | ESCRITURACAO_GRAVE |
| CP-2 | Bem com crédito de ICMS ⇒ `0300` + `G125` (entrada) + `G130` com a chave da NF-e; apropriação de 1/48 por mês | M1-R06 | ESCRITURACAO_GRAVE |
| CP-3 | Bem com crédito de PIS/COFINS ⇒ `F120` (depreciação) ou `F130` (aquisição) | M3-07 | ALERTA |
| CP-4 | Adições ao imobilizado na ECD ≈ Σ NF-e x.551 do ano | M12-02 | ALERTA |
| CP-5 | `0300.COD_CTA` / `F120.COD_CTA` existem na ECD como contas do ativo | ENT-030/032 | ESCRITURACAO_GRAVE |

---

## C8. Cadeia das RETENÇÕES NA FONTE (serviços)

```
NFS-e emitida (tomador PJ/órgão público retém): vRetIRRF, vRetCSLL, PIS/COFINS retidos, vRetCP, ISS retido
   ├── PIS/COFINS ─► EFD-Contrib.  A100.VL_PIS_RET/VL_COFINS_RET → F600 (CNPJ fonte) → 1300/1700 → dedução M200/M600
   ├── IRRF/CSLL ─► ECF  Y570 (CNPJ_FON, IR_RET, CSLL_RET) → dedução N630/N670 (real) | P300/P500 (presumido)
   ├── INSS (CP) ─► ECD conta de INSS retido a compensar   (EFD-Reinf – fora do escopo)
   └── ISS retido ─► NFS-e (tpRetISSQN) – não gera crédito; reduz o ISS a recolher pelo prestador
```

| CP | Verificação | Mapa | Severidade |
|---|---|---|---|
| CP-1 | Σ PIS/COFINS retidos nas NFS-e por tomador ≥ `F600` do mesmo CNPJ | M4-R03 | ESCRITURACAO_GRAVE (dedução sem lastro) |
| CP-2 | Σ IRRF/CSLL retidos nas NFS-e por tomador ≈ `Y570` do mesmo `CNPJ_FON` | M12-03 | ALERTA |
| CP-3 | Fontes pagadoras de `F600` ⊂ fontes do `Y570` (quem retém CSRF retém, em regra, o conjunto) | M10-04 | ALERTA |
| CP-4 | Deduções na ECF de IR/CSLL retidos ≤ `Y570` | — | ESCRITURACAO_GRAVE |

---

## C9. Cadeia dos RECEBIMENTOS ELETRÔNICOS (DIMP)

```
DIMP (instituição X / intermediador Y, mês, estabelecimento/UF)
   │ CP-1: X e Y cadastrados no 0150 e informados no 1601 (COD_PART_IP / COD_PART_IT)
   │ CP-2: valor DIMP = 1601 (TOT_VS + TOT_ISS + TOT_OUTROS)
   │ CP-3: TOT_VS ≤ Σ documentos de saída no campo do ICMS pagos eletronicamente (detPag)
   │ CP-4: TOT_ISS ≈ Σ NFS-e recebidas eletronicamente
   │ CP-5: (presumido caixa) F525 com recebimentos via administradora/instituição
   ▼
ECD  contas de bancos/recebíveis de cartão (movimento)                      CP-6
```

| CP | Severidade |
|---|---|
| CP-1 | ESCRITURACAO_GRAVE (instituição que declarou recebimentos não consta no 1601) |
| CP-2 | DIMP > 1601: ESCRITURACAO_GRAVE |
| CP-3 / CP-4 | DIMP/1601 > documentos: ESCRITURACAO_GRAVE (indício de saídas sem documento fiscal), depois de esgotar as hipóteses de M6.3 |
| CP-5 / CP-6 | ALERTA |

---

## C10. Cadeia do RESULTADO → IRPJ/CSLL

```
ECD  I355 (resultado antes do encerramento) → J150 (DRE) → lucro líquido antes IR/CS
   │ CP-1: hash/recuperação (C040, K155, K355)
   ▼
ECF  L300/P150 → M300/M350 (Parte A: + adições − exclusões − compensações)
   │ CP-2: ajustes vinculados a contas/lançamentos existentes na ECD (M310/M312)
   │ CP-3: Parte B – saldo inicial = saldo final da ECF anterior (E020 → M500) (CNT)
   │ CP-4: compensação de prejuízo/base negativa ≤ 30% (Lei 9.065/95 art. 15; Lei 8.981/95 arts. 42 e 58)
   ▼
N500..N670 (real) / P200..P500 (presumido) → IRPJ/CSLL devidos − estimativas − retenções (Y570)
   │ CP-5: IRPJ/CSLL devidos ≈ despesa/passivo de IRPJ/CSLL na ECD
```

| CP | Mapa | Severidade |
|---|---|---|
| CP-1 | M9-01..06 | ESCRITURACAO_GRAVE |
| CP-2 | M9-13, M9-14 | ESCRITURACAO_GRAVE / ALERTA |
| CP-3 | M9-22 | ESCRITURACAO_GRAVE |
| CP-4 | INT-ECF-005 | ESCRITURACAO_GRAVE |
| CP-5 | M9-15 | ALERTA |

---

## Ordem de execução recomendada para o agente

1. **PAR** – enquadramento e regime (NÚCLEO-1 §5). Se falhar, o parecer começa pela incoerência de enquadramento.
2. **Entidades** – ENT-001..033 (estabelecimentos, participantes, itens, contas).
3. **Documentos** – M1, M2, M4 (presença A→B e B→A, por chave).
4. **Escriturações entre si** – M3, M5, M7, M8, M10, M11.
5. **Contabilidade → ECF** – M9.
6. **Evidência externa** – M6 (DIMP).
7. **Cadeias C1–C10** – consolidam as quebras e marcam como "não conclusivo" o que depende de um CP quebrado.
8. **Parecer** – agrupar por cadeia, por severidade. Cada achado com: documento/chave, valores de cada lado, diferença, regra, fonte oficial e hipóteses legítimas a confirmar.
