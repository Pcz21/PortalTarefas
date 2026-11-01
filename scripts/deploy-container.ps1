param(
 [Parameter(Mandatory)][string]$ImageRef,
 [Parameter(Mandatory)][ValidatePattern('^portaltarefas-[a-z0-9-]+$')][string]$ContainerName,
 [Parameter(Mandatory)][string]$EnvFile,
 [Parameter(Mandatory)][string]$Network,
 [Parameter(Mandatory)][string]$CertificateDirectory,
 [Parameter(Mandatory)][string]$KeyVolume,
 [ValidateRange(1024,65535)][int]$HttpPort=8188,[ValidateRange(1024,65535)][int]$HttpsPort=8448,
 [string]$RecordDirectory='qa/deploy'
)
$ErrorActionPreference='Stop'
function Invoke-DockerLocal {
 param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
 $result=& docker @Arguments 2>&1
 if($LASTEXITCODE -ne 0){throw 'Operação Docker falhou. Consulte o estado dos contêineres sem divulgar variáveis de ambiente.'}
 return ($result -join "`n").Trim()
}
if(!(Test-Path -LiteralPath $EnvFile -PathType Leaf)){throw 'Arquivo externo de ambiente ausente'}
if(!(Test-Path -LiteralPath $CertificateDirectory -PathType Container)){throw 'Diretório externo de TLS ausente'}
# Resolve uma referência já disponível localmente; não faz pull nem publicação.
$imageId=Invoke-DockerLocal @('image','inspect','--format','{{.Id}}',$ImageRef)
if($imageId -notmatch '^sha256:[a-f0-9]{64}$'){throw 'Imagem local não identificada por digest'}
$stamp=Get-Date -Format yyyyMMddHHmmssfff
$previous="$ContainerName-anterior-$stamp"
$failed="$ContainerName-falhou-$stamp"
$names=Invoke-DockerLocal @('ps','-a','--format','{{.Names}}')
$hadPrevious=$ContainerName -in ($names -split "`r?`n")
New-Item -ItemType Directory -Force $RecordDirectory | Out-Null
$record=[ordered]@{data=(Get-Date).ToUniversalTime().ToString('o');imagem=$imageId;container=$ContainerName;anterior=$null;resultado='iniciado'}
if($hadPrevious){
 $record.anterior=Invoke-DockerLocal @('inspect','--format','{{.Image}}',$ContainerName)
 Invoke-DockerLocal @('stop',$ContainerName) | Out-Null
 Invoke-DockerLocal @('rename',$ContainerName,$previous) | Out-Null
}
try {
 Invoke-DockerLocal @('run','-d','--name',$ContainerName,'--label','projeto=PortalTarefas','--network',$Network,
 '-p',"127.0.0.1:${HttpPort}:8080",'-p',"127.0.0.1:${HttpsPort}:8443",
 '--env-file',(Resolve-Path -LiteralPath $EnvFile).Path,'-e','Database__Inicializar=false',
 '--mount',"type=bind,source=$((Resolve-Path -LiteralPath $CertificateDirectory).Path),target=/certs,readonly",
 '--mount',"type=volume,source=$KeyVolume,target=/home/app/.aspnet/DataProtection-Keys",$imageId) | Out-Null
 $healthy=$false
 for($i=0;$i -lt 60;$i++){
  $state=(Invoke-DockerLocal @('inspect','--format','{{json .State}}',$ContainerName)) | ConvertFrom-Json
  if($state.Status -eq 'exited'){throw 'Candidato encerrou antes do smoke test'}
  if($state.Health.Status -eq 'healthy'){$healthy=$true;break}
  Start-Sleep -Milliseconds 1000
 }
 if(!$healthy){throw 'Candidato não ficou saudável'}
 foreach($route in @('/health/live','/health/ready')){
  $response=Invoke-WebRequest "http://127.0.0.1:$HttpPort$route" -TimeoutSec 10
  if($response.StatusCode -ne 200){throw 'Smoke test de saúde falhou'}
 }
 $record.resultado='promovido'
 $record | ConvertTo-Json | Set-Content (Join-Path $RecordDirectory "deploy-$stamp.json")
 Write-Output "Imagem aprovada: $imageId. Contêiner anterior preservado: $hadPrevious."
} catch {
 $names=Invoke-DockerLocal @('ps','-a','--format','{{.Names}}')
 if($ContainerName -in ($names -split "`r?`n")){
  Invoke-DockerLocal @('stop',$ContainerName) | Out-Null
  Invoke-DockerLocal @('rename',$ContainerName,$failed) | Out-Null
 }
 if($hadPrevious){
  Invoke-DockerLocal @('rename',$previous,$ContainerName) | Out-Null
  Invoke-DockerLocal @('start',$ContainerName) | Out-Null
  $restored=$false
  for($i=0;$i -lt 30;$i++){
   try{$restored=(Invoke-WebRequest "http://127.0.0.1:$HttpPort/health/ready" -TimeoutSec 3).StatusCode -eq 200}catch{$restored=$false}
   if($restored){break};Start-Sleep -Milliseconds 1000
  }
  $record.resultado=if($restored){'rollback confirmado'}else{'rollback requer intervenção'}
 }else{$record.resultado='falha sem versão anterior'}
 $record | ConvertTo-Json | Set-Content (Join-Path $RecordDirectory "deploy-$stamp.json")
 throw "Publicação local rejeitada: $($record.resultado). Candidato preservado para diagnóstico."
}

