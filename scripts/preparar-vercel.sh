#!/usr/bin/env sh
# Gera .env.vercel (gitignored) com as variáveis do projeto Vercel (services agente + rag) no modo de chave simétrica.
# Cria a chave HS256 e emite os tokens; OPENAI_API_KEY vem do .env local, se existir. Connection strings do Neon ficam
# para preencher. Não sobrescreve um .env.vercel existente. Nunca versionar o arquivo gerado.
#
# Uso: sh scripts/preparar-vercel.sh https://SEU-PROJETO.vercel.app
set -eu

raiz=$(cd "$(dirname "$0")/.." && pwd)
destino="$raiz/.env.vercel"
url=${1:?Informe a URL pública do projeto, ex.: https://agente-parecer.vercel.app}
url=${url%/}

if [ -f "$destino" ]; then
  echo ".env.vercel já existe; apague-o para gerar de novo (os tokens antigos deixam de valer se a chave mudar)." >&2
  exit 1
fi

AUTENTICACAO_CHAVE=$(openssl rand -base64 48)
AUTENTICACAO_EMISSOR=infis-agente-parecer-teste
AUTENTICACAO_AUDIENCE=agente-parecer
export AUTENTICACAO_CHAVE AUTENTICACAO_EMISSOR AUTENTICACAO_AUDIENCE

token() { dotnet run "$raiz/scripts/emitir-token.cs" -- "$@"; }

openai=""
if [ -f "$raiz/.env" ]; then
  openai=$(grep '^OPENAI_API_KEY=' "$raiz/.env" | head -1 | cut -d= -f2- || true)
fi

umask 077
cat > "$destino" <<EOF
# --- Variáveis do projeto Vercel (compartilhadas pelos services agente e rag) ---
ASPNETCORE_ENVIRONMENT=Production
Autenticacao__ChaveSimetrica=$AUTENTICACAO_CHAVE
Autenticacao__Emissor=$AUTENTICACAO_EMISSOR
Autenticacao__Audience=$AUTENTICACAO_AUDIENCE
OpenAI__ApiKey=$openai
Postgres__AplicarMigracoes=true

# Neon: formato Npgsql (chave=valor), não a URI postgres://. Endpoint direto (sem -pooler).
ConnectionStrings__Agente=Host=PREENCHER.neon.tech;Database=agente;Username=PREENCHER;Password=PREENCHER;SSL Mode=Require
ConnectionStrings__Rag=Host=PREENCHER.neon.tech;Database=rag;Username=PREENCHER;Password=PREENCHER;SSL Mode=Require

Parecer__Habilitado=true
Parecer__Modelo=gpt-4o-mini
BaseConhecimento__Endpoint=$url/rag/mcp
BaseConhecimento__TokenAcesso=$(token api-agente 90 rag.consulta)
BaseConhecimento__TimeoutSegundos=30
A2A__Url=$url/a2a
# Chave Bearer compartilhada da SOFIA (valor fornecido pelo treinamento; protege card e /a2a).
SOFIA_A2A_TOKEN=PREENCHER
RateLimiting__PermitLimitPorMinuto=60

# --- Só para uso local (não cadastrar na Vercel) ---
# Token de cliente (SOFIA / testes manuais) para /api/v1/analises e /a2a.
LOCAL_TOKEN_CLIENTE=$(token sofia 30)
# Token administrativo para ingerir a base em /rag/api/v1/documentos.
LOCAL_TOKEN_RAG_ADMIN=$(token ingestao 7 rag.admin)
EOF

echo "Gerado $destino. Preencha as connection strings do Neon$( [ -z "$openai" ] && echo ' e OpenAI__ApiKey' )."
