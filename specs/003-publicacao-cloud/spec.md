# Feature 003 – Publicação experimental (Vercel Hobby)

**Constitution:** 2.1.0 · **Skill:** `sofia-devops-free-cloud`

## Objetivo

Preparar a **Api.Agente** (servidor A2A/REST já existente) para hospedagem experimental em Vercel Hobby via **Container Runtime**, com Postgres de consumo no **Neon Free**. Não recriar a API nem alterar contratos `lote.v1` / A2A v0.3.x.

## Fora de escopo

Deploy real, criação de contas, MongoDB (D9 ainda pendente), reescrever RAG, workers, WebSockets.

## Requisitos

- RF-P01: Imagem HTTP a partir de `src/Api.Agente/Api.Agente.csproj` → `Api.Agente.dll`, escutando `0.0.0.0:${PORT:-80}` (a Vercel roteia para a 80 e não injeta `PORT`).
- RF-P02: `/health` anônimo; Agent Card anônimo com `url` apontando ao RPC (configurado ou derivado do Host).
- RF-P03: Segredos só em variáveis de ambiente (`ConnectionStrings__Agente`, `OpenAI__ApiKey`, `Autenticacao__*`).
- RF-P04: Neon com TLS; migração existente `agente_consumo_llm` (não inventar schema RAG neste serviço).
- RF-P05: Teste primário liga cruzamento, A2A, parecer LLM, MCP/RAG e consumo. Api.RAG é o segundo projeto Vercel. Timeout da plataforma pode cortar a request; isso não desliga o parecer.

## Limites “gratuitos”

Vercel Hobby: function serverless (cold start, timeout, memória), uso pessoal/não comercial. Neon Free: pausa e cotas. OpenAI: custo à parte. Atlas: só quando a memória Mongo existir.
