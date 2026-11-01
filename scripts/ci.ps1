param([string]$Configuration='Release',[string]$ArtifactDirectory='qa/artefatos-ci')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
 dotnet tool restore; if($LASTEXITCODE -ne 0){throw 'Tool restore falhou'}
 dotnet restore PortalTarefas.slnx --locked-mode; if($LASTEXITCODE -ne 0){throw 'Restore falhou'}
 dotnet build PortalTarefas.slnx -c $Configuration --no-restore; if($LASTEXITCODE -ne 0){throw 'Build falhou'}
 dotnet test PortalTarefas.Tests -c $Configuration --no-build --logger 'trx;LogFileName=testes.trx' --results-directory qa/testes-ci; if($LASTEXITCODE -ne 0){throw 'Testes .NET falharam; publicação bloqueada'}
 npm --prefix PortalTarefas.Frontend ci; if($LASTEXITCODE -ne 0){throw 'npm ci falhou'}
 npm --prefix PortalTarefas.Frontend test; if($LASTEXITCODE -ne 0){throw 'Testes React falharam'}
 npm --prefix PortalTarefas.Frontend run build; if($LASTEXITCODE -ne 0){throw 'Build React falhou'}
 New-Item -ItemType Directory -Force PortalTarefas.Web/wwwroot/spa | Out-Null
 Copy-Item PortalTarefas.Frontend/dist/* PortalTarefas.Web/wwwroot/spa -Recurse -Force
 dotnet publish PortalTarefas.Web -c $Configuration --no-restore -o $ArtifactDirectory; if($LASTEXITCODE -ne 0){throw 'Publish falhou'}
 Write-Output 'ARTEFATO PUBLICADO APÓS TODAS AS VERIFICAÇÕES'
} finally {Pop-Location}
