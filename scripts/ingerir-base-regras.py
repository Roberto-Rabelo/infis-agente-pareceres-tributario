"""Equivalente a scripts/ingerir-base-regras.ps1 (mesma chave, versão e corpo) para quem não tem pwsh.

Uso: RAG_TOKEN_ADMIN=<token rag.admin> python3 scripts/ingerir-base-regras.py https://<projeto>.vercel.app/rag [docs]
"""
import hashlib, json, os, pathlib, re, sys, urllib.error, urllib.request

api = sys.argv[1].rstrip("/")
pasta = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else pathlib.Path(__file__).resolve().parent.parent / "docs"
token = os.environ.get("RAG_TOKEN_ADMIN") or sys.exit("Defina RAG_TOKEN_ADMIN com um token de papel rag.admin.")
obrigacoes = ["NFE", "NFCE", "NFSE", "DIMP", "EFD_ICMS_IPI", "EFD_CONTRIBUICOES", "ECD", "ECF"]

for md in sorted(pasta.glob("*.md"), key=lambda p: p.name):
    conteudo = md.read_text(encoding="utf-8-sig")
    hash_ = hashlib.sha256(conteudo.encode("utf-8")).hexdigest()
    titulo = next((re.sub(r"^#\s+", "", l) for l in conteudo.split("\n") if re.match(r"^#\s+", l)), "").strip() or md.stem
    corpo = {
        "chave": "docs/" + md.name.lower(),
        "titulo": titulo,
        "tipo": "BASE_REGRAS_INTERNA",
        "emissor": "Projeto infis-agente-parecer-tributário (base de estudo interna)",
        "versao": "sha256-" + hash_[:16],
        "origem": "docs/" + md.name,
        "obrigacoes": obrigacoes,
        "formato": "markdown",
        "conteudo": conteudo,
    }
    req = urllib.request.Request(api + "/api/v1/documentos", data=json.dumps(corpo, ensure_ascii=False).encode("utf-8"),
                                 headers={"Authorization": "Bearer " + token, "Content-Type": "application/json; charset=utf-8"})
    try:
        with urllib.request.urlopen(req, timeout=300) as r:
            resp = json.load(r)
            print(f"{md.name:<45} {resp.get('situacao', ''):<13} {resp.get('trechos', ''):>4} trechos")
    except urllib.error.HTTPError as e:
        print(f"{md.name:<45} HTTP {e.code}: {e.read().decode('utf-8', 'replace')[:200]}")
