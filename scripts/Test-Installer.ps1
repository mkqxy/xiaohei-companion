[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $projectRoot ('build\installer-tests\case-' + [Guid]::NewGuid().ToString('N'))
$sandboxHome = Join-Path $testRoot '用户 空间'
$sandboxPackage = Join-Path $testRoot 'package copy'
$installer = Join-Path $PSScriptRoot 'Install-Native.ps1'
$script:checks = 0
function Assert-Case([bool]$Condition,[string]$Message) { $script:checks++; if (!$Condition) { throw $Message } }
function Expect-Failure([scriptblock]$Action,[string]$Message) { $failed=$false; try { & $Action } catch { $failed=$true }; Assert-Case $failed $Message }
New-Item -ItemType Directory -Path $testRoot,$sandboxPackage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'native') -Destination $sandboxPackage -Recurse
$target = Join-Path $sandboxHome 'pets\xiaohei-heixiu'
$manifest = Join-Path $target 'pet.json'
$atlas = Join-Path $target 'spritesheet.webp'
$expectedHash = (Get-FileHash -LiteralPath (Join-Path $projectRoot 'native\xiaohei-heixiu\spritesheet.webp')).Hash

& $installer -PackageRoot $projectRoot -CodexHome $sandboxHome
Assert-Case ((Get-FileHash -LiteralPath $atlas).Hash -eq $expectedHash) 'Fresh install must preserve exact atlas bytes.'
$firstWrite = (Get-Item -LiteralPath $atlas).LastWriteTimeUtc
& $installer -PackageRoot $projectRoot -CodexHome $sandboxHome
Assert-Case ((Get-Item -LiteralPath $atlas).LastWriteTimeUtc -eq $firstWrite) 'Identical reinstall must not rewrite files.'
Assert-Case (!(Test-Path -LiteralPath (Join-Path $sandboxHome 'pet-backups'))) 'Identical reinstall must not create a backup.'

$oldManifest = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8
[IO.File]::WriteAllText($manifest,($oldManifest -replace '小黑和嘿咻','旧版测试'),(New-Object Text.UTF8Encoding($false)))
$oldManifestHash = (Get-FileHash -LiteralPath $manifest).Hash
& $installer -PackageRoot $projectRoot -CodexHome $sandboxHome
$backups = @(Get-ChildItem -LiteralPath (Join-Path $sandboxHome 'pet-backups') -Directory)
Assert-Case ($backups.Count -eq 1) 'Changed install must preserve one old directory.'
Assert-Case ((Get-FileHash -LiteralPath (Join-Path $backups[0].FullName 'pet.json')).Hash -eq $oldManifestHash) 'Backup must contain the exact old manifest.'
Assert-Case ((Get-FileHash -LiteralPath $atlas).Hash -eq $expectedHash) 'Replacement must install the expected atlas.'
Assert-Case (@(Get-ChildItem -LiteralPath (Join-Path $sandboxHome 'pets') -Directory).Count -eq 1) 'Backups must stay outside the native picker.'

$badManifestPath = Join-Path $sandboxPackage 'native\xiaohei-heixiu\pet.json'
$badManifest = Get-Content -LiteralPath $badManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$badManifest.spritesheetPath = '..\outside.webp'
[IO.File]::WriteAllText($badManifestPath,($badManifest | ConvertTo-Json),(New-Object Text.UTF8Encoding($false)))
Expect-Failure { & $installer -PackageRoot $sandboxPackage -CodexHome $sandboxHome } 'Path traversal in a manifest must be rejected.'
Assert-Case ((Get-FileHash -LiteralPath $atlas).Hash -eq $expectedHash) 'Rejected manifest must leave the installed atlas untouched.'
Copy-Item -LiteralPath (Join-Path $projectRoot 'native\xiaohei-heixiu\pet.json') -Destination $badManifestPath -Force
$badAtlasPath = Join-Path $sandboxPackage 'native\xiaohei-heixiu\spritesheet.webp'
[IO.File]::WriteAllBytes($badAtlasPath,[byte[]](1,2,3,4))
Expect-Failure { & $installer -PackageRoot $sandboxPackage -CodexHome $sandboxHome } 'Invalid WebP must be rejected before installation.'
Assert-Case ((Get-FileHash -LiteralPath $atlas).Hash -eq $expectedHash) 'Rejected WebP must leave the installed atlas untouched.'

# Simulate an OS failure at the staged-directory commit, after the old target was preserved.
[IO.File]::WriteAllText($manifest,($oldManifest -replace '小黑和嘿咻','回滚测试'),(New-Object Text.UTF8Encoding($false)))
$preFailureManifestHash = (Get-FileHash -LiteralPath $manifest).Hash
$global:XiaoheiInstallerTestFailCommit = $true
function Move-Item {
    [CmdletBinding()]param([string]$LiteralPath,[string]$Destination)
    if ($global:XiaoheiInstallerTestFailCommit -and (Split-Path $LiteralPath -Leaf) -like '.pet-install-*') { $global:XiaoheiInstallerTestFailCommit=$false; throw 'Simulated commit failure.' }
    Microsoft.PowerShell.Management\Move-Item -LiteralPath $LiteralPath -Destination $Destination
}
try { Expect-Failure { & $installer -PackageRoot $projectRoot -CodexHome $sandboxHome } 'Commit failure must report a failure.' }
finally { Remove-Item Function:\Move-Item; Remove-Variable -Name XiaoheiInstallerTestFailCommit -Scope Global -ErrorAction SilentlyContinue }
Assert-Case ((Get-FileHash -LiteralPath $manifest).Hash -eq $preFailureManifestHash) 'Commit failure must restore the exact old manifest.'
Assert-Case ((Get-FileHash -LiteralPath $atlas).Hash -eq $expectedHash) 'Commit failure must restore the old atlas.'
Assert-Case (@(Get-ChildItem -LiteralPath $sandboxHome -Directory -Force | Where-Object Name -Like '.pet-install-*').Count -eq 0) 'Failed stage must be removed after rollback.'
Write-Output ('PASS: ' + $script:checks + ' isolated installer assertions. Sandbox: ' + $testRoot)
