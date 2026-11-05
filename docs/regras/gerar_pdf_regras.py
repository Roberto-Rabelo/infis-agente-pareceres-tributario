import glob, html, json, os, subprocess, sys
from collections import Counter

raiz = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
base = os.path.join(raiz, 'docs', 'regras')
saida_pdf = sys.argv[1] if len(sys.argv) > 1 else os.path.join(base, 'catalogo-regras.pdf')
saida_html = os.path.splitext(saida_pdf)[0] + '.html'


def carregar(caminho):
    with open(caminho, encoding='utf-8') as f:
        return json.load(f)


fontes = {f['id']: f for f in carregar(os.path.join(base, 'fontes.json'))['fontes']}
motor = carregar(os.path.join(raiz, 'catalog', 'regras.json'))
implementadas = {r['id'] for r in motor['regras'] if r.get('tipo') == 'CRUZAMENTO'}

NOMES = {"NFE": "NF-e/NFC-e", "NFCE": "NFC-e", "NFSE": "NFS-e", "DIMP": "DIMP", "EFD_ICMS_IPI": "EFD ICMS/IPI",
         "EFD_CONTRIB": "EFD-Contribuições", "ECD": "ECD", "ECF": "ECF", "EXTERNO": "Tabela externa"}
REL = {"PRE_DIRETA": "Presença direta – existe em A ⇒ deve existir em B (falta = omissão)",
       "PRE_REVERSA": "Presença reversa – existe em B ⇒ deve ter origem em A (falta = sem lastro)",
       "IDT": "Identidade – mesmo fato, mesmo valor", "AGR": "Agregação – soma de vários registros = um valor",
       "LIM": "Limite – um lado não pode exceder o outro", "CNT": "Continuidade – saldo final = saldo inicial seguinte",
       "PAR": "Compatibilidade de parâmetros de regime", "EXC": "Exclusividade legítima – filtro aplicado antes"}
EVID = {"PVA": "validador oficial", "MANUAL": "texto do Guia/Manual/NT", "NORMA": "lei/IN/convênio",
        "DERIVADA": "dedução a partir da definição oficial dos campos"}
GRUPOS = {"FUNDACAO": "Parâmetros, regime, entidades e exclusões", "DOCUMENTOS": "Documentos × escriturações",
          "ESCRITURACOES": "Escriturações entre si", "CONTABIL_ECF": "Contabilidade × ECF",
          "DIMP": "DIMP", "CONTINUIDADE": "Continuidade mensal"}
SEVERIDADES = [
    ("ESTRUTURA_GRAVE", "Estrutura grave", "#7f1d1d",
     "Falha estrutural que compromete a própria base do cruzamento (ex.: regime ou identificação incompatível)."),
    ("ESCRITURACAO_GRAVE", "Escrituração grave", "#b91c1c",
     "Indício de omissão, divergência de valor ou crédito indevido que afeta a apuração."),
    ("ALERTA", "Alerta", "#b45309",
     "Divergência que exige verificação ou explicação, mas admite hipóteses legítimas frequentes."),
    ("INFO", "Informativo", "#1d4ed8",
     "Filtros, pré-requisitos e constatações sem impacto direto na apuração."),
]

regras = []
for arq in sorted(glob.glob(os.path.join(base, '0*.json'))):
    d = carregar(arq)
    for ordem, r in enumerate(d['regras']):
        r['_grupo'] = d['grupo']
        r['_ordem'] = (arq, ordem)
        regras.append(r)

e = html.escape


def lado(x):
    s = f"{e(NOMES.get(x['obrigacao'], x['obrigacao']))} → <code>{e(x['elemento'])}</code>"
    if x.get('filtro'):
        s += f"<br><span class='sub'>universo: {e(x['filtro'])}</span>"
    if x.get('agregacao'):
        s += f"<br><span class='sub'>agregação: {e(x['agregacao'])}</span>"
    return s


def linha(rotulo, valor):
    return f"<tr><th>{rotulo}</th><td>{valor}</td></tr>" if valor else ""


def cartao(r):
    op = e(r['operador'])
    if r.get('tolerancia'):
        op += f" · tolerância: {e(str(r['tolerancia']))}"
    sev = e(r['severidade'])
    if r.get('severidadeAtenuada'):
        sa = r['severidadeAtenuada']
        sev += f" · atenuada para <b>{e(sa['severidade'])}</b> quando: {e(sa['quando'])}"
    fts = "; ".join(e(fontes[f]['documento'] + (f" v{fontes[f]['versao']}" if fontes[f].get('versao') else ""))
                    for f in r['fontes'])
    hip = "".join(f"<li>{e(h)}</li>" for h in r.get('hipotesesLegitimas') or [])
    selos = ""
    if r['id'] in implementadas:
        selos += "<span class='selo impl'>implementada no motor</span>"
    if r.get('conferirCampo'):
        selos += "<span class='selo conf'>campo a conferir</span>"
    vig = e(r['vigenciaInicio']) + (f" a {e(r['vigenciaFim'])}" if r.get('vigenciaFim') else "")
    return f"""
<div class='regra'>
  <div class='cab'><span class='id'>{e(r['id'])}</span> {e(r['titulo'])} {selos}</div>
  <table>
    {linha('Grupo / mapa', e(GRUPOS.get(r['_grupo'], r['_grupo'])) + (f" · {e(r['mapa'])}" if r.get('mapa') else ''))}
    {linha('Tipo de relação', f"<b>{e(r['tipoRelacao'])}</b> – {e(REL.get(r['tipoRelacao'], ''))}")}
    {linha('Origem', lado(r['origem']))}
    {linha('Destino', lado(r['destino']) if r.get('destino') else '')}
    {linha('Chave de junção', e(', '.join(r['chaveJuncao'])) if r.get('chaveJuncao') else '')}
    {linha('Critério (operador)', op)}
    {linha('Condição', e(r['condicaoDescritiva']) if r.get('condicaoDescritiva') else '')}
    {linha('Aplicável se', e(r['aplicavelSe']) if r.get('aplicavelSe') else '')}
    {linha('Severidade', sev)}
    {linha('Evidência', f"{e(r['evidencia'])} ({e(EVID.get(r['evidencia'], ''))})")}
    {linha('Hipóteses legítimas', f'<ul>{hip}</ul>' if hip else '')}
    {linha('Filtros de exclusão', e(', '.join(r['filtrosExclusao'])) if r.get('filtrosExclusao') else '')}
    {linha('Depende de', e(', '.join(r['dependeDe'])) if r.get('dependeDe') else '')}
    {linha('Cadeias', e(', '.join(r['cadeias'])) if r.get('cadeias') else '')}
    {linha('Executor', e(r['executor']) if r.get('executor') else '')}
    {linha('Fontes', fts)}
    {linha('Vigência', vig)}
    {linha('Mensagem do parecer', f"<i>{e(r['mensagemParecer'])}</i>")}
  </table>
</div>"""


cont_sev = Counter(r['severidade'] for r in regras)
cont_rel = Counter(r['tipoRelacao'] for r in regras)
cont_grp = Counter(r['_grupo'] for r in regras)

resumo_sev = "".join(
    f"<tr><td><span class='bolinha' style='background:{cor}'></span>{rot}</td><td class='n'>{cont_sev[c]}</td><td>{e(desc)}</td></tr>"
    for c, rot, cor, desc in SEVERIDADES)
resumo_grp = "".join(f"<tr><td>{e(GRUPOS[g])}</td><td class='n'>{cont_grp[g]}</td></tr>" for g in GRUPOS)
resumo_rel = "".join(f"<tr><td><b>{k}</b> – {e(REL[k])}</td><td class='n'>{cont_rel[k]}</td></tr>"
                     for k, _ in cont_rel.most_common())

indice, corpo = [], []
for c, rot, cor, desc in SEVERIDADES:
    lista = [r for r in regras if r['severidade'] == c]
    if not lista:
        continue
    indice.append(f"<h3 style='color:{cor}'>{rot} ({len(lista)})</h3><p class='ids'>"
                  + ", ".join(e(r['id']) for r in lista) + "</p>")
    corpo.append(f"<section class='sev' style='--cor:{cor}'><h2>{rot} – {len(lista)} regras</h2>"
                 f"<p class='desc'>{e(desc)}</p>" + "".join(cartao(r) for r in lista) + "</section>")

doc = f"""<!doctype html><html lang='pt-br'><head><meta charset='utf-8'>
<title>Catálogo de Regras de Cruzamento</title>
<style>
@page {{ size: A4; margin: 14mm 12mm; }}
body {{ font-family: 'Segoe UI', Arial, sans-serif; font-size: 9.5pt; color: #111; }}
h1 {{ font-size: 20pt; margin: 0 0 4px; }}
h2 {{ font-size: 15pt; color: var(--cor); border-bottom: 3px solid var(--cor); padding-bottom: 3px; }}
h3 {{ font-size: 11pt; margin: 10px 0 2px; }}
.capa p {{ margin: 3px 0; }}
.sev {{ break-before: page; }}
.desc {{ color: #444; margin-top: -4px; }}
table {{ border-collapse: collapse; width: 100%; }}
.resumo td, .resumo th {{ border: 1px solid #ccc; padding: 4px 6px; text-align: left; vertical-align: top; }}
.resumo th {{ background: #f1f5f9; }}
.n {{ text-align: right; font-weight: bold; width: 50px; }}
.bolinha {{ display: inline-block; width: 10px; height: 10px; border-radius: 50%; margin-right: 6px; }}
.ids {{ font-family: Consolas, monospace; font-size: 8.5pt; color: #333; line-height: 1.5; }}
.regra {{ border: 1px solid #d4d4d8; border-left: 5px solid var(--cor); border-radius: 4px; margin: 8px 0; break-inside: avoid; }}
.cab {{ background: #f4f4f5; padding: 5px 8px; font-weight: 600; font-size: 10.5pt; }}
.id {{ font-family: Consolas, monospace; color: var(--cor); margin-right: 4px; }}
.regra th {{ width: 125px; text-align: left; vertical-align: top; color: #555; font-weight: 600; padding: 2px 8px; }}
.regra td {{ padding: 2px 8px 2px 0; vertical-align: top; }}
.regra ul {{ margin: 0; padding-left: 16px; }}
code {{ font-family: Consolas, monospace; font-size: 8.5pt; background: #f4f4f5; padding: 0 2px; }}
.sub {{ color: #555; font-size: 8.5pt; }}
.selo {{ font-size: 7.5pt; font-weight: 600; padding: 1px 6px; border-radius: 8px; margin-left: 6px; vertical-align: middle; }}
.impl {{ background: #dcfce7; color: #166534; }}
.conf {{ background: #fef3c7; color: #92400e; }}
</style></head><body>
<div class='capa'>
<h1>Catálogo de Regras de Cruzamento</h1>
<p><b>infis-agente-parecer-tributário</b> · catálogo v1.0.0 · {len(regras)} regras</p>
<p>Gerado a partir de <code>docs/regras/*.json</code>. Em caso de divergência, o JSON é a referência.
O selo <span class='selo impl'>implementada no motor</span> indica as {len(implementadas)} regras de cruzamento já presentes em
<code>catalog/regras.json</code> (v{e(motor['versao'])}); o selo <span class='selo conf'>campo a conferir</span> indica campo ainda a confirmar no PDF oficial.</p>
<h3>Por severidade</h3>
<table class='resumo'><tr><th>Severidade</th><th>Qtd.</th><th>Critério</th></tr>{resumo_sev}</table>
<h3>Por grupo</h3>
<table class='resumo'><tr><th>Grupo</th><th>Qtd.</th></tr>{resumo_grp}</table>
<h3>Por tipo de relação</h3>
<table class='resumo'><tr><th>Tipo</th><th>Qtd.</th></tr>{resumo_rel}</table>
<h3>Como ler uma regra</h3>
<p><b>Origem → Destino</b>: obrigação e registro/campo de cada lado. <b>Critério</b>: operador aplicado e tolerância.
<b>Severidade atenuada</b>: aplicada quando a condição indicada é atendida. <b>Hipóteses legítimas</b>: explicações aceitáveis
que tornam a divergência regular. <b>Filtros de exclusão</b>: regras EXC aplicadas antes. <b>Depende de</b>: se essas regras falharem,
esta é reportada como NÃO CONCLUSIVA. Regras de evidência DERIVADA nunca concluem irregularidade sem avaliar as hipóteses legítimas.</p>
</div>
<section class='sev' style='--cor:#111'><h2>Índice por severidade</h2>{''.join(indice)}</section>
{''.join(corpo)}
</body></html>"""

with open(saida_html, 'w', encoding='utf-8') as f:
    f.write(doc)

navegadores = [r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
               r"C:\Program Files\Google\Chrome\Application\chrome.exe"]
nav = next(n for n in navegadores if os.path.exists(n))
subprocess.run([nav, '--headless=new', '--disable-gpu', '--no-pdf-header-footer',
                f'--print-to-pdf={saida_pdf}', 'file:///' + saida_html.replace('\\', '/')], check=True)
os.remove(saida_html)
print('ok', len(regras), dict(cont_sev), saida_pdf)
