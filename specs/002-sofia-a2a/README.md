# Integração SOFIA (servidor A2A)

Este agente **não** é a SOFIA. O orquestrador descobre o card e envia `lote.v1` (como `exemplos/lote-exemplo.json`) no TextPart A2A.

## Testes

```bash
dotnet test tests/Api.Agente.Tests/Api.Agente.Tests.csproj --filter "FullyQualifiedName~A2A|FullyQualifiedName~Cors|FullyQualifiedName~AnaliseEndpointTests"
```

## Verificação manual

1. Subir a Api.Agente (`http://localhost:5117`).
2. Pedidos da seção 5 de `fluxo-analise.http` (card, OPTIONS, `message/send`).
3. De outro container na rede `agentes-network`: `http://agente-parecer:8080/.well-known/agent-card.json` (não usar `localhost` do host).
