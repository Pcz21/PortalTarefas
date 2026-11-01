$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location (Join-Path $root 'PortalTarefas.Frontend')
try { npm ci; if($LASTEXITCODE -ne 0){throw 'npm ci falhou'}; npm run build; if($LASTEXITCODE -ne 0){throw 'Build React falhou'} } finally { Pop-Location }
$dest=Join-Path $root 'PortalTarefas.Web\wwwroot\spa'
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $root 'PortalTarefas.Frontend\dist\*') $dest -Recurse -Force
Write-Output 'SPA publicada em /spa/ no mesmo servidor da API.'
