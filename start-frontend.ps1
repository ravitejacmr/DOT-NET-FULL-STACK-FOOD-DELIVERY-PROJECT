$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -LiteralPath (Join-Path $projectRoot 'frontend')
if (-not (Test-Path -LiteralPath 'node_modules')) { npm install }
npm start
