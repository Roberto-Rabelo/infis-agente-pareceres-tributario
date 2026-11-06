# Teste primário na Vercel — todos os recursos implementados

`Parecer__Habilitado=false` era só um **atalho de risco** (timeout do Hobby). Para o teste que você quer, o parecer **fica ligado**. Sem `OpenAI__ApiKey` + `ConnectionStrings__Agente` a Api.Agente **não inicia**.

Este agente **não publica** na sua conta. Você importa o GitHub e cola as variáveis.

## O que entra no teste (já existe no código)

| Recurso | Como liga |
|---|---|
| Cruzamento REST `POST /api/v1/analises` | JWT + lote.v1 |
| A2A `message/send` + Agent Card | JWT no RPC; card anônimo |
| Parecer LLM | `Parecer__Habilitado=true` + OpenAI + Neon `Agente` |
| Fundamentação MCP | Api.RAG publicada + `BaseConhecimento__Endpoint` + token |
| Consumo de tokens | mesma connection `Agente` |
| CORS | só se houver frontend; SOFIA backend não precisa |
| Health | `GET /health` |

**Fora do teste (não implementado):** memória MongoDB (D9), cliente SOFIA, NFC-e.

## Dois projetos Vercel (obrigatório para RAG + parecer)

1. **Agente** — `Dockerfile.vercel` na raiz, env de `.env.example`. `vercel.json` pede `maxDuration` 300 s (Hobby pode capar; se a análise cortar, o log da Vercel mostra timeout).
2. **RAG** — no dashboard, Dockerfile = `deploy/Dockerfile.vercel.rag`, env de `deploy/.env.rag.example`. Depois copie a URL HTTPS para `BaseConhecimento__Endpoint=https://…/mcp`.

Neon: dois databases (`agente` e `rag`), SSL. A RAG cria `vector` sozinha na migração.

JWT: o mesmo issuer nos dois projetos. Token do agente precisa do papel que a RAG exige para `/mcp` (`rag.consulta`).

## Ordem

1. Neon (`agente` + `rag`).
2. Deploy RAG → `/health` 200.
3. Ingerir a base (`scripts/ingerir-base-regras.ps1` apontando para a URL da RAG), se ainda não houver trechos.
4. Deploy Agente com parecer **true** e endpoint MCP preenchido.
5. `GET /health`, `GET /.well-known/agent-card.json`, `POST /api/v1/analises` e `POST /a2a` com lote de exemplo e JWT.

Custo OpenAI corre em cada análise com parecer. Hobby: cold start + limite de duração; se 300 s não for aceito no plano, o parecer ainda está configurado — a plataforma que corta.
