# Cadastro do agente no catálogo A2A da SOFIA

Dados para o responsável pela SOFIA incluir este agente em `A2A:Agents` (skill `sofia-a2a-autenticacao`). A chave não registra o agente sozinha: a SOFIA só descobre agentes cadastrados.

**Situação:** cadastro central **pendente** (feito pelo professor/responsável pela SOFIA).

## Objeto para `A2A:Agents`

Adicionar à lista e preservar os demais agentes. `CredentialRef` e `DiscoveryCredentialRef` recebem o **nome** da variável, não a chave.

```json
{
  "IdAgente": "agente-parecer-tributario",
  "ExpectedName": "Agente Parecer Tributário",
  "Enabled": true,
  "CardUri": "https://agente-parecer-tributario.vercel.app/.well-known/agent-card.json",
  "AllowedDestinations": [
    "https://agente-parecer-tributario.vercel.app/.well-known/agent-card.json",
    "https://agente-parecer-tributario.vercel.app/a2a"
  ],
  "AllowedAddresses": [
    "PENDENTE: a Vercel não publica IP fixo; definir com o responsável pela SOFIA"
  ],
  "AllowedCapabilities": [
    "analisar_documentos"
  ],
  "CredentialRef": "SOFIA_A2A_TOKEN",
  "DiscoveryCredentialRef": "SOFIA_A2A_TOKEN"
}
```

- `ExpectedName` precisa ser **idêntico** ao `name` do Agent Card (`A2A:Nome`), com acento.
- `agente.idAgente` no `ResultadoAnaliseSofia` é `agente-parecer-tributario` (`A2A:IdAgente`).
- **IP:** a Vercel atende por IPs de borda compartilhados e dinâmicos. Validar com o responsável pela SOFIA como a política de destino (`AllowedAddresses`/DNS) trata esse caso, sem desativar a proteção.

## Configuração do agente

| Onde | Variável | Valor |
|---|---|---|
| Vercel (Production) | `SOFIA_A2A_TOKEN` | chave compartilhada do treinamento (segredo, nunca versionado) |
| Vercel (Production) | `A2A__Url` | `https://agente-parecer-tributario.vercel.app/a2a` |
| Local / compose | `SOFIA_A2A_TOKEN` | mesma chave, no `.env` local (gitignored) |

Sem `SOFIA_A2A_TOKEN` (ou com menos de 16 caracteres), o card e o `/a2a` respondem 503: a integração fica indisponível, nunca aberta.

## Verificação

```bash
curl -i https://agente-parecer-tributario.vercel.app/.well-known/agent-card.json
```
Sem chave: `401` com `WWW-Authenticate: Bearer`.

```bash
curl -s -H "Authorization: Bearer $SOFIA_A2A_TOKEN" https://agente-parecer-tributario.vercel.app/.well-known/agent-card.json
```
Com chave: `200` e o card (`protocolVersion 0.3.0`, `preferredTransport JSONRPC`, modos `text/plain`).

## Limitação de conteúdo

Este agente cruza obrigações já validadas e espera em `documentos[].texto` o JSON do lote `lote.v1` (ver `docs/contrato-entrada-orquestrador.md`). Documentos com outro conteúdo (por exemplo, XML de NF-e extraído) retornam `status: "erro"` com `LOTE_AUSENTE`, numa Task `completed`.
