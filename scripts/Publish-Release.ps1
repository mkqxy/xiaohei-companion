[CmdletBinding()]
param(
    [string]$PackageDirectory = '',
    [string]$OutputDirectory = '',
    [string]$Repository = '',
    [string]$NotesFile = '',
    [switch]$IncludeSource,
    [switch]$IncludeNative,
    [switch]$Publish
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$version = (Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^(\d+)\.(\d+)\.(\d+)(?:-[A-Za-z0-9.]+)?$') { throw 'Invalid VERSION.' }
$fileVersion = $Matches[1] + '.' + $Matches[2] + '.' + $Matches[3] + '.0'
$shortVersion = $Matches[1] + '.' + $Matches[2]
$tag = 'v' + $version
$prerelease = $version.Contains('-')
if (!$PackageDirectory) { $PackageDirectory = Join-Path $projectRoot 'releases' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot ('releases\publish\' + $tag) }
$packages = [IO.Path]::GetFullPath($PackageDirectory)
$outRoot = [IO.Path]::GetFullPath($OutputDirectory)
$groupName = '小黑桌面版-' + $shortVersion + '-Windows'
$windowsZip = Join-Path $packages ($groupName + '.zip')
$sourceZip = Join-Path $packages ('小黑桌宠-' + $shortVersion + '-源码.zip')
$nativeZip = Join-Path $packages '小黑-Codex原生外观.zip'
if (!(Test-Path -LiteralPath $windowsZip -PathType Leaf)) {
    throw ('Run scripts/Package-Release.ps1 -GroupShare first. Missing: ' + $windowsZip)
}
if ($IncludeSource -and !(Test-Path -LiteralPath $sourceZip -PathType Leaf)) { throw ('Missing: ' + $sourceZip) }
if ($IncludeNative -and !(Test-Path -LiteralPath $nativeZip -PathType Leaf)) { throw ('Missing: ' + $nativeZip) }
if (!$NotesFile) {
    $versionNotes = Join-Path $projectRoot ('docs\releases\' + $version + '.md')
    if (Test-Path -LiteralPath $versionNotes -PathType Leaf) { $NotesFile = $versionNotes }
}
if ($NotesFile) {
    $NotesFile = (Resolve-Path -LiteralPath $NotesFile -ErrorAction Stop).Path
    if (!(Test-Path -LiteralPath $NotesFile -PathType Leaf)) { throw 'NotesFile must be a file.' }
}
if ($Repository -and $Repository -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9._-]+$') {
    throw 'Repository must use the GitHub OWNER/REPO format.'
}
if ($Publish -and !$Repository) { throw 'Use -Repository OWNER/REPO when publishing.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$stageRoot = Join-Path $projectRoot ('build\publish-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead($windowsZip)
try {
    $entry = $archive.GetEntry($groupName + '/XiaoheiCompanion.exe')
    if ($null -eq $entry) { throw 'The Windows ZIP does not contain the expected executable.' }
    $checkExe = Join-Path $stageRoot 'check.exe'
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $checkExe)
    $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($checkExe)
    if ($info.FileVersion -ne $fileVersion -or $info.ProductVersion -ne $version) {
        throw ('Package version differs from VERSION. Rebuild and package ' + $version + ' before continuing.')
    }
} finally { $archive.Dispose() }

if ($IncludeSource) {
    $archive = [IO.Compression.ZipFile]::OpenRead($sourceZip)
    try {
        $entry = $archive.GetEntry('Xiaohei-Source-' + $version + '/VERSION')
        if ($null -eq $entry) { throw 'The source ZIP does not match VERSION.' }
        $reader = New-Object IO.StreamReader($entry.Open())
        try { $sourceVersion = $reader.ReadToEnd().Trim() } finally { $reader.Dispose() }
        if ($sourceVersion -ne $version) { throw 'The source ZIP does not match VERSION.' }
    } finally { $archive.Dispose() }
}

Copy-Item -LiteralPath $windowsZip -Destination (Join-Path $stageRoot 'Xiaohei-Windows.zip')
$assetNames = @('Xiaohei-Windows.zip')
if ($IncludeSource) {
    Copy-Item -LiteralPath $sourceZip -Destination (Join-Path $stageRoot 'Xiaohei-Source.zip')
    $assetNames += 'Xiaohei-Source.zip'
}
if ($IncludeNative) {
    Copy-Item -LiteralPath $nativeZip -Destination (Join-Path $stageRoot 'Xiaohei-Codex-Pet.zip')
    $assetNames += 'Xiaohei-Codex-Pet.zip'
}
$checksums = @($assetNames | ForEach-Object {
    (Get-FileHash -LiteralPath (Join-Path $stageRoot $_) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_
})
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllLines((Join-Path $stageRoot 'SHA256SUMS.txt'), $checksums, $utf8)
$assetNames += 'SHA256SUMS.txt'
$notes = @(
    ('# 小黑桌面伙伴 ' + $version),
    '',
    'Windows 10 / 11：下载 Xiaohei-Windows.zip，全部解压，再双击「启动小黑.cmd」。',
    '',
    '更新前请先右键旧版小黑，选择「退出小黑」。已有设置与随手记保存在本机用户目录。',
    '',
    '《罗小黑战记》非官方同人作品。原创代码采用 MIT 许可；角色与衍生素材权益见包内 ASSET_NOTICE.md。'
)
if ($NotesFile) { $notes = @(Get-Content -LiteralPath $NotesFile -Encoding UTF8) }
if ($Repository) {
    $releasePage = 'https://github.com/' + $Repository + '/releases/latest'
    $downloadUrl = $releasePage + '/download/Xiaohei-Windows.zip'
    if ($prerelease) {
        $releasePage = 'https://github.com/' + $Repository + '/releases/tag/' + $tag
        $downloadUrl = 'https://github.com/' + $Repository + '/releases/download/' + $tag + '/Xiaohei-Windows.zip'
    }
    $notes += @('', ('[下载桌面版](' + $downloadUrl + ')'), ('[查看发行说明](' + $releasePage + ')'))
}
[IO.File]::WriteAllLines((Join-Path $stageRoot 'release-notes.md'), $notes, $utf8)
New-Item -ItemType Directory -Path $outRoot -Force | Out-Null
foreach ($name in $assetNames + @('release-notes.md')) {
    Copy-Item -LiteralPath (Join-Path $stageRoot $name) -Destination (Join-Path $outRoot $name) -Force
}
Write-Output ('Prepared version ' + $version + ' in ' + $outRoot)
foreach ($name in $assetNames) { Write-Output (Join-Path $outRoot $name) }
if ($Repository) {
    Write-Output ('Release page: ' + $releasePage)
    Write-Output ('Download: ' + $downloadUrl)
}
if (!$Publish) { return }

$gh = Get-Command gh -ErrorAction Stop
& $gh.Source auth status --hostname github.com
if ($LASTEXITCODE -ne 0) { throw 'Sign into GitHub CLI with gh auth login before publishing.' }
# Create a draft first, so a failed upload leaves the previous public download intact.
$assets = @($assetNames | ForEach-Object { Join-Path $outRoot $_ })
$arguments = @('release', 'create', $tag, '--repo', $Repository,
    '--verify-tag', '--draft', '--title', ('小黑桌面伙伴 ' + $version),
    '--notes-file', (Join-Path $outRoot 'release-notes.md')) + $assets
if ($prerelease) { $arguments += '--prerelease' }
& $gh.Source @arguments
if ($LASTEXITCODE -ne 0) {
    throw ('Release creation or upload failed. An existing release is never overwritten. Use a new VERSION, or inspect the draft for ' + $tag + ' in GitHub before retrying.')
}
$arguments = @('release', 'edit', $tag, '--repo', $Repository, '--draft=false')
if ($prerelease) { $arguments += '--latest=false' } else { $arguments += '--latest' }
& $gh.Source @arguments
if ($LASTEXITCODE -ne 0) { throw ('The uploaded draft could not be published. Inspect ' + $tag + ' in GitHub.') }
Write-Output ('Published ' + $releasePage)
