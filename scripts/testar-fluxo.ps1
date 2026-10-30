# Envia um lote para a Api.Agente local e mostra o resultado.
# Requer: Docker (rag-postgres), Api.RAG e Api.Agente rodando em Development
# e os user-secrets/user-jwts ja configurados.
param(
    [string]$Lote = "exemplos/lote-exemplo.json",
    [string]$Saida = "exemplos/resposta-exemplo.json",
    [string]$UrlAgente = "http://localhost:5117",
    [string]$UrlRag = "http://localhost:5203"
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz

foreach ($api in @(@{ Nome = "Api.RAG"; Url = $UrlRag }, @{ Nome = "Api.Agente"; Url = $UrlAgente })) {
    try { Invoke-RestMethod "$($api.Url)/health" -TimeoutSec 5 | Out-Null }
    catch {
        Write-Host "$($api.Nome) nao responde em $($api.Url). Suba com: dotnet run --project src/$($api.Nome)" -ForegroundColor Red
        exit 1
    }
}

$tokens = dotnet user-jwts list --project src/Api.Agente --output json | Out-String | ConvertFrom-Json
$token = ($tokens.PSObject.Properties.Value | Where-Object Name -eq "orquestrador-local" | Select-Object -First 1).Token
if (-not $token) {
    $token = (dotnet user-jwts create --project src/Api.Agente --name orquestrador-local --valid-for 30d --output token).Trim()
}

$corpo = [IO.File]::ReadAllBytes((Resolve-Path $Lote))
$resposta = Invoke-WebRequest -Method Post -Uri "$UrlAgente/api/v1/analises" -ContentType "application/json" -Body $corpo `
    -Headers @{ Authorization = "Bearer $token"; "X-Correlation-Id" = "teste-local-$(Get-Date -Format yyyyMMddHHmmss)" } `
    -SkipHttpErrorCheck

Write-Host "HTTP $($resposta.StatusCode)"
if ($resposta.StatusCode -ne 200) { $resposta.Content; exit 1 }

[IO.File]::WriteAllText((Join-Path $raiz $Saida), $resposta.Content, (New-Object Text.UTF8Encoding $false))
$json = $resposta.Content | ConvertFrom-Json

Write-Host "Status:  $($json.resultado.status)"
Write-Host "Achados:"
$json.resultado.achados | ForEach-Object { Write-Host "  $($_.regraId) [$($_.natureza)] $($_.descricao)" }
Write-Host "Limitacoes:"
$json.resultado.limitacoes | ForEach-Object { Write-Host "  $_" }
Write-Host "Consumo LLM:"
$json.consumo | ForEach-Object { Write-Host "  $($_.operacao) tentativa $($_.tentativa): $($_.status)/$($_.resultado) entrada=$($_.tokensEntrada) saida=$($_.tokensSaida)" }
if ($json.parecer) { Write-Host "Parecer: $($json.parecer.sintese)" } else { Write-Host "Parecer: nao gerado" }
Write-Host "Resposta completa em $Saida"
