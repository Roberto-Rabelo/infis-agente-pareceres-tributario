# Contrato de entrada para o orquestrador

Este agente **não lê XML nem SPED**. O orquestrador monta um JSON com as obrigações que os agentes de origem já validaram (layout/PVA) e envia `POST /api/v1/analises`.

Schema: [`contracts/entrada/lote.v1.schema.json`](../contracts/entrada/lote.v1.schema.json).  
Exemplo da requisição (Swagger e `.http`): [`exemplos/lote-exemplo.json`](../exemplos/lote-exemplo.json).  
Casos negativos (reprovado, omissão, etc.): [`exemplos/cenarios/`](../exemplos/cenarios/).

## 1. HTTP

| Item | Valor |
|---|---|
| Método / rota | `POST /api/v1/analises` |
| Auth | `Authorization: Bearer <token>` |
| Corpo | JSON do lote (sem campos extras; desserialização estrita) |
| `X-Correlation-Id` | **Header opcional**, não vai no JSON. `1–64` caracteres `[A-Za-z0-9_-]`. Se faltar ou for inválido, a API gera um GUID e devolve o mesmo header na resposta. Serve para logs, MCP/RAG e registro de consumo do LLM. Cada request ganha também um `execucaoId` novo. |

O lote **não é persistido**. Só o consumo do LLM (quando o parecer está ligado) vai para o Postgres do agente.

## 2. Envelope (sempre)

```json
{
  "versaoContrato": "1.0.0",
  "loteId": "opcional-ate-100-chars",
  "contribuinte": { "cnpj": "11222333" },
  "periodo": { "inicio": "2026-03-01", "fim": "2026-03-31" },
  "obrigacoes": [ ]
}
```

| Campo | Obrigatório | Regra |
|---|---|---|
| `versaoContrato` | sim | `1.x.x` |
| `loteId` | não | Identificador do orquestrador |
| `contribuinte.cnpj` | sim | Raiz (8) ou CNPJ completo (14, com DV) |
| `periodo.inicio` / `fim` | sim | ISO `AAAA-MM-DD`; fim ≥ início |
| `obrigacoes` | sim | Mínimo 1, máximo `Entrada:MaximoObrigacoesPorLote` (500). Só entram os arquivos **que existirem** naquele contribuinte/período |

Tipo ausente no array = arquivo não enviado = **ausência de evidências** nos cruzamentos que precisarem dele. Nunca omissão do contribuinte.

## 3. Cada obrigação (quando o arquivo existir)

Campos comuns (todos obrigatórios no item):

`id`, `tipo`, `versaoLayout`, `contribuinte` (`cnpj` 14 posições com DV; `inscricaoEstadual` e `uf` quando couber), `periodo`, `finalidade` (`ORIGINAL` ou `SUBSTITUTA`), `identificacao`, `validacaoOrigem`, `registros`.

`id` único no lote. `registros` é um mapa: chave = código do registro SPED (`0000`, `C100`, …) ou grupo do XML (`ide`, `emit`, `ICMSTot`, `evento`, …). Cada ocorrência tem `linha` (número no arquivo, ou `null` no XML) e `campos` (valores **sempre texto**: datas `AAAA-MM-DD`, decimais com ponto, CNPJ sem máscara).

### `validacaoOrigem` (o que o agente de origem já fez)

Para a obrigação **cruzar**, precisa `status: "APROVADO"` e nenhuma ocorrência `tipo: "ERRO"`. Advertências (`ADVERTENCIA`) não derrubam a aptidão; o agente as ecoa.

`REPROVADO` ou erro de PVA → PRE-003: obrigação **não apta**. O lote inteiro **não** é rejeitado.

### `identificacao`

| Tipo | O que enviar |
|---|---|
| `NFE`, `NFCE`, `NFSE` | `chaveAcesso` com **44** caracteres `[0-9A-Z]` |
| `EFD_ICMS_IPI`, `EFD_CONTRIBUICOES`, `ECD`, `ECF`, `DIMP` | `recibo` e/ou `hash` (pelo menos um). `reciboAnterior` se `finalidade` = `SUBSTITUTA` |

## 4. Escopo deste agente — sete obrigações, nenhum NFC-e

O orquestrador envia **somente**:

| Arquivo | `tipo` | `versaoLayout` |
|---|---|---|
| NF-e | `NFE` | `4.00` |
| NFS-e Padrão Nacional | `NFSE` | `1.01` |
| SPED Fiscal | `EFD_ICMS_IPI` | `020` (2026) |
| SPED Contribuições | `EFD_CONTRIBUICOES` | `006` |
| DIMP | `DIMP` | `09` até 31/05/2026; `10` a partir de 01/06/2026 |
| ECD | `ECD` | `9.00` |
| ECF | `ECF` | `012` |

NFC-e **não entra** neste produto. O exemplo `exemplos/lote-exemplo.json` tem exatamente esses sete tipos (duas NF-e: saída e entrada, para o M1).

A SOFIA descobre o agente em `GET /.well-known/agent-card.json` e envia o **mesmo JSON de lote** no TextPart de `POST /a2a` (`message/send`, JWT). O envelope SOFIA v1 também é aceito, com o lote em `documentos[].texto`. CORS não é necessário nessa chamada servidor-servidor.

Só o par **NF-e × EFD ICMS/IPI** tem validador hoje (M1). Os demais tipos, se APROVADOS e com o layout da tabela, ficam **aptos** em `cobertura` e aguardam M2–M12.

## 5. Pré-condições (aptidão)

| Id | Se falhar |
|---|---|
| PRE-001 | Contrato da obrigação (CNPJ, chave 44, recibo/hash, tipos) |
| PRE-002 | Não recusa. Layout já veio validado da origem/orquestrador |
| PRE-003 | Sem relatório, `REPROVADO` ou ocorrência `ERRO` |
| PRE-004 | Escrituração substituída pela vigente; documento duplicado pela chave |
| PRE-005 | CNPJ-raiz diferente do lote, ou período sem sobreposição com o lote |

Obrigação inapta **não** entra no motor. O lote segue com as demais.

## 6. Registros de ligação (preencher quando o arquivo tiver o dado)

Sem esses campos o motor **não presume** omissão: gera `AUSENCIA_EVIDENCIAS` no cruzamento correspondente.

| Tipo | Grupos / registros mínimos para cruzar de verdade |
|---|---|
| `NFE` | `ide` (`mod`, `serie`, `nNF`, `dhEmi`, `tpNF`), `emit` (`CNPJ`, `UF`), `dest` (`CNPJ`), `ICMSTot` (`vNF`, `vICMS`, `vBC`). `evento.tpEvento` = `110111` se cancelada. `pag` / `detPag` se houver DIMP |
| `NFCE` | Mesmo núcleo da NF-e; `mod` = `65` |
| `NFSE` | Identificação DPS (`dhEmi`, `dCompet`, valores, prestador/tomador); liga com EFD-Contribuições `A100` |
| `DIMP` | Totais e participantes (instituição/intermediador); liga com NF-e `pag` e EFD `1601` |
| `EFD_ICMS_IPI` | `0000`, `0150`, `C100` (`CHV_NFE`, `COD_SIT`, `IND_OPER`, `IND_EMIT`, `VL_DOC`, `VL_ICMS`, …). `C170` / `C190` / `C197` / `1601` conforme o cruzamento |
| `EFD_CONTRIBUICOES` | `0000`, `A100` (NFS-e), `C100`/`C180` (NF-e) |
| `ECD` | `I010`, `I050` / `I155` (saldos); hash recuperado na ECF |
| `ECF` | Bloco C (hash da ECD), bases IRPJ/CSLL |

Detalhamento oficial dos campos: [`01-panorama-obrigacoes.md`](01-panorama-obrigacoes.md).

## 7. O que o motor faz hoje

Só regras **M1** (NF-e × EFD ICMS/IPI) em `catalog/regras.json` + `ValidadoresNfeEfd`. M2–M12, DIMP, ECD×ECF e NFS-e ainda são catálogo de estudo (`docs/regras/`), não validador.

O LLM **não** monta o lote nem decide o cruzamento. Ele só redige o parecer se `Parecer:Habilitado` estiver ligado, a partir dos achados do motor.

## 8. Resposta (o que olhar)

No envelope 1.1.0:

- `resultado.status` — `CONCLUIDA`, `CONCLUIDA_PARCIAL`, `INCONCLUSIVA`, `FALHA_TECNICA` (nenhum significa “empresa regular”)
- `resultado.aptidao` / `cobertura` — o que entrou e o que ficou de fora
- `resultado.achados` / `regras` — inconsistência vs ausência de evidências
- `resultado.revisaoHumana`
- `parecer` — só se o LLM estiver habilitado
- header `X-Correlation-Id` ecoado
