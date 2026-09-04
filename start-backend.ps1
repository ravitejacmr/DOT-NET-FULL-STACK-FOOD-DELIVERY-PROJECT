$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$localSdk = 'C:\Users\ADMIN\AppData\Local\Microsoft\dotnet-sdk\dotnet.exe'
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetExecutable = if ($dotnetCommand -and (& $dotnetCommand.Source --list-sdks)) { $dotnetCommand.Source } elseif (Test-Path -LiteralPath $localSdk) { $localSdk } else { throw '.NET 8 SDK is required.' }
& $dotnetExecutable run --project (Join-Path $projectRoot 'backend\src\FoodFlow.Api\FoodFlow.Api.csproj') --launch-profile http
