# 05 – Modelo de Regras e Contrato do Agente

Proposta de modelo para transformar os arquivos 02 e 03 em **catálogo de regras versionado** consumido pelo agente de parecer.

## 1. Princípios

1. **Regra é dado, não código.** O catálogo (JSON/YAML) fica versionado; o motor (.NET) apenas interpreta. Leiautes mudam todo ano.
2. **Toda regra carrega a sua base** (`evidencia`, `fonte`, `versaoLeiaute`). O agente LLM não cria regra nova: só aplica o catálogo e redige o parecer.
3. **Cálculo determinístico fora do LLM.** Somatórios, *matching* por chave e comparação de valores rodam no motor; o LLM recebe as **ocorrências** e as hipóteses legítimas para redigir o parecer e pedir justificativas.
4. **Vigência por período.** Cada regra tem `vigenciaInicio`/`vigenciaFim` e versões de leiaute aplicáveis.
5. **Regras [DERIVADA] nunca concluem sozinhas** por irregularidade: o parecer deve listar `hipotesesLegitimas`.

## 2. Esquema da regra (JSON)

```json
{
  "id": "CRZ-NFE-EFD-002",
  "titulo": "NF-e cancelada escriturada como regular (ou o inverso)",
  "origem": { "obrigacao": "NFE", "elemento": "evento 110111" },
  "destino": { "obrigacao": "EFD_ICMS_IPI", "elemento": "C100.COD_SIT" },
  "condicao": "nfe.eventos.contains(110111) <=> efd.C100[CHV_NFE].COD_SIT in ('02','03')",
  "evidencia": "MANUAL",
  "fonte": [
    { "documento": "Guia Prático EFD ICMS/IPI", "versao": "3.2.2", "secao": "Registro C100" }
  ],
  "severidade": "ESCRITURACAO_GRAVE",
  "hipotesesLegitimas": [],
  "vigenciaInicio": "2026-01-01",
  "vigenciaFim": null,
  "versoesLeiaute": { "EFD_ICMS_IPI": ["020"] },
  "parametros": { "tolerancia": null },
  "mensagemParecer": "A NF-e {chave} possui evento de cancelamento homologado em {dataEvento}, porém foi escriturada com COD_SIT {codSit} no período {periodo}."
}
```

## 3. Modelo de domínio sugerido (.NET)

```csharp
public enum Evidencia { Pva, Manual, Norma, Derivada }
public enum Severidade { EstruturaGrave, EscrituracaoGrave, Alerta, Info }
public enum Obrigacao { NFe, NFCe, NFSe, Dimp, EfdIcmsIpi, EfdContribuicoes, Ecd, Ecf }

public sealed record FonteOficial(string Documento, string Versao, string Secao);

public sealed record Regra(
    string Id,
    string Titulo,
    Obrigacao Origem,
    Obrigacao? Destino,
    Evidencia Evidencia,
    Severidade Severidade,
    IReadOnlyList<FonteOficial> Fontes,
    IReadOnlyList<string> HipotesesLegitimas,
    DateOnly VigenciaInicio,
    DateOnly? VigenciaFim,
    IReadOnlyDictionary<Obrigacao, string[]> VersoesLeiaute);

public sealed record Ocorrencia(
    string RegraId,
    Severidade Severidade,
    string Periodo,                 // AAAA-MM ou AAAA
    string Estabelecimento,         // CNPJ alfanumérico (string)
    string? ChaveDocumento,
    decimal? ValorOrigem,
    decimal? ValorDestino,
    IReadOnlyDictionary<string, string> Contexto);
```

## 4. Contrato de entrada do agente de parecer

O agente de parecer recebe dos agentes anteriores (validação/extração):

| Insumo | Conteúdo mínimo |
|---|---|
| Metadados por obrigação | CNPJ, período, versão de leiaute, recibo/hash, original/retificadora |
| Ocorrências | Lista de `Ocorrencia` geradas pelo motor de regras |
| Relatório do PVA | Erros e advertências (texto original) |
| Cobertura | Quais obrigações/períodos foram efetivamente recebidos (para não concluir “omissão” por falta de insumo) |

## 5. Estrutura do parecer (saída)

1. Identificação (contribuinte, período, obrigações analisadas e versões de leiaute).
2. Escopo e limitações (obrigações/períodos não recebidos; regras não aplicadas por falta de insumo).
3. **Erros graves de estrutura** (`ESTRUTURA_GRAVE`) – invalidam conclusões dependentes.
4. **Erros graves de escrituração** (`ESCRITURACAO_GRAVE`) – com valor, documento e fonte normativa.
5. **Alertas de atenção** (`ALERTA`) – com as hipóteses legítimas a serem confirmadas pelo contribuinte.
6. Informativos.
7. Fontes oficiais citadas (documento, versão, seção).

## 6. Regras de precedência

- Se existe `ESTRUTURA_GRAVE` em uma obrigação, regras de cruzamento que dependam dela são marcadas como **não conclusivas**.
- Ausência de insumo ≠ omissão: só reportar “documento não escriturado” quando a escrituração do período **foi recebida**.
- Divergência temporal (mês seguinte) rebaixa para `ALERTA` quando a regra prevê essa hipótese.
