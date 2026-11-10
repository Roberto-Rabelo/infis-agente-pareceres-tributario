# Publicação na Vercel – roteiro passo a passo

Um projeto Vercel com **dois services** de container (`vercel.json`):

| Service | Dockerfile | Rotas públicas |
|---|---|---|
| `agente` | `Dockerfile.vercel` | `/health`, `/.well-known/agent-card.json`, `/a2a`, `/api/v1/analises` |
| `rag` | `deploy/Dockerfile.vercel.rag` | `/rag/health`, `/rag/mcp`, `/rag/api/v1/documentos` |

A Vercel entrega ao service o caminho original (`/rag/...`); a imagem da RAG liga `Hospedagem__PathBase=/rag`.
Os containers escutam na porta 80 (padrão da Vercel, que não injeta `PORT`). Duração por requisição: padrão de 300 s da plataforma; o `vercel.json` não define `maxDuration`.
As variáveis do projeto são compartilhadas pelos dois services (os nomes não colidem). Lista em [`.env.example`](../../.env.example).

**JWT:** sem IdP, o teste usa chave simétrica HS256 (`Autenticacao__ChaveSimetrica`, `__Emissor`, `__Audience`). Serve só para teste: segredo compartilhado, sem rotação nem revogação. Para produção, troque por `Autenticacao__Authority` (OIDC) e passe a obter o token da RAG por client credentials.

**Fora do teste (não implementado):** memória MongoDB (D9), cliente SOFIA, NFC-e.

## 1. Pré-requisitos locais

- `dotnet test` verde e build das imagens: `docker build --platform linux/amd64 -f Dockerfile.vercel .` e `-f deploy/Dockerfile.vercel.rag .`
- Vercel CLI: `npm i -g vercel` e `vercel login`.

## 2. Neon

1. Criar projeto Neon na mesma região da Vercel (padrão `iad1` → AWS `us-east-1`).
2. Criar os databases `agente` e `rag`. A RAG cria a extensão `vector` e o schema no primeiro boot; a Api.Agente cria `agente_consumo_llm`.
3. Copiar as connection strings do endpoint **direto** (sem `-pooler`) em formato Npgsql: `Host=...;Database=agente;Username=...;Password=...;SSL Mode=Require`.

## 3. Projeto Vercel e variáveis

1. `vercel link` na raiz (cria o projeto e define a URL `https://<projeto>.vercel.app`).
2. `sh scripts/preparar-vercel.sh https://<projeto>.vercel.app` → gera `.env.vercel` com a chave, os tokens e a OpenAI do `.env` local.
3. Preencher as duas connection strings do Neon em `.env.vercel`.
4. Cadastrar na Vercel (Production) todas as variáveis acima de "Só para uso local". As linhas `LOCAL_TOKEN_*` ficam só na sua máquina.

## 4. Deploy e verificação

1. `vercel deploy --prod` (ou push na `main` com o repositório ligado ao projeto).
2. `GET /health` e `GET /rag/health` → 200. `GET /.well-known/agent-card.json` → `url` = `A2A__Url`.
3. Ingerir a base com `LOCAL_TOKEN_RAG_ADMIN` em `POST /rag/api/v1/documentos`: `RAG_TOKEN_ADMIN=... python3 scripts/ingerir-base-regras.py https://<projeto>.vercel.app/rag` (ou o `.ps1` com `-ApiRag` igual).
4. `POST /api/v1/analises` e `POST /a2a` com `LOCAL_TOKEN_CLIENTE` e `exemplos/lote-exemplo.json`: parecer `CONCLUIDA`, `trechosCitados` > 0 e linha nova em `agente_consumo_llm` no Neon.

Custo OpenAI corre em cada análise com parecer e na ingestão. Hobby: uso pessoal/não comercial; confirmar antes de uso institucional (T15).

## Rollback

Reverter o commit e redeploy, ou promover o deploy anterior no dashboard. O compose local segue como caminho de desenvolvimento.
