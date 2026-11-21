[CmdletBinding()]
param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $PSScriptRoot 'dist' }
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'This WPF application builds on Windows only.' }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (!(Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler was not found. See docs/build.md.' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$sourceRoot = Join-Path $PSScriptRoot 'src'
$atlas = Join-Path $PSScriptRoot 'assets\xiaohei\spritesheet.png'
if (!(Test-Path -LiteralPath $atlas)) { throw 'Missing assets/xiaohei/spritesheet.png.' }
$wpf = Join-Path $framework 'WPF'
$references = @('System.dll','System.Core.dll','System.Device.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll','System.Web.Extensions.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $wpf $_) }
$executable = Join-Path $outputRoot 'XiaoheiCompanion.exe'
$arguments = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output','/codepage:65001','/langversion:5',('/out:' + $executable),('/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')),('/resource:' + $atlas + ',Xiaohei.DefaultAtlas.png')) + $references
if (Test-Path -LiteralPath (Join-Path $sourceRoot 'pet.ico')) { $arguments += '/win32icon:' + (Join-Path $sourceRoot 'pet.ico') }
$sources = @(Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -File | Sort-Object Name)
if ($sources.Count -eq 0) { throw 'No C# sources were found.' }
$arguments += @($sources.FullName)
& $compiler $arguments
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' }

foreach ($folder in @('assets','editor','profiles')) {
    $source = Join-Path $PSScriptRoot $folder
    if (Test-Path -LiteralPath $source) {
        $destination = Join-Path $outputRoot $folder
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $destination -Recurse -Force
    }
}
foreach ($name in @('README.md','LICENSE','ASSET_NOTICE.md')) {
    $source = Join-Path $PSScriptRoot $name
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $outputRoot $name) -Force }
}
Write-Output $executable
