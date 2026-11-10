# Tarefas 003

## Automatizáveis (repo)

- [x] T01 Matriz de compatibilidade
- [x] T02 `Dockerfile.vercel` + `.dockerignore`
- [x] T03 `.env.example` com nomes reais de `IConfiguration`
- [x] T04 Resolver URL do Agent Card; rate limit opcional
- [x] T05 Testes de URL e regressão A2A/REST
- [x] T06 Workflow CI (`dotnet test`), sem `vercel --prod`
- [x] T07 Porta 80 por padrão nos Dockerfiles da Vercel; `vercel.json` em modo services (`agente` em `/`, `rag` em `/rag`)
- [x] T08 JWT por chave simétrica (teste sem IdP), `scripts/emitir-token.cs` e `scripts/preparar-vercel.sh`

## Manuais (você executa; este agente não cria conta nem publica)

- [ ] T10 `dotnet test` e, se Docker local existir, `docker build -f Dockerfile.vercel -t agente-parecer .`
- [ ] T11 Criar projeto Neon Free com databases `agente` e `rag`, `SSL Mode=Require`
- [ ] T12 `vercel link` na raiz (projeto único, services do `vercel.json`)
- [ ] T13 Gerar `.env.vercel` (`scripts/preparar-vercel.sh`), preencher Neon e cadastrar as variáveis
- [ ] T14 GET `/health` e Agent Card; POST REST e A2A **com parecer** no envelope; conferir consumo no Neon
- [ ] T15 Confirmar Hobby vs uso institucional INFIS; rollback = revert do Git + redeploy anterior
