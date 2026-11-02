param(
    [Parameter(Mandatory = $true)]
    [string]$Server,
    [int]$Port = 22,
    [string]$KeyPath
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'Apartmani/Apartmani.csproj'
$publish = Join-Path $root 'Apartmani/bin/Release/deploy-package'
$id = [guid]::NewGuid().ToString('N')
$archive = Join-Path $root "Apartmani/bin/atokicasp-release-$id.tar.gz"
$remoteArchive = "/tmp/atokicasp-release-$id.tar.gz"
$sshOptions = @('-p', "$Port", '-o', 'BatchMode=yes')
$scpOptions = @('-P', "$Port", '-o', 'BatchMode=yes')
if ($KeyPath) {
    $sshOptions += @('-i', $KeyPath)
    $scpOptions += @('-i', $KeyPath)
}

dotnet publish $project -c Release -r linux-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
tar -czf $archive --exclude=./wwwroot/img --exclude=./appsettings.json '--exclude=./appsettings.*.json' -C $publish .
if ($LASTEXITCODE -ne 0) { throw 'Could not package the release.' }
try {
    scp @scpOptions $archive "${Server}:$remoteArchive"
    if ($LASTEXITCODE -ne 0) { throw 'Could not upload the release.' }
    scp @scpOptions (Join-Path $PSScriptRoot 'deploy-server.sh') "${Server}:/tmp/atokicasp-deploy-$id.sh"
    if ($LASTEXITCODE -ne 0) { throw 'Could not upload the deployment script.' }
    ssh @sshOptions $Server "bash /tmp/atokicasp-deploy-$id.sh $remoteArchive"
    if ($LASTEXITCODE -ne 0) { throw 'Deployment failed; check the server output.' }
}
finally {
    ssh @sshOptions $Server "rm -f -- $remoteArchive /tmp/atokicasp-deploy-$id.sh"
    Remove-Item -LiteralPath $archive -ErrorAction SilentlyContinue
}
