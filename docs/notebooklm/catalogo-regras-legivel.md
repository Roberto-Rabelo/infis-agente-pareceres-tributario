# Catálogo de Regras de Coesão – versão legível

> Fonte gerada automaticamente a partir de `docs/regras/*.json` (catálogo v1.0.0) para leitura humana e uso no NotebookLM. Em caso de divergência, o JSON é a referência. Cada regra tem um ID único (ex.: M1-P01) que deve ser citado nas respostas.

## Como ler uma regra

- **Origem → Destino**: obrigação e registro/campo de cada lado.
- **Tipo de relação**: o que está sendo verificado (presença, identidade, soma, limite, continuidade, regime, exclusão).
- **Severidade**: ESTRUTURA_GRAVE > ESCRITURACAO_GRAVE > ALERTA > INFO. A *severidade atenuada* se aplica quando a condição indicada é atendida.
- **Evidência**: PVA, MANUAL, NORMA ou DERIVADA. Regras DERIVADAS nunca concluem irregularidade sem avaliar as hipóteses legítimas.
- **Hipóteses legítimas**: explicações aceitáveis que tornam a divergência regular e devem ser perguntadas ao contribuinte.
- **Filtros de exclusão**: regras EXC aplicadas antes, que retiram do universo fatos que legitimamente não precisam aparecer.
- **Depende de**: regras que precisam ter passado; se falharam, esta é reportada como NÃO CONCLUSIVA.
- **Campo a conferir**: o nome/código do campo ainda precisa ser confirmado no PDF oficial antes de produção.

## Grupo FUNDACAO – 39 regras

Regras executadas ANTES dos cruzamentos de valor: compatibilidade de regime (PAR), filtros de exclusividade legítima (EXC) e coesão de entidades compartilhadas (ENT).

### PAR-001 – Regime normal (CRT=3) exige EFD-Contribuições e, se contribuinte de ICMS/IPI, EFD ICMS/IPI

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** NF-e/NFC-e → `emit/CRT` (universo: NF-e emitidas pelo contribuinte no período) · agregação: MES
- **Destino:** EFD-Contribuições → `0000` · agregação: MES
- **Chave de junção:** CNPJ_BASE, PERIODO
- **Operador:** EXISTE
- **Condição:** Se existe NF-e com CRT=3 no mês, deve existir EFD-Contribuições do mês para o CNPJ base e EFD ICMS/IPI do estabelecimento emitente (se inscrito no ICMS).
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** IN RFB 1.252/2012 – EFD-Contribuições; Ajuste SINIEF 02/2009 – EFD ICMS/IPI; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Exclusão do Simples Nacional no curso do período | Escrituração do período ainda não recebida pelo agente (cobertura)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Há NF-e emitidas em regime normal (CRT=3) em {periodo}, mas não foi recebida a {obrigacaoAusente} correspondente."

### PAR-002 – Simples Nacional/MEI (CRT 1 ou 4) incompatível com EFD-Contribuições e ECF

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** NF-e/NFC-e → `emit/CRT` (universo: CRT in (1,4)) · agregação: MES
- **Destino:** EFD-Contribuições → `0000; ECF 0000`
- **Chave de junção:** CNPJ_BASE, PERIODO
- **Operador:** NAO_EXISTE
- **Condição:** NF-e com CRT 1/4 no mesmo período em que existem EFD-Contribuições ou ECF de lucro real/presumido indica incoerência de enquadramento.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** LC 123/2006 – Simples Nacional; IN RFB 1.252/2012 – EFD-Contribuições; IN RFB 2.004/2021 – ECF
- **Hipóteses legítimas:** Desenquadramento/exclusão do Simples no período com NF-e emitidas antes da data de efeito | Erro de parametrização do emissor (CRT informado errado)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e emitidas com CRT {crt} (Simples/MEI) coexistem com {obrigacao} de regime não simplificado em {periodo}. Enquadramento incoerente: os demais cruzamentos de valor ficam não conclusivos."

### PAR-003 – Lucro presumido/arbitrado exige PIS/COFINS cumulativo

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECF → `0010.FORMA_TRIB, 0010.FORMA_TRIB_PER` (universo: FORMA_TRIB in (3,4,5,6,7) – trimestres P ou A)
- **Destino:** EFD-Contribuições → `0110.COD_INC_TRIB` · agregação: MES
- **Chave de junção:** CNPJ_BASE, MES_DO_TRIMESTRE
- **Operador:** COMPATIVEL
- **Condição:** Nos meses de trimestre tributado pelo presumido/arbitrado, COD_INC_TRIB deve ser 2 (cumulativo).
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** Lei 10.637/2002 – PIS não cumulativo (art. 3º; art. 8º); Lei 10.833/2003 – COFINS não cumulativa (art. 3º; art. 10); Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "A ECF indica {formaTrib} no trimestre {trimestre}, mas a EFD-Contribuições de {mes} informa COD_INC_TRIB={codIncTrib} (esperado 2 – cumulativo)."

### PAR-004 – Lucro real com PIS/COFINS exclusivamente cumulativo

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECF → `0010.FORMA_TRIB, FORMA_TRIB_PER` (universo: trimestres R (real))
- **Destino:** EFD-Contribuições → `0110.COD_INC_TRIB` · agregação: MES
- **Chave de junção:** CNPJ_BASE, MES
- **Operador:** COMPATIVEL
- **Condição:** Lucro real espera COD_INC_TRIB 1 ou 3. Valor 2 só é admissível se todas as receitas estiverem nas exceções do art. 8º da Lei 10.637/02 e art. 10 da Lei 10.833/03.
- **Severidade:** ALERTA
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** Lei 10.637/2002 – PIS não cumulativo (art. 3º; art. 8º); Lei 10.833/2003 – COFINS não cumulativa (art. 3º; art. 10)
- **Hipóteses legítimas:** Atividades integralmente listadas nas exceções legais à não cumulatividade
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Lucro real em {periodo} com EFD-Contribuições exclusivamente cumulativa (COD_INC_TRIB=2). Confirmar se todas as receitas estão nas exceções legais."

### PAR-005 – Regime de caixa do presumido deve ser o mesmo para IRPJ/CSLL e PIS/COFINS

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECF → `Indicador de regime de reconhecimento de receitas do presumido (ex.: Y672.IND_REG_APUR)`
- **Destino:** EFD-Contribuições → `0110.IND_REG_CUM` · agregação: MES
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** COMPATIVEL
- **Condição:** ECF regime de caixa <=> IND_REG_CUM=1 em todos os meses; ECF competência <=> IND_REG_CUM in (2,9).
- **Aplicável se:** ECF FORMA_TRIB presumido em algum trimestre
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** IN SRF 104/1998 – Regime de caixa no lucro presumido (conferir dispositivo consolidado na IN RFB 1.700/2017); Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Regime de reconhecimento de receitas divergente: ECF={regimeEcf}, EFD-Contribuições IND_REG_CUM={indRegCum} em {meses}."

### PAR-006 – Lucro real exige ECD recuperada na ECF

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECF → `0010.FORMA_TRIB` (universo: FORMA_TRIB in (1,2))
- **Destino:** ECD → `0000 (ano); ECF C040`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** EXISTE
- **Condição:** Lucro real: deve existir ECD do período e a ECF deve conter Bloco C (C040) recuperando-a.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** IN RFB 2.003/2021 – ECD; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "ECF de lucro real em {ano} sem ECD correspondente recebida/recuperada."

### PAR-007 – Presumido: forma de escrituração na ECF coerente com existência de ECD

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECF → `0010.TIP_ESC_PRE, Bloco Q`
- **Destino:** ECD → `0000 (ano)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** COMPATIVEL
- **Condição:** TIP_ESC_PRE=C exige ECD do período; TIP_ESC_PRE=L exige Bloco Q (Livro Caixa) e ausência de ECD.
- **Aplicável se:** ECF FORMA_TRIB presumido
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; IN RFB 2.003/2021 – ECD
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "ECF de lucro presumido com TIP_ESC_PRE={tipEscPre}, incompatível com {situacaoEcd}."

### PAR-008 – Plano referencial da ECD compatível com forma de tributação e qualificação da PJ na ECF

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECD → `0000.COD_PLAN_REF`
- **Destino:** ECF → `0010.FORMA_TRIB, 0010.COD_QUALIF_PJ`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** COMPATIVEL
- **Condição:** Tabela de compatibilidade COD_PLAN_REF x (FORMA_TRIB, COD_QUALIF_PJ) conforme manuais ECD/ECF.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026); Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Plano referencial da ECD ({codPlanRef}) incompatível com a tributação/qualificação da ECF ({formaTrib}/{codQualif})."

### PAR-009 – Indicador de atividade industrial coerente com IPI nos documentos e na apuração

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** EFD ICMS/IPI → `0000.IND_ATIV`
- **Destino:** NF-e/NFC-e → `total/ICMSTot/vIPI (saídas próprias); EFD E520` · agregação: MES
- **Chave de junção:** CNPJ, MES
- **Operador:** COMPATIVEL
- **Condição:** IND_ATIV=1 com NF-e de saída própria com IPI destacado, ou IND_ATIV=0 com IPI destacado e sem E520, é incoerente.
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Equiparação a industrial (RIPI) não refletida no indicador
- **Cadeias de valor:** C3
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estabelecimento {cnpj} com IND_ATIV={indAtiv} e IPI destacado de R$ {vIPI} em {mes}."

### PAR-010 – Situação especial e período coerentes entre ECD, ECF e encerramento das EFDs

- **Mapa/seção do núcleo:** NUCLEO-1 §5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** ECD → `0000.IND_SIT_ESP, DT_INI, DT_FIN`
- **Destino:** ECF → `0000.SIT_ESPECIAL, DT_SIT_ESP, DT_INI, DT_FIN`
- **Chave de junção:** CNPJ_BASE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Condição:** Mesmo período e mesma situação especial (cisão, fusão, incorporação, extinção) nas duas escriturações; não deve haver EFD mensal após a data do evento de extinção/incorporação.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026); Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Período/situação especial divergente: ECD {ecdPeriodo}/{ecdSit} x ECF {ecfPeriodo}/{ecfSit}."

### EXC-001 – Transferências entre estabelecimentos não compõem receita

- **Mapa/seção do núcleo:** NUCLEO-1 §4.1
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `det/prod/CFOP` (universo: natureza TRANSFERENCIA (PARAM:TABELA_CFOP_RECEITA))
- **Operador:** FILTRA
- **Condição:** Remove do universo de receita (EFD-Contribuições, ECD, ECF). Continuam exigidas nas EFD ICMS/IPI do remetente e do destinatário.
- **Severidade:** INFO
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Convênio SINIEF s/nº de 1970 (Tabela CFOP; art. 7º §1º-A – Carta de Correção); Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave} tratado como transferência (sem receita)."

### EXC-002 – Remessas, retornos e bonificações sem receita

- **Mapa/seção do núcleo:** NUCLEO-1 §4.1
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `det/prod/CFOP` (universo: natureza REMESSA, RETORNO, BONIFICACAO (PARAM:TABELA_CFOP_RECEITA))
- **Operador:** FILTRA
- **Condição:** Remove do universo de presença exigida na EFD-Contribuições e do universo de receita contábil. Continuam exigidas na EFD ICMS/IPI.
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave} sem repercussão em receita/PIS/COFINS (CFOP {cfop})."

### EXC-003 – NF-e cancelada ou denegada fora dos universos de receita e crédito

- **Mapa/seção do núcleo:** NUCLEO-1 §4.1
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `evento 110111 / protocolo de denegação`
- **Operador:** FILTRA
- **Condição:** Remove de receita/crédito. A presença exigida na EFD ICMS/IPI passa a ser como COD_SIT 02/03/04 (regras M1-P02/P03).
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; Guia Prático da EFD ICMS/IPI v3.2.2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave} cancelado/denegado."

### EXC-004 – NF-e destinada com desconhecimento ou operação não realizada

- **Mapa/seção do núcleo:** NUCLEO-2 M1.5
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `eventos 210220, 210240` (universo: dest/CNPJ = contribuinte)
- **Operador:** FILTRA
- **Condição:** Remove da exigência de escrituração como entrada (M1-P05).
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** NT 2020.001 – Manifestação do Destinatário v1.60
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} com manifestação {evento}: não exigida como entrada."

### EXC-005 – Documento de outro estabelecimento

- **Mapa/seção do núcleo:** NUCLEO-2 M1.5
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `emit/CNPJ, dest/CNPJ`
- **Operador:** FILTRA
- **Condição:** Na EFD ICMS/IPI (por estabelecimento), exige-se apenas documento cujo CNPJ completo do papel (emit/dest) seja o do 0000. Documentos de outras filiais vão para a EFD da filial.
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave} pertence ao estabelecimento {cnpj}."

### EXC-006 – Presumido em regime de caixa: documento ainda não recebido

- **Mapa/seção do núcleo:** NUCLEO-2 M2.4 / M4.4
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `documentos de receita (NF-e/NFS-e)` (universo: IND_REG_CUM=1)
- **Operador:** FILTRA
- **Condição:** Com IND_REG_CUM=1, a receita é exigida em F500/F510/F525 somente no período do recebimento; documentos não recebidos não geram omissão no mês de emissão.
- **Aplicável se:** EFD_CONTRIB 0110.IND_REG_CUM = 1
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; IN SRF 104/1998 – Regime de caixa no lucro presumido (conferir dispositivo consolidado na IN RFB 1.700/2017)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave}: receita reconhecida pelo regime de caixa."

### EXC-007 – NF-e somente com IBS/CBS (sem ICMS/IPI)

- **Mapa/seção do núcleo:** NUCLEO-2 M1.5; 04-reforma
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `grupo IBS/CBS (NT 2025.002) sem ICMS/IPI`
- **Operador:** FILTRA
- **Condição:** Aplicar a exceção ao C100 prevista no Guia Prático EFD ICMS/IPI 3.2.x para documentos que contenham apenas tributos da Reforma Tributária do Consumo (conferir texto exato da exceção).
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; NT 2025.002 – Reforma Tributária do Consumo (IBS/CBS/IS) na NF-e/NFC-e v1.52
- **Vigência a partir de:** 2026-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Documento {chave} contém apenas IBS/CBS; exceção do Guia aplicada."

### EXC-008 – Estoque de terceiros em poder do informante não compõe o ativo

- **Mapa/seção do núcleo:** NUCLEO-1 §4.1; NUCLEO-3 C6
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** EFD ICMS/IPI → `H010.IND_PROP` (universo: IND_PROP = 2)
- **Operador:** FILTRA
- **Condição:** Remove do total de inventário comparado com ECD/ECF.
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Itens de propriedade de terceiros excluídos do cruzamento de estoque."

### EXC-009 – Aquisições sem direito/opção de crédito de PIS/COFINS

- **Mapa/seção do núcleo:** NUCLEO-2 M2.4
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `NF-e de entrada` (universo: COD_INC_TRIB=2 ou aquisição sem crédito)
- **Operador:** FILTRA
- **Condição:** Aquisições sem crédito podem não constar da EFD-Contribuições; não gerar omissão.
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Aquisição {chave} sem crédito: escrituração na EFD-Contribuições não exigida."

### EXC-010 – NFS-e substituída ou cancelada

- **Mapa/seção do núcleo:** NUCLEO-1 §4.2
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NFS-e → `eventos de cancelamento/substituição`
- **Operador:** FILTRA
- **Condição:** Somente a NFS-e substituta (vigente) compõe receita; a substituída e a cancelada saem do universo.
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** NFS-e Padrão Nacional – Anexo II – Eventos v1.01 (20260122)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NFS-e {chave} substituída/cancelada excluída."

### EXC-011 – Receitas e créditos sem documento fiscal

- **Mapa/seção do núcleo:** NUCLEO-1 §4.3
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** EFD-Contribuições → `F100, F120, F130, F150, F200, F600, I, P`
- **Operador:** FILTRA
- **Condição:** Valores destes registros não exigem NF-e/NFS-e de lastro; o lastro é a contabilidade (ECD).
- **Severidade:** INFO
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Registro {registro} não exige documento fiscal eletrônico."

### EXC-012 – Serviço com documento municipal fora do padrão nacional

- **Mapa/seção do núcleo:** NUCLEO-2 M4.4
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** EFD-Contribuições → `A100` (universo: CHV_NFSE vazio)
- **Operador:** FILTRA
- **Condição:** A100 sem chave nacional não é buscado no universo de NFS-e nacional; reportar como lacuna de cobertura (INFO).
- **Severidade:** INFO
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "A100 {numDoc} sem chave de NFS-e nacional: lastro não verificável neste agente."

### EXC-013 – Entrada ainda dentro da janela de escrituração

- **Mapa/seção do núcleo:** NUCLEO-2 M1.1
- **Tipo de relação:** EXC – Exclusividade legítima (filtro aplicado antes)
- **Origem:** NF-e/NFC-e → `ide/dhEmi` (universo: dest = contribuinte; mês de análise < mês(dhEmi) + PARAM:JANELA_MESES_ENTRADA)
- **Operador:** FILTRA · tolerância: PARAM:JANELA_MESES_ENTRADA
- **Condição:** Não acusar omissão de entrada enquanto a janela não terminou ou enquanto as EFD dos meses seguintes não foram recebidas.
- **Severidade:** INFO
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} ainda dentro da janela de escrituração de entrada."

### ENT-001 – Estabelecimento com EFD ICMS/IPI consta do 0140 da EFD-Contribuições

- **Mapa/seção do núcleo:** NUCLEO-1 §3.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `0000.CNPJ` · agregação: MES
- **Destino:** EFD-Contribuições → `0140.CNPJ` · agregação: MES
- **Chave de junção:** CNPJ, MES
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** PAR-001
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estabelecimento {cnpj} possui EFD ICMS/IPI em {mes}, mas não consta do registro 0140 da EFD-Contribuições."

### ENT-002 – Estabelecimento do 0140 com mercadorias e IE possui EFD ICMS/IPI

- **Mapa/seção do núcleo:** NUCLEO-1 §3.1
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD-Contribuições → `0140 (com IE) + Bloco C com movimento` · agregação: MES
- **Destino:** EFD ICMS/IPI → `0000` · agregação: MES
- **Chave de junção:** CNPJ, MES
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Ajuste SINIEF 02/2009 – EFD ICMS/IPI; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Dispensa da UF | Escrituração do estabelecimento não fornecida ao agente (cobertura)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estabelecimento {cnpj} com movimento de mercadorias na EFD-Contribuições em {mes} sem EFD ICMS/IPI correspondente."

### ENT-003 – Emitente de NF-e de saída possui EFD ICMS/IPI no mês

- **Mapa/seção do núcleo:** NUCLEO-1 §3.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `emit/CNPJ` (universo: estabelecimentos do grupo, CRT=3) · agregação: MES
- **Destino:** EFD ICMS/IPI → `0000.CNPJ` · agregação: MES
- **Chave de junção:** CNPJ, MES
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Ajuste SINIEF 02/2009 – EFD ICMS/IPI
- **Hipóteses legítimas:** Dispensa da UF | Cobertura incompleta de insumos
- **Depende de:** PAR-001
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estabelecimento {cnpj} emitiu {qtd} NF-e em {mes} sem EFD ICMS/IPI do período."

### ENT-004 – Raiz do CNPJ igual em todas as escriturações analisadas

- **Mapa/seção do núcleo:** NUCLEO-1 §3.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `0000.CNPJ`
- **Destino:** ECF → `0000.CNPJ; EFD_CONTRIB 0000.CNPJ; EFD_ICMS_IPI 0000.CNPJ`
- **Chave de junção:** CNPJ_BASE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESTRUTURA_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026); Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Guia Prático da EFD-Contribuições v1.35; Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** SCP (TIP_ECD/TIP_ECF = 2) com CNPJ próprio
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Escriturações de raízes de CNPJ diferentes no mesmo lote: {lista}. Lote inconsistente."

### ENT-005 – Inscrição estadual coerente entre EFD, NF-e e 0140

- **Mapa/seção do núcleo:** NUCLEO-1 §3.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `0000.IE`
- **Destino:** NF-e/NFC-e → `emit/IE (próprias do estabelecimento); EFD_CONTRIB 0140.IE`
- **Chave de junção:** CNPJ, MES
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Alteração cadastral no mês
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "IE divergente para {cnpj}: EFD={ieEfd}, NF-e={ieNfe}, 0140={ie0140}."

### ENT-010 – Participante do C100 = emitente/destinatário do XML

- **Mapa/seção do núcleo:** NUCLEO-1 §3.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.COD_PART -> 0150.CNPJ/CPF`
- **Destino:** NF-e/NFC-e → `dest/CNPJ|CPF (saída própria) ou emit/CNPJ (entrada de terceiros)`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: participante escriturado {cnpjEfd} difere do XML {cnpjXml}."

### ENT-011 – Mesmo documento com mesmo participante nas duas EFD

- **Mapa/seção do núcleo:** NUCLEO-1 §3.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.COD_PART -> 0150.CNPJ`
- **Destino:** EFD-Contribuições → `C100.COD_PART -> 0150.CNPJ`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave} com participantes diferentes nas EFD ({cnpjIcms} x {cnpjContrib})."

### ENT-012 – Instituição de pagamento/intermediador do 1601 = declarante da DIMP

- **Mapa/seção do núcleo:** NUCLEO-1 §3.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `1601.COD_PART_IP, 1601.COD_PART_IT -> 0150.CNPJ`
- **Destino:** DIMP → `CNPJ do declarante`
- **Chave de junção:** CNPJ_INSTITUICAO, MES, CNPJ_ESTABELECIMENTO
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Ato COTEPE/ICMS 65/2018 – DIMP (Versão 10 a partir de 01/06/2026, redação do Ato COTEPE/ICMS 124/25) vDIMP v10
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Instituição {cnpjInst} não casa entre DIMP e registro 1601 de {cnpj} em {mes}."

### ENT-020 – Cadastro do item (saída própria) coerente com o XML

- **Mapa/seção do núcleo:** NUCLEO-1 §3.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `0200.COD_ITEM, COD_NCM, COD_BARRA, CEST, EX_IPI`
- **Destino:** NF-e/NFC-e → `det/prod/cProd, NCM, cEAN, CEST, EXTIPI` (universo: saídas próprias)
- **Chave de junção:** COD_ITEM=cProd
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA · atenuada para INFO quando: Divergência apenas de descrição ou código de barras
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Reclassificação de NCM no período (0205 – alteração do item)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem}: NCM no cadastro {ncmEfd} x NCM no XML {ncmXml}."

### ENT-021 – Item de entrada de terceiros: NCM do fornecedor coerente com o cadastro do informante

- **Mapa/seção do núcleo:** NUCLEO-1 §3.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C170.NUM_ITEM, C170.COD_ITEM -> 0200.COD_NCM`
- **Destino:** NF-e/NFC-e → `det/@nItem, prod/NCM` (universo: entradas de terceiros)
- **Chave de junção:** CHV_NFE, NUM_ITEM=nItem
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Item recebido com classificação diferente do cadastro interno (verificar destinação)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} item {nItem}: NCM do fornecedor {ncmXml} x cadastro do informante {ncmEfd}."

### ENT-022 – Unidade diferente da de estoque exige fator de conversão

- **Mapa/seção do núcleo:** NUCLEO-1 §3.3
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `C170.UNID <> 0200.UNID_INV`
- **Destino:** EFD ICMS/IPI → `0220 (UNID_CONV, FAT_CONV)`
- **Chave de junção:** COD_ITEM, UNID
- **Operador:** EXISTE
- **Condição:** Além da existência do 0220, QTD convertida deve ser coerente com qCom/qTrib do XML.
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem} movimentado em {unid} sem fator de conversão para {unidInv}."

### ENT-023 – Mesmo item com mesmo NCM/descrição nas duas EFD

- **Mapa/seção do núcleo:** NUCLEO-1 §3.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `0200.COD_ITEM, COD_NCM, DESCR_ITEM, TIPO_ITEM`
- **Destino:** EFD-Contribuições → `0200.COD_ITEM, COD_NCM, DESCR_ITEM, TIPO_ITEM`
- **Chave de junção:** COD_ITEM, MES
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem} com cadastro divergente entre EFD ICMS/IPI e EFD-Contribuições ({campo})."

### ENT-030 – Contas do 0500 das EFD existem no plano de contas da ECD

- **Mapa/seção do núcleo:** NUCLEO-1 §3.4
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD-Contribuições → `0500.COD_CTA (e EFD_ICMS_IPI 0500.COD_CTA)` · agregação: ANO
- **Destino:** ECD → `I050.COD_CTA` · agregação: ANO
- **Chave de junção:** COD_CTA, ANO
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mudança de plano de contas no ano (ECD 0000.IND_MUDANC_PC + I157) – aceitar código antigo mapeado
- **Cadeias de valor:** C7
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta} usada na {obrigacao} não existe no plano de contas da ECD de {ano}."

### ENT-031 – Natureza e tipo da conta iguais entre 0500 e I050 (conta analítica)

- **Mapa/seção do núcleo:** NUCLEO-1 §3.4
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD-Contribuições → `0500.COD_NAT_CC, 0500.IND_CTA`
- **Destino:** ECD → `I050.COD_NAT, I050.IND_CTA`
- **Chave de junção:** COD_CTA
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Condição:** Contas referenciadas em registros de movimento devem ser analíticas (IND_CTA = A).
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** ENT-030
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta}: natureza/tipo na EFD ({natEfd}/{indEfd}) difere da ECD ({natEcd}/{indEcd})."

### ENT-032 – Natureza da conta coerente com o uso no registro

- **Mapa/seção do núcleo:** NUCLEO-1 §3.4
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** EFD-Contribuições → `COD_CTA em C181/A170/F500 (receita), F120/F130 (ativo); EFD_ICMS_IPI H010 (estoque), 0300 (ativo)`
- **Destino:** ECD → `I050.COD_NAT`
- **Chave de junção:** COD_CTA
- **Operador:** COMPATIVEL
- **Condição:** Receita -> COD_NAT 04; estoque e ativo imobilizado -> COD_NAT 01.
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Depende de:** ENT-030
- **Cadeias de valor:** C7
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta} (natureza {codNat}) usada em {registro}, que espera natureza {esperada}."

### ENT-033 – Contas vinculadas a ajustes do e-Lalur existem e movimentaram na ECD

- **Mapa/seção do núcleo:** NUCLEO-1 §3.4
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** ECF → `M310.COD_CTA`
- **Destino:** ECD → `I050.COD_CTA, I155/I355 com movimento`
- **Chave de junção:** COD_CTA, ANO
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Ajuste do e-Lalur/e-Lacs vinculado à conta {codCta}, sem saldo/movimento na ECD."

## Grupo DOCUMENTOS – 51 regras

Mapas M1 (NF-e x EFD ICMS/IPI), M2 (NF-e x EFD-Contribuições), M4 (NFS-e x EFD-Contribuições) e M5 (NFS-e x EFD ICMS/IPI).

### M1-P01 – NF-e própria autorizada escriturada na EFD ICMS/IPI

- **Mapa/seção do núcleo:** M1.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `infNFe/@Id` (universo: emit/CNPJ = 0000.CNPJ; autorizada; sem evento 110111) · agregação: DOCUMENTO
- **Destino:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: IND_EMIT=0; COD_SIT in (00,01,06,07,08))
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Condição:** Período de alocação: mês de dhEmi.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** NF-e emitida no último dia do mês com saída no mês seguinte – conferir se escriturada no mês seguinte com COD_SIT 01 (extemporâneo)
- **Filtros de exclusão aplicados antes:** EXC-003, EXC-005, EXC-007
- **Depende de:** PAR-001, ENT-003
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} emitida em {dhEmi} (R$ {vNF}) não está escriturada na EFD ICMS/IPI de {cnpj}/{mes}. Indício de omissão de saída."

### M1-P02 – NF-e própria cancelada escriturada com COD_SIT 02/03

- **Mapa/seção do núcleo:** M1.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `infNFe/@Id` (universo: emit/CNPJ = 0000.CNPJ; evento 110111 homologado)
- **Destino:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: COD_SIT in (02,03))
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Filtros de exclusão aplicados antes:** EXC-005
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e cancelada {chave} não informada como cancelada na EFD ICMS/IPI de {mes}."

### M1-P03 – NF-e própria denegada escriturada com COD_SIT 04

- **Mapa/seção do núcleo:** M1.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `infNFe/@Id` (universo: emit/CNPJ = 0000.CNPJ; protocolo de denegação)
- **Destino:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: COD_SIT = 04)
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Filtros de exclusão aplicados antes:** EXC-005
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e denegada {chave} não informada com COD_SIT 04 em {mes}."

### M1-P04 – Numeração inutilizada escriturada com COD_SIT 05

- **Mapa/seção do núcleo:** M1.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `protocolo de inutilização (faixa mod/série/números)`
- **Destino:** EFD ICMS/IPI → `C100 (COD_MOD, SER, NUM_DOC)` (universo: COD_SIT = 05)
- **Chave de junção:** CNPJ, COD_MOD, SER, NUM_DOC
- **Operador:** EXISTE
- **Aplicável se:** data da inutilização < PARAM:DATA_VIGENCIA_NT_2023_002
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; NT 2023.002 – Eliminação da autorização assíncrona e dos serviços de inutilização v1.02
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Faixa inutilizada {serie}/{ini}-{fim} não informada na EFD de {mes}."

### M1-P05 – NF-e destinada ao estabelecimento escriturada como entrada

- **Mapa/seção do núcleo:** M1.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `infNFe/@Id` (universo: dest/CNPJ = 0000.CNPJ; autorizada; sem 110111)
- **Destino:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: IND_EMIT=1; IND_OPER=0)
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE · tolerância: PARAM:JANELA_MESES_ENTRADA
- **Condição:** Procurar no mês de dhEmi e nos meses da janela (DT_E_S).
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Encontrada em mês posterior dentro da janela (apenas deslocamento)
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; NT 2020.001 – Manifestação do Destinatário v1.60
- **Hipóteses legítimas:** Mercadoria ainda não recebida | Operação desconhecida pelo destinatário sem manifestação registrada
- **Filtros de exclusão aplicados antes:** EXC-003, EXC-004, EXC-005, EXC-013
- **Cadeias de valor:** C2, C6, C7
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} de {emitente} (R$ {vNF}) destinada a {cnpj} não foi escriturada como entrada até {mesLimite}. Indício de omissão de entrada."

### M1-P06 – NF-e com Confirmação da Operação (210200) escriturada como entrada

- **Mapa/seção do núcleo:** M1.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `evento 210200` (universo: dest/CNPJ = 0000.CNPJ)
- **Destino:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: IND_EMIT=1; IND_OPER=0)
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE · tolerância: PARAM:JANELA_MESES_ENTRADA
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** NT 2020.001 – Manifestação do Destinatário v1.60; Guia Prático da EFD ICMS/IPI v3.2.2
- **Filtros de exclusão aplicados antes:** EXC-003, EXC-013
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "O contribuinte confirmou a operação da NF-e {chave} (evento 210200), mas não a escriturou como entrada."

### M1-R01 – C100 regular de NF-e tem XML autorizado e não cancelado

- **Mapa/seção do núcleo:** M1.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: COD_MOD in (55,65); COD_SIT in (00,01,06,07,08))
- **Destino:** NF-e/NFC-e → `infNFe/@Id + protocolo` (universo: autorizada; sem 110111)
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Ajuste SINIEF 07/2005 – NF-e
- **Hipóteses legítimas:** XML não fornecido ao agente (cobertura) – reportar como não verificável, não como erro
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "C100 {chave} escriturado como regular, mas a NF-e {situacao} (inexistente/cancelada/denegada)."

### M1-R02 – C100 cancelado tem evento de cancelamento homologado

- **Mapa/seção do núcleo:** M1.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD ICMS/IPI → `C100.CHV_NFE` (universo: COD_SIT in (02,03); IND_EMIT=0)
- **Destino:** NF-e/NFC-e → `evento 110111`
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} informada como cancelada na EFD, mas está autorizada sem evento de cancelamento. Débito de ICMS de R$ {vICMS} omitido."

### M1-R03 – C100 de emissão própria pertence ao estabelecimento

- **Mapa/seção do núcleo:** M1.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `0000.CNPJ` (universo: C100.IND_EMIT=0)
- **Destino:** NF-e/NFC-e → `emit/CNPJ`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} escriturada como emissão própria de {cnpjEfd}, mas o emitente é {cnpjXml}."

### M1-R04 – C100 de entrada de terceiros é destinado ao estabelecimento

- **Mapa/seção do núcleo:** M1.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `0000.CNPJ` (universo: C100.IND_EMIT=1; IND_OPER=0)
- **Destino:** NF-e/NFC-e → `dest/CNPJ`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; LC 87/1996 – ICMS (art. 20, §5º – CIAP 1/48)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Entrada {chave} escriturada por {cnpjEfd}, mas a NF-e é destinada a {cnpjDest}. Crédito sem lastro."

### M1-R05 – Documento referenciado (C113) coerente com NFref

- **Mapa/seção do núcleo:** M1.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C113.CHV_DOCe`
- **Destino:** NF-e/NFC-e → `ide/NFref/refNFe`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "NF-e {chave}: documento referenciado no C113 ({refEfd}) difere do XML ({refXml})."

### M1-R06 – Bem do CIAP com NF-e de aquisição válida

- **Mapa/seção do núcleo:** M1.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD ICMS/IPI → `G130.CHV_NFE_CTA`
- **Destino:** NF-e/NFC-e → `infNFe/@Id` (universo: autorizada; sem 110111; dest = estabelecimento)
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; LC 87/1996 – ICMS (art. 20, §5º – CIAP 1/48)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C7
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Crédito de CIAP baseado na NF-e {chave}, que {situacao}."

### M1-I01 – Identificação do documento igual (modelo/série/número/data)

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.COD_MOD, SER, NUM_DOC, DT_DOC`
- **Destino:** NF-e/NFC-e → `ide/mod, serie, nNF, dhEmi(data)`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Divergência apenas em DT_DOC
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** M1-P01
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: {campo} escriturado {valorEfd} x XML {valorXml}."

### M1-I02 – Data de entrada não anterior à emissão

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NF-e/NFC-e → `ide/dhEmi`
- **Destino:** EFD ICMS/IPI → `C100.DT_E_S` (universo: IND_OPER=0)
- **Chave de junção:** CHV_NFE
- **Operador:** MAIOR_OU_IGUAL · sem tolerância (valor exato)
- **Condição:** DT_E_S >= data(dhEmi). Para saída própria, DT_E_S = data(dhSaiEnt) quando informado.
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: data de entrada {dtES} anterior à emissão {dhEmi}."

### M1-I03 – Valor total do documento igual

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.VL_DOC`
- **Destino:** NF-e/NFC-e → `total/ICMSTot/vNF`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: VL_DOC escriturado R$ {vlDoc} x vNF R$ {vNF} (diferença R$ {dif})."

### M1-I04 – Componentes do valor (mercadorias, desconto, frete, seguro, outras) iguais

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.VL_MERC, VL_DESC, VL_FRT, VL_SEG, VL_OUT_DA, IND_FRT, IND_PGTO`
- **Destino:** NF-e/NFC-e → `vProd, vDesc, vFrete, vSeg, vOutro, transp/modFrete, detPag/indPag`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ALERTA · atenuada para INFO quando: Somente IND_FRT ou IND_PGTO divergentes
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: {campo} escriturado {valorEfd} x XML {valorXml}."

### M1-I05 – ICMS próprio da saída própria igual ao destacado

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.VL_BC_ICMS, VL_ICMS` (universo: IND_EMIT=0)
- **Destino:** NF-e/NFC-e → `ICMSTot/vBC, vICMS`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: ICMS escriturado R$ {vlIcms} x destacado R$ {vICMS}."

### M1-I06 – Crédito de ICMS na entrada não excede o destacado

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NF-e/NFC-e → `ICMSTot/vBC, vICMS`
- **Destino:** EFD ICMS/IPI → `C100.VL_BC_ICMS, VL_ICMS` (universo: IND_EMIT=1; IND_OPER=0)
- **Chave de junção:** CHV_NFE
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Condição:** Valor escriturado <= valor do XML. Valor menor ou zero é legítimo (uso e consumo, ativo via CIAP, vedação).
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** LC 87/1996 – ICMS (art. 20, §5º – CIAP 1/48); Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Ajuste de crédito por documento em C197 com fundamento legal
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Entrada {chave}: crédito escriturado R$ {vlIcms} supera o ICMS destacado R$ {vICMS}."

### M1-I07 – ICMS-ST da saída própria igual ao retido

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.VL_BC_ICMS_ST, VL_ICMS_ST` (universo: IND_EMIT=0)
- **Destino:** NF-e/NFC-e → `ICMSTot/vBCST, vST`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: ICMS-ST escriturado R$ {vlSt} x retido R$ {vST}."

### M1-I08 – ICMS-ST retido pelo substituto apurado no E200/E210 da UF de destino

- **Mapa/seção do núcleo:** M1.3; NUCLEO-3 C2 CP-3b
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `vST > 0; dest/enderDest/UF` (universo: emissão própria) · agregação: MES
- **Destino:** EFD ICMS/IPI → `E200.UF, E210`
- **Chave de junção:** CNPJ, MES, UF_DESTINO
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Há ICMS-ST retido para {uf} em {mes} (R$ {vST}) sem apuração E200/E210 dessa UF."

### M1-I09 – IPI escriturado igual ao destacado (própria) e limitado (terceiros)

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.VL_IPI`
- **Destino:** NF-e/NFC-e → `ICMSTot/vIPI`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Condição:** Própria: igualdade. Terceiros: escriturado <= destacado.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Entrada sem direito a crédito de IPI (valor menor legítimo)
- **Cadeias de valor:** C3
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: IPI escriturado R$ {vlIpi} x destacado R$ {vIPI}."

### M1-I10 – PIS/COFINS do C100 da EFD ICMS/IPI iguais ao XML (própria)

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.VL_PIS, VL_COFINS` (universo: IND_EMIT=0)
- **Destino:** NF-e/NFC-e → `ICMSTot/vPIS, vCOFINS`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Campos informativos na EFD ICMS/IPI; a apuração é feita na EFD-Contribuições
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: PIS/COFINS na EFD ICMS/IPI divergentes do XML."

### M1-I11 – DIFAL/FCP da NF-e refletidos no C101 e na apuração E300/E310

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** NF-e/NFC-e → `ICMSTot/vFCPUFDest, vICMSUFDest, vICMSUFRemet`
- **Destino:** EFD ICMS/IPI → `C101.VL_FCP_UF_DEST, VL_ICMS_UF_DEST, VL_ICMS_UF_REM; E300/E310 da UF de destino`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: DIFAL/FCP do XML não refletido no C101/E310 ({detalhe})."

### M1-I12 – COD_SIT coerente com a situação e a finalidade da NF-e

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.COD_SIT`
- **Destino:** NF-e/NFC-e → `protocolo, eventos, ide/finNFe`
- **Chave de junção:** CHV_NFE
- **Operador:** COMPATIVEL
- **Condição:** Autorizada -> 00/01; complementar (finNFe=2) -> 06/07; cancelada -> 02/03; denegada -> 04.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** COD_SIT 08 (regime especial ou norma específica)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: COD_SIT {codSit} incompatível com a situação {situacaoXml}."

### M1-I13 – Itens do C170 coerentes com os itens do XML

- **Mapa/seção do núcleo:** M1.4
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C170.NUM_ITEM, QTD, UNID, VL_ITEM, VL_DESC`
- **Destino:** NF-e/NFC-e → `det/@nItem, qCom/qTrib, uCom/uTrib, vProd, vDesc`
- **Chave de junção:** CHV_NFE, NUM_ITEM=nItem
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Condição:** Quantidade comparada após aplicação do fator 0220 quando a unidade diferir.
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Saída própria por NF-e: C170 dispensado pelo Guia (salvo exceções) – não exigir
- **Depende de:** ENT-022
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} item {nItem}: {campo} escriturado {valorEfd} x XML {valorXml}."

### M1-I14 – CFOP e CST de ICMS dos itens coerentes

- **Mapa/seção do núcleo:** M1.4
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C170.CFOP, CST_ICMS / C190.CFOP, CST_ICMS`
- **Destino:** NF-e/NFC-e → `prod/CFOP, ICMS/orig+CST (ou CSOSN)`
- **Chave de junção:** CHV_NFE, NUM_ITEM=nItem
- **Operador:** COMPATIVEL
- **Condição:** Própria: igualdade. Terceiros: CFOP convertido (5->1, 6->2, 7->3) ajustado à destinação; CST do ponto de vista do informante.
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Convênio SINIEF s/nº de 1970 (Tabela CFOP; art. 7º §1º-A – Carta de Correção)
- **Hipóteses legítimas:** Destinação diferente da presumida pelo fornecedor (revenda x consumo x ativo)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} item {nItem}: CFOP/CST escriturado {efd} incompatível com o XML {xml}."

### M1-I15 – Analítico C190 = agregação dos itens do XML (saída própria)

- **Mapa/seção do núcleo:** M1.4
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NF-e/NFC-e → `itens: vProd, vFrete, vSeg, vOutro, vDesc, vBC, vICMS, vBCST, vST, vIPI` · agregação: GRUPO_CST_CFOP_ALIQ
- **Destino:** EFD ICMS/IPI → `C190.VL_OPR, VL_BC_ICMS, VL_ICMS, VL_BC_ICMS_ST, VL_ICMS_ST, VL_IPI` (universo: IND_EMIT=0)
- **Chave de junção:** CHV_NFE, CST_ICMS, CFOP, ALIQ_ICMS
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Condição:** Composição de VL_OPR conforme definição do campo no Guia vigente (conferir inclusão de FCP-ST).
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "NF-e {chave}: analítico C190 ({cst}/{cfop}/{aliq}) não corresponde aos itens do XML ({campo}: {efd} x {xml})."

### M1-I16 – Benefício fiscal (cBenef) com ajuste correspondente na escrituração

- **Mapa/seção do núcleo:** M1.4
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `det/prod/cBenef` (universo: UF exige cBenef e benefício afeta apuração)
- **Destino:** EFD ICMS/IPI → `C197 / E111 (código de ajuste da tabela 5.3 da UF)`
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Benefício sem reflexo em ajuste de apuração (ex.: isenção com CST próprio)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} com cBenef {cBenef} sem ajuste correspondente na EFD."

### M1-I17 – Devolução (finNFe=4) com documento referenciado e CFOP de devolução

- **Mapa/seção do núcleo:** M1.3
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `ide/finNFe = 4`
- **Destino:** EFD ICMS/IPI → `C113 + CFOP de devolução no C190`
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Devolução {chave} sem C113 e/ou CFOP de devolução na EFD."

### M2-P01 – NF-e de saída com receita presente na EFD-Contribuições

- **Mapa/seção do núcleo:** M2.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `infNFe/@Id; itens` (universo: emit = estabelecimento do 0140; autorizada; CFOP de receita (PARAM:TABELA_CFOP_RECEITA))
- **Destino:** EFD-Contribuições → `C100.CHV_NFE (IND_ESCRI=2) | C180/C181/C185 por item (IND_ESCRI=1)`
- **Chave de junção:** CHV_NFE | COD_ITEM+PERIODO
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Filtros de exclusão aplicados antes:** EXC-001, EXC-002, EXC-003, EXC-006
- **Depende de:** PAR-001, PAR-003, PAR-005
- **Cadeias de valor:** C1, C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e de venda {chave} (R$ {vNF}) não está na EFD-Contribuições de {mes}. Indício de receita fora da apuração de PIS/COFINS."

### M2-P02 – Aquisições com direito a crédito não aproveitadas

- **Mapa/seção do núcleo:** M2.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `NF-e de entrada` (universo: dest = estabelecimento; COD_INC_TRIB in (1,3))
- **Destino:** EFD-Contribuições → `C100/C170 (CST 50-66) | C190/C191/C195`
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** INFO
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Aquisição sem direito a crédito | Opção por não creditar
- **Filtros de exclusão aplicados antes:** EXC-003, EXC-009
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e de aquisição {chave} não consta da EFD-Contribuições (possível crédito não aproveitado)."

### M2-P03 – Devolução de venda refletida na EFD-Contribuições

- **Mapa/seção do núcleo:** M2.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `finNFe=4` (universo: contribuinte = dest (devolução recebida))
- **Destino:** EFD-Contribuições → `C100/C170 com CST de crédito sobre devolução | C190/C191/C195 | ajuste de receita`
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Devolução de venda {chave} sem reflexo na EFD-Contribuições."

### M2-R01 – Crédito por NF-e com documento válido destinado à PJ

- **Mapa/seção do núcleo:** M2.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD-Contribuições → `C100.CHV_NFE + C170 (CST 50-66)` (universo: IND_OPER=0)
- **Destino:** NF-e/NFC-e → `infNFe/@Id` (universo: autorizada; sem 110111; dest/CNPJ in 0140)
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Lei 10.637/2002 – PIS não cumulativo (art. 3º; art. 8º); Lei 10.833/2003 – COFINS não cumulativa (art. 3º; art. 10)
- **Hipóteses legítimas:** XML não fornecido ao agente (cobertura)
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Crédito de PIS/COFINS sobre a NF-e {chave}, que {situacao}."

### M2-R02 – Consolidado de aquisições não excede as NF-e do período

- **Mapa/seção do núcleo:** M2.2
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NF-e/NFC-e → `itens vProd` (universo: entradas do fornecedor/item no período) · agregação: PARTICIPANTE
- **Destino:** EFD-Contribuições → `C191/C195.VL_ITEM (CNPJ_CPF_PART)`
- **Chave de junção:** CNPJ_CPF_PART, COD_ITEM, PERIODO
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Aquisições consolidadas de {fornecedor} (R$ {vlEfd}) superam as NF-e do período (R$ {vlXml})."

### M2-R03 – Consolidado de vendas = soma das NF-e de saída do item

- **Mapa/seção do núcleo:** M2.2
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NF-e/NFC-e → `itens vProd (líquido de descontos)` (universo: saídas próprias com receita no período) · agregação: ITEM
- **Destino:** EFD-Contribuições → `C181/C185.VL_ITEM (por COD_ITEM, CST, CFOP)`
- **Chave de junção:** COD_ITEM, CST, CFOP, DT_DOC_INI..DT_DOC_FIN
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Consolidado maior que as NF-e
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Filtros de exclusão aplicados antes:** EXC-001, EXC-002, EXC-003
- **Cadeias de valor:** C1, C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem}: vendas consolidadas R$ {vlEfd} x NF-e R$ {vlXml} em {mes}."

### M2-R04 – Base de crédito sem o ICMS incidente na aquisição

- **Mapa/seção do núcleo:** M2.2
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NF-e/NFC-e → `vProd - vICMS (+ demais componentes da base)`
- **Destino:** EFD-Contribuições → `C170.VL_BC_PIS, VL_BC_COFINS (CST 50-66)`
- **Chave de junção:** CHV_NFE, NUM_ITEM=nItem
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** NORMA (lei/IN/convênio)
- **Fontes:** Lei 14.592/2023 (conversão da MP 1.159/2023) – exclusão do ICMS da base de crédito (art. 3º, §2º, III das Leis 10.637/02 e 10.833/03); Lei 10.637/2002 – PIS não cumulativo (art. 3º; art. 8º); Lei 10.833/2003 – COFINS não cumulativa (art. 3º; art. 10)
- **Hipóteses legítimas:** Decisão judicial própria do contribuinte
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2023-05-01
- **Mensagem do parecer:** "NF-e {chave} item {nItem}: base de crédito R$ {base} inclui o ICMS da aquisição (R$ {vICMS})."

### M2-I01 – Cabeçalho igual entre EFD-Contribuições e XML

- **Mapa/seção do núcleo:** M2.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD-Contribuições → `C100.VL_DOC, DT_DOC, COD_SIT`
- **Destino:** NF-e/NFC-e → `vNF, dhEmi, situação`
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Somente data divergente
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave}: {campo} na EFD-Contribuições {efd} x XML {xml}."

### M2-I02 – CST PIS/COFINS da saída própria igual ao XML

- **Mapa/seção do núcleo:** M2.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD-Contribuições → `C170/C181/C185.CST_PIS, CST_COFINS`
- **Destino:** NF-e/NFC-e → `det/imposto/PIS/CST, COFINS/CST`
- **Chave de junção:** CHV_NFE, NUM_ITEM | COD_ITEM
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: A divergência não reduz a contribuição
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Hipóteses legítimas:** Correção posterior do enquadramento documentada
- **Cadeias de valor:** C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} item {item}: CST PIS/COFINS escriturado {cstEfd} x XML {cstXml}."

### M2-I03 – Base e valor de PIS/COFINS da saída própria coerentes com o XML

- **Mapa/seção do núcleo:** M2.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD-Contribuições → `C170.VL_BC_PIS, ALIQ_PIS, VL_PIS, VL_BC_COFINS, ALIQ_COFINS, VL_COFINS`
- **Destino:** NF-e/NFC-e → `PIS/vBC, pPIS, vPIS; COFINS/vBC, pCOFINS, vCOFINS`
- **Chave de junção:** CHV_NFE, NUM_ITEM
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; STF – RE 574.706 (Tema 69) – exclusão do ICMS da base do PIS/COFINS
- **Hipóteses legítimas:** Exclusão do ICMS da base de débito (Tema 69) aplicada na escrituração e não no XML
- **Cadeias de valor:** C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e {chave} item {item}: base/valor de PIS/COFINS escriturados divergem do XML ({detalhe})."

### M2-I04 – NCM consolidado coerente com o XML

- **Mapa/seção do núcleo:** M2.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD-Contribuições → `C180.COD_NCM, C190.COD_NCM`
- **Destino:** NF-e/NFC-e → `prod/NCM`
- **Chave de junção:** COD_ITEM, PERIODO
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem}: NCM consolidado {ncmEfd} x NCM nas NF-e {ncmXml}."

### M2-I05 – CST 04/06 (monofásico/alíquota zero) com NCM compatível

- **Mapa/seção do núcleo:** M2.3
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** EFD-Contribuições → `CST_PIS/CST_COFINS in (04,06)`
- **Destino:** Tabela externa → `Tabela legal de NCM monofásicos/alíquota zero (fora deste catálogo)`
- **Chave de junção:** COD_NCM
- **Operador:** COMPATIVEL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem} (NCM {ncm}) com CST {cst} – confirmar enquadramento legal."

### M4-P01 – NFS-e emitida presente na EFD-Contribuições

- **Mapa/seção do núcleo:** M4.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NFS-e → `chave NFS-e` (universo: prest/CNPJ in 0140; vigente)
- **Destino:** EFD-Contribuições → `A100.CHV_NFSE (IND_OPER=1, IND_EMIT=0) | F500/F525 (caixa) | F550 (competência consolidada)`
- **Chave de junção:** CHV_NFSE
- **Operador:** EXISTE
- **Condição:** Período de alocação: competência (dCompet); no regime de caixa, período do recebimento.
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209)
- **Filtros de exclusão aplicados antes:** EXC-006, EXC-010
- **Depende de:** PAR-001, PAR-005
- **Cadeias de valor:** C1, C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NFS-e {chave} (R$ {vServ}, competência {dCompet}) não está na EFD-Contribuições."

### M4-P02 – NFS-e tomada com possível crédito não aproveitado

- **Mapa/seção do núcleo:** M4.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NFS-e → `chave NFS-e` (universo: toma/CNPJ in 0140; COD_INC_TRIB in (1,3))
- **Destino:** EFD-Contribuições → `A100.CHV_NFSE (IND_OPER=0)`
- **Chave de junção:** CHV_NFSE
- **Operador:** EXISTE
- **Severidade:** INFO
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Serviço não caracterizado como insumo
- **Filtros de exclusão aplicados antes:** EXC-010
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NFS-e tomada {chave} não consta da EFD-Contribuições (possível crédito não aproveitado)."

### M4-P03 – Retenção de PIS/COFINS da NFS-e refletida no A100 e no F600

- **Mapa/seção do núcleo:** M4.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NFS-e → `tribFed/piscofins com retenção` (universo: prest/CNPJ in 0140)
- **Destino:** EFD-Contribuições → `A100.VL_PIS_RET, VL_COFINS_RET; F600 (CNPJ = tomador)`
- **Chave de junção:** CHV_NFSE, CNPJ_TOMADOR
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209)
- **Hipóteses legítimas:** Retenção destacada mas não efetuada pelo tomador
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "NFS-e {chave} com PIS/COFINS retidos sem reflexo em A100/F600."

### M4-R01 – A100 com chave tem NFS-e vigente

- **Mapa/seção do núcleo:** M4.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD-Contribuições → `A100.CHV_NFSE`
- **Destino:** NFS-e → `chave NFS-e` (universo: vigente (não cancelada/substituída); CNPJ no papel correspondente)
- **Chave de junção:** CHV_NFSE
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; NFS-e Padrão Nacional – Anexo II – Eventos v1.01 (20260122)
- **Hipóteses legítimas:** NFS-e não fornecida ao agente (cobertura)
- **Filtros de exclusão aplicados antes:** EXC-012
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "A100 {chave} sem NFS-e vigente correspondente ({situacao})."

### M4-R02 – Crédito sobre serviço tomado com NFS-e válida

- **Mapa/seção do núcleo:** M4.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD-Contribuições → `A100 (IND_OPER=0) + A170 CST 50-56`
- **Destino:** NFS-e → `chave NFS-e` (universo: toma/CNPJ = estabelecimento; vigente)
- **Chave de junção:** CHV_NFSE
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Filtros de exclusão aplicados antes:** EXC-012
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Crédito sobre serviço {chave} sem NFS-e tomada válida."

### M4-R03 – Retenções deduzidas (F600) limitadas às retenções das NFS-e por fonte pagadora

- **Mapa/seção do núcleo:** M4.2
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NFS-e → `PIS/COFINS retidos` (universo: prest = contribuinte) · agregação: PARTICIPANTE
- **Destino:** EFD-Contribuições → `F600.VL_RET_PIS, VL_RET_COFINS (CNPJ fonte pagadora)`
- **Chave de junção:** CNPJ_TOMADOR=F600.CNPJ, MES
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209)
- **Hipóteses legítimas:** Retenções sobre outros pagamentos sem NFS-e nacional (ex.: documentos municipais pré-adesão)
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Retenções informadas no F600 para {fonte} (R$ {vlF600}) superam as retidas nas NFS-e (R$ {vlNfse})."

### M4-I01 – De-para A100/A170 x NFS-e

- **Mapa/seção do núcleo:** M4.3
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD-Contribuições → `A100.NUM_DOC, SER, DT_DOC, DT_EXE_SERV, COD_PART, VL_DOC, VL_DESC, VL_ISS, VL_PIS, VL_COFINS, VL_PIS_RET, VL_COFINS_RET; A170.CST_PIS, CST_COFINS`
- **Destino:** NFS-e → `nNFSe, serie, dhEmi, dCompet, toma|prest, vServ, descontos, vISSQN, tribFed/piscofins (CST, valores, retenções)`
- **Chave de junção:** CHV_NFSE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Divergência apenas em datas, série ou descrição
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** M4-R01
- **Cadeias de valor:** C4, C8
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "NFS-e {chave}: {campo} escriturado {efd} x documento {nfse}."

### M5-01 – Estabelecimento do DF: ISS/NFS-e no Bloco B da EFD ICMS/IPI

- **Mapa/seção do núcleo:** M5
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NFS-e → `chave NFS-e` (universo: prest = estabelecimento com 0000.UF = DF)
- **Destino:** EFD ICMS/IPI → `Bloco B`
- **Chave de junção:** CNPJ, MES
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Estabelecimento do DF {cnpj} com NFS-e em {mes} sem escrituração no Bloco B."

### M5-02 – Bloco B com movimento fora do DF

- **Mapa/seção do núcleo:** M5
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** EFD ICMS/IPI → `Bloco B com movimento`
- **Destino:** EFD ICMS/IPI → `0000.UF`
- **Chave de junção:** CNPJ, MES
- **Operador:** COMPATIVEL
- **Severidade:** ALERTA
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estabelecimento {cnpj} ({uf}) com Bloco B em movimento."

### M5-03 – NFS-e recebida por meio eletrônico compõe 1601.TOT_ISS

- **Mapa/seção do núcleo:** M5
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NFS-e → `vServ` (universo: prest = estabelecimento; pagamento eletrônico (quando conhecido)) · agregação: MES
- **Destino:** EFD ICMS/IPI → `1601.TOT_ISS`
- **Chave de junção:** CNPJ, MES
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** NFS-e não informa meio de pagamento – comparação depende da DIMP
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Serviços recebidos eletronicamente em {mes} (R$ {vlNfse}) x 1601.TOT_ISS (R$ {totIss})."

## Grupo ESCRITURACOES – 33 regras

Mapas M3 (EFD ICMS/IPI x EFD-Contribuições), M7 (EFD ICMS/IPI x ECD), M8 (EFD-Contribuições x ECD), M10 (EFD-Contribuições x ECF) e M11 (EFD ICMS/IPI x ECF).

### M3-01 – Mesmo documento com atributos idênticos nas duas EFD

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.DT_DOC, VL_DOC, COD_SIT, IND_OPER, IND_EMIT`
- **Destino:** EFD-Contribuições → `C100.DT_DOC, VL_DOC, COD_SIT, IND_OPER, IND_EMIT` (universo: C010.IND_ESCRI=2)
- **Chave de junção:** CHV_NFE
- **Operador:** IGUAL · tolerância: PARAM:TOL_VALOR_DOCUMENTO
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Somente DT_DOC divergente
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** ENT-001
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave}: {campo} diverge entre EFD ICMS/IPI ({icms}) e EFD-Contribuições ({contrib})."

### M3-02 – Situação do documento (cancelado/regular) coerente entre as EFD

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `C100.COD_SIT`
- **Destino:** EFD-Contribuições → `C100.COD_SIT`
- **Chave de junção:** CHV_NFE
- **Operador:** COMPATIVEL
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documento {chave} cancelado em uma EFD e regular na outra."

### M3-03 – Saída com CFOP de receita na EFD ICMS/IPI presente na EFD-Contribuições

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `C100/C190` (universo: IND_OPER=1; CFOP de receita (PARAM:TABELA_CFOP_RECEITA); COD_SIT regular)
- **Destino:** EFD-Contribuições → `C100.CHV_NFE | C180/C181 do item no período`
- **Chave de junção:** CHV_NFE | COD_ITEM+PERIODO
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Filtros de exclusão aplicados antes:** EXC-001, EXC-002, EXC-006
- **Depende de:** ENT-001, PAR-005
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Saída {chave} (CFOP {cfop}, R$ {valor}) escriturada na EFD ICMS/IPI e ausente na EFD-Contribuições."

### M3-04 – Entrada com crédito de PIS/COFINS escriturada também na EFD ICMS/IPI

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** EFD-Contribuições → `C100.CHV_NFE com C170 CST 50-66` (universo: IND_OPER=0; COD_MOD 55)
- **Destino:** EFD ICMS/IPI → `C100.CHV_NFE (IND_OPER=0) de algum estabelecimento do 0140`
- **Chave de junção:** CHV_NFE
- **Operador:** EXISTE · tolerância: PARAM:JANELA_MESES_ENTRADA
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Estabelecimento sem inscrição estadual / dispensado de EFD ICMS/IPI
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Crédito de PIS/COFINS sobre {chave} sem a entrada correspondente na EFD ICMS/IPI."

### M3-06 – Agregado mensal de receita de mercadorias: EFD ICMS/IPI x EFD-Contribuições

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ C190.VL_OPR (saídas, CFOP de receita) − ICMS-ST − IPI` (universo: todos os estabelecimentos do 0140) · agregação: MES
- **Destino:** EFD-Contribuições → `Σ receitas de mercadorias (C170/C181, parcela em M210.VL_REC_BRT e M400)` · agregação: MES
- **Chave de junção:** CNPJ_BASE, MES
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Condição:** Origem maior que destino além da tolerância = receita fora da apuração de PIS/COFINS.
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Destino maior que origem, ou diferença abaixo de PARAM:MATERIALIDADE_RECEITA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35; Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Hipóteses legítimas:** Regime de caixa (presumido) | Devoluções tratadas como ajuste de receita em período distinto | Vendas de ativo imobilizado
- **Filtros de exclusão aplicados antes:** EXC-001, EXC-002, EXC-003
- **Depende de:** M3-03
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Receita de mercadorias em {mes}: EFD ICMS/IPI R$ {icms} x EFD-Contribuições R$ {contrib} (diferença R$ {dif})."

### M3-05 – Cadastro do item igual entre as EFD

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `0200`
- **Destino:** EFD-Contribuições → `0200`
- **Chave de junção:** COD_ITEM, MES
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Ver ENT-023: item {codItem} com cadastro divergente."

### M3-07 – Bem do CIAP com crédito de PIS/COFINS correspondente (F120/F130)

- **Mapa/seção do núcleo:** M3.1
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `0300 + G125 (entrada do bem)`
- **Destino:** EFD-Contribuições → `F120 | F130` (universo: COD_INC_TRIB in (1,3))
- **Chave de junção:** COD_CTA, IDENT_BEM
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Bem sem direito a crédito de PIS/COFINS | Opção por não creditar
- **Cadeias de valor:** C5, C7
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Bem {bem} com crédito de ICMS no CIAP sem crédito correspondente de PIS/COFINS (verificar direito)."

### M7-01 – Contas usadas na EFD ICMS/IPI existem na ECD como analíticas

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `0500.COD_CTA; COD_CTA em C170, H010, 0300` · agregação: ANO
- **Destino:** ECD → `I050.COD_CTA (IND_CTA=A)`
- **Chave de junção:** COD_CTA, ANO
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mudança de plano de contas (I157)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta} usada na EFD ICMS/IPI ({registro}) inexistente/sintética na ECD."

### M7-02 – Inventário de 31/12 = saldo contábil de estoques

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `Σ H010.VL_ITEM (ou VL_ITEM_IR) da EFD de fevereiro/ano+1, MOT_INV=01` (universo: IND_PROP in (0,1); todos os estabelecimentos) · agregação: ANO
- **Destino:** ECD → `Σ I155.VL_SLD_FIN em 31/12 das contas de estoque (PARAM:TABELA_CONTAS_POR_FUNCAO / H010.COD_CTA)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mercadorias em trânsito/importação em andamento contabilizadas em contas de estoque | Provisão para perdas em conta redutora | Critério de avaliação divergente (deve ser o mesmo)
- **Filtros de exclusão aplicados antes:** EXC-008
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Inventário de 31/12/{ano} na EFD (R$ {inv}) x estoques na ECD (R$ {ecd}): diferença R$ {dif}."

### M7-03 – Valor do inventário para fins de IR (VL_ITEM_IR) = saldo contábil de estoque

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `Σ H010.VL_ITEM_IR (quando informado)` (universo: IND_PROP in (0,1); todos os estabelecimentos) · agregação: ANO
- **Destino:** ECD → `Σ I155.VL_SLD_FIN (31/12) das contas de estoque`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Condição:** Quando VL_ITEM_IR estiver informado, ele tem preferência sobre VL_ITEM na comparação com a contabilidade (M7-02).
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mesmas da M7-02
- **Filtros de exclusão aplicados antes:** EXC-008
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Inventário (valor para IR) R$ {inv} x estoques na ECD R$ {ecd} em 31/12/{ano}."

### M7-04 – Inventário por conta contábil = saldo da conta

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ H010.VL_ITEM por COD_CTA` (universo: IND_PROP in (0,1)) · agregação: CONTA
- **Destino:** ECD → `I155.VL_SLD_FIN (31/12) do mesmo COD_CTA`
- **Chave de junção:** COD_CTA, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mesmas da M7-02
- **Filtros de exclusão aplicados antes:** EXC-008
- **Depende de:** M7-01
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta}: inventário R$ {inv} x saldo contábil R$ {sld}."

### M7-05 – ICMS a recolher no ano x passivo de ICMS

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ E110.VL_ICMS_RECOLHER + DEB_ESP + E210 + E310` · agregação: ANO
- **Destino:** ECD → `Movimento credor (I155.VL_CRED) das contas ICMS_RECOLHER (PARAM:TABELA_CONTAS_POR_FUNCAO)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Parcelamentos e autuações lançados na mesma conta | Transferências de saldo credor entre estabelecimentos
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "ICMS a recolher apurado no ano R$ {efd} x movimento do passivo de ICMS R$ {ecd}."

### M7-06 – Saldo credor de ICMS em dezembro x ICMS a recuperar

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `Σ E110.VL_SLD_CREDOR_TRANSPORTAR (dezembro, todos os estabelecimentos)`
- **Destino:** ECD → `I155.VL_SLD_FIN (31/12) das contas ICMS_RECUPERAR`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Crédito de CIAP em conta própria | Créditos acumulados em controle extra-apuração
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Saldo credor de ICMS em 31/12 R$ {efd} x ICMS a recuperar contábil R$ {ecd}."

### M7-07 – IPI a recolher no ano x passivo de IPI

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ E520 (saldo devedor do período)` · agregação: ANO
- **Destino:** ECD → `Movimento credor das contas IPI_RECOLHER`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Aplicável se:** EFD_ICMS_IPI 0000.IND_ATIV = 0
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Cadeias de valor:** C3
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "IPI a recolher no ano R$ {efd} x passivo de IPI R$ {ecd}."

### M7-08 – Receita de vendas (EFD ICMS/IPI) x receita bruta contábil

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ C190.VL_OPR saídas com CFOP de receita − ICMS-ST − IPI` · agregação: ANO
- **Destino:** ECD → `I355/I155 das contas RECEITA_BRUTA (vendas de mercadorias)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: ECD maior que EFD, ou diferença abaixo de PARAM:MATERIALIDADE_RECEITA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026); Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Hipóteses legítimas:** Competência contábil x data de saída | Venda para entrega futura | Devoluções contabilizadas como redutora
- **Filtros de exclusão aplicados antes:** EXC-001, EXC-002, EXC-003
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Receita de vendas no ano: EFD ICMS/IPI R$ {efd} x ECD R$ {ecd}."

### M7-09 – Bens do CIAP x imobilizado contábil

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `0300.COD_CTA; G125`
- **Destino:** ECD → `I050/I155 das contas IMOBILIZADO`
- **Chave de junção:** COD_CTA, ANO
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Cadeias de valor:** C7
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Bem {bem} do CIAP vinculado à conta {codCta} sem correspondente no imobilizado contábil."

### M7-10 – Estoque do Bloco K em 31/12 = inventário do Bloco H (quantidades)

- **Mapa/seção do núcleo:** M7
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `K200.QTD (DT_EST = 31/12), IND_EST`
- **Destino:** EFD ICMS/IPI → `H010.QTD (DT_INV = 31/12), IND_PROP`
- **Chave de junção:** CNPJ, COD_ITEM, POSSE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Estabelecimento não obrigado ao Bloco K ou obrigado em nível simplificado
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Item {codItem}: K200 (31/12) {qtdK} x H010 {qtdH}."

### M8-01 – Plano de contas da EFD-Contribuições existe na ECD

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD-Contribuições → `0500.COD_CTA, COD_NAT_CC, IND_CTA` · agregação: ANO
- **Destino:** ECD → `I050`
- **Chave de junção:** COD_CTA, ANO
- **Operador:** EXISTE
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mudança de plano de contas (I157)
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Ver ENT-030/031 – conta {codCta}."

### M8-02 – Receitas por conta na EFD-Contribuições x contas de receita da ECD

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `Σ receitas por COD_CTA (C181, A170, F100, F500, F550, M410, M810)` · agregação: CONTA
- **Destino:** ECD → `Movimento credor líquido das contas de receita (I155/I355)` · agregação: CONTA
- **Chave de junção:** COD_CTA, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Condição:** ECD maior que EFD-Contribuições = receita contabilizada fora da apuração de PIS/COFINS.
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: EFD-Contribuições maior que ECD, ou diferença abaixo de PARAM:MATERIALIDADE_RECEITA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Regime de caixa (presumido) | Receitas não operacionais/ganho de capital fora do campo das contribuições | Reversões de provisões
- **Depende de:** ENT-030, PAR-005
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta de receita {codCta}: EFD-Contribuições R$ {efd} x ECD R$ {ecd} no ano."

### M8-03 – Receitas não tributadas (M400/M800) com contas existentes e movimentadas

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD-Contribuições → `M400/M410.COD_CTA, M800/M810.COD_CTA`
- **Destino:** ECD → `I050 + I155 com movimento`
- **Chave de junção:** COD_CTA, ANO
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Cadeias de valor:** C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Receita não tributada informada na conta {codCta} sem movimento na ECD."

### M8-04 – Crédito sobre depreciação limitado à depreciação contabilizada

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** ECD → `Depreciação do período das contas dos bens` · agregação: CONTA
- **Destino:** EFD-Contribuições → `F120 (valor dos encargos de depreciação)` · agregação: CONTA
- **Chave de junção:** COD_CTA, MES
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C5, C7
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Encargos de depreciação no F120 (R$ {f120}) superam a depreciação contabilizada (R$ {ecd}) em {mes}."

### M8-05 – PIS/COFINS a recolher x passivo contábil

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `M200.VL_TOT_CONT_REC + M600 equivalente` · agregação: MES
- **Destino:** ECD → `Movimento credor das contas PIS_COFINS_RECOLHER (I155 mensal)`
- **Chave de junção:** CNPJ_BASE, MES
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Cadeias de valor:** C4
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "PIS/COFINS a recolher em {mes}: EFD R$ {efd} x ECD R$ {ecd}."

### M8-06 – Retenções sofridas (F600) x tributos retidos a compensar

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `Σ F600.VL_RET_PIS, VL_RET_COFINS` · agregação: ANO
- **Destino:** ECD → `Movimento devedor das contas RETENCOES_COMPENSAR`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Conta única para CSRF (PIS/COFINS/CSLL) – comparar após segregação
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Retenções de PIS/COFINS no F600 R$ {efd} x contabilizadas R$ {ecd}."

### M8-07 – Créditos de PIS/COFINS x ativo a recuperar

- **Mapa/seção do núcleo:** M8
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `M100/M500 (créditos apurados) e saldos 1100/1500` · agregação: ANO
- **Destino:** ECD → `Contas PIS_COFINS_RECUPERAR (movimento e saldo em 31/12)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Aplicável se:** COD_INC_TRIB in (1,3)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Créditos registrados diretamente como redução de custo
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Créditos de PIS/COFINS na EFD R$ {efd} x ativo a recuperar R$ {ecd}."

### M10-02 – Presumido: receita da EFD-Contribuições no trimestre limitada à base do IRPJ/CSLL

- **Mapa/seção do núcleo:** M10
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** EFD-Contribuições → `Σ receitas (F500/F550, A100, C180/C181, M400)` · agregação: TRIMESTRE
- **Destino:** ECF → `P200 (receita bruta por atividade) / P400` · agregação: TRIMESTRE
- **Chave de junção:** CNPJ_BASE, TRIMESTRE
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Condição:** Receita (sujeita ou não a PIS/COFINS) na EFD-Contribuições <= receita considerada na base presumida.
- **Aplicável se:** ECF trimestre presumido
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Receitas na EFD-Contribuições tratadas como ganho de capital/receita financeira em linhas próprias da ECF (somar antes de comparar)
- **Depende de:** PAR-003, PAR-005
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Trimestre {t}: receitas na EFD-Contribuições R$ {efd} superam a receita base do presumido R$ {ecf}."

### M10-03 – Real: receita bruta da DRE x receita bruta da EFD-Contribuições

- **Mapa/seção do núcleo:** M10
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `Σ M210.VL_REC_BRT + receitas M400` · agregação: ANO
- **Destino:** ECF → `L300 – receita bruta (contas referenciais)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Aplicável se:** ECF lucro real
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Decreto 8.426/2015 – PIS/COFINS sobre receitas financeiras (não cumulativo)
- **Hipóteses legítimas:** Receitas financeiras e outras receitas compõem a EFD-Contribuições mas não a receita bruta | Vendas canceladas e devoluções
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Receita bruta {ano}: EFD-Contribuições R$ {efd} x ECF (L300) R$ {ecf}."

### M10-04 – Fontes pagadoras com retenção de PIS/COFINS (F600) presentes no Y570

- **Mapa/seção do núcleo:** M10
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD-Contribuições → `F600.CNPJ (fonte pagadora)` · agregação: ANO
- **Destino:** ECF → `Y570.CNPJ_FON`
- **Chave de junção:** CNPJ_FONTE, ANO
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Fonte que retém apenas PIS/COFINS (sem IRRF/CSLL) por hipótese legal específica
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Fonte pagadora {cnpj} reteve PIS/COFINS (F600) e não consta do Y570."

### M10-05 – Receita por estabelecimento (Y540) x EFD-Contribuições

- **Mapa/seção do núcleo:** M10
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `Receitas por estabelecimento (0140 + blocos A/C/F)` · agregação: ESTABELECIMENTO
- **Destino:** ECF → `Y540.CNPJ_ESTAB, VL_REC_ESTAB`
- **Chave de junção:** CNPJ, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Estabelecimento {cnpj}: receita EFD-Contribuições R$ {efd} x Y540 R$ {ecf}."

### M10-06 – PIS/COFINS sobre receitas x deduções da receita na DRE

- **Mapa/seção do núcleo:** M10
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD-Contribuições → `Σ M210/M610 contribuição sobre receita bruta` · agregação: ANO
- **Destino:** ECF → `L300/P150 – deduções (PIS/COFINS)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** PIS/COFINS sobre receitas financeiras contabilizados como despesa financeira
- **Cadeias de valor:** C4
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "PIS/COFINS sobre receitas R$ {efd} x deduções na DRE da ECF R$ {ecf}."

### M11-01 – Inventário de 31/12 x estoque na ECF

- **Mapa/seção do núcleo:** M11
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** EFD ICMS/IPI → `Σ H010 (IND_PROP 0/1), todos os estabelecimentos`
- **Destino:** ECF → `L100/P100 (estoques); presumido sem ECD: Y672.VL_EST`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Mesmas da M7-02
- **Filtros de exclusão aplicados antes:** EXC-008
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Inventário EFD R$ {efd} x estoque na ECF R$ {ecf} em 31/12/{ano}."

### M11-02 – Compras de mercadorias x Y672 (presumido)

- **Mapa/seção do núcleo:** M11
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ entradas com CFOP de compra` · agregação: ANO
- **Destino:** ECF → `Y672.VL_COMPRA_MERC`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Aplicável se:** ECF presumido
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Compras de mercadorias: EFD R$ {efd} x Y672 R$ {ecf}."

### M11-03 – Aquisições de ativo x Y672 (presumido)

- **Mapa/seção do núcleo:** M11
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** EFD ICMS/IPI → `Σ entradas CFOP 1.551/2.551/3.551` · agregação: ANO
- **Destino:** ECF → `Y672.VL_COMPRA_ATIVO`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Aplicável se:** ECF presumido
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Cadeias de valor:** C7
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Aquisições de ativo: EFD R$ {efd} x Y672 R$ {ecf}."

### M11-04 – Atividade industrial coerente com custos de produção na ECF

- **Mapa/seção do núcleo:** M11
- **Tipo de relação:** PAR – Compatibilidade de parâmetros de regime
- **Origem:** EFD ICMS/IPI → `0000.IND_ATIV = 0`
- **Destino:** ECF → `L210 (custos) / atividade econômica`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** COMPATIVEL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** L210 não exigido para a forma de tributação/porte
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estabelecimento industrial sem informação de custos de produção coerente na ECF."

## Grupo CONTABIL_ECF – 21 regras

Mapas M9 (ECD x ECF) e M12 (documentos x ECD/ECF).

### M9-01 – ECF recupera a última ECD válida do período

- **Mapa/seção do núcleo:** M9.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `hash da última ECD válida (original ou substituta – IND_FIN_ESC/COD_HASH_SUB)`
- **Destino:** ECF → `C040.HASH_ECD`
- **Chave de junção:** CNPJ_BASE, PERIODO
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Depende de:** PAR-006, PAR-007
- **Cadeias de valor:** C1, C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "A ECF recuperou a ECD de hash {hashEcf}, mas a ECD vigente do período é {hashEcd}. A ECF deve ser retificada."

### M9-02 – Período e situação especial iguais (ECD x ECF)

- **Mapa/seção do núcleo:** M9.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `0000.DT_INI, DT_FIN, IND_SIT_ESP`
- **Destino:** ECF → `C040.DT_INI, DT_FIN, IND_SIT_ESP; 0000`
- **Chave de junção:** CNPJ_BASE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Depende de:** PAR-010
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Período/situação especial da ECD ({ecd}) diferente do recuperado na ECF ({ecf})."

### M9-03 – Plano de contas e mapeamento referencial iguais

- **Mapa/seção do núcleo:** M9.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `I050, I051`
- **Destino:** ECF → `C050, C051`
- **Chave de junção:** COD_CTA
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Depende de:** M9-01
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta}: plano/mapeamento referencial diverge entre ECD e ECF."

### M9-04 – Saldos patrimoniais da ECD = K155 da ECF

- **Mapa/seção do núcleo:** M9.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `I155 (VL_SLD_INI, VL_DEB, VL_CRED, VL_SLD_FIN)`
- **Destino:** ECF → `C155 -> K155`
- **Chave de junção:** COD_CTA, COD_CCUS, PERIODO
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** PVA (validador oficial)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Depende de:** M9-01
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta} período {periodo}: saldo ECD {ecd} x K155 {ecf}."

### M9-05 – Saldos de resultado antes do encerramento = K355 da ECF

- **Mapa/seção do núcleo:** M9.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `I355 (VL_CTA, IND_DC)`
- **Destino:** ECF → `C355 -> K355`
- **Chave de junção:** COD_CTA, COD_CCUS, DT_RES
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** PVA (validador oficial)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Depende de:** M9-01
- **Cadeias de valor:** C1, C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta de resultado {codCta}: ECD {ecd} x K355 {ecf}."

### M9-06 – Saldos por conta referencial (K156/K356) = agregação do mapeamento

- **Mapa/seção do núcleo:** M9.1
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** ECD → `I155/I355 agregados via I051` · agregação: CONTA
- **Destino:** ECF → `K156 / K356 (COD_CTA_REF)`
- **Chave de junção:** COD_CTA_REF, PERIODO
- **Operador:** SOMA_IGUAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Depende de:** M9-03
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta referencial {codRef}: soma do mapeamento {ecd} x ECF {ecf}."

### M9-10 – Balanço da ECD x balanço referencial da ECF

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** ECD → `J100 / saldos I155 mapeados`
- **Destino:** ECF → `L100 (real) | P100 (presumido)`
- **Chave de junção:** COD_CTA_REF, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** M9-06
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Balanço: grupo {grupo} na ECD {ecd} x ECF {ecf}."

### M9-11 – DRE da ECD x DRE referencial da ECF

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `J150 (resultado antes do IRPJ/CSLL)`
- **Destino:** ECF → `L300 (real) | P150 (presumido)`
- **Chave de junção:** CNPJ_BASE, PERIODO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** M9-05
- **Cadeias de valor:** C1, C2, C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Resultado antes do IRPJ/CSLL: ECD {ecd} x ECF {ecf}."

### M9-12 – Lucro líquido contábil = ponto de partida do e-Lalur/e-Lacs

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `Lucro líquido antes do IRPJ/CSLL (J150)`
- **Destino:** ECF → `M300 / M350 (lucro líquido antes do IRPJ/CSLL)`
- **Chave de junção:** CNPJ_BASE, PERIODO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Aplicável se:** ECF lucro real
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** M9-11
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Lucro líquido da ECD {ecd} x ponto de partida do e-Lalur/e-Lacs {ecf} ({periodo})."

### M9-13 – Ajuste do e-Lalur vinculado a lançamento existente na ECD

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** PRE_REVERSA – Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)
- **Origem:** ECF → `M310.COD_CTA + M312.NUM_LCTO`
- **Destino:** ECD → `I200.NUM_LCTO / I250.COD_CTA`
- **Chave de junção:** NUM_LCTO, COD_CTA
- **Operador:** EXISTE
- **Aplicável se:** ECF lucro real
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Depende de:** ENT-033
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Ajuste {codigoAjuste} referencia o lançamento {numLcto}/conta {codCta}, inexistente na ECD."

### M9-14 – Valor do ajuste vinculado limitado ao movimento da conta

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** ECD → `Movimento/saldo da conta vinculada (I155/I355)`
- **Destino:** ECF → `M310.VALOR`
- **Chave de junção:** COD_CTA, PERIODO
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Aplicável se:** ECF lucro real
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Ajuste que considera parcela de movimentos de períodos anteriores
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Ajuste vinculado à conta {codCta} (R$ {ajuste}) supera o movimento contábil (R$ {mov})."

### M9-15 – IRPJ/CSLL apurados x contas contábeis de IRPJ/CSLL

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECF → `N630/N670 (real) | P300/P500 (presumido)`
- **Destino:** ECD → `Contas IRPJ_CSLL (despesa e passivo) – PARAM:TABELA_CONTAS_POR_FUNCAO`
- **Chave de junção:** CNPJ_BASE, PERIODO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Tributos diferidos (IR/CS diferidos) contabilizados | Ajustes de exercícios anteriores
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "IRPJ/CSLL devidos na ECF R$ {ecf} x contabilizados R$ {ecd}."

### M9-16 – Estoque contábil = estoque na ECF

- **Mapa/seção do núcleo:** M9.2
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** ECD → `I155 (31/12) contas de estoque`
- **Destino:** ECF → `L100/P100 estoques`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Depende de:** M9-06
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Estoque em 31/12: ECD R$ {ecd} x ECF R$ {ecf}."

### M9-20 – Saldo inicial do ano (ECD) = saldo final da ECD anterior

- **Mapa/seção do núcleo:** M9.3
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** ECD → `I155.VL_SLD_FIN (último período do ano anterior), contas patrimoniais`
- **Destino:** ECD → `I155.VL_SLD_INI (primeiro período do ano)`
- **Chave de junção:** COD_CTA, COD_CCUS
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Aplicável se:** ECD do ano anterior disponível
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mudança de plano de contas com I157 (transferência de saldos)
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Conta {codCta}: saldo final {anoAnt} {fim} x saldo inicial {ano} {ini}."

### M9-21 – Saldos recuperados da ECF anterior (E010/E015) = saldos iniciais

- **Mapa/seção do núcleo:** M9.3
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** ECF → `ECF anterior – saldos finais`
- **Destino:** ECF → `E010/E015 (ECF atual)`
- **Chave de junção:** COD_CTA
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Aplicável se:** ECF do ano anterior disponível
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Saldo recuperado da ECF anterior para {codCta} diverge do saldo final daquela ECF."

### M9-22 – Parte B: saldo inicial = saldo final da ECF anterior

- **Mapa/seção do núcleo:** M9.3
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** ECF → `ECF anterior M500.SD_FIM_LAL (via E020)`
- **Destino:** ECF → `M010/M500.SD_INI_LAL (ECF atual)`
- **Chave de junção:** COD_CTA_B, COD_TRIBUTO
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Aplicável se:** ECF lucro real
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** MANUAL (texto do Guia/Manual/NT)
- **Fontes:** Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Conta da Parte B originada no ano (sem saldo anterior)
- **Cadeias de valor:** C10
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Conta da Parte B {conta} ({tributo}): saldo inicial {ini} x saldo final da ECF anterior {fim}. Prejuízo/base negativa/adições sem origem."

### M12-01 – Documentos de receita do ano x receita bruta contábil

- **Mapa/seção do núcleo:** M12
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NF-e/NFC-e → `Σ NF-e de saída com CFOP de receita + Σ NFS-e emitidas (vigentes)` · agregação: ANO
- **Destino:** ECD → `Contas RECEITA_BRUTA (I355) / ECF L300 | P150`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: ECD maior que documentos, ou diferença abaixo de PARAM:MATERIALIDADE_RECEITA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209); Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026); Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Hipóteses legítimas:** Competência contábil x emissão | Entrega futura | Vendas canceladas em outro período | Receitas sem documento eletrônico (exportação com outro modelo, documentos municipais pré-adesão)
- **Filtros de exclusão aplicados antes:** EXC-001, EXC-002, EXC-003, EXC-010
- **Cadeias de valor:** C1
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Documentos de receita {ano} R$ {docs} x receita bruta contábil R$ {ecd}."

### M12-02 – Aquisições de ativo (NF-e x.551) x adições ao imobilizado

- **Mapa/seção do núcleo:** M12
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NF-e/NFC-e → `Σ vNF de NF-e de entrada com CFOP 1.551/2.551/3.551` · agregação: ANO
- **Destino:** ECD → `I155.VL_DEB das contas IMOBILIZADO`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Bens registrados em intangível/imobilizado em andamento | Créditos de impostos recuperáveis excluídos do custo
- **Filtros de exclusão aplicados antes:** EXC-003, EXC-004
- **Cadeias de valor:** C7
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "NF-e de aquisição de ativo R$ {docs} x adições ao imobilizado R$ {ecd}."

### M12-03 – IRRF/CSLL retidos nas NFS-e x Y570 por fonte pagadora

- **Mapa/seção do núcleo:** M12
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NFS-e → `Σ vRetIRRF, vRetCSLL por tomador` (universo: prest = contribuinte; vigentes) · agregação: PARTICIPANTE
- **Destino:** ECF → `Y570.CNPJ_FON, IR_RET, CSLL_RET`
- **Chave de junção:** CNPJ_TOMADOR=CNPJ_FON, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209); Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 vLeiaute 12
- **Hipóteses legítimas:** Retenção destacada mas não efetuada | Retenções sobre outros rendimentos (aplicações financeiras) no mesmo Y570
- **Filtros de exclusão aplicados antes:** EXC-010
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Tomador {cnpj}: IRRF/CSLL retidos nas NFS-e R$ {nfse} x Y570 R$ {ecf}."

### M12-04 – Retenção previdenciária nas NFS-e x conta de INSS a compensar

- **Mapa/seção do núcleo:** M12
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NFS-e → `Σ vRetCP` · agregação: ANO
- **Destino:** ECD → `Contas RETENCOES_COMPENSAR (INSS)`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e v1.01 (20260209); Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Compensação na própria folha no mês (conta zerada)
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Retenção previdenciária nas NFS-e R$ {nfse} x contabilizada R$ {ecd}."

### M12-05 – Entradas confirmadas (210200) refletidas na contabilidade

- **Mapa/seção do núcleo:** M12
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** NF-e/NFC-e → `Σ vNF de NF-e com evento 210200` · agregação: ANO
- **Destino:** ECD → `Movimento credor de fornecedores / devedor de estoque, custo e despesa`
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** NT 2020.001 – Manifestação do Destinatário v1.60; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Compras pagas à vista sem passar por fornecedores
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Entradas confirmadas no ano R$ {docs} sem reflexo proporcional na contabilidade (R$ {ecd})."

## Grupo DIMP – 8 regras

Mapa M6 – DIMP como evidência externa x NF-e/NFC-e/NFS-e, EFD ICMS/IPI (1601), EFD-Contribuições (F525) e ECD. Registros internos da DIMP devem ser mapeados pelo PDF da Versão 10 (Ato COTEPE/ICMS 65/18) – todas as regras marcadas conferirCampo.

### M6-01 – Total da DIMP por instituição de pagamento = registro 1601

- **Mapa/seção do núcleo:** M6.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** DIMP → `Total transacionado (declarante = instituição X)` · agregação: INSTITUICAO_PAGAMENTO
- **Destino:** EFD ICMS/IPI → `1601 (COD_PART_IP -> CNPJ X): TOT_VS + TOT_ISS + TOT_OUTROS`
- **Chave de junção:** CNPJ_INSTITUICAO, CNPJ_ESTABELECIMENTO, MES
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Condição:** DIMP maior que 1601 = recebimentos não declarados pelo contribuinte.
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: 1601 maior que DIMP (possível diferença de data de transação x liquidação)
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Convênio ICMS 134/2016 – Informações de meios de pagamento; Ato COTEPE/ICMS 65/2018 – DIMP (Versão 10 a partir de 01/06/2026, redação do Ato COTEPE/ICMS 124/25) vDIMP v10; Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Estornos/chargebacks | Diferença entre data da transação e data do documento | Transações de outro estabelecimento/UF
- **Depende de:** ENT-012
- **Cadeias de valor:** C1, C9
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "{mes}: DIMP declarada por {instituicao} R$ {dimp} x registro 1601 de {cnpj} R$ {efd}."

### M6-02 – Total da DIMP por intermediador = registro 1601 (COD_PART_IT)

- **Mapa/seção do núcleo:** M6.1
- **Tipo de relação:** IDT – Identidade (mesmo fato, mesmo valor)
- **Origem:** DIMP → `Total intermediado (declarante = intermediador Y)` · agregação: INSTITUICAO_PAGAMENTO
- **Destino:** EFD ICMS/IPI → `1601 (COD_PART_IT -> CNPJ Y)`
- **Chave de junção:** CNPJ_INTERMEDIADOR, CNPJ_ESTABELECIMENTO, MES
- **Operador:** IGUAL · tolerância: PARAM:TOL_AGREGADO_ABSOLUTO
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Convênio ICMS 134/2016 – Informações de meios de pagamento; Ato COTEPE/ICMS 65/2018 – DIMP (Versão 10 a partir de 01/06/2026, redação do Ato COTEPE/ICMS 124/25) vDIMP v10; Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Mesmas da M6-01
- **Depende de:** ENT-012
- **Cadeias de valor:** C1, C9
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "{mes}: intermediador {intermediador} declarou R$ {dimp} x 1601 R$ {efd}."

### M6-03 – Recebimentos eletrônicos (DIMP) explicados por documentos fiscais

- **Mapa/seção do núcleo:** M6.1
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** NF-e/NFC-e → `Σ detPag.vPag com tPag eletrônico (03, 04, 17, 20 e demais eletrônicos) de NF-e/NFC-e + NFS-e pagas eletronicamente` · agregação: MES
- **Destino:** DIMP → `Total transacionado do estabelecimento` · agregação: MES
- **Chave de junção:** CNPJ_ESTABELECIMENTO, MES
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:MATERIALIDADE_DIMP
- **Condição:** DIMP <= documentos + recebimentos justificados (lista de hipóteses). Excesso da DIMP = indício de saídas sem documento fiscal.
- **Severidade:** ESCRITURACAO_GRAVE · atenuada para ALERTA quando: Diferença abaixo de PARAM:MATERIALIDADE_DIMP, ou hipóteses legítimas ainda não esgotadas/justificadas
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Convênio ICMS 134/2016 – Informações de meios de pagamento; Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json)
- **Hipóteses legítimas:** Transferências entre contas do próprio titular | Empréstimos e aportes | Adiantamentos de clientes (fato gerador futuro) | Estornos/chargebacks | Vendas de outro estabelecimento ou UF | Recebimento de vendas a prazo de períodos anteriores
- **Depende de:** M6-01
- **Cadeias de valor:** C1, C9
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "{mes}: recebimentos eletrônicos declarados na DIMP R$ {dimp} x documentos com pagamento eletrônico R$ {docs}. Diferença R$ {dif} a justificar."

### M6-04 – Total anual da DIMP x receita bruta (ECD/ECF)

- **Mapa/seção do núcleo:** M6.1
- **Tipo de relação:** LIM – Limite (um lado não pode exceder o outro)
- **Origem:** ECD → `Receita bruta (contas RECEITA_BRUTA) + recebimentos sem fato gerador justificados` · agregação: ANO
- **Destino:** DIMP → `Σ total transacionado` · agregação: ANO
- **Chave de junção:** CNPJ_BASE, ANO
- **Operador:** MENOR_OU_IGUAL · tolerância: PARAM:MATERIALIDADE_DIMP
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Convênio ICMS 134/2016 – Informações de meios de pagamento; Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 vLeiaute 9 (mai/2026)
- **Hipóteses legítimas:** Mesmas da M6-03
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "DIMP anual R$ {dimp} x receita bruta contábil R$ {ecd}."

### M6-05 – Presumido em regime de caixa: recebimentos via instituição no F525

- **Mapa/seção do núcleo:** M6.1
- **Tipo de relação:** AGR – Agregação (soma de vários registros = um valor)
- **Origem:** DIMP → `Total transacionado` · agregação: MES
- **Destino:** EFD-Contribuições → `F525 com indicação de recebimento via administradora/instituição (conferir códigos de IND_REC)`
- **Chave de junção:** CNPJ_BASE, MES
- **Operador:** SOMA_IGUAL · tolerância: PARAM:TOL_AGREGADO_PERCENTUAL
- **Aplicável se:** EFD_CONTRIB 0110.IND_REG_CUM = 1
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35; Convênio ICMS 134/2016 – Informações de meios de pagamento
- **Hipóteses legítimas:** Diferença entre data da transação e data do crédito em conta
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "{mes}: DIMP R$ {dimp} x recebimentos via instituição no F525 R$ {f525}."

### M6-10 – Credenciadora informada na NF-e presente na DIMP

- **Mapa/seção do núcleo:** M6.2
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `pag/detPag/card/CNPJ` · agregação: MES
- **Destino:** DIMP → `CNPJ do declarante`
- **Chave de junção:** CNPJ_INSTITUICAO, MES
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes v4.00; Ato COTEPE/ICMS 65/2018 – DIMP (Versão 10 a partir de 01/06/2026, redação do Ato COTEPE/ICMS 124/25) vDIMP v10
- **Hipóteses legítimas:** DIMP da instituição não fornecida ao agente (cobertura)
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Credenciadora {cnpj} informada em NF-e de {mes} não aparece na DIMP recebida."

### M6-11 – Intermediador informado na NF-e presente na DIMP

- **Mapa/seção do núcleo:** M6.2
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** NF-e/NFC-e → `ide/indIntermed = 1; infIntermed/CNPJ` · agregação: MES
- **Destino:** DIMP → `CNPJ do declarante (intermediador)`
- **Chave de junção:** CNPJ_INTERMEDIADOR, MES
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** NT 2020.006 – Intermediador e agenciador de transação comercial v1.31; Convênio ICMS 134/2016 – Informações de meios de pagamento
- **Hipóteses legítimas:** DIMP do intermediador não fornecida ao agente (cobertura)
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Intermediador {cnpj} informado em NF-e de {mes} não aparece na DIMP recebida."

### M6-12 – Instituição do 1601 presente na DIMP

- **Mapa/seção do núcleo:** M6.2
- **Tipo de relação:** PRE_DIRETA – Presença direta (existe em A ⇒ deve existir em B; falta = omissão)
- **Origem:** EFD ICMS/IPI → `1601.COD_PART_IP / COD_PART_IT -> 0150.CNPJ`
- **Destino:** DIMP → `CNPJ do declarante`
- **Chave de junção:** CNPJ_INSTITUICAO, MES
- **Operador:** EXISTE
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2; Ato COTEPE/ICMS 65/2018 – DIMP (Versão 10 a partir de 01/06/2026, redação do Ato COTEPE/ICMS 124/25) vDIMP v10
- **Hipóteses legítimas:** DIMP da instituição não fornecida ao agente (cobertura)
- **Cadeias de valor:** C9
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Instituição {cnpj} informada no 1601 de {mes} não aparece na DIMP recebida."

## Grupo CONTINUIDADE – 6 regras

Continuidade entre períodos consecutivos de uma mesma obrigação (saldo final do mês t = saldo inicial do mês t+1). Só é verificável pelo agente de coesão, porque exige dois arquivos de períodos diferentes.

### CNT-ICMS-001 – Saldo credor de ICMS transportado = saldo credor anterior do mês seguinte

- **Mapa/seção do núcleo:** NUCLEO-3 C2 CP-3
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** EFD ICMS/IPI → `E110.VL_SLD_CREDOR_TRANSPORTAR (mês t)`
- **Destino:** EFD ICMS/IPI → `E110.VL_SLD_CREDOR_ANT (mês t+1)`
- **Chave de junção:** CNPJ, IE
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Estorno/transferência de saldo credor autorizado e lançado como ajuste (E111) – verificar o ajuste, não o saldo
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "{cnpj}: saldo credor transportado em {mes} R$ {fim} x saldo credor anterior em {mesSeg} R$ {ini}."

### CNT-ICMS-002 – Saldo credor de ICMS-ST e DIFAL transportado por UF

- **Mapa/seção do núcleo:** NUCLEO-3 C2
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** EFD ICMS/IPI → `E210 / E310 saldo credor a transportar (mês t, por UF)`
- **Destino:** EFD ICMS/IPI → `E210 / E310 saldo credor anterior (mês t+1, mesma UF)`
- **Chave de junção:** CNPJ, UF
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Nenhuma hipótese legítima prevista nos leiautes para esta divergência – solicitar justificativa documental ao contribuinte
- **Cadeias de valor:** C2
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "{cnpj}/{uf}: saldo credor de {apuracao} não transportado corretamente de {mes} para {mesSeg}."

### CNT-IPI-001 – Saldo credor de IPI transportado

- **Mapa/seção do núcleo:** NUCLEO-3 C3 CP-3
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** EFD ICMS/IPI → `E520.VL_SC_IPI (mês t)`
- **Destino:** EFD ICMS/IPI → `E520.VL_SD_ANT_IPI (mês t+1)`
- **Chave de junção:** CNPJ
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Aplicável se:** EFD_ICMS_IPI 0000.IND_ATIV = 0
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Apuração de IPI em período diferente do mensal quando previsto
- **Cadeias de valor:** C3
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "{cnpj}: saldo credor de IPI de {mes} R$ {fim} x saldo anterior de {mesSeg} R$ {ini}."

### CNT-CTB-001 – Saldo de créditos de PIS/COFINS (1100/1500) transportado

- **Mapa/seção do núcleo:** NUCLEO-3 C5 CP-4
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** EFD-Contribuições → `1100/1500.SLD_CRED_FIM (mês t), por PER_APU_CRED e COD_CRED`
- **Destino:** EFD-Contribuições → `1100/1500 saldo de abertura (mês t+1), mesmo PER_APU_CRED e COD_CRED`
- **Chave de junção:** CNPJ_BASE, PER_APU_CRED, COD_CRED
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Aplicável se:** COD_INC_TRIB in (1,3)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Crédito utilizado em PER/DCOMP ou ressarcimento (informado nos campos próprios)
- **Cadeias de valor:** C5
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Crédito {codCred} do período {perApu}: saldo final {mes} R$ {fim} x saldo inicial {mesSeg} R$ {ini}."

### CNT-CTB-002 – Saldo de retenções a deduzir (1300/1700) transportado

- **Mapa/seção do núcleo:** NUCLEO-3 C8
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** EFD-Contribuições → `1300/1700 saldo de retenção a utilizar (mês t)`
- **Destino:** EFD-Contribuições → `1300/1700 saldo de abertura (mês t+1)`
- **Chave de junção:** CNPJ_BASE, IND_NAT_RET, PR_REC_RET
- **Operador:** SALDO_FINAL_IGUAL_INICIAL · sem tolerância (valor exato)
- **Severidade:** ESCRITURACAO_GRAVE
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD-Contribuições v1.35
- **Hipóteses legítimas:** Retenção utilizada em PER/DCOMP ou restituição
- **Cadeias de valor:** C8
- **Vigência a partir de:** 2025-01-01
- **Campo a conferir no PDF oficial:** sim
- **Mensagem do parecer:** "Retenção do período {prRec}: saldo a utilizar não transportado corretamente de {mes} para {mesSeg}."

### CNT-INV-001 – Inventário de 31/12 (EFD de fevereiro) = estoque inicial do Bloco K de janeiro

- **Mapa/seção do núcleo:** NUCLEO-3 C6
- **Tipo de relação:** CNT – Continuidade (saldo final = saldo inicial seguinte)
- **Origem:** EFD ICMS/IPI → `H010.QTD (DT_INV = 31/12)`
- **Destino:** EFD ICMS/IPI → `K200.QTD (DT_EST = 31/12) informado na EFD de dezembro`
- **Chave de junção:** CNPJ, COD_ITEM, POSSE
- **Operador:** IGUAL · sem tolerância (valor exato)
- **Severidade:** ALERTA
- **Evidência:** DERIVADA (dedução a partir da definição oficial dos campos (não é regra cruzada publicada))
- **Fontes:** Guia Prático da EFD ICMS/IPI v3.2.2
- **Hipóteses legítimas:** Estabelecimento não obrigado ao Bloco K completo
- **Cadeias de valor:** C6
- **Vigência a partir de:** 2025-01-01
- **Mensagem do parecer:** "Ver M7-10 – item {codItem}."

## Cadeias de valor e pontos de controle

Cadeias de valor ponta a ponta (NUCLEO-3). Cada ponto de controle (CP) agrupa regras do catálogo. Se um CP quebra, os CPs seguintes da mesma cadeia são reportados como NAO_CONCLUSIVO (não como erros independentes).

### C1 – Receita

- **CP-0** – DIMP explicada por documentos e pelo 1601: M6-01, M6-02, M6-03
- **CP-1** – Documento de receita escriturado: M1-P01, M1-R02, M2-P01, M4-P01, M1-I03, M1-I15
- **CP-2** – Receita EFD ICMS/IPI x EFD-Contribuições: M3-03, M3-06, M2-R03
- **CP-3** – Totalizadores de apuração (PVA, agente de origem): INT-CTB-004
- **CP-4** – Receita escriturada x contabilidade: M8-02, M7-08, M12-01
- **CP-5** – ECF recupera a ECD vigente: M9-01, M9-05, M9-11
- **CP-6** – Receita base de IRPJ/CSLL: M10-02, M10-03

### C2 – ICMS

- **CP-1** – ICMS do documento = escriturado (própria) / crédito limitado (entrada): M1-I05, M1-I06, M1-I07, M1-R01, M1-R04, M1-P05, INT-EFD-012
- **CP-2** – Totalizadores da apuração (PVA, agente de origem): INT-EFD-002
- **CP-3** – Continuidade, ST e DIFAL: CNT-ICMS-001, CNT-ICMS-002, M1-I08, M1-I11
- **CP-4** – Apuração x contabilidade: M7-05, M7-06
- **CP-5** – Dedução na DRE: M9-11

### C3 – IPI (aplicável se EFD_ICMS_IPI 0000.IND_ATIV = 0)

- **CP-1** – IPI do documento = escriturado: M1-I09, PAR-009
- **CP-3** – Continuidade do saldo credor: CNT-IPI-001
- **CP-4** – Apuração x contabilidade: M7-07

### C4 – PIS/COFINS – débito

- **CP-1** – CST/base/valor da saída própria = documento: M2-I02, M2-I03, M4-I01, M2-P01, M4-P01, M2-R03
- **CP-1b** – Receitas não tributadas com contas existentes: M8-03
- **CP-4** – Contribuição a recolher x passivo: M8-05
- **CP-5** – Deduções na DRE: M10-06

### C5 – PIS/COFINS – crédito (aplicável se EFD_CONTRIB 0110.COD_INC_TRIB in (1,3))

- **CP-1** – Crédito com documento válido: M2-R01, M2-R02, M4-R02, M3-04
- **CP-2** – Base sem ICMS da aquisição: M2-R04
- **CP-3** – Créditos sobre ativo com lastro contábil: M8-04, M3-07
- **CP-4** – Continuidade e contabilidade: CNT-CTB-001, M8-07
- **CP-5** – Regime cumulativo sem crédito: PAR-003

### C6 – Estoque

- **CP-1** – Inventário de 31/12 informado (agente de origem): INT-EFD-009
- **CP-2** – Bloco K x Bloco H: M7-10, CNT-INV-001
- **CP-3** – Inventário x contabilidade: M7-02, M7-03, M7-04
- **CP-4** – Contabilidade x ECF: M9-16, M11-01
- **CP-5** – Entradas refletidas: M1-P05, M1-P06, M12-05

### C7 – Ativo imobilizado

- **CP-1** – NF-e de ativo escriturada: M1-P05
- **CP-2** – CIAP com NF-e válida: M1-R06
- **CP-3** – Crédito de PIS/COFINS sobre o bem: M3-07, M8-04
- **CP-4** – Adições ao imobilizado: M12-02, M7-09, M11-03
- **CP-5** – Contas do ativo existentes: ENT-030, ENT-032

### C8 – Retenções na fonte

- **CP-1** – PIS/COFINS retidos: NFS-e x F600: M4-P03, M4-R03, M4-I01
- **CP-2** – IRRF/CSLL retidos: NFS-e x Y570: M12-03
- **CP-3** – Fontes pagadoras coerentes: M10-04
- **CP-4** – Contabilização e continuidade: M8-06, M12-04, CNT-CTB-002

### C9 – Recebimentos eletrônicos (DIMP)

- **CP-1** – Instituições casadas: ENT-012, M6-10, M6-11, M6-12
- **CP-2** – DIMP = 1601: M6-01, M6-02
- **CP-3** – DIMP x documentos: M6-03, M5-03
- **CP-5** – Presumido caixa: M6-05
- **CP-6** – DIMP x receita contábil: M6-04

### C10 – Resultado -> IRPJ/CSLL

- **CP-1** – Recuperação da ECD: M9-01, M9-02, M9-03, M9-04, M9-05, M9-06, M9-10, M9-11
- **CP-2** – Ajustes vinculados à contabilidade: M9-12, M9-13, M9-14, ENT-033
- **CP-3** – Continuidade da Parte B e dos saldos: M9-20, M9-21, M9-22
- **CP-4** – Limite de compensação (agente de origem): INT-ECF-005
- **CP-5** – Tributos apurados x contabilidade: M9-15

## Ordem de execução

1. Compatibilidade de regime (regras PAR-)
2. Filtros de exclusividade legítima (regras EXC-)
3. Entidades compartilhadas (regras ENT-)
4. Documentos x escriturações (regras M1-, M2-, M4-, M5-)
5. Escriturações entre si (regras M3-, M7-, M8-, M10-, M11-)
6. Continuidade entre períodos (regras CNT-)
7. Contabilidade x ECF (regras M9-, M12-)
8. Evidência externa (DIMP) (regras M6-)
9. Consolidação por cadeia

Política de quebra: Reportar a(s) regra(s) violada(s) com sua severidade. Marcar como NAO_CONCLUSIVO, citando o CP de origem da quebra. Marcar como SEM_EVIDENCIA (cobertura) – nunca como erro.

## Parâmetros do projeto (sem valor definido – não há norma oficial)

- **TOL_VALOR_DOCUMENTO** (decimal): Tolerância absoluta (R$) para igualdade de valores entre documento e escrituração (arredondamento).
- **TOL_AGREGADO_PERCENTUAL** (decimal): Tolerância percentual para cruzamentos agregados (mês/ano) entre escriturações. Abaixo dela, divergência vira ALERTA; acima, mantém a severidade da regra.
- **TOL_AGREGADO_ABSOLUTO** (decimal): Tolerância absoluta (R$) combinada com a percentual (aplicar a maior / menor – decisão do projeto).
- **JANELA_MESES_ENTRADA** (int): Quantidade de meses após dhEmi em que uma NF-e de entrada pode ser escriturada sem caracterizar omissão (escrituração pela data de entrada – DT_E_S).
- **MATERIALIDADE_DIMP** (decimal): Limite (R$ ou %) a partir do qual a diferença DIMP x documentos passa de ALERTA a ESCRITURACAO_GRAVE.
- **MATERIALIDADE_RECEITA** (decimal): Limite (R$ ou %) a partir do qual divergências de receita entre documentos/escriturações e contabilidade passam a ESCRITURACAO_GRAVE.
- **TABELA_CFOP_RECEITA** (tabela): Tabela CFOP -> {geraReceita: bool, natureza: VENDA|DEVOLUCAO|TRANSFERENCIA|REMESSA|RETORNO|BONIFICACAO|ATIVO|USO_CONSUMO|OUTROS}. Construída a partir da Tabela CFOP oficial (Convênio SINIEF s/nº 1970). Não existe lista oficial 'CFOP de receita'.
- **TABELA_CONTAS_POR_FUNCAO** (tabela): Mapeamento, por contribuinte, das contas da ECD por função: ESTOQUE, ICMS_RECOLHER, ICMS_RECUPERAR, IPI_RECOLHER, PIS_COFINS_RECOLHER, PIS_COFINS_RECUPERAR, RETENCOES_COMPENSAR, IMOBILIZADO, RECEITA_BRUTA, IRPJ_CSLL. Preferir derivar do mapeamento referencial (I051) e completar manualmente.
- **DATA_VIGENCIA_NT_2023_002** (date): Data de produção da eliminação da inutilização (NT 2023.002 v1.02). Depois dela não há novos COD_SIT 05.
- **DATA_OBRIGATORIEDADE_UB12_10** (date): Data de obrigatoriedade em produção do grupo IBS/CBS (regra UB12-10) conforme NT 2025.002 v1.52.

## Fontes oficiais

- **GP_EFD_ICMS_IPI** – Guia Prático da EFD ICMS/IPI (versão 3.2.2) – verificada em fonte primária em 29/10/2026 – http://sped.rfb.gov.br/pasta/show/1573
- **LEIAUTE_EFD_020** – NT 2025.001 v1.0 – Leiaute EFD ICMS/IPI versão 020 (01/01/2026 a 31/12/2026) – Ato COTEPE/ICMS 79/2025 (versão 020) – verificada em fonte primária em 29/10/2026 – http://sped.rfb.gov.br/pasta/show/1573
- **GP_EFD_CONTRIB** – Guia Prático da EFD-Contribuições (versão 1.35) – verificada em fonte primária em 29/10/2026 – http://sped.rfb.gov.br/pasta/show/1989
- **MANUAL_ECD_L9** – Manual de Orientação do Leiaute 9 da ECD – Anexo ao ADE Cofis nº 1/2026 (versão Leiaute 9 (mai/2026)) – verificada em fonte primária em 29/10/2026 – http://sped.rfb.gov.br/pasta/show/1569
- **MANUAL_ECF_L12** – Manual de Orientação do Leiaute 12 da ECF – Anexo ao ADE Cofis nº 2/2026 (versão Leiaute 12) – verificada em fonte primária em 29/10/2026 – http://sped.rfb.gov.br/projeto/show/269
- **MOC_NFE** – Manual de Orientação do Contribuinte NF-e/NFC-e – leiaute 4.00 e NTs vigentes (versão 4.00) – verificada em fonte primária em 29/10/2026 – https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY=
- **NT_2020_001** – NT 2020.001 – Manifestação do Destinatário (versão 1.60) – verificada em fonte primária em 29/10/2026 – https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY=
- **NT_2020_006** – NT 2020.006 – Intermediador e agenciador de transação comercial (versão 1.31) – verificada em fonte primária em 29/10/2026 – https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY=
- **NT_2023_002** – NT 2023.002 – Eliminação da autorização assíncrona e dos serviços de inutilização (versão 1.02) – verificada em fonte primária em 29/10/2026 – https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY=
- **NT_2025_002** – NT 2025.002 – Reforma Tributária do Consumo (IBS/CBS/IS) na NF-e/NFC-e (versão 1.52) – verificada em fonte primária em 29/10/2026 – https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY=
- **NFSE_ANEXO_I** – NFS-e Padrão Nacional – Anexo I – Leiaute DPS/NFS-e (versão 1.01 (20260209)) – verificada em fonte primária em 29/10/2026 – https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual
- **NFSE_ANEXO_II** – NFS-e Padrão Nacional – Anexo II – Eventos (versão 1.01 (20260122)) – verificada em fonte primária em 29/10/2026 – https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual
- **CONV_ICMS_134_16** – Convênio ICMS 134/2016 – Informações de meios de pagamento – conferir texto vigente – https://www.confaz.fazenda.gov.br
- **ATO_COTEPE_65_18** – Ato COTEPE/ICMS 65/2018 – DIMP (Versão 10 a partir de 01/06/2026, redação do Ato COTEPE/ICMS 124/25) (versão DIMP v10) – verificada em fonte primária em 29/10/2026 – https://www.confaz.fazenda.gov.br/legislacao/atos/2018/ato-cotepe-icms-65-18
- **AJUSTE_SINIEF_07_05** – Ajuste SINIEF 07/2005 – NF-e – conferir texto vigente – https://www.confaz.fazenda.gov.br
- **AJUSTE_SINIEF_02_09** – Ajuste SINIEF 02/2009 – EFD ICMS/IPI – conferir texto vigente – https://www.confaz.fazenda.gov.br
- **CONV_SINIEF_SN_1970** – Convênio SINIEF s/nº de 1970 (Tabela CFOP; art. 7º §1º-A – Carta de Correção) – conferir texto vigente – https://www.confaz.fazenda.gov.br
- **IN_RFB_1252_2012** – IN RFB 1.252/2012 – EFD-Contribuições – conferir texto vigente – https://normas.receita.fazenda.gov.br
- **IN_RFB_2003_2021** – IN RFB 2.003/2021 – ECD – conferir texto vigente – https://normas.receita.fazenda.gov.br
- **IN_RFB_2004_2021** – IN RFB 2.004/2021 – ECF – conferir texto vigente – https://normas.receita.fazenda.gov.br
- **IN_SRF_104_1998** – IN SRF 104/1998 – Regime de caixa no lucro presumido (conferir dispositivo consolidado na IN RFB 1.700/2017) – conferir texto vigente – https://normas.receita.fazenda.gov.br
- **LEI_10637_02** – Lei 10.637/2002 – PIS não cumulativo (art. 3º; art. 8º) – conferir texto vigente – https://www.planalto.gov.br/ccivil_03/leis/2002/l10637.htm
- **LEI_10833_03** – Lei 10.833/2003 – COFINS não cumulativa (art. 3º; art. 10) – conferir texto vigente – https://www.planalto.gov.br/ccivil_03/leis/2003/l10.833.htm
- **LEI_14592_23** – Lei 14.592/2023 (conversão da MP 1.159/2023) – exclusão do ICMS da base de crédito (art. 3º, §2º, III das Leis 10.637/02 e 10.833/03) – conferir texto vigente – https://www.planalto.gov.br
- **STF_RE_574706** – STF – RE 574.706 (Tema 69) – exclusão do ICMS da base do PIS/COFINS – conferir texto vigente – https://portal.stf.jus.br
- **LC_87_96** – LC 87/1996 – ICMS (art. 20, §5º – CIAP 1/48) – conferir texto vigente – https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp87.htm
- **LC_123_06** – LC 123/2006 – Simples Nacional – conferir texto vigente – https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp123.htm
- **LEI_9065_95** – Lei 9.065/1995, art. 15 – limite de 30% na compensação de prejuízo fiscal – conferir texto vigente – https://www.planalto.gov.br/ccivil_03/leis/l9065.htm
- **LEI_8981_95** – Lei 8.981/1995, arts. 42 e 58 – limite de 30% (IRPJ e CSLL) – conferir texto vigente – https://www.planalto.gov.br/ccivil_03/leis/l8981.htm
- **DEC_8426_15** – Decreto 8.426/2015 – PIS/COFINS sobre receitas financeiras (não cumulativo) – conferir texto vigente – https://www.planalto.gov.br
- **LC_214_25** – LC 214/2025 – IBS, CBS e IS – conferir texto vigente – https://www.planalto.gov.br
- **DECISAO_PROJETO** – Decisão técnica do projeto (parâmetro sem norma oficial – ver parametros.json) – conferir texto vigente
