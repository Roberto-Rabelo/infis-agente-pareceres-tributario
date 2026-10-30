<#
.SYNOPSIS
    Grava no .env da raiz (não versionado) os tokens locais usados por fluxo-analise.http.

.DESCRIPTION
    Reaproveita os tokens do `dotnet user-jwts` com mais de 1 dia de validade e cria os que faltam.
    Esses tokens só são aceitos em Development (ADR D15 pendente). As demais variáveis do .env são preservadas.

    AGENTE_TOKEN        Api.Agente, chamador da análise (orquestrador-local)
    RAG_TOKEN_ADMIN     Api.RAG, papel rag.admin (ingestão e listagem)
    RAG_TOKEN_CONSULTA  Api.RAG, papel rag.consulta (MCP, o mesmo usado pela Api.Agente)

.EXAMPLE
    ./scripts/preparar-http.ps1
#>
param([int] $DiasValidade = 30)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz

function Get-TokenLocal([string] $Projeto, [string] $Nome, [string] $Papel) {
    $lista = dotnet user-jwts list --project $Projeto --output json | Out-String
    $tokens = if ($lista.TrimStart().StartsWith('{')) { ($lista | ConvertFrom-Json).PSObject.Properties.Value } else { @() }
    $limite = [DateTimeOffset]::UtcNow.AddDays(1)
    $existente = $tokens | Where-Object { $_.Name -eq $Nome -and [DateTimeOffset]$_.Expires -gt $limite } | Select-Object -First 1
    if ($existente) { return $existente.Token }

    $argumentos = @('user-jwts', 'create', '--project', $Projeto, '--name', $Nome, '--valid-for', "$($DiasValidade)d", '--output', 'token')
    if ($Papel) { $argumentos += @('--role', $Papel) }
    return (& dotnet @argumentos | Out-String).Trim()
}

$valores = [ordered]@{
    AGENTE_TOKEN       = Get-TokenLocal 'src/Api.Agente' 'orquestrador-local' $null
    RAG_TOKEN_ADMIN    = Get-TokenLocal 'src/Api.RAG' 'ingestao-local' 'rag.admin'
    RAG_TOKEN_CONSULTA = Get-TokenLocal 'src/Api.RAG' 'api-agente' 'rag.consulta'
}

$arquivo = Join-Path $raiz '.env'
$linhas = if (Test-Path $arquivo) { [IO.File]::ReadAllLines($arquivo) } else { @() }
$mantidas = $linhas | Where-Object { $_ -notmatch "^($($valores.Keys -join '|'))=" }
$novas = @($mantidas) + ($valores.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
[IO.File]::WriteAllLines($arquivo, [string[]]$novas, (New-Object Text.UTF8Encoding $false))

Write-Host "Tokens gravados em .env: $($valores.Keys -join ', ')"
