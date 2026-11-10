# Plano Técnico 004: Motor com o catálogo completo

**Especificação:** [spec.md](spec.md) · **Tarefas:** [tasks.md](tasks.md) · **Status:** Proposta

## 1. Estratégia

A ideia é construir a infraestrutura uma vez e depois aprovar e ligar regras por bloco. A construção não depende das decisões pendentes (DP-1 a DP-6), porque tudo pode entrar com as regras em `PROPOSTA`. Só a **aprovação** de cada regra depende delas.

```
Fase 0  Decisões humanas (paralela a tudo) ─────────────────────────────┐
Fase 1  Catálogo único + parâmetros + executores genéricos + M1 migrado │
Fase 2  PAR, EXC, ENT (passos 1–3)                                       │ cada regra só
Fase 3  M1 restante, M2, M4, M5 (passo 4)                                │ vira APROVADA
Fase 4  M3, M7, M8, M10, M11 (passo 5)                                   │ quando DP-1..3
Fase 5  CNT (passo 6)                                    ← DP-4          │ estiverem
Fase 6  M9, M12 (passo 7)                                ← DP-5          │ resolvidas
Fase 7  M6 DIMP (passo 8)                                                │
Fase 8  Consolidação por cadeia C1–C10, cobertura no parecer, avaliação ┘
```

## 2. Decisões técnicas

| ID | Decisão | Por quê |
|---|---|---|
| D401 | **Fonte única do catálogo = `docs/regras/*.json`**, lida **diretamente** pelo motor (copiada para `regras/` na saída). `catalog/regras.json` foi removido. As pré-condições, que não são regras fiscais, ficam em `docs/regras/precondicoes.json`. | Na implementação, um gerador intermediário só acrescentava um artefato e um teste de defasagem. Ler a fonte elimina a defasagem. `.dockerignore`/`.vercelignore` passam a incluir `docs/regras`. |
| D402 | **Status e aprovação ficam num arquivo próprio**, `docs/regras/aprovacoes.json` (`id`, `status`, `aprovadoPor`, `testes`, as fontes efetivamente conferidas e `dependenciasDispensadas` com motivo). A versão desse arquivo é a versão do catálogo no envelope (0.3.0). | O conteúdo fiscal da regra e a decisão de aprová-la têm responsáveis e ciclos diferentes, e a revisão no PR fica mais clara. |
| D403 | **Parâmetros carregados de `docs/regras/parametros.json`** (copiado para a saída). `ParametrosComparacao` passa a resolver `PARAM:*` pelo nome. `Cruzamento:Tolerancia`/`Materialidade` do appsettings são removidos para não haver duas fontes. | FR-403/404. A regra declara `tolerancia: "PARAM:TOL_VALOR_DOCUMENTO"`, e o motor precisa resolver isso. |
| D404 | **Acessor de elementos por tipo de obrigação.** Uma expressão `REGISTRO.CAMPO` (SPED) ou `grupo/campo` (XML NF-e/NFS-e/DIMP) é interpretada uma vez, ao carregar o catálogo, e validada contra o que o mapeador de entrada conhece. Listas como `C100.VL_PIS, VL_COFINS` viram vários pares comparados um a um. | Os elementos de `origem`/`destino` estão em texto. Interpretar na carga faz uma expressão inválida derrubar a inicialização, e não a análise. |
| D405 | **Executores genéricos por tipo de relação e operador:** `ExecutorPresenca` (PRE_DIRETA/PRE_REVERSA + EXISTE), `ExecutorIdentidade` (IDT + IGUAL/COMPATIVEL), `ExecutorLimite` (LIM + MENOR/MAIOR_OU_IGUAL), `ExecutorAgregacao` (AGR + SOMA_IGUAL/MENOR_OU_IGUAL), `ExecutorContinuidade` (CNT), `ExecutorExclusao` (EXC + FILTRA, registra em `ContextoCruzamento.Exclusoes`). `PAR` e casos que fogem do padrão continuam com `ValidadorRegra` à mão, registrado pelo `id`. | Os quatro primeiros cobrem cerca de 118 das 158 regras. O validador à mão fica só onde a regra não cabe no formato. |
| D406 | **Validador à mão tem precedência sobre o genérico** para o mesmo `id`. | Permite migrar M1 aos poucos e tratar exceções sem mexer no executor genérico. |
| D407 | **Mensagens** vêm de `mensagemParecer`, com os marcadores (`{chave}`, `{cnpj}`...) preenchidos pela evidência. Severidade e atenuação vêm de `severidade` e `severidadeAtenuada`. | Constitution: severidade definida pelo catálogo, nunca pelo LLM. |
| D408 | **Fixtures sintéticas por tipo de obrigação** (`tests/.../Fixtures/Lotes.cs` estendido) e cenários em `exemplos/cenarios/` por bloco: um consistente e um com divergência para cada regra. | Constitution §5: teste positivo e negativo por regra, sem dado real. |

## 3. Fases

### Fase 0 — Decisões (paralela; não bloqueia a construção)
DP-1 a DP-6 da spec. Resultado esperado: `parametros.json` com valores, `fontes.json` com `verificadoEmFontePrimaria`, `aprovacoes.json` preenchido por bloco, a tabela de equivalência de IDs e as decisões de período (CNT, ECD/ECF anuais).

### Fase 1 — Infraestrutura (bloqueante para as fases 2 a 8)
1. Carregador lê `docs/regras` + `aprovacoes.json` + `precondicoes.json` direto (D401 revisada).
2. Modelo `Regra` com os campos declarativos. `ValidadorCatalogo` amplia a checagem: regra aprovada exige parâmetros com valor, executor resolvido e testes listados.
3. Resolução de parâmetros (D403) e acessor de elementos (D404).
4. Executores `Presenca`, `Identidade`, `Limite` e `Exclusao`.
5. **Migrar as 10 regras M1** para os executores genéricos. Os 10 cenários atuais precisam dar exatamente os mesmos achados (FR-406). Só então remover o código à mão equivalente.
6. Ordem de execução por `ordemExecucao` (FR-407) e cobertura por bloco no resultado (FR-410).

**Saída (entregue):** catálogo com 158 regras (10 aprovadas, 148 propostas); M1-I03/I05/I06 pelo executor genérico, as outras 7 M1 com validador dedicado (dependem de condições em texto livre); suíte verde; regressão achado a achado sem diferença. Medição e pendências em spec §8.

**Ajuste para as próximas fases:** o maior ganho está em **chave composta** (CNPJ/período/conta) e no **ExecutorAgregacao**, que juntos destravam a maior parte de M3, M7–M12 e ENT. O trabalho no catálogo de reescrever elementos e filtros em texto livre no formato estruturado é **conteúdo fiscal** e passa pelo responsável.

### Fase 2 — Passos 1 a 3: PAR (10), EXC (13), ENT (16) = 39
Vêm primeiro porque os outros blocos dependem deles: EXC filtra documentos legítimos antes das presenças, e PAR bloqueia regras de valor incompatíveis com o regime. PAR usa validadores à mão (operador COMPATIVEL sobre o regime). EXC e ENT usam os executores genéricos.

### Fase 3 — Passo 4: M1 restante (19), M2 (12), M4 (7), M5 (3) = 41
Fixtures de EFD-Contribuições (blocos A, C, F, M) e de NFS-e. Depende de `JANELA_MESES_ENTRADA`, `TOL_VALOR_DOCUMENTO` e `TABELA_CFOP_RECEITA`.

### Fase 4 — Passo 5: M3, M7, M8, M10, M11 = 33
Construir o `ExecutorAgregacao`: soma por chave, mês ou conta, com tolerância percentual e absoluta e materialidade. Fixtures de ECD e ECF. Depende de `TABELA_CONTAS_POR_FUNCAO` e `MATERIALIDADE_RECEITA`.

### Fase 5 — Passo 6: CNT = 6 (depende de DP-4)
`ExecutorContinuidade`. Pede lote com dois períodos ou referência ao mês anterior, conforme decidido em DP-4. Se a decisão for mudar o contrato, a mudança vai para `lote.v1.1`, compatível com a v1.

### Fase 6 — Passo 7: M9 (16), M12 (5) = 21 (depende de DP-5)
ECD × ECF anuais. Inclui as 2 regras de evidência `PVA`, que podem virar só leitura do resultado da origem (FR-409).

### Fase 7 — Passo 8: M6 DIMP = 8
Fixtures de DIMP. Depende de `MATERIALIDADE_DIMP`.

### Fase 8 — Consolidação
Pontos de controle C1 a C10 com `politicaQuebra` (FR-408). Cobertura por bloco no parecer e no `ResultadoAnaliseSofia` (`observacoes`). Conjunto de avaliação `eval/parecer` ampliado com um lote completo dos 8 tipos.

## 4. Riscos

| Risco | Mitigação |
|---|---|
| Expressões de `origem`/`destino` ambíguas no texto do catálogo | O acessor (D404) falha na carga e aponta a regra. Essas regras ficam `PROPOSTA` ou passam a ter validador à mão. |
| Regra aprovada sem parâmetro com valor | O motor não inicia (FR-404) e um teste de governança cobre isso. |
| A migração de M1 mudar achados em produção | Regressão exigida pelos 10 cenários (FR-406), e o código antigo só sai depois dela. |
| O lote mensal não cobre CNT e as escriturações anuais | DP-4 e DP-5 decididas antes das fases 5 e 6. Até lá, essas regras ficam como não aplicáveis, com o motivo informado. |
| Aprovação humana lenta para 148 regras | A aprovação é por bloco (`aprovacoes.json`), e cada fase entrega valor sozinha. |

## 5. Rastreabilidade

`docs/regras/*.json` (id) → `aprovacoes.json` (status e testes) → executor (genérico ou à mão) → teste positivo e negativo → cenário em `exemplos/cenarios`. O teste de governança cobre a cadeia toda para cada regra `APROVADA`.
