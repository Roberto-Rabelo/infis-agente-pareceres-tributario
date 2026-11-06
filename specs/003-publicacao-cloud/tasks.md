# Tarefas 003

## Automatizáveis (repo)

- [x] T01 Matriz de compatibilidade
- [x] T02 `Dockerfile.vercel` + `.dockerignore`
- [x] T03 `.env.example` com nomes reais de `IConfiguration`
- [x] T04 Resolver URL do Agent Card; rate limit opcional
- [x] T05 Testes de URL e regressão A2A/REST
- [x] T06 Workflow CI (`dotnet test`), sem `vercel --prod`

## Manuais (você executa; este agente não cria conta nem publica)

- [ ] T10 `dotnet test` e, se Docker local existir, `docker build -f Dockerfile.vercel -t agente-parecer .`
- [ ] T11 Criar projeto Neon Free, `SSL Mode=Require`, opcional `CREATE EXTENSION vector` no banco RAG
- [ ] T12 Importar o GitHub na Vercel Hobby, Root Directory = raiz do repo, Dockerfile = `Dockerfile.vercel`
- [ ] T13 Cadastrar env de `.env.example` **com** `Parecer__Habilitado=true`, OpenAI, Neon Agente, JWT e MCP da RAG
- [ ] T14 GET `/health` e Agent Card; POST REST e A2A **com parecer** no envelope; conferir consumo no Neon
- [ ] T15 Confirmar Hobby vs uso institucional INFIS; rollback = revert do Git + redeploy anterior
