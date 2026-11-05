# Feature 002 – Servidor A2A e CORS para a SOFIA

**Constitution:** 2.1.0 · **Relacionada:** US8 de `specs/001-cruzamento-obrigacoes`

## Objetivo

Expor este agente como **servidor A2A v0.3.x** para o orquestrador SOFIA, reusando o cruzamento REST (`lote.v1`). CORS só para origens de navegador autorizadas.

## Fora de escopo

Cliente A2A da SOFIA, registry, dispatch paralelo, agregação, parser de XML/SPED, NFC-e, nova rota REST de análise.

## Requisitos

- RF01: Agent Card em `GET /.well-known/agent-card.json` com identidade, skill `analisar_documentos` e `url` do RPC.
- RF02: `POST /a2a` JSON-RPC `message/send` com TextPart contendo lote.v1 ou `SolicitacaoAnaliseSofia` (lote em `documentos[].texto`). Rejeitar documento sem texto/identificador no envelope SOFIA.
- RF03: Artifact `ResultadoAnaliseSofia` com status, análise, achados e IDs ecoados.
- RF04: Preservar `idExecucao` e `idSolicitacao`; distinguir lote inválido (`status: erro`) de cruzamento sem achados (`concluido`/`parcial`).
- RF05: Este agente não chama outros agentes.
- RNF01: JWT no RPC; logs sem lote, textos fiscais nem `usuario.nome`.
- RNF03: `versaoContrato` SOFIA `1.0`; lote `1.x.x`.

## CORS

- Origem permitida: preflight ecoa `Access-Control-Allow-Origin`.
- Origem desconhecida: sem permissão CORS para ela.
- Sem `Origin`: pipeline normal (rede + JWT).

## Aceite

1. Card público descreve capacidades e aponta `/a2a`.
2. RPC sem JWT → 401.
3. Mesmo lote REST e A2A → achados determinísticos idênticos.
4. Envelope SOFIA sem texto no documento → Artifact `status: erro`.
