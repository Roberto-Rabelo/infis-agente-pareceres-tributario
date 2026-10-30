# ADR D10 – Modelo do parecer e saída estruturada

- **Status:** Proposto. **Modelo indicado pelo responsável em 2026-10-29: OpenAI `gpt-4o-mini`** (`Parecer:Modelo`). A aprovação definitiva depende do resultado de T075 registrado no plano.
- **Data:** 2026-10-29
- **SDK:** `Microsoft.SemanticKernel` 1.81.0 (conector OpenAI)

## Decisão proposta

- **Orquestração:** Semantic Kernel com `IChatCompletionService`. Com `Parecer:Habilitado = false` (padrão), o envelope traz só o resultado determinístico e a limitação `Parecer por LLM desabilitado`. Com o parecer habilitado, a inicialização falha se faltar `OpenAI:ApiKey`, `Parecer:Modelo` ou `ConnectionStrings:Agente`.
- **Saída estruturada estrita:** o schema `contracts/saida/parecer.v1.schema.json` (sem `$schema`/`$id`) é enviado como `response_format` `json_schema` com `strict: true`. A resposta é validada de novo no servidor (schema + regras semânticas) e o resultado do provedor nunca é aceito sem essa validação (P9).
- **Contrato sem campos operacionais (P10):** execução, correlação, modelo, versões, tempos e consumo ficam no envelope (`versaoEnvelope` 1.1.0, `versaoContratoParecer`, `versaoPrompt`, `consumo[]`, `trechosCitados[]`). O teste `ContratoSaida_SemCamposOperacionais` garante isso.
- **Validação semântica (`ValidadorParecer`):** status igual ao do sistema; cada achado repetido uma única vez, com a mesma regra, natureza e severidade; hipóteses legítimas copiadas; nenhum achado novo (o que o modelo encontrar além disso vai para `indiciosIa`, sempre `ALERTA` e com trecho de apoio); toda referência é um `trechoId` recuperado nesta execução; revisão humana obrigatória quando o sistema a exige ou há indício da IA.
- **Prompt:** `prompts/parecer/v1.0.0.md` contém só regras de comportamento. Os dados vão em mensagem separada, delimitados por `<<<DADOS … DADOS>>>`, e são tratados como não confiáveis (P8). CNPJ mascarado e evidências sem valor bruto (minimização; D19 pendente).
- **Ferramenta controlada (P11):** uma única função, `base_conhecimento-consultar` (somente leitura, via MCP). Invocação automática desligada: o sistema confere a lista de permissão (`Parecer:FerramentasAutorizadas`), aplica o limite `Parecer:MaximoChamadasFerramenta` (3) e valida os argumentos (pergunta ≤ 500 caracteres, até 10 IDs no padrão do catálogo, códigos de obrigação conhecidos). Esgotado o limite, a próxima chamada ao modelo vai sem ferramentas.
- **Parâmetros de geração:** `Temperatura` e `MaximoTokensSaida` nulos (padrão do modelo) até existir avaliação que justifique outro valor.

## Pendências para aprovação

1. Registrar no plano o resultado de `AvaliacaoParecerTests.Avaliacao_ComModeloReal` (T075) com `gpt-4o-mini`, que suporta saída estruturada estrita e chamada de ferramenta.
2. Definir os critérios de aprovação do parecer no plano (proposta em `plan.md` §6).
3. D19 (dados enviados ao provedor) e política de retenção do provedor.
