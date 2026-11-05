# Plano técnico 002 – A2A / CORS

**Spec:** [spec.md](spec.md) · **Tasks:** [tasks.md](tasks.md) · **ADR:** [D14](../../docs/adr/D14-a2a.md)

## Verificação da Constitution

P12 (fronteiras): servidor A2A na Api.Agente; sem cliente SOFIA. P10: IDs e envelope operacional montados pelo sistema. P8: texto do documento/lote é dado. P17: testes sem LLM. 5.1: card anônimo; RPC autenticado.

## Implementação

- `ServicoExecucaoAnalise` compartilhado por REST e A2A.
- JSON-RPC próprio (NuGet `A2A.V0_3` só em preview).
- `Cors:AllowedOrigins` vazio = CORS desligado.
- Compose: `agente-parecer` na rede externa `agentes-network`; SOFIA usa `http://agente-parecer:8080`.

## Verificação

`dotnet test tests/Api.Agente.Tests` (filtros `A2A`, `Cors`, `AnaliseEndpoint`). Curls em `fluxo-analise.http` seção 5. Sem afirmação de E2E contra SOFIA real.
