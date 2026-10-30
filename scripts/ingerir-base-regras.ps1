<#
.SYNOPSIS
    Ingere a base de regras de cruzamento (docs/*.md) na Api.RAG como BASE_REGRAS_INTERNA.

.DESCRIPTION
    A versão de cada documento é derivada do hash do conteúdo: reexecutar sem alterações é idempotente
    (resposta JA_EXISTENTE) e qualquer edição gera nova versão, que substitui a anterior.
    O token administrativo (papel rag.admin) é lido de $env:RAG_TOKEN_ADMIN e nunca deve ser versionado.

.EXAMPLE
    $env:RAG_TOKEN_ADMIN = '<token>'
    ./scripts/ingerir-base-regras.ps1 -ApiRag https://localhost:7101
#>
param(
    [Parameter(Mandatory = $true)][string] $ApiRag,
    [string] $Pasta = (Join-Path $PSScriptRoot '..\docs'),
    [string] $Token = $env:RAG_TOKEN_ADMIN
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Defina $env:RAG_TOKEN_ADMIN com um token de papel rag.admin.'
}

$obrigacoes = @('NFE', 'NFCE', 'NFSE', 'DIMP', 'EFD_ICMS_IPI', 'EFD_CONTRIBUICOES', 'ECD', 'ECF')
$sha = [System.Security.Cryptography.SHA256]::Create()

Get-ChildItem -Path $Pasta -Filter '*.md' -File | Sort-Object Name | ForEach-Object {
    $conteudo = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    $hash = -join ($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($conteudo)) | ForEach-Object { $_.ToString('x2') })
    $titulo = ($conteudo -split "`n" | Where-Object { $_ -match '^#\s+' } | Select-Object -First 1) -replace '^#\s+', ''
    if ([string]::IsNullOrWhiteSpace($titulo)) { $titulo = $_.BaseName }

    $corpo = [ordered]@{
        chave      = 'docs/' + $_.Name.ToLowerInvariant()
        titulo     = $titulo.Trim()
        tipo       = 'BASE_REGRAS_INTERNA'
        emissor    = 'Projeto infis-agente-parecer-tributário (base de estudo interna)'
        versao     = 'sha256-' + $hash.Substring(0, 16)
        origem     = 'docs/' + $_.Name
        obrigacoes = $obrigacoes
        formato    = 'markdown'
        conteudo   = $conteudo
    } | ConvertTo-Json -Depth 5

    $resposta = Invoke-RestMethod -Method Post -Uri ($ApiRag.TrimEnd('/') + '/api/v1/documentos') `
        -Headers @{ Authorization = "Bearer $Token" } `
        -ContentType 'application/json; charset=utf-8' `
        -Body ([System.Text.Encoding]::UTF8.GetBytes($corpo))
    '{0,-45} {1,-13} {2,4} trechos' -f $_.Name, $resposta.situacao, $resposta.trechos
}
