# Gera um JWT para o Authorize do Swagger publicado na Vercel e copia para a área de transferência.
# Entrada: a CHAVE Autenticacao__ChaveSimetrica do painel da Vercel (base64, não é um token eyJ...).
# Saída: o TOKEN, que é o que vai no Authorize do Swagger.
$repo = Split-Path -Parent $PSScriptRoot

$seguro = Read-Host 'Cole o valor de Autenticacao__ChaveSimetrica (a chave, sem aspas)' -AsSecureString
$chave = ([Net.NetworkCredential]::new('', $seguro).Password).Trim().Trim('"').Trim("'")

if ($chave.StartsWith('eyJ')) {
    Write-Host 'Isso é um token (eyJ...), não a chave. Cole o valor da variável Autenticacao__ChaveSimetrica da Vercel.' -ForegroundColor Red
    return
}
try { $bytes = [Convert]::FromBase64String($chave) } catch { $bytes = $null }
if ($null -eq $bytes -or $bytes.Length -lt 32) {
    Write-Host "Valor inválido: a chave precisa ser base64 com pelo menos 32 bytes (veio com $($chave.Length) caracteres)." -ForegroundColor Red
    return
}

$env:AUTENTICACAO_CHAVE = $chave
$env:AUTENTICACAO_EMISSOR = 'infis-agente-parecer-teste'
$env:AUTENTICACAO_AUDIENCE = 'agente-parecer'
Push-Location $repo
try {
    $saida = dotnet run scripts/emitir-token.cs -- sofia 30 2>&1
    $codigo = $LASTEXITCODE
}
finally {
    Pop-Location
    Remove-Item Env:AUTENTICACAO_CHAVE -ErrorAction SilentlyContinue
}

$token = $saida | Where-Object { "$_" -match '^eyJ[\w-]+\.[\w-]+\.[\w-]+$' } | Select-Object -Last 1
if ($codigo -ne 0 -or -not $token) {
    Write-Host 'O gerador de token falhou:' -ForegroundColor Red
    $saida | ForEach-Object { Write-Host "  $_" }
    return
}
$token = "$token".Trim()

$status = try {
    (Invoke-WebRequest -UseBasicParsing -Method Post -Uri 'https://agente-parecer-tributario.vercel.app/api/v1/analises' `
        -Headers @{ Authorization = "Bearer $token" } -ContentType 'application/json' -Body '{}').StatusCode
} catch { $_.Exception.Response.StatusCode.value__ }

if ($status -eq 401) {
    Write-Host 'A Vercel recusou o token (401): essa chave não é a que está em Autenticacao__ChaveSimetrica, ou o emissor/audience na Vercel são outros.' -ForegroundColor Red
    return
}
Set-Clipboard -Value $token
Write-Host "Token aceito pela Vercel (HTTP $status para um lote vazio) e copiado para a área de transferência." -ForegroundColor Green
Write-Host 'No Swagger: Authorize -> cole (Ctrl+V) o token, sem "Bearer" -> Authorize.'
