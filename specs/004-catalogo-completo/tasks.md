# Tarefas 004 — Catálogo completo (158 regras)

[spec.md](spec.md) · [plan.md](plan.md). `[P]` = pode rodar em paralelo. `[H]` = decisão ou aprovação humana (este agente não decide).

## Fase 0 — Decisões (paralela às fases 1 e 2)

- [ ] T400 [H] DP-1: definir os 10 parâmetros de `docs/regras/parametros.json` e a regra que combina tolerância percentual e absoluta
- [ ] T401 [H] DP-2: verificar na fonte primária as 19 fontes pendentes de `fontes.json` (= T011 da spec 001)
- [ ] T402 [H] DP-3: modelo de aprovação por bloco em `aprovacoes.json`, com responsável e data
- [ ] T403 [H] DP-4: como CNT recebe o mês anterior (dois períodos no lote ou referência)
- [ ] T404 [H] DP-5: período de cruzamento de ECD/ECF anuais com lotes mensais
- [ ] T405 [H] DP-6: tabela de equivalência `CRZ-*` × IDs novos (= T002 da spec 001)

## Fase 1 — Infraestrutura (bloqueante para as fases 2 a 8)

- [x] T410 Criar `docs/regras/aprovacoes.json`: as 10 M1 e PRE-001 a 005 como `APROVADA` (mesmo `aprovadoPor` de hoje), as outras 148 como `PROPOSTA`
- [x] T411 ~~Gerador~~ → o carregador lê `docs/regras` direto (D401 revisada); `catalog/regras.json` removido e as pré-condições movidas para `docs/regras/precondicoes.json`
- [x] T412 ~~Teste de defasagem~~ → dispensável sem gerador; `CatalogoCompletoTests` cobre a carga do catálogo real
- [x] T413 `Regra`/`CarregadorCatalogo`: ler `origem`, `destino`, `chaveJuncao`, `operador`, `tolerancia`, `severidadeAtenuada`, `filtrosExclusao`, `aplicavelSe`, `cadeias`, `mensagemParecer`
- [x] T414 Resolver `PARAM:*` a partir de `parametros.json`; remover `Cruzamento:Tolerancia`/`Materialidade` do appsettings (D403)
- [x] T415 [P] Testes do acessor de elementos (D404): SPED `REGISTRO.CAMPO`, XML `grupo/campo`, listas de campos e expressão inválida (falha na carga)
- [x] T416 Implementar o acessor de elementos por tipo de obrigação
- [x] T417 `ValidadorCatalogo`: regra aprovada sem parâmetro com valor, sem executor ou sem testes → o motor não inicia (FR-404). Testes de governança
- [x] T418 [P] Testes dos executores `Presenca`, `Identidade`, `Limite` e `Exclusao` com regras sintéticas
- [x] T419 Implementar os quatro executores e o registro "validador à mão tem precedência" (D405, D406)
- [~] T420 Migrar M1 para os executores genéricos: **M1-I03, I05, I06 migradas** (regressão sem diferença nos 10 cenários); P01, P02, R01–R04 e I01 seguem dedicadas (condições em texto livre: mês de alocação, cancelamento, papel do estabelecimento, normalização de série/número)
- [x] T421 Ordem por `cadeias.json > ordemExecucao` e `NAO_CONCLUSIVA` por dependência (FR-407)
- [x] T422 Cobertura por bloco no resultado e no `ResultadoAnaliseSofia.observacoes` (FR-410)
- [x] T423 Atualizar `docs/contrato-entrada-orquestrador.md` e o README do catálogo com o estado real da cobertura

- [ ] T424 Chave de junção composta (CNPJ/CNPJ_BASE + MES/ANO/PERIODO, COD_CTA + ANO) e junção escrituração × escrituração — maior ganho medido (~40 regras)
- [ ] T425 [H] Reescrever no formato estruturado os elementos e filtros em texto livre do catálogo (21 elementos, 13 EXC, 8 condições) — conteúdo fiscal
- [ ] T426 [H] Resolver as pendências de conteúdo da spec §8 (severidade INFO, ordem ENT-032/M9-16, obrigação EXTERNO)

## Fase 2 — PAR (10), EXC (13), ENT (16)

- [ ] T430 [P] Fixtures sintéticas: ECF (regime), EFD-Contribuições (0110/0111), ECD (I050/I155)
- [ ] T431 [P] Testes positivo e negativo das 13 EXC
- [ ] T432 EXC pelo `ExecutorExclusao`, registrando em `ContextoCruzamento.Exclusoes`
- [ ] T433 [P] Testes das 10 PAR, incluindo o bloqueio de regras de valor dependentes
- [ ] T434 Validadores à mão das PAR (COMPATIVEL)
- [ ] T435 [P] Testes das 16 ENT (incluindo a mudança de plano de contas `IND_MUDANC_PC`/`I157`)
- [ ] T436 ENT pelos executores genéricos
- [ ] T437 [H] Aprovar o bloco (PAR, EXC, ENT) em `aprovacoes.json`

## Fase 3 — M1 restante (19), M2 (12), M4 (7), M5 (3)

- [ ] T440 [P] Fixtures: NF-e com PIS/COFINS, EFD-Contribuições blocos A/C/F/M, NFS-e padrão nacional
- [ ] T441 [P] Testes das 19 M1 restantes (P03 a P06, R05, R06, I02, I04, I07 a I17)
- [ ] T442 [P] Testes das 12 M2, das 7 M4 e das 3 M5
- [ ] T443 Ligar ao executor genérico; validador à mão onde o teste mostrar necessidade
- [ ] T444 [H] Aprovar o bloco

## Fase 4 — M3, M7, M8, M10, M11 (33)

- [ ] T450 [P] Testes do `ExecutorAgregacao` (soma por chave, mês e conta; tolerância percentual + absoluta; materialidade; atenuação)
- [ ] T451 Implementar o `ExecutorAgregacao`
- [ ] T452 [P] Fixtures de ECD (I200/I250, I355) e ECF (L300, M300, P150...)
- [ ] T453 [P] Testes das 33 regras
- [ ] T454 Ligar as 33 regras; validadores à mão onde preciso
- [ ] T455 [H] Aprovar o bloco

## Fase 5 — CNT (6) — depende de T403

- [ ] T460 Contrato: ajuste para mês anterior conforme DP-4 (`lote.v1.1` compatível, se for o caso)
- [ ] T461 [P] Testes e `ExecutorContinuidade`
- [ ] T462 [H] Aprovar o bloco

## Fase 6 — M9 (16), M12 (5) — depende de T404

- [ ] T470 [P] Testes das 21 regras; as de evidência PVA passam a ler o resultado da origem (FR-409)
- [ ] T471 Ligar as regras; validadores à mão onde preciso
- [ ] T472 [H] Aprovar o bloco

## Fase 7 — M6 DIMP (8)

- [ ] T480 [P] Fixtures de DIMP; testes das 8 regras
- [ ] T481 Ligar as regras
- [ ] T482 [H] Aprovar o bloco

## Fase 8 — Consolidação

- [ ] T490 Pontos de controle C1 a C10 com `politicaQuebra` (FR-408)
- [ ] T491 Regras externas `INT-*` lidas de `validacaoOrigem.ocorrencias` (FR-409)
- [ ] T492 Lote completo sintético (8 tipos) consistente → zero inconsistências e cobertura de 100% das aplicáveis
- [ ] T493 Ampliar `eval/parecer` com o lote completo e um conjunto de divergências por bloco
- [ ] T494 Atualizar ADR D21, constitution (tabela de decisões) e `docs/cadastro-sofia.md` (capacidade e cobertura)
