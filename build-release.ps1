<#
.SYNOPSIS
    Builds the Noumenon release artifacts: the end-user installer, the portable zips and the
    release notes.

.DESCRIPTION
    1. Builds the solution for x64 Release - the build lays out the two VST2 deploy folders and
       the VST3 bundle - and publishes the standalone (framework-dependent).
    2. Runs the three test suites and the native plugin load test (skip with -SkipTests).
    3. Downloads the bundled prerequisites into installer\redist (cached; gitignored).
    4. Compiles installer\Noumenon.iss with Inno Setup 6.
    5. Packs the portable zips (VST3, VST2, standalone), each with its own copy of the factory
       content, for users who install by hand.
    6. Writes RELEASE-NOTES-<version>.md from the version's section of CHANGELOG.md, with the
       file sizes and SHA-256 hashes appended.
    7. With -TestInstall, compiles a sandboxed variant of the installer, installs it silently
       into a temporary folder (no elevation, nothing outside that folder but an HKCU value),
       checks the layout, loads the installed plugins with tools\Test-PluginLoad.ps1, uninstalls
       and checks that nothing is left.

    Output lands in dist\Noumenon-<version>\, one folder per release, so earlier releases stay
    as they were. The version comes from <Version> in Directory.Build.props, the single place it
    is set.

.PARAMETER RefreshDownloads
    Downloads the two redistributables again instead of using the copies cached in
    installer\redist (after Microsoft ships a newer runtime).

.PARAMETER NoVst2
    Leaves the VST2 plugins out of the installer and the zips: only the standalone and the VST3
    module ship.

.PARAMETER SignCommand
    Signs our binaries and the installer with Authenticode: a command line with $f where the file
    name goes, for example 'signtool sign /fd sha256 /tr http://timestamp.digicert.com /td sha256 /a $f'.
    Without it the release is unsigned and SmartScreen warns on first run.

.EXAMPLE
    .\build-release.ps1
    .\build-release.ps1 -SkipDownload -SkipTests     # reuse installer\redist, skip the test suites
    .\build-release.ps1 -NoVst2 -TestInstall
#>
[CmdletBinding()]
param(
    [switch]$SkipDownload,
    [switch]$RefreshDownloads,
    [switch]$SkipTests,
    [switch]$SkipZip,
    [switch]$NoVst2,
    [switch]$TestInstall,
    [string]$SignCommand
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$solution          = Join-Path $root 'Noumenon.slnx'
$standaloneProject = Join-Path $root 'Noumenon.Standalone\Noumenon.Standalone.csproj'
$standaloneOut     = Join-Path $root 'Noumenon.Standalone\bin\x64\Release\net10.0-windows\publish'
$vst3Bundle        = Join-Path $root 'Noumenon.Vst3\bin\x64\Release\net10.0-windows\deploy\Noumenon.vst3'
$vst2Deploy        = Join-Path $root 'Noumenon.Vst2\bin\x64\Release\net10.0-windows\deploy'
$vst2FxDeploy      = Join-Path $root 'Noumenon.Vst2.Fx\bin\x64\Release\net10.0-windows\deploy'
$contentDir        = Join-Path $root 'content'
$installerDir      = Join-Path $root 'installer'
$issFile           = Join-Path $installerDir 'Noumenon.iss'
$licensesDir       = Join-Path $installerDir 'licenses'
$redistDir         = Join-Path $installerDir 'redist'
$distDir           = Join-Path $root 'dist'
$changelog         = Join-Path $root 'CHANGELOG.md'

# Prerequisites bundled into the installer. Both are permanent Microsoft "latest" redirects.
$prerequisites = @(
    @{ Name = 'windowsdesktop-runtime-win-x64.exe'
       Url  = 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe'
       What = '.NET 10 Desktop Runtime (x64)' },
    # The v14 channel, not Visual Studio 2022's (aka.ms/vs/17 still serves 14.44): VST.NET's
    # bridge is linked with the 14.5x toolset and needs a runtime at least that new.
    @{ Name = 'VC_redist.x64.exe'
       Url  = 'https://aka.ms/vc14/vc_redist.x64.exe'
       What = 'Visual C++ v14 Redistributable (x64)' }
)

function Write-Step($message)
{
    Write-Host ''
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Format-Size($bytes)
{
    return '{0:N1} MB' -f ($bytes / 1MB)
}

function Invoke-Checked([string]$what, [scriptblock]$command)
{
    & $command
    if ($LASTEXITCODE -ne 0)
    {
        throw "$what failed with exit code $LASTEXITCODE."
    }
}

# The SDK may be a user-local install that is not on PATH, and a machine-wide dotnet.exe may be a
# runtime-only muxer: take the first candidate that actually has a .NET 10 SDK.
function Find-Dotnet
{
    $candidates = @()
    if ($env:DOTNET_ROOT)
    {
        $candidates += Join-Path $env:DOTNET_ROOT 'dotnet.exe'
    }
    $onPath = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($onPath)
    {
        $candidates += $onPath.Source
    }
    $candidates += Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    $candidates += Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'

    foreach ($candidate in $candidates)
    {
        if (-not (Test-Path $candidate))
        {
            continue
        }
        $sdks = & $candidate --list-sdks 2>$null
        if ($sdks | Where-Object { $_ -match '^10\.' })
        {
            return $candidate
        }
    }
    throw 'No .NET 10 SDK found (looked at DOTNET_ROOT, PATH, %USERPROFILE%\.dotnet and Program Files).'
}

function Find-Iscc
{
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($key in @('HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
                       'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
                       'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'))
    {
        $location = (Get-ItemProperty $key -ErrorAction SilentlyContinue).InstallLocation
        if ($location)
        {
            $candidates += Join-Path $location 'ISCC.exe'
        }
    }
    $iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc)
    {
        throw 'Inno Setup 6 not found. Install it with:  winget install --id JRSoftware.InnoSetup -e'
    }
    return $iscc
}

function Remove-Symbols([string]$folder)
{
    Get-ChildItem $folder -Recurse -Filter *.pdb -ErrorAction SilentlyContinue | Remove-Item -Force
}

function Assert-File([string]$path, [string]$what)
{
    if (-not (Test-Path $path))
    {
        throw "$what is missing: $path"
    }
}

# Our own binaries - the standalone, our assemblies, the plugin entry points named after us - are
# signed; third-party DLLs stay as their vendors shipped them.
function Invoke-Sign([string[]]$folders)
{
    if (-not $SignCommand)
    {
        return
    }
    foreach ($folder in $folders)
    {
        $files = Get-ChildItem $folder -Recurse -File | Where-Object { $_.Name -like 'Noumenon*' -and $_.Extension -in '.exe', '.dll', '.vst3', '.vst2' }
        foreach ($file in $files)
        {
            Write-Host "    sign     $($file.Name)"
            Invoke-Checked "Signing $($file.Name)" { Invoke-Expression ($SignCommand.Replace('$f', "`"$($file.FullName)`"")) }
        }
    }
}

function New-Zip([string]$zipPath, [hashtable]$layout)
{
    if (Test-Path $zipPath)
    {
        Remove-Item $zipPath -Force
    }

    # Staged under named folders so the zip extracts into them, not into loose DLLs.
    $stage = Join-Path ([IO.Path]::GetTempPath()) ('noumenon-zip-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    try
    {
        foreach ($target in $layout.Keys)
        {
            $destination = Join-Path $stage $target
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            Copy-Item (Join-Path $layout[$target] '*') $destination -Recurse
        }
        Remove-Symbols $stage
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zipPath
    }
    finally
    {
        Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Get-ReleaseNotes([string]$version)
{
    if (-not (Test-Path $changelog))
    {
        return "Noumenon $version"
    }
    $lines = Get-Content $changelog -Encoding UTF8
    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++)
    {
        if ($lines[$i] -match "^## \[?$([regex]::Escape($version))\]?(\s|$)")
        {
            $start = $i + 1
            break
        }
    }
    if ($start -lt 0)
    {
        Write-Warning "CHANGELOG.md has no section for $version."
        return "Noumenon $version"
    }
    $end = $lines.Count
    for ($i = $start; $i -lt $lines.Count; $i++)
    {
        if ($lines[$i] -match '^## ')
        {
            $end = $i
            break
        }
    }
    # An empty section, or a heading on the last line, has nothing to copy ($start..($end - 1)
    # would count downwards and pull the headings in).
    if ($start -ge $end)
    {
        Write-Warning "The CHANGELOG.md section for $version is empty."
        return "Noumenon $version"
    }
    return (($lines[$start..($end - 1)]) -join "`n").Trim()
}

# --- version ----------------------------------------------------------------------------------

[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$version = ($props.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
if (-not $version)
{
    throw 'Could not read <Version> from Directory.Build.props.'
}
$version = $version.Trim()
$releaseDir = Join-Path $distDir "Noumenon-$version"
$includeVst2 = -not $NoVst2
$dotnet = Find-Dotnet
Write-Step "Building Noumenon $version  (dotnet: $dotnet)"

# --- 1. build and publish ---------------------------------------------------------------------

Write-Step 'Building the solution (x64 Release)'
Invoke-Checked 'dotnet build' { & $dotnet build $solution -c Release -p:Platform=x64 --nologo -v minimal }

Write-Step 'Publishing the standalone'
if (Test-Path $standaloneOut)
{
    Remove-Item $standaloneOut -Recurse -Force
}
Invoke-Checked 'dotnet publish' { & $dotnet publish $standaloneProject -c Release -p:Platform=x64 --nologo -v minimal -o $standaloneOut }

Remove-Symbols $standaloneOut
Remove-Symbols $vst3Bundle
Assert-File (Join-Path $standaloneOut 'Noumenon.Standalone.exe') 'The standalone'
Assert-File (Join-Path $vst3Bundle 'Contents\x86_64-win\Noumenon.vst3') 'The VST3 proxy'
Assert-File (Join-Path $vst3Bundle 'Contents\x86_64-win\Noumenon.dll') 'The VST3 assembly'
Assert-File (Join-Path $contentDir 'presets') 'The factory presets'
if ($includeVst2)
{
    Remove-Symbols $vst2Deploy
    Remove-Symbols $vst2FxDeploy
    Assert-File (Join-Path $vst2Deploy 'Noumenon.Vst2.dll') 'The VST2 bridge'
    Assert-File (Join-Path $vst2FxDeploy 'NoumenonFx.Vst2.dll') 'The VST2 FX bridge'
}
foreach ($folder in @($standaloneOut, $vst3Bundle, $contentDir))
{
    $files = Get-ChildItem $folder -Recurse -File
    Write-Host ("    {0,-60} {1,4} files, {2}" -f (Resolve-Path $folder -Relative), $files.Count, (Format-Size ($files | Measure-Object -Property Length -Sum).Sum))
}

# --- 2. tests -----------------------------------------------------------------------------------

if (-not $SkipTests)
{
    Write-Step 'Running the test suites'
    Invoke-Checked 'Noumenon.Tests' { & $dotnet test (Join-Path $root 'Noumenon.Tests\Noumenon.Tests.csproj') -c Release --nologo -v quiet }
    Invoke-Checked 'Noumenon.Vst2.Tests' { & $dotnet test (Join-Path $root 'Noumenon.Vst2.Tests\Noumenon.Vst2.Tests.csproj') -c Release -p:Platform=x64 --nologo -v quiet }
    Invoke-Checked 'Noumenon.Vst3.Tests' { & $dotnet test (Join-Path $root 'Noumenon.Vst3.Tests\Noumenon.Vst3.Tests.csproj') -c Release -p:Platform=x64 --nologo -v quiet }

    Write-Step 'Loading the Release plugins natively (tools\Test-PluginLoad.ps1)'
    $plugins = @($vst3Bundle)
    if ($includeVst2)
    {
        $plugins += (Join-Path $vst2Deploy 'Noumenon.Vst2.dll'), (Join-Path $vst2FxDeploy 'NoumenonFx.Vst2.dll')
    }
    Invoke-Checked 'Test-PluginLoad' { & "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tools\Test-PluginLoad.ps1') -Configuration Release -Dll ($plugins -join ';') }
}

# --- 3. sign ------------------------------------------------------------------------------------

if ($SignCommand)
{
    Write-Step 'Signing our binaries'
    $signed = @($standaloneOut, $vst3Bundle)
    if ($includeVst2)
    {
        $signed += $vst2Deploy, $vst2FxDeploy
    }
    Invoke-Sign $signed
}

# --- 4. prerequisites ---------------------------------------------------------------------------

Write-Step 'Preparing bundled prerequisites'
if (-not (Test-Path $redistDir))
{
    New-Item -ItemType Directory -Path $redistDir -Force | Out-Null
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

foreach ($prerequisite in $prerequisites)
{
    $target = Join-Path $redistDir $prerequisite.Name
    if ($RefreshDownloads -and (Test-Path $target))
    {
        Remove-Item $target -Force
    }
    if (Test-Path $target)
    {
        Write-Host ("    cached   {0} ({1})" -f $prerequisite.Name, (Format-Size (Get-Item $target).Length))
        continue
    }
    if ($SkipDownload)
    {
        throw "$($prerequisite.Name) is missing from $redistDir and -SkipDownload was given."
    }

    Write-Host "    download $($prerequisite.What)..."
    $progressPreferenceBefore = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'      # Invoke-WebRequest is ~10x faster without it
    try
    {
        Invoke-WebRequest -Uri $prerequisite.Url -OutFile $target -UseBasicParsing
    }
    finally
    {
        $ProgressPreference = $progressPreferenceBefore
    }
    Write-Host ("    saved    {0} ({1})" -f $prerequisite.Name, (Format-Size (Get-Item $target).Length))
}

# --- 5. installer -------------------------------------------------------------------------------

Write-Step 'Compiling the installer'
$iscc = Find-Iscc
if (-not (Test-Path $releaseDir))
{
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}
# Whatever an earlier run left for this version goes, so the notes list only what this run made.
Get-ChildItem $releaseDir -File | Where-Object { $_.Name -like "Noumenon-$version-*" -or $_.Name -eq "RELEASE-NOTES-$version.md" } | Remove-Item -Force
$artifacts = @()

$defines = @(
    "/DAppVersion=$version",
    "/DStandaloneDir=$standaloneOut",
    "/DVst3Bundle=$vst3Bundle",
    "/DVst2Dir=$vst2Deploy",
    "/DVst2FxDir=$vst2FxDeploy",
    "/DContentDir=$contentDir",
    "/DLicensesDir=$licensesDir",
    "/DRedistDir=$redistDir"
)
if ($includeVst2)
{
    $defines += '/DIncludeVst2'
}
# The sign tool's name and its command travel together, and only to the real installer: the
# sandboxed build for -TestInstall is never signed.
$signArguments = @()
if ($SignCommand)
{
    $signArguments += '/DSignTool=noumenon', "/Snoumenon=$SignCommand"
}

Invoke-Checked 'ISCC' { & $iscc '/Q' @defines @signArguments "/DOutputDir=$releaseDir" $issFile }

$setupExe = Join-Path $releaseDir "Noumenon-$version-Setup.exe"
Assert-File $setupExe 'The installer'
$artifacts += $setupExe

# --- 6. portable zips ---------------------------------------------------------------------------

if (-not $SkipZip)
{
    Write-Step 'Packing the portable zips'
    $zip = Join-Path $releaseDir "Noumenon-$version-VST3-portable.zip"
    New-Zip $zip @{ 'Noumenon.vst3' = $vst3Bundle }
    $artifacts += $zip
    if ($includeVst2)
    {
        $zip = Join-Path $releaseDir "Noumenon-$version-VST2-portable.zip"
        New-Zip $zip @{ 'Noumenon VST2\Noumenon' = $vst2Deploy; 'Noumenon VST2\Noumenon FX' = $vst2FxDeploy }
        $artifacts += $zip
    }
    $zip = Join-Path $releaseDir "Noumenon-$version-Standalone-portable.zip"
    New-Zip $zip @{ 'Noumenon' = $standaloneOut; 'Noumenon\Content' = $contentDir; 'Noumenon\Licenses' = $licensesDir }
    $artifacts += $zip
}

# --- 7. release notes ---------------------------------------------------------------------------

Write-Step 'Writing the release notes'
$table = @('', '## Files', '', '| File | Size | SHA-256 |', '|---|---|---|')
foreach ($artifact in ($artifacts | ForEach-Object { Get-Item $_ } | Sort-Object Name))
{
    $hash = (Get-FileHash $artifact.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $table += "| ``$($artifact.Name)`` | $(Format-Size $artifact.Length) | ``$hash`` |"
}
$notesPath = Join-Path $releaseDir "RELEASE-NOTES-$version.md"
$notes = (Get-ReleaseNotes $version) + "`n" + ($table -join "`n") + "`n"
[IO.File]::WriteAllText($notesPath, $notes, (New-Object Text.UTF8Encoding $false))
Write-Host "    $notesPath"

# --- 8. sandboxed install test ------------------------------------------------------------------

if ($TestInstall)
{
    Write-Step 'Testing the installer in a sandbox'
    $sandbox = Join-Path ([IO.Path]::GetTempPath()) ('noumenon-install-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    $sandboxOut = Join-Path $sandbox 'setup'
    New-Item -ItemType Directory -Path $sandboxOut -Force | Out-Null
    Invoke-Checked 'ISCC (sandbox)' { & $iscc '/Q' @defines "/DSandbox=$sandbox" "/DOutputDir=$sandboxOut" '/FNoumenon-sandbox-setup' $issFile }

    $components = 'standalone,vst3,content'
    if ($includeVst2)
    {
        $components += ',vst2\instrument,vst2\effect'
    }
    $log = Join-Path $sandbox 'install.log'
    $process = Start-Process (Join-Path $sandboxOut 'Noumenon-sandbox-setup.exe') -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$log`"", "/COMPONENTS=`"$components`"" -Wait -PassThru
    if ($process.ExitCode -ne 0)
    {
        throw "The sandboxed install failed with exit code $($process.ExitCode); see $log."
    }

    $app = Join-Path $sandbox 'Program Files\Noumenon'
    $bundle = Join-Path $sandbox 'Common Files\VST3\Noumenon.vst3'
    $expected = @(
        (Join-Path $app 'Noumenon.Standalone.exe'),
        (Join-Path $app 'Noumenon.ico'),
        (Join-Path $app 'unins000.exe'),
        (Join-Path $app 'Content\presets'),
        (Join-Path $app 'Licenses\THIRD-PARTY-NOTICES.txt'),
        (Join-Path $bundle 'Contents\x86_64-win\Noumenon.vst3'),
        (Join-Path $bundle 'Contents\x86_64-win\Noumenon.dll')
    )
    $plugins = @($bundle)
    if ($includeVst2)
    {
        $expected += (Join-Path $sandbox 'Common Files\VST2\Noumenon\Noumenon.Vst2.dll'), (Join-Path $sandbox 'Common Files\VST2\Noumenon FX\NoumenonFx.Vst2.dll')
        $plugins += (Join-Path $sandbox 'Common Files\VST2\Noumenon\Noumenon.Vst2.dll'), (Join-Path $sandbox 'Common Files\VST2\Noumenon FX\NoumenonFx.Vst2.dll')
    }
    foreach ($path in $expected)
    {
        Assert-File $path 'An installed file'
    }
    if (Test-Path (Join-Path $bundle 'Contents\x86_64-win\content'))
    {
        throw 'The installed VST3 bundle carries its own content; it should read the shared copy.'
    }
    $registered = (Get-ItemProperty 'HKCU:\Software\Noumenon' -ErrorAction SilentlyContinue).ContentFolder
    if ($registered -ne (Join-Path $app 'Content'))
    {
        throw "The content folder is registered as '$registered', expected '$(Join-Path $app 'Content')'."
    }
    $installedFiles = Get-ChildItem $sandbox -Recurse -File | Where-Object { $_.FullName -notlike "$sandboxOut*" -and $_.FullName -ne $log }
    Write-Host ("    installed {0} files, {1}; content registered at {2}" -f $installedFiles.Count, (Format-Size ($installedFiles | Measure-Object -Property Length -Sum).Sum), $registered)

    Invoke-Checked 'Test-PluginLoad (installed)' { & "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tools\Test-PluginLoad.ps1') -Dll ($plugins -join ';') }

    # Each installed plugin's own engine, asked where its factory content is: with no content
    # beside it, the answer must be the shared copy the registry names. Run from the sandbox, so
    # the working directory cannot point at the repository's content.
    $factoryTool = Join-Path $root 'Noumenon.Factory\bin\x64\Release\net10.0-windows\Noumenon.Factory.dll'
    $pluginFolders = @(Join-Path $bundle 'Contents\x86_64-win')
    if ($includeVst2)
    {
        $pluginFolders += (Join-Path $sandbox 'Common Files\VST2\Noumenon'), (Join-Path $sandbox 'Common Files\VST2\Noumenon FX')
    }
    Push-Location $sandbox
    try
    {
        foreach ($folder in $pluginFolders)
        {
            Invoke-Checked "Factory content seen from $folder" { & $dotnet $factoryTool locate $folder --expect (Join-Path $app 'Content') }
        }
    }
    finally
    {
        Pop-Location
    }

    # The uninstaller copies itself to %TEMP% and returns at once; wait for the folder to go.
    Start-Process (Join-Path $app 'unins000.exe') -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait
    $deadline = (Get-Date).AddSeconds(60)
    while ((Test-Path (Join-Path $app 'unins000.exe')) -and (Get-Date) -lt $deadline)
    {
        Start-Sleep -Milliseconds 500
    }
    $left = Get-ChildItem $sandbox -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notlike "$sandboxOut*" -and $_.FullName -ne $log }
    if ($left)
    {
        throw "The uninstaller left files behind: $(($left | Select-Object -First 5 | ForEach-Object { $_.FullName }) -join ', ')"
    }
    if (Test-Path 'HKCU:\Software\Noumenon')
    {
        throw 'The uninstaller left HKCU:\Software\Noumenon behind.'
    }
    Write-Host '    uninstalled cleanly'
    Remove-Item $sandbox -Recurse -Force -ErrorAction SilentlyContinue
}

# --- summary ------------------------------------------------------------------------------------

Write-Step 'Done'
Write-Host "    $releaseDir"
Get-ChildItem $releaseDir -File | Where-Object { $_.Name -like "*$version*" } | ForEach-Object {
    Write-Host ("    {0}  ({1})" -f $_.Name, (Format-Size $_.Length))
}
Write-Host ''
Write-Host '    Upload these to:  https://github.com/simasgg/Noumenon/releases' -ForegroundColor DarkGray
