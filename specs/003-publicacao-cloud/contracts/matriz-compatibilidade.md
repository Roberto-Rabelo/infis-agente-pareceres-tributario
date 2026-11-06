# Matriz de compatibilidade – Vercel Hobby

| Item | Situação | Notas |
|---|---|---|
| ASP.NET Core / net10.0 | OK | `Api.Agente.dll`, `Api.RAG.dll` |
| Sem `.sln` | OK | restore no csproj |
| Sem `IHostedService` / WebSocket | OK | |
| Estado só em memória | OK | catálogo copiado para a imagem |
| `NpgsqlDataSource` singleton | OK | reuso de conexão |
| `/health` | OK | anônimo |
| Agent Card + A2A JSON-RPC | OK | JWT no RPC; card anônimo |
| CORS | OK | só se `Cors:AllowedOrigins` |
| Swagger em Production | OK | já bloqueado |
| Postgres consumo (`ConnectionStrings:Agente`) | Adaptação | Neon + `SSL Mode=Require` |
| RAG pgvector (`ConnectionStrings:Rag`) | Adaptação | **outro** container/projeto; MCP HTTP |
| Parecer LLM (timeout 60s + retentativas) | OK no teste primário | `Parecer__Habilitado=true` + OpenAI + Neon. `vercel.json` `maxDuration` 300 s; Hobby pode capar abaixo disso |
| Lote até 50 MB | Adaptação | limite de body da Vercel pode ser menor |
| MongoDB Atlas | Bloqueio funcional | memória D9 **não implementada**; não criar variável inventada |
| Worker / disco durável | OK (ausente) | |
| Plano Hobby institucional INFIS | Bloqueio de política | confirmar termos (pessoal/não comercial) |

## Rollback

1. Reverter o commit de `Dockerfile.vercel` / env na Vercel.
2. Redeploy da imagem anterior no dashboard.
3. Compose local continua sendo o caminho de desenvolvimento (`docker-compose.rag.yml`).
