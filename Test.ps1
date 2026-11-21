[CmdletBinding()]
param([string]$AppDirectory = '')
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($AppDirectory)) { $AppDirectory = Join-Path $PSScriptRoot 'dist' }
$appRoot = [IO.Path]::GetFullPath($AppDirectory)
$app = Join-Path $appRoot 'XiaoheiCompanion.exe'
if (!(Test-Path -LiteralPath $app)) { throw 'Build the application first: .\Build.ps1' }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (!(Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) { $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$compiler = Join-Path $framework 'csc.exe'
$wpf = Join-Path $framework 'WPF'
$references = @('System.dll','System.Core.dll','System.Device.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll','System.Web.Extensions.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $wpf $_) }
$testRoot = Join-Path $PSScriptRoot ('build\tests\' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$previousData = $env:XIAOHEI_DATA_DIR
try {
    foreach ($name in @('ModelTests','ClockTests','SpriteTests','LocationFocusTests','UiSmokeTests')) {
        $source = Join-Path $PSScriptRoot ('tests\' + $name + '.cs')
        if (!(Test-Path -LiteralPath $source)) { throw ('Required test is missing: ' + $name) }
        $testExe = Join-Path $appRoot ($name + '.exe')
        & $compiler /nologo /utf8output /codepage:65001 /langversion:5 ('/out:' + $testExe) ('/reference:' + $app) $references $source
        if ($LASTEXITCODE -ne 0) { throw ('Test compilation failed: ' + $name) }
        $env:XIAOHEI_DATA_DIR = Join-Path $testRoot $name
        New-Item -ItemType Directory -Path $env:XIAOHEI_DATA_DIR -Force | Out-Null
        if ($name -eq 'SpriteTests') {
            & $testExe $env:XIAOHEI_DATA_DIR (Join-Path $PSScriptRoot 'tests\fixtures\browser-profile.json')
        } else {
            & $testExe $env:XIAOHEI_DATA_DIR
        }
        if ($LASTEXITCODE -ne 0) { throw ('Test failed: ' + $name) }
        Move-Item -LiteralPath $testExe -Destination (Join-Path $testRoot ($name + '.exe')) -Force
    }
} finally { $env:XIAOHEI_DATA_DIR = $previousData }
Write-Output ('All tests passed. Isolated test output: ' + $testRoot)
