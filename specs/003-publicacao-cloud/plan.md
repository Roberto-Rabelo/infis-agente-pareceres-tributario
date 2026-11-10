# Plano técnico 003 – Publicação cloud

**Spec:** [spec.md](spec.md) · **Matriz:** [contracts/matriz-compatibilidade.md](contracts/matriz-compatibilidade.md)

## Hosts

| Serviço | csproj | DLL | Dockerfile |
|---|---|---|---|
| Agente (A2A/REST) | `src/Api.Agente/Api.Agente.csproj` | `Api.Agente.dll` | `Dockerfile.vercel` (raiz) |
| RAG (MCP/pgvector) | `src/Api.RAG/Api.RAG.csproj` | `Api.RAG.dll` | `Dockerfile.vercel.rag` (service `rag`, prefixo `/rag`) |

Não há `.sln` na raiz. Compose local (`docker-compose.rag.yml`, `docker-compose.agente.yml`) permanece.

## Alterações mínimas de código

- `ResolvedorUrlA2A`: URL pública do card sem hardcode de localhost em Production.
- `LimitadorRequisicoes`: opcional (`RateLimiting:PermitLimitPorMinuto`, padrão 0).
- `AutenticacaoJwt` (agente e RAG): modo de teste por chave simétrica HS256 (`Autenticacao:ChaveSimetrica`, `Emissor`, `Audience`), usado só sem `Autenticacao:Authority`. Chave < 256 bits ou sem emissor/audience mantém a falha fechada. Tokens: `scripts/emitir-token.cs`.
- Api.RAG: `Hospedagem:PathBase` (`/rag` na imagem da Vercel), porque o service recebe o caminho original.
- Sem mudança de controllers, schemas ou motor de regras.

## Bancos

- **Neon:** `ConnectionStrings__Agente` (consumo LLM) e `ConnectionStrings__Rag` + extensão `vector` (já criada pelo `MigradorPostgres`).
- **Atlas:** não ligar. Memória conversacional não está no código (D9).

## Alternativa se o timeout da Vercel for insuficiente

Teste primário: parecer **ligado**. Se o Hobby recusar duração longa, o rollback operacional é outro host (Render Docker / App Service), não desligar o LLM.

## Verificação da Constitution

P12: Api.Agente não referencia Api.RAG (MCP HTTP). 5.1: Swagger continua desligado em Production. Segredos fora do Git.
