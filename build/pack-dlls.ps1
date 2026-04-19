# Build release DLLs and stage them in artifacts/ for vendoring into Buttr.Unity.
# Run from the repo root: pwsh build/pack-dlls.ps1

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$artifacts = Join-Path $repoRoot 'artifacts'

dotnet build "$repoRoot/Buttr.sln" -c Release | Out-Host

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

$dlls = @(
    "$repoRoot/src/Buttr.Core/bin/Release/netstandard2.1/Buttr.Core.dll",
    "$repoRoot/src/Buttr.Injection/bin/Release/netstandard2.1/Buttr.Injection.dll"
)

foreach ($dll in $dlls) {
    if (-not (Test-Path $dll)) { throw "Missing build output: $dll" }
    Copy-Item $dll $artifacts -Force
    Write-Host "Staged $(Split-Path -Leaf $dll)"
}
