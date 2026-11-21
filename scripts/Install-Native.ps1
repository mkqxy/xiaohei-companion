[CmdletBinding()]
param(
    [string]$PackageRoot = '',
    [string]$CodexHome = $(if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE '.codex' })
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($PackageRoot)) { $PackageRoot = Split-Path $PSScriptRoot -Parent }

function Get-FullPath([string]$Path) { return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar) }
function Assert-Under([string]$Path, [string]$Parent) {
    $absolute = Get-FullPath $Path
    $root = Get-FullPath $Parent
    if (!$absolute.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw ('Unsafe installation path: ' + $absolute) }
    return $absolute
}
function Assert-NotLink([string]$Path) {
    if ((Test-Path -LiteralPath $Path) -and ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw ('A linked installation path is not supported: ' + $Path) }
}
function Get-WebPSize([byte[]]$Bytes) {
    if ($Bytes.Length -lt 20 -or [Text.Encoding]::ASCII.GetString($Bytes,0,4) -ne 'RIFF' -or [Text.Encoding]::ASCII.GetString($Bytes,8,4) -ne 'WEBP') { throw 'The spritesheet is not a WebP file.' }
    $offset = 12L
    while ($offset + 8 -le $Bytes.Length) {
        $type = [Text.Encoding]::ASCII.GetString($Bytes,[int]$offset,4)
        $length = [BitConverter]::ToUInt32($Bytes,[int]$offset + 4)
        $payload = $offset + 8
        if ($payload + $length -gt $Bytes.Length) { throw 'The WebP chunk is truncated.' }
        if ($type -eq 'VP8X' -and $length -ge 10) {
            $w = 1 + [int]$Bytes[$payload+4] + ([int]$Bytes[$payload+5] -shl 8) + ([int]$Bytes[$payload+6] -shl 16)
            $h = 1 + [int]$Bytes[$payload+7] + ([int]$Bytes[$payload+8] -shl 8) + ([int]$Bytes[$payload+9] -shl 16)
            return @($w,$h)
        }
        if ($type -eq 'VP8L' -and $length -ge 5 -and $Bytes[$payload] -eq 47) {
            $bits = [BitConverter]::ToUInt32($Bytes,[int]$payload + 1)
            return @((1 + ($bits -band 16383)), (1 + (($bits -shr 14) -band 16383)))
        }
        if ($type -eq 'VP8 ' -and $length -ge 10 -and $Bytes[$payload+3] -eq 157 -and $Bytes[$payload+4] -eq 1 -and $Bytes[$payload+5] -eq 42) {
            return @(([BitConverter]::ToUInt16($Bytes,[int]$payload+6) -band 16383),([BitConverter]::ToUInt16($Bytes,[int]$payload+8) -band 16383))
        }
        $offset = $payload + $length + ($length % 2)
    }
    throw 'Could not read WebP dimensions.'
}

$package = Get-FullPath $PackageRoot
$source = Join-Path $package 'native\xiaohei-heixiu'
$sourceManifest = Join-Path $source 'pet.json'
$sourceAtlas = Join-Path $source 'spritesheet.webp'
Assert-NotLink $source
foreach ($file in @($sourceManifest,$sourceAtlas)) { if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw ('Missing package file: ' + $file) }; Assert-NotLink $file }
$manifest = Get-Content -LiteralPath $sourceManifest -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.id -ne 'xiaohei-heixiu' -or $manifest.spriteVersionNumber -ne 2 -or $manifest.spritesheetPath -ne 'spritesheet.webp' -or [string]::IsNullOrWhiteSpace($manifest.displayName)) { throw 'Invalid native v2 manifest.' }
$dimensions = Get-WebPSize ([IO.File]::ReadAllBytes($sourceAtlas))
if ($dimensions[0] -ne 1536 -or $dimensions[1] -ne 2288) { throw ('Expected 1536 x 2288, got ' + ($dimensions -join ' x ')) }
$sourceHash = (Get-FileHash -LiteralPath $sourceAtlas -Algorithm SHA256).Hash
$manifestHash = (Get-FileHash -LiteralPath $sourceManifest -Algorithm SHA256).Hash
$homeRoot = Get-FullPath $CodexHome
if ($homeRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) -eq ([IO.Path]::GetPathRoot($homeRoot)).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'CodexHome must be a directory below the filesystem root.' }
$petsRoot = Assert-Under (Join-Path $homeRoot 'pets') $homeRoot
$target = Assert-Under (Join-Path $petsRoot $manifest.id) $petsRoot
Assert-NotLink $petsRoot
Assert-NotLink $target
New-Item -ItemType Directory -Path $homeRoot,$petsRoot -Force | Out-Null

if ((Test-Path -LiteralPath (Join-Path $target 'pet.json')) -and (Test-Path -LiteralPath (Join-Path $target 'spritesheet.webp'))) {
    if ((Get-FileHash -LiteralPath (Join-Path $target 'spritesheet.webp')).Hash -eq $sourceHash -and (Get-FileHash -LiteralPath (Join-Path $target 'pet.json')).Hash -eq $manifestHash) {
        Write-Host 'Already installed: Xiaohei and HeiXiu. No files changed.'
        Write-Host 'Open Settings > Pets > Refresh, then select the pet. Use /pet to show it.'
        return
    }
}

$token = [Guid]::NewGuid().ToString('N')
$stage = Assert-Under (Join-Path $homeRoot ('.pet-install-' + $token)) $homeRoot
$backupsRoot = Assert-Under (Join-Path $homeRoot 'pet-backups') $homeRoot
Assert-NotLink $backupsRoot
$backup = Assert-Under (Join-Path $backupsRoot ($manifest.id + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $token)) $backupsRoot
$movedOld = $false
$committed = $false
try {
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    Copy-Item -LiteralPath $sourceAtlas -Destination (Join-Path $stage 'spritesheet.webp')
    Copy-Item -LiteralPath $sourceManifest -Destination (Join-Path $stage 'pet.json')
    if ((Get-FileHash -LiteralPath (Join-Path $stage 'spritesheet.webp')).Hash -ne $sourceHash -or (Get-FileHash -LiteralPath (Join-Path $stage 'pet.json')).Hash -ne $manifestHash) { throw 'Staged installation failed checksum verification.' }
    if (Test-Path -LiteralPath $target) {
        New-Item -ItemType Directory -Path $backupsRoot -Force | Out-Null
        # All directory move paths were resolved and checked above; backups stay outside the pet picker.
        Move-Item -LiteralPath $target -Destination $backup
        $movedOld = $true
    }
    Move-Item -LiteralPath $stage -Destination $target
    $committed = $true
    if ((Get-FileHash -LiteralPath (Join-Path $target 'spritesheet.webp')).Hash -ne $sourceHash -or (Get-FileHash -LiteralPath (Join-Path $target 'pet.json')).Hash -ne $manifestHash) { throw 'Installed files failed checksum verification.' }
} catch {
    $originalError = $_
    if ($committed -and (Test-Path -LiteralPath $target)) {
        $failed = Assert-Under (Join-Path $homeRoot ('.pet-failed-' + $token)) $homeRoot
        Move-Item -LiteralPath $target -Destination $failed
    }
    if ($movedOld -and (Test-Path -LiteralPath $backup) -and !(Test-Path -LiteralPath $target)) { Move-Item -LiteralPath $backup -Destination $target }
    throw $originalError
} finally {
    # The stage is a freshly created child of the explicitly checked Codex home, never an original source.
    if (Test-Path -LiteralPath $stage) { $checkedStage = Assert-Under $stage $homeRoot; Remove-Item -LiteralPath $checkedStage -Recurse -Force }
}
Write-Host ('Installed: ' + $target)
Write-Host ('SHA256: ' + $sourceHash)
if ($movedOld) { Write-Host ('Previous version preserved: ' + $backup) }
Write-Host 'Open Settings > Pets > Refresh, select Xiaohei and HeiXiu, then use /pet.'
Write-Host 'If the preview is correct but the floating pet stays blue, fully Quit and reopen the app.'
Write-Host 'This installer does not change your current pet selection or any app settings.'
