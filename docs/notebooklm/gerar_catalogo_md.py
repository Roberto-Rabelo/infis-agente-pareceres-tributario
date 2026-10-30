import json, glob, os, sys

base, out = sys.argv[1], sys.argv[2]
fontes = {f['id']: f for f in json.load(open(os.path.join(base, 'fontes.json'), encoding='utf-8'))['fontes']}
params = json.load(open(os.path.join(base, 'parametros.json'), encoding='utf-8'))['parametros']
cad = json.load(open(os.path.join(base, 'cadeias.json'), encoding='utf-8'))

NOMES = {"NFE": "NF-e/NFC-e", "NFCE": "NFC-e", "NFSE": "NFS-e", "DIMP": "DIMP", "EFD_ICMS_IPI": "EFD ICMS/IPI",
         "EFD_CONTRIB": "EFD-Contribuições", "ECD": "ECD", "ECF": "ECF", "EXTERNO": "Tabela externa"}
REL = {"PRE_DIRETA": "Presença direta (existe em A ⇒ deve existir em B; falta = omissão)",
       "PRE_REVERSA": "Presença reversa (existe em B ⇒ deve ter origem em A; falta = sem lastro)",
       "IDT": "Identidade (mesmo fato, mesmo valor)", "AGR": "Agregação (soma de vários registros = um valor)",
       "LIM": "Limite (um lado não pode exceder o outro)", "CNT": "Continuidade (saldo final = saldo inicial seguinte)",
       "PAR": "Compatibilidade de parâmetros de regime", "EXC": "Exclusividade legítima (filtro aplicado antes)"}
EVID = {"PVA": "validador oficial", "MANUAL": "texto do Guia/Manual/NT", "NORMA": "lei/IN/convênio",
        "DERIVADA": "dedução a partir da definição oficial dos campos (não é regra cruzada publicada)"}

L = []
w = L.append
w("# Catálogo de Regras de Coesão – versão legível")
w("")
w("> Fonte gerada automaticamente a partir de `docs/regras/*.json` (catálogo v1.0.0) para leitura humana e uso no NotebookLM. "
  "Em caso de divergência, o JSON é a referência. Cada regra tem um ID único (ex.: M1-P01) que deve ser citado nas respostas.")
w("")
w("## Como ler uma regra")
w("")
w("- **Origem → Destino**: obrigação e registro/campo de cada lado.")
w("- **Tipo de relação**: o que está sendo verificado (presença, identidade, soma, limite, continuidade, regime, exclusão).")
w("- **Severidade**: ESTRUTURA_GRAVE > ESCRITURACAO_GRAVE > ALERTA > INFO. A *severidade atenuada* se aplica quando a condição indicada é atendida.")
w("- **Evidência**: PVA, MANUAL, NORMA ou DERIVADA. Regras DERIVADAS nunca concluem irregularidade sem avaliar as hipóteses legítimas.")
w("- **Hipóteses legítimas**: explicações aceitáveis que tornam a divergência regular e devem ser perguntadas ao contribuinte.")
w("- **Filtros de exclusão**: regras EXC aplicadas antes, que retiram do universo fatos que legitimamente não precisam aparecer.")
w("- **Depende de**: regras que precisam ter passado; se falharam, esta é reportada como NÃO CONCLUSIVA.")
w("- **Campo a conferir**: o nome/código do campo ainda precisa ser confirmado no PDF oficial antes de produção.")
w("")

for arq in sorted(glob.glob(os.path.join(base, '0*.json'))):
    d = json.load(open(arq, encoding='utf-8'))
    w(f"## Grupo {d['grupo']} – {len(d['regras'])} regras")
    w("")
    w(d['descricao'])
    w("")
    for r in d['regras']:
        w(f"### {r['id']} – {r['titulo']}")
        w("")
        o = r['origem']
        w(f"- **Mapa/seção do núcleo:** {r.get('mapa', '-')}")
        w(f"- **Tipo de relação:** {r['tipoRelacao']} – {REL[r['tipoRelacao']]}")
        w(f"- **Origem:** {NOMES[o['obrigacao']]} → `{o['elemento']}`" + (f" (universo: {o['filtro']})" if o.get('filtro') else "") + (f" · agregação: {o['agregacao']}" if o.get('agregacao') else ""))
        if 'destino' in r:
            de = r['destino']
            w(f"- **Destino:** {NOMES[de['obrigacao']]} → `{de['elemento']}`" + (f" (universo: {de['filtro']})" if de.get('filtro') else "") + (f" · agregação: {de['agregacao']}" if de.get('agregacao') else ""))
        if r.get('chaveJuncao'):
            w(f"- **Chave de junção:** {', '.join(r['chaveJuncao'])}")
        w(f"- **Operador:** {r['operador']}" + (f" · tolerância: {r['tolerancia']}" if r.get('tolerancia') else (" · sem tolerância (valor exato)" if r['operador'] in ('IGUAL','SOMA_IGUAL','MENOR_OU_IGUAL','MAIOR_OU_IGUAL','SALDO_FINAL_IGUAL_INICIAL') else "")))
        if r.get('condicaoDescritiva'):
            w(f"- **Condição:** {r['condicaoDescritiva']}")
        if r.get('aplicavelSe'):
            w(f"- **Aplicável se:** {r['aplicavelSe']}")
        w(f"- **Severidade:** {r['severidade']}" + (f" · atenuada para {r['severidadeAtenuada']['severidade']} quando: {r['severidadeAtenuada']['quando']}" if r.get('severidadeAtenuada') else ""))
        w(f"- **Evidência:** {r['evidencia']} ({EVID[r['evidencia']]})")
        w(f"- **Fontes:** " + "; ".join(f"{fontes[f]['documento']}" + (f" v{fontes[f]['versao']}" if fontes[f].get('versao') else "") for f in r['fontes']))
        if r['hipotesesLegitimas']:
            w(f"- **Hipóteses legítimas:** " + " | ".join(r['hipotesesLegitimas']))
        if r.get('filtrosExclusao'):
            w(f"- **Filtros de exclusão aplicados antes:** {', '.join(r['filtrosExclusao'])}")
        if r.get('dependeDe'):
            w(f"- **Depende de:** {', '.join(r['dependeDe'])}")
        if r.get('cadeias'):
            w(f"- **Cadeias de valor:** {', '.join(r['cadeias'])}")
        w(f"- **Vigência a partir de:** {r['vigenciaInicio']}")
        if r.get('conferirCampo'):
            w("- **Campo a conferir no PDF oficial:** sim")
        w(f"- **Mensagem do parecer:** \"{r['mensagemParecer']}\"")
        w("")

w("## Cadeias de valor e pontos de controle")
w("")
w(cad['descricao'])
w("")
for c in cad['cadeias']:
    w(f"### {c['id']} – {c['nome']}" + (f" (aplicável se {c['aplicavelSe']})" if c.get('aplicavelSe') else ""))
    w("")
    for cp in c['pontosControle']:
        w(f"- **{cp['cp']}** – {cp['descricao']}: {', '.join(cp['regras'])}")
    w("")
w("## Ordem de execução")
w("")
for p in cad['ordemExecucao']:
    w(f"{p['passo']}. {p['nome']}" + (f" (regras {', '.join(p['prefixos'])})" if p['prefixos'] else ""))
w("")
w(f"Política de quebra: {cad['politicaQuebra']['cpQuebrado']} {cad['politicaQuebra']['cpsSeguintes']} {cad['politicaQuebra']['regraNaoExecutadaPorFaltaDeInsumo']}")
w("")
w("## Parâmetros do projeto (sem valor definido – não há norma oficial)")
w("")
for p in params:
    w(f"- **{p['nome']}** ({p['tipo']}): {p['descricao']}")
w("")
w("## Fontes oficiais")
w("")
for f in fontes.values():
    w(f"- **{f['id']}** – {f['documento']}" + (f" (versão {f['versao']})" if f.get('versao') else "") +
      f" – {'verificada em fonte primária em 29/10/2026' if f['verificadoEmFontePrimaria'] else 'conferir texto vigente'}" + (f" – {f['url']}" if f.get('url') else ""))
open(out, 'w', encoding='utf-8').write("\n".join(L) + "\n")
print("ok", len(L))
