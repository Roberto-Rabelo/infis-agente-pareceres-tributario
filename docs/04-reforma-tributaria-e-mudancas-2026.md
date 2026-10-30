# 04 – Reforma Tributária do Consumo e Mudanças de 2026

Mudanças oficiais que afetam diretamente as regras dos arquivos 02 e 03. Itens marcados **(conferir)** foram vistos apenas em fonte secundária ou dependem de cronograma interno da própria NT.

## 1. IBS / CBS / IS nos documentos fiscais

| Documento | Norma técnica oficial | Impacto nas regras |
|---|---|---|
| NF-e / NFC-e | **NT 2025.002 v1.52** (publicada 01/10/2026) – inclusão de campos e regras de validação da RTC (grupo de IBS/CBS/IS por item e totalizadores) | Nova família de regras INT-NFE-009. Obrigatoriedade em produção da regra UB12-10 segue cronograma da própria NT **(conferir)**. |
| NF-e / NFC-e | **NT 2026.010 v1.00** – modelo do DANFE para a RTC | Sem impacto em regras de cruzamento. |
| NF-e | **NT 2026.006 v1.00** – vinculação entre NF-e e transação financeira sujeita ao *split payment* | Futuro elo documental **NF-e ↔ transação de pagamento**, complementar à DIMP. Criar regras somente após a vigência. |
| NFS-e | Anexo I v1.01 (grupo IBS/CBS), Anexo C – indicador de operação IBS/CBS v1.01 | Novas regras de coerência entre `cTribNac`/NBS e indicador de operação. |
| EFD ICMS/IPI | Guia Prático 3.2.x – exceção no `C100` para documentos que contenham apenas IBS/CBS; seção sobre a Reforma Tributária do Consumo | CRZ-NFE-EFD-013: documento só com IBS/CBS não deve gerar `ESCRITURACAO_GRAVE` por ausência na EFD ICMS/IPI **(conferir texto da exceção na versão do Guia do período)**. |

**Base legal:** LC 214/2025. Em 2026 há período de teste de IBS e CBS; o cumprimento das obrigações acessórias com os novos campos é condição para dispensa do recolhimento **(conferir artigos da LC 214/2025 aplicáveis ao período de teste antes de codificar regra de severidade)**.

Recomendação para o agente: em 2026, divergências exclusivamente de IBS/CBS devem ser classificadas como `ALERTA` (não há cruzamento oficial consolidado com EFD/ECD/ECF para esses tributos nos leiautes vigentes verificados).

## 2. CNPJ alfanumérico

| Fonte oficial | Conteúdo |
|---|---|
| NT Conjunta 2025.001 (Portal NF-e, 08/05/2025) | Orientações de implementação do CNPJ alfanumérico em todos os DF-e |
| NT 2026.004 v1.01 (08/06/2026) | Atualização do schema NF-e/NFC-e |
| ADE Cofis nº 1/2026 (manual ECD Leiaute 9, atualização) | Compatibilidade com o CNPJ alfanumérico na ECD |

Impacto de arquitetura (.NET): CNPJ como `string` de 14 posições, validação de DV pelo algoritmo do novo padrão, *matching* entre obrigações sempre por string normalizada (sem máscara). Nunca `long`/`decimal`.

## 3. Outras mudanças de NF-e relevantes para cruzamento

| NT | Efeito |
|---|---|
| NT 2023.002 v1.02 (27/10/2026) – eliminação da autorização assíncrona e dos serviços de **inutilização** | `COD_SIT = 05` (inutilizado) deixa de ter novos casos após a vigência; regra CRZ-NFE-EFD-003 deve ser condicionada à data. |
| NT 2026.007 v1.10 – validação contra LCC-RFB e CCC | Emissão para destinatário com situação cadastral irregular passa a ser rejeitada conforme regras; reforça CRZ-REG-006. |
| NT 2026.001 v1.02b – Provedor de Assinatura e Autorização (PAA) | Sem impacto nas regras de cruzamento. |

## 4. Escriturações SPED

| Obrigação | Mudança | Impacto |
|---|---|---|
| ECF Leiaute 12 | Indicação de CEBAS para entidades de assistência social, saúde e educação | INT-ECF-006 |
| ECD Leiaute 9 (ADE Cofis 1/2026) | Sem alteração de leiaute para AC 2025; CNPJ alfanumérico | — |
| EFD ICMS/IPI | Leiaute versão 020 em 2026 (Ato COTEPE/ICMS 79/2025). Leiaute 2027 por Ato COTEPE/ICMS 69/2026 **(conferir)** | INT-COM-002 |
| DIMP | Versão 10 a partir de 01/06/2026 (Ato COTEPE/ICMS 124/25) | INT-DIMP-001 |
| NFS-e | Padrão nacional; obrigatoriedade para optantes do Simples Nacional por resolução do CGSN **(conferir data no DOU)** | Determina se a NFS-e de um optante do SN deve existir no ADN |
