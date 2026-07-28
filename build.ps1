[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]] $BuildArguments
)

$ErrorActionPreference = 'Stop'
$buildProject = Join-Path $PSScriptRoot 'build/_build.csproj'

dotnet run --project $buildProject -- @BuildArguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
