# Como montar o notebook no NotebookLM

Objetivo: estudar e consultar as regras de coesão entre **NF-e, NFS-e, DIMP, EFD ICMS/IPI, EFD-Contribuições, ECD e ECF**, com respostas sempre amarradas às fontes do projeto.

---

## 1. Fontes a enviar (nesta ordem)

| # | Arquivo | Por quê |
|---|---|---|
| 1 | `docs/README.md` | Versões oficiais vigentes, tipos de evidência e escala de severidade |
| 2 | `docs/NUCLEO-1-principios-entidades-matriz.md` | Camadas, tipos de relação, entidades compartilhadas, matriz de presença, matriz de regime |
| 3 | `docs/NUCLEO-2-mapas-bidirecionais.md` | Mapas M1–M12: A→B, B→A, de-para campo a campo, exclusivos legítimos |
| 4 | `docs/NUCLEO-3-cadeias-de-valor.md` | Cadeias C1–C10 e ordem de execução |
| 5 | `docs/notebooklm/catalogo-regras-legivel.md` | As 158 regras do catálogo JSON em texto (uma seção por regra, com ID) |
| 6 | `docs/01-panorama-obrigacoes.md` | Base legal e registros-chave de cada obrigação |
| 7 | `docs/02-regras-intra-obrigacao.md` | Regras internas (executadas pelos agentes de origem) |
| 8 | `docs/04-reforma-tributaria-e-mudancas-2026.md` | IBS/CBS, CNPJ alfanumérico, mudanças 2026 |
| 9 | `docs/05-modelo-regras-agente.md` | Modelo de dados e contrato do agente |
| 10 | `docs/imagens/mapa-coesao-obrigacoes.png` *(opcional)* | Visão geral ilustrada, se o NotebookLM aceitar imagem como fonte na sua conta |

**Não envie** `docs/03-regras-cruzamento.md`: é a versão anterior, substituída pelo NÚCLEO-2, e pode gerar respostas conflitantes.
**Os arquivos `.json`** não são necessários, porque o item 5 tem o mesmo conteúdo em texto.

**Recomendado (fontes oficiais primárias)**: inclua também os PDFs oficiais, baixados dos links do `README.md` §5. Assim o NotebookLM confere os campos no texto da Receita/CONFAZ:
- Guia Prático EFD ICMS/IPI v3.2.2
- Guia Prático EFD-Contribuições v1.35
- Manual da ECD – Leiaute 9 (ADE Cofis 1/2026)
- Manual da ECF – Leiaute 12 (ADE Cofis 2/2026)
- MOC NF-e 4.00 e NT 2025.002 v1.52
- Anexo I NFS-e (leiaute DPS/NFS-e v1.01)
- Manual DIMP Versão 10 (CONFAZ)

---

## 2. Instrução do chat (colar em "Configurar notebook" / personalização da conversa)

### 2.1 Versão completa

```
PAPEL
Você é um tutor técnico de compliance fiscal e tributário brasileiro, especialista em SPED e documentos fiscais eletrônicos. Seu aluno é um arquiteto de software .NET que está construindo um agente de IA. Esse agente recebe obrigações JÁ VALIDADAS (layout/PVA) e verifica apenas a COESÃO entre elas: NF-e/NFC-e, NFS-e, DIMP, EFD ICMS/IPI, EFD-Contribuições, ECD e ECF.

REGRAS DE RESPOSTA
1. Responda somente com base nas fontes deste notebook. Se a informação não estiver nas fontes, diga claramente "não consta nas fontes" e não complete com conhecimento próprio.
2. Sempre cite o ID da regra (ex.: M1-P01, ENT-030, PAR-005, CNT-ICMS-001), o mapa (M1–M12) e/ou a cadeia (C1–C10) envolvidos.
3. Para cada regra, explique sempre:
   a) o que existe na obrigação de ORIGEM e o que deve existir no DESTINO, com registro e campo;
   b) o tipo de relação: PRE→ (omissão), ←PRE (sem lastro), IDT (mesmo valor), AGR (soma), LIM (limite), CNT (continuidade), PAR (regime), EXC (exclusividade legítima);
   c) a severidade: ESTRUTURA_GRAVE, ESCRITURACAO_GRAVE, ALERTA ou INFO, e quando ela é atenuada;
   d) a evidência: PVA, MANUAL, NORMA ou DERIVADA, e a fonte oficial;
   e) as hipóteses legítimas que tornam a divergência regular.
4. Diferencie sempre:
   - erro grave de estrutura;
   - erro grave de escrituração (impacto em tributo ou base);
   - alerta de atenção (pode ter explicação legítima).
   Regras DERIVADAS nunca concluem irregularidade sem antes listar as hipóteses legítimas.
5. Lembre a ordem de execução do motor: PAR → EXC → ENT → documentos → escriturações → continuidade → ECD/ECF → DIMP → consolidação por cadeia. Se um ponto de controle quebra, os seguintes da cadeia ficam NÃO CONCLUSIVOS. Falta de insumo gera SEM_EVIDÊNCIA, nunca erro.
6. Quando a regra estiver marcada "Campo a conferir no PDF oficial", avise que o nome/código do campo ainda precisa ser confirmado. Quando citar uma fonte marcada "conferir texto vigente", avise também.
7. Parâmetros (tolerâncias, janela de meses, materialidade, tabela CFOP de receita, contas por função) NÃO têm valor definido em norma: são decisões do projeto. Nunca invente valores para eles.
8. Use exemplos numéricos simples e fictícios para ilustrar (ex.: NF-e de R$ 1.000,00 com ICMS de R$ 180,00). Deixe claro que são ilustrativos.
9. Ao final de explicações longas, ofereça: (a) um resumo em tabela, (b) o fluxo em passos, (c) 2 perguntas de verificação de entendimento.
10. Quando o aluno pedir visão de implementação, traduza a regra em: chave de junção, conjuntos comparados, operador, tolerância, filtros EXC aplicados antes e dependências (dependeDe). Use pseudocódigo C#/LINQ quando ajudar.

ESTILO
Português do Brasil, técnico e direto. Tabelas para de-para de campos. Sem jargão desnecessário. Nunca afirme algo sobre legislação que não esteja nas fontes.
```

### 2.2 Versão curta (se o campo de instrução tiver limite pequeno)

```
Tutor de compliance fiscal (SPED/DF-e) para um arquiteto .NET que constrói um agente de coesão entre NF-e, NFS-e, DIMP, EFD ICMS/IPI, EFD-Contribuições, ECD e ECF. Responda só com as fontes; se não houver, diga "não consta nas fontes". Cite sempre o ID da regra (M1-P01, PAR-005…), origem→destino com registro/campo, tipo de relação, severidade, evidência e hipóteses legítimas. Regras DERIVADAS nunca concluem erro sem hipóteses. Parâmetros não têm valor normativo: não invente. Use tabelas e exemplos fictícios.
```

---

## 3. Roteiro de estudo (perguntas para fazer no chat, em sequência)

**Módulo 1 – Visão geral**
1. "Explique as 5 camadas de complementaridade entre as obrigações e por que descer as camadas revela omissão e subir revela falta de lastro."
2. "Monte uma tabela com cada obrigação, quem a produz, a periodicidade, a granularidade (estabelecimento ou matriz) e a versão de leiaute vigente."
3. "Quais são as entidades compartilhadas (estabelecimento, participante, item, conta, documento) e qual campo representa cada uma em cada obrigação?"

**Módulo 2 – Regime e filtros (passos 1 a 3)**

4. "Explique as regras PAR-001 a PAR-010 com um exemplo de incoerência de enquadramento para cada uma."
5. "Liste todas as regras EXC e, para cada uma, um fato que pareceria omissão mas é legítimo."
6. "Por que ENT-030 e ENT-031 (contas 0500 ↔ I050) são pré-condição para os cruzamentos contábeis?"

**Módulo 3 – Documentos × escriturações**

7. "Percorra o mapa M1 (NF-e × EFD ICMS/IPI): presença A→B, presença B→A, de-para campo a campo e exclusivos legítimos."
8. "Qual a diferença entre M1-I05 e M1-I06? Por que na entrada a relação é de limite e não de igualdade?"
9. "Explique a tabela de correspondência de sentido entre tpNF/papel do CNPJ e IND_OPER/IND_EMIT."
10. "Compare M2 (NF-e × EFD-Contribuições) e M4 (NFS-e × EFD-Contribuições), inclusive o efeito do regime de caixa."

**Módulo 4 – Escriturações entre si**

11. "Explique M3-06: como comparar a receita da EFD ICMS/IPI (por estabelecimento) com a da EFD-Contribuições (centralizada)? Quais CFOPs ficam de fora?"
12. "Explique M7-02/M7-03: inventário de 31/12 × estoque contábil. Por que IND_PROP=2 é excluído?"
13. "Quais regras de M8 e M10 mostram receita fora da apuração de PIS/COFINS ou do IRPJ presumido?"

**Módulo 5 – ECD × ECF e continuidade**

14. "Explique M9-01 a M9-22. O que acontece se a ECD for substituída depois da entrega da ECF?"
15. "Liste as regras CNT e explique por que só o agente de coesão consegue verificá-las."

**Módulo 6 – DIMP**

16. "Explique o mapa M6: por que a DIMP é evidência externa, como ela se relaciona com o registro 1601 e quais hipóteses explicam DIMP maior que os documentos?"

**Módulo 7 – Cadeias e parecer**

17. "Percorra a cadeia C1 (Receita) ponto de controle por ponto de controle, com um exemplo em que o CP-2 quebra. Quais CPs ficam não conclusivos?"
18. "Faça o mesmo para C2 (ICMS), C5 (crédito PIS/COFINS) e C10 (Resultado → IRPJ/CSLL)."
19. "Simule um parecer para este cenário fictício: NF-e de venda de R$ 50.000 autorizada e não escriturada na EFD ICMS/IPI, mas presente na EFD-Contribuições. Quais regras disparam, com que severidade, e quais ficam não conclusivas?"
20. "Simule o cenário: DIMP de R$ 120.000 no mês, 1601 de R$ 80.000 e NFC-e com pagamento eletrônico de R$ 78.000."

**Módulo 8 – Implementação**

21. "Para a regra M1-I15, descreva chave de junção, conjuntos, agregação, operador, tolerância, filtros EXC e dependências, com pseudocódigo LINQ."
22. "Quais regras estão marcadas para conferir o campo no PDF oficial? Agrupe por obrigação."
23. "Quais parâmetros o projeto ainda precisa definir, e quais regras dependem de cada um?"

**Verificação**

24. "Faça um quiz de 15 perguntas de múltipla escolha sobre os mapas M1–M12, com gabarito comentado citando os IDs."
25. "Faça 10 estudos de caso curtos. Em cada um, devo dizer se é ESTRUTURA_GRAVE, ESCRITURACAO_GRAVE, ALERTA ou INFO, e qual regra se aplica."

---

## 4. Personalização do Resumo em Áudio (Audio Overview)

```
Explique para um arquiteto de software, que não é contador, como as obrigações NF-e, NFS-e, DIMP, EFD ICMS/IPI, EFD-Contribuições, ECD e ECF se complementam. Use a ideia das 5 camadas e siga uma venda de mercadoria de ponta a ponta (cadeia C1 – Receita): da NF-e até a ECF. Mostre onde cada valor deve aparecer e o que significa quando falta. Depois, siga uma compra (cadeia C5 – crédito de PIS/COFINS) e o inventário (cadeia C6 – Estoque). Diferencie omissão, falta de lastro e divergência legítima. Cite os IDs das regras (M1-P01, M3-06, M7-02, M9-01…). Não invente legislação nem valores de tolerância.
```

Sugestões de outros áudios, um por tema:
- "Somente a cadeia C10: da contabilidade (ECD) ao IRPJ/CSLL (ECF), e-Lalur e Parte B."
- "Somente a DIMP (M6) e o registro 1601: como recebimentos eletrônicos revelam saídas sem documento."
- "Somente regime tributário (PAR-001 a PAR-010): como um enquadramento incoerente invalida todo o resto."

---

## 5. Outros recursos do NotebookLM

| Recurso | Pedido sugerido |
|---|---|
| **Guia de estudo** | "Gere um guia de estudo organizado por mapa (M1–M12), com glossário dos registros e campos." |
| **Mapa mental** | Gere a partir do NÚCLEO-1 e do NÚCLEO-2: obrigação → mapas → regras |
| **Briefing / FAQ** | "Gere um FAQ com as 30 dúvidas mais prováveis de um desenvolvedor implementando o motor de regras." |
| **Linha do tempo** | "Linha do tempo das mudanças de 2025–2027 (leiautes, DIMP v10, NT 2025.002, CNPJ alfanumérico, NFS-e nacional)." |

---

## 6. Manutenção

Quando o catálogo JSON mudar, gere de novo o `catalogo-regras-legivel.md` e **substitua** a fonte no NotebookLM. O gerador é o script [gerar_catalogo_md.py](gerar_catalogo_md.py), que lê `docs/regras/*.json`. Para rodar, a partir da pasta `docs`: `python notebooklm/gerar_catalogo_md.py regras notebooklm/catalogo-regras-legivel.md`. Se a fonte antiga ficar no notebook junto com a nova, o NotebookLM vai misturar as versões.
