[CmdletBinding()]
param([string]$OutputDirectory = '', [switch]$GroupShare)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot 'releases' }
$version = (Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?$') { throw 'Invalid VERSION.' }
$outRoot = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outRoot -Force | Out-Null
$stageRoot = Join-Path $projectRoot ('build\package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression

function Copy-AllowedFiles([string]$SourceDirectory,[string]$DestinationDirectory,[string[]]$Extensions) {
    if (!(Test-Path -LiteralPath $SourceDirectory -PathType Container)) { return }
    $sourceFull = [IO.Path]::GetFullPath($SourceDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar)
    foreach ($file in @(Get-ChildItem -LiteralPath $sourceFull -File -Recurse -Force)) {
        if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $file.Name -match '^(settings|profile|note)\.(json|txt)$') { continue }
        if ($file.Extension.ToLowerInvariant() -notin $Extensions) { continue }
        $relative = $file.FullName.Substring($sourceFull.Length + 1)
        $destination = Join-Path $DestinationDirectory $relative
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }
}
function Copy-RootFile([string]$Name,[string]$Destination) {
    $source = Join-Path $projectRoot $Name
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw ('Missing release input: ' + $Name) }
    Copy-Item -LiteralPath $source -Destination (Join-Path $Destination $Name)
}
function Copy-Previews([string]$Stage,[string[]]$Names) {
    $destination = Join-Path $Stage 'media'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    foreach ($name in $Names) {
        $source = Join-Path (Join-Path $projectRoot 'media') $name
        if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw ('Missing release preview: media/' + $name) }
        Copy-Item -LiteralPath $source -Destination (Join-Path $destination $name)
    }
}
function New-ReleaseZip([string]$Directory,[string]$Name) {
    $zip = Join-Path $outRoot ($Name + '.zip')
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    # Add explicit forward-slash entries, including dotfiles. Windows PowerShell's
    # Framework ZipFile.CreateFromDirectory can otherwise emit backslash names.
    $container = (Split-Path $Directory -Parent).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $archive = [IO.Compression.ZipFile]::Open($zip,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in @(Get-ChildItem -LiteralPath $Directory -File -Recurse -Force | Sort-Object FullName)) {
            $entryName = $file.FullName.Substring($container.Length + 1).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$entryName,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
    return $zip
}

$nativeName = 'Xiaohei-Native-' + $version
$nativeContainer = Join-Path $stageRoot 'native'
$nativeStage = Join-Path $nativeContainer $nativeName
New-Item -ItemType Directory -Path (Join-Path $nativeStage 'scripts') -Force | Out-Null
Copy-AllowedFiles (Join-Path $projectRoot 'native') (Join-Path $nativeStage 'native') @('.json','.webp')
Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts\Install-Native.ps1') -Destination (Join-Path $nativeStage 'scripts\Install-Native.ps1')
foreach ($name in @('Install-Native.cmd','README.md','LICENSE','ASSET_NOTICE.md','VERSION')) { Copy-RootFile $name $nativeStage }
[IO.File]::WriteAllLines((Join-Path $nativeStage 'QUICKSTART.txt'),@('1. Extract this whole folder.','2. Double-click Install-Native.cmd. No administrator access is needed.','3. Settings > Pets > Refresh > select Xiaohei and HeiXiu. Enter /pet to show it.','Hold and drag left/right to trigger directional running.','If the preview is correct but the desktop pet is still blue, fully Quit and reopen the app.','See docs/install.md and ASSET_NOTICE.md.'),(New-Object Text.UTF8Encoding($false)))
Copy-AllowedFiles (Join-Path $projectRoot 'docs') (Join-Path $nativeStage 'docs') @('.md')
$readmePreviews = @('demo.gif','01-封面.png','变身前后.png','来客动作.png')
$releasePreviews = $readmePreviews + @('demo.mp4','小红书文案.txt','editor-desktop.png','editor-export-preview.png')
Copy-Previews $nativeStage $readmePreviews

$portableName = 'Xiaohei-Portable-' + $version
$portableContainer = Join-Path $stageRoot 'portable'
$portableStage = Join-Path $portableContainer $portableName
New-Item -ItemType Directory -Path $portableStage -Force | Out-Null
$app = Join-Path $projectRoot 'dist\XiaoheiCompanion.exe'
if (!(Test-Path -LiteralPath $app)) { throw 'Build the application before packaging.' }
Copy-Item -LiteralPath $app -Destination (Join-Path $portableStage 'XiaoheiCompanion.exe')
foreach ($name in @('Start-Companion.cmd','README.md','LICENSE','ASSET_NOTICE.md','VERSION')) { Copy-RootFile $name $portableStage }
[IO.File]::WriteAllLines((Join-Path $portableStage 'QUICKSTART.txt'),@('1. Extract this whole folder.','2. Double-click XiaoheiCompanion.exe (or Start-Companion.cmd).','3. Tap for varied responses, double-click to jump, or hold briefly for a gentle response.','Hover to switch form, split the tail, invite Huangshou or Bidiu, or open Tools. Right-click to open the pocket.','Open editor/index.html for offline previews and profile editing.','This standalone program does not read Codex task progress.','See docs/interaction.md, README.md and ASSET_NOTICE.md.'),(New-Object Text.UTF8Encoding($false)))
foreach ($folder in @('assets','editor','profiles')) { Copy-AllowedFiles (Join-Path $projectRoot $folder) (Join-Path $portableStage $folder) @('.wav','.png','.webp','.gif','.mp4','.jpg','.jpeg','.html','.js','.css','.json','.md','.txt') }
Copy-Previews $portableStage $releasePreviews
Copy-AllowedFiles (Join-Path $projectRoot 'docs') (Join-Path $portableStage 'docs') @('.md')

$sourceName = 'Xiaohei-Source-' + $version
$sourceContainer = Join-Path $stageRoot 'source'
$sourceStage = Join-Path $sourceContainer $sourceName
New-Item -ItemType Directory -Path $sourceStage -Force | Out-Null
foreach ($name in @('Build.ps1','Test.ps1','Install-Native.cmd','Start-Companion.cmd','README.md','LICENSE','ASSET_NOTICE.md','VERSION','.gitignore')) { Copy-RootFile $name $sourceStage }
foreach ($folder in @('src','tests','scripts','docs','assets','native','profiles','editor','.github')) {
    Copy-AllowedFiles (Join-Path $projectRoot $folder) (Join-Path $sourceStage $folder) @('.wav','.cs','.ps1','.py','.md','.json','.png','.webp','.gif','.mp4','.jpg','.jpeg','.html','.js','.css','.ico','.manifest','.yml','.yaml','.txt')
}
Copy-Previews $sourceStage $releasePreviews

if ($GroupShare) {
    $shortVersion = ($version -split '\.')[0..1] -join '.'
    $groupName = '小黑桌面版-' + $shortVersion + '-Windows'
    $groupStage = Join-Path $stageRoot $groupName
    New-Item -ItemType Directory -Path $groupStage | Out-Null
    Copy-Item -LiteralPath $app -Destination $groupStage
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Start-Companion.cmd') -Destination (Join-Path $groupStage '启动小黑.cmd')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\group-quickstart.txt') -Destination (Join-Path $groupStage '使用说明.txt')
    foreach ($name in @('LICENSE','ASSET_NOTICE.md')) { Copy-RootFile $name $groupStage }
    $nativeGroupName = '小黑-Codex原生外观'
    $nativeGroupStage = Join-Path $stageRoot $nativeGroupName
    New-Item -ItemType Directory -Path $nativeGroupStage | Out-Null
    foreach ($folder in @('native','scripts')) { Copy-Item -LiteralPath (Join-Path $nativeStage $folder) -Destination $nativeGroupStage -Recurse }
    foreach ($name in @('Install-Native.cmd','LICENSE','ASSET_NOTICE.md')) { Copy-RootFile $name $nativeGroupStage }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\native-quickstart.txt') -Destination (Join-Path $nativeGroupStage '安装说明.txt')
    $artifacts = @((New-ReleaseZip $groupStage $groupName),(New-ReleaseZip $nativeGroupStage $nativeGroupName),(New-ReleaseZip $sourceStage ('小黑桌宠-' + $shortVersion + '-源码')))
} else {
    $artifacts = @((New-ReleaseZip $nativeStage $nativeName),(New-ReleaseZip $portableStage $portableName),(New-ReleaseZip $sourceStage $sourceName))
}
$checksumLines = @($artifacts | ForEach-Object { ((Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()) + '  ' + (Split-Path $_ -Leaf) })
[IO.File]::WriteAllLines((Join-Path $outRoot 'SHA256SUMS.txt'),$checksumLines,(New-Object Text.UTF8Encoding($false)))
$artifacts | ForEach-Object { Write-Output $_ }
Write-Output (Join-Path $outRoot 'SHA256SUMS.txt')
