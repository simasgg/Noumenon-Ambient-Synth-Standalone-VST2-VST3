; Noumenon - Inno Setup script for the end-user installer.
;
; One offline .exe that installs, as the user picks:
;
;   * the standalone application        -> {commonpf64}\Noumenon
;   * the VST3 module (Noumenon and     -> {commoncf64}\VST3\Noumenon.vst3, the folder every
;     Noumenon FX in one bundle)            VST3 host scans
;   * the two VST2 plugins (optional,   -> {commoncf64}\VST2\Noumenon and \Noumenon FX, folders
;     off by default)                       FL Studio scans by default
;   * the factory content (presets and  -> {commonpf64}\Noumenon\Content, once for all of them;
;     samples), always                      its path goes to HKLM\SOFTWARE\Noumenon\ContentFolder,
;                                           where the plugins look when no content sits next to them
;
; and silently installs what is missing of the two prerequisites (both embedded, so no internet
; is needed at install time):
;
;   * .NET 10 Desktop Runtime (x64) - everything here is framework-dependent. A self-contained or
;     app-local runtime is NOT an option for the plugins: VST.NET's ijwhost calls
;     hostfxr_initialize_for_runtime_config, which rejects self-contained configs (0x80008093), and
;     both it and NPlug's proxy find hostfxr through the machine-wide install registered under
;     HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64 (32-bit registry view).
;   * Visual C++ v14 Redistributable (x64) - only for the VST2 plugins: VST.NET's native bridge
;     imports VCRUNTIME140.dll, and it is linked with a recent toolset, so any older 14.x runtime
;     (VC++ 2015's, or a bare vcruntime140.dll another program dropped in System32) is not enough:
;     the installed runtime must be at least the bundled redistributable's version, which the
;     compiler reads from the file. The standalone and NPlug's statically linked VST3 proxy need
;     nothing.
;
; Build it with build-release.ps1 in the repository root, which passes the defines below. Two
; more defines exist for testing: Sandbox=<folder> installs everything under that folder without
; elevation (the registry value goes to HKCU), and ForcePrerequisites runs the two redistributable
; installers even when the machine has them (point RedistDir at stand-ins). The sandbox has an
; AppId of its own and ignores the previous install folder, so it never meets a real
; installation on the same machine.

#define AppName       "Noumenon"
#define AppPublisher  "Noumenon"
#define AppUrl        "https://github.com/simasgg/Noumenon"

#ifndef AppVersion
  #define AppVersion  "0.5.2"
#endif
#ifndef StandaloneDir
  #define StandaloneDir "..\Noumenon.Standalone\bin\x64\Release\net10.0-windows\publish"
#endif
#ifndef Vst3Bundle
  #define Vst3Bundle  "..\Noumenon.Vst3\bin\x64\Release\net10.0-windows\deploy\Noumenon.vst3"
#endif
#ifndef Vst2Dir
  #define Vst2Dir     "..\Noumenon.Vst2\bin\x64\Release\net10.0-windows\deploy"
#endif
#ifndef Vst2FxDir
  #define Vst2FxDir   "..\Noumenon.Vst2.Fx\bin\x64\Release\net10.0-windows\deploy"
#endif
#ifndef ContentDir
  #define ContentDir  "..\content"
#endif
#ifndef LicensesDir
  #define LicensesDir "licenses"
#endif
#ifndef RedistDir
  #define RedistDir   "redist"
#endif
#ifndef OutputDir
  #define OutputDir   "..\dist"
#endif

#define DotNetExe     "windowsdesktop-runtime-win-x64.exe"
#define VcRedistExe   "VC_redist.x64.exe"

#ifdef IncludeVst2
  #if Pos(":", RedistDir) == 0
    #define VcRedistPath AddBackslash(SourcePath) + RedistDir + "\" + VcRedistExe
  #else
    #define VcRedistPath AddBackslash(RedistDir) + VcRedistExe
  #endif
  #define VcRedistVersion GetVersionNumbersString(VcRedistPath)
  #if VcRedistVersion == ""
    #error The bundled Visual C++ redistributable has no version number: check RedistDir.
  #endif
#endif

#ifdef Sandbox
  #define ProgramRoot Sandbox + "\Program Files"
  #define CommonRoot  Sandbox + "\Common Files"
#else
  #define ProgramRoot "{commonpf64}"
  #define CommonRoot  "{commoncf64}"
#endif

[Setup]
#ifdef Sandbox
AppId={{FAF926A4-1AAC-4C37-8FC4-D90DF27BA194}
#else
AppId={{353DF8FB-06C3-44D1-924E-78F739FF755A}
#endif
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppName} installer - standalone, VST3 and VST2

DefaultDirName={#ProgramRoot}\{#AppName}
DefaultGroupName={#AppName}
#ifdef Sandbox
UsePreviousAppDir=no
#else
UsePreviousAppDir=yes
#endif
DisableProgramGroupPage=yes
DisableReadyPage=no
AllowNoIcons=yes

; Program Files, Common Files, HKLM and the prerequisite installers all need elevation.
#ifdef Sandbox
PrivilegesRequired=lowest
CreateUninstallRegKey=no
#else
PrivilegesRequired=admin
#endif
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

; Offer to close a DAW (or the standalone) that has a plugin file locked, instead of failing.
CloseApplications=yes
RestartApplications=no

OutputDir={#OutputDir}
OutputBaseFilename={#AppName}-{#AppVersion}-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic
WizardImageFile=WizardImage.bmp,WizardImage-150.bmp,WizardImage-200.bmp
WizardSmallImageFile=WizardSmallImage.bmp,WizardSmallImage-150.bmp,WizardSmallImage-200.bmp
SetupIconFile=..\Noumenon.Standalone\Noumenon.ico
UninstallDisplayIcon={app}\Noumenon.ico
UninstallDisplayName={#AppName} {#AppVersion}
SetupLogging=yes
#ifdef SignTool
SignTool={#SignTool}
SignedUninstaller=yes
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nNoumenon is a generative ambient loop instrument: a standalone application, and a VST3 plugin that holds both Noumenon (an instrument) and Noumenon FX (the same instrument as an effect, with an audio input). Anything else it needs - the Microsoft .NET 10 Desktop Runtime, and for the optional VST2 plugins the Visual C++ runtime - is installed automatically if it is missing.%n%nPlease close your music software before continuing.
FinishedLabel=Noumenon has been installed.%n%nIn your music software, rescan the plugins: Noumenon is listed as an instrument, Noumenon FX as an effect. In FL Studio: Options > Manage plugins > Find installed plugins.%n%nYour own presets go to Documents\Noumenon\Presets; the factory bank is in the preset menu at the top left of the panel.
FinishedLabelNoIcons=Noumenon has been installed.%n%nIn your music software, rescan the plugins: Noumenon is listed as an instrument, Noumenon FX as an effect. In FL Studio: Options > Manage plugins > Find installed plugins.%n%nYour own presets go to Documents\Noumenon\Presets; the factory bank is in the preset menu at the top left of the panel.

[Types]
Name: "standard"; Description: "Standalone, VST3 and the factory content"
Name: "custom"; Description: "Custom"; Flags: iscustom

[Components]
Name: "standalone"; Description: "Noumenon standalone application"; Types: standard custom
Name: "vst3"; Description: "VST3 plugin: Noumenon and Noumenon FX in one module"; Types: standard custom
#ifdef IncludeVst2
Name: "vst2"; Description: "VST2 plugins, for hosts without VST3"
Name: "vst2\instrument"; Description: "Noumenon (instrument)"
Name: "vst2\effect"; Description: "Noumenon FX (effect with audio input)"
#endif
Name: "content"; Description: "Factory presets and samples"; Types: standard custom; Flags: fixed

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Components: standalone; Flags: unchecked

[Files]
; The standalone, as dotnet publish lays it out (framework-dependent).
Source: "{#StandaloneDir}\*"; DestDir: "{app}"; Components: standalone; Excludes: "*.pdb"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

; The factory content, once for every host. The deploy folders carry their own copy for the
; portable zips; the installed plugins read this one instead (see [Registry]). What a release
; drops is deleted once the install has succeeded (DeleteDroppedContent in [Code]).
Source: "{#ContentDir}\*"; DestDir: "{app}\Content"; Components: content; \
    Flags: ignoreversion recursesubdirs createallsubdirs

; The VST3 bundle: NPlug's proxy (Noumenon.vst3), the assemblies and the runtimeconfig. Keep the
; bundle together - the proxy loads everything beside it.
Source: "{#Vst3Bundle}\*"; DestDir: "{#CommonRoot}\VST3\Noumenon.vst3"; Components: vst3; \
    Excludes: "*.pdb,\Contents\x86_64-win\content"; Flags: ignoreversion recursesubdirs createallsubdirs

#ifdef IncludeVst2
; The VST2 deploy folders: the native bridge (the .dll a host loads) and everything it boots.
Source: "{#Vst2Dir}\*"; DestDir: "{#CommonRoot}\VST2\Noumenon"; Components: vst2\instrument; \
    Excludes: "*.pdb,\content"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Vst2FxDir}\*"; DestDir: "{#CommonRoot}\VST2\Noumenon FX"; Components: vst2\effect; \
    Excludes: "*.pdb,\content"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif

Source: "{#LicensesDir}\*"; DestDir: "{app}\Licenses"; Flags: ignoreversion
Source: "..\Noumenon.Standalone\Noumenon.ico"; DestDir: "{app}"; Flags: ignoreversion

; Prerequisites: embedded, extracted to {tmp} only when actually missing.
Source: "{#RedistDir}\{#DotNetExe}"; Flags: dontcopy noencryption nocompression
#ifdef IncludeVst2
Source: "{#RedistDir}\{#VcRedistExe}"; Flags: dontcopy noencryption nocompression
#endif

[Registry]
; Where the plugins find the factory content when none sits next to them (FactoryContent.Installed).
Root: HKA; Subkey: "Software\Noumenon"; Flags: uninsdeletekeyifempty
Root: HKA; Subkey: "Software\Noumenon"; ValueType: string; ValueName: "ContentFolder"; ValueData: "{app}\Content"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Noumenon"; ValueType: string; ValueName: "InstallFolder"; ValueData: "{app}"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Noumenon"; ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"; Flags: uninsdeletevalue

#ifndef Sandbox
[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\Noumenon.Standalone.exe"; IconFilename: "{app}\Noumenon.ico"; Components: standalone
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Noumenon.Standalone.exe"; IconFilename: "{app}\Noumenon.ico"; Components: standalone; Tasks: desktopicon
#endif

[Run]
Filename: "{app}\Noumenon.Standalone.exe"; Description: "Start Noumenon now (it starts playing straight away)"; \
    Components: standalone; Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
Type: dirifempty; Name: "{#CommonRoot}\VST3\Noumenon.vst3"
#ifdef IncludeVst2
Type: dirifempty; Name: "{#CommonRoot}\VST2\Noumenon"
Type: dirifempty; Name: "{#CommonRoot}\VST2\Noumenon FX"
#endif
Type: dirifempty; Name: "{app}"

[Code]
const
  ERROR_SUCCESS_REBOOT_REQUIRED = 3010;
  ERROR_NEWER_VERSION_INSTALLED = 1638;

{ ---- prerequisite detection ------------------------------------------------------------ }

{ True when a released Microsoft.WindowsDesktop.App 10.x shared framework is under the given
  root. A prerelease one (10.0.0-rc.2...) does not count: hostfxr does not roll a release's
  request forward onto a prerelease runtime. }
function HasDesktopRuntime10(installRoot: String): Boolean;
var
  frameworkDir: String;
  findRec: TFindRec;
begin
  Result := False;
  if installRoot = '' then
    exit;

  frameworkDir := AddBackslash(installRoot) + 'shared\Microsoft.WindowsDesktop.App';
  if not DirExists(frameworkDir) then
    exit;

  if FindFirst(frameworkDir + '\10.*', findRec) then
  begin
    try
      repeat
        if ((findRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0) and (Pos('-', findRec.Name) = 0) then
        begin
          Result := True;
          exit;
        end;
      until not FindNext(findRec);
    finally
      FindClose(findRec);
    end;
  end;
end;

{ Mirrors what hostfxr does: the install location registered in the 32-bit registry view (the
  .NET installer writes it there, under WOW6432Node), the 64-bit view as a fallback, then the
  default folder. }
function IsDotNetDesktop10Installed(): Boolean;
var
  installRoot: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64', 'InstallLocation', installRoot) then
    Result := HasDesktopRuntime10(installRoot);
  if not Result and RegQueryStringValue(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64', 'InstallLocation', installRoot) then
    Result := HasDesktopRuntime10(installRoot);
  if not Result then
    Result := HasDesktopRuntime10(ExpandConstant('{commonpf64}\dotnet'));
end;

{ The installed VC++ v14 x64 runtime's version, from the key its redistributable writes (in the
  given registry view); False when there is none. }
function InstalledVcRuntime(rootKey: Integer; var version: Int64): Boolean;
var
  installed, major, minor, build, rebuild: Cardinal;
  key: String;
begin
  Result := False;
  key := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64';
  if not RegQueryDWordValue(rootKey, key, 'Installed', installed) then
    exit;
  if installed <> 1 then
    exit;
  if not RegQueryDWordValue(rootKey, key, 'Major', major) then
    exit;
  if not RegQueryDWordValue(rootKey, key, 'Minor', minor) then
    exit;
  if not RegQueryDWordValue(rootKey, key, 'Bld', build) then
    exit;
  if not RegQueryDWordValue(rootKey, key, 'Rbld', rebuild) then
    rebuild := 0;

  version := PackVersionComponents(major, minor, build, rebuild);
  Result := True;
end;

{ True when the installed VC++ v14 x64 runtime is at least the bundled redistributable's
  version; a runtime the registry does not describe is installed anyway (the redistributable
  itself reports a newer one as already there). }
function IsVcRedistInstalled(): Boolean;
var
  installed, bundled: Int64;
begin
  Result := False;
#ifdef IncludeVst2
  if not StrToVersion('{#VcRedistVersion}', bundled) then
    exit;

  if InstalledVcRuntime(HKLM32, installed) or InstalledVcRuntime(HKLM64, installed) then
  begin
    Log('Noumenon: VC++ x64 runtime ' + VersionToStr(installed) + ', bundled {#VcRedistVersion}');
    Result := ComparePackedVersion(installed, bundled) >= 0;
  end;
#endif
end;

function NeedsVcRedist(): Boolean;
begin
#ifdef IncludeVst2
  Result := WizardIsComponentSelected('vst2\instrument') or WizardIsComponentSelected('vst2\effect');
#else
  Result := False;
#endif
end;

{ ---- prerequisite installation ---------------------------------------------------------- }

procedure SetPreparingText(const text: String);
begin
  WizardForm.PreparingLabel.Caption := text;
  WizardForm.Update();
end;

{ Extracts an embedded redistributable and runs it silently. Returns '' on success or a
  human-readable message describing the failure. }
function RunRedist(const fileName, displayName: String): String;
var
  resultCode: Integer;
begin
  Result := '';

  SetPreparingText('Installing ' + displayName + '. This can take a few minutes...');

  try
    ExtractTemporaryFile(fileName);
  except
    Result := 'Could not unpack ' + displayName + ' from the installer.';
    exit;
  end;

  if not Exec(ExpandConstant('{tmp}\') + fileName, '/install /quiet /norestart', '',
              SW_SHOW, ewWaitUntilTerminated, resultCode) then
  begin
    Result := 'Could not start the ' + displayName + ' installer (' +
              SysErrorMessage(resultCode) + ').';
    exit;
  end;

  Log('Noumenon: ' + fileName + ' exited with ' + IntToStr(resultCode));
  if (resultCode <> 0) and (resultCode <> ERROR_SUCCESS_REBOOT_REQUIRED) and
     (resultCode <> ERROR_NEWER_VERSION_INSTALLED) then
    Result := displayName + ' could not be installed (error code ' +
              IntToStr(resultCode) + ').' + #13#10#13#10 +
              'Please install it manually, then run this setup again.';
end;

function YesNo(value: Boolean): String;
begin
  if value then
    Result := 'yes'
  else
    Result := 'no';
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  hasVcRedist: Boolean;
  hasDotNet: Boolean;
begin
  Result := '';

  { Recorded in the setup log (SetupLogging=yes) so a failed install can be diagnosed from
    the user's %TEMP%\Setup Log*.txt without them having to check anything by hand. }
  hasVcRedist := IsVcRedistInstalled();
  hasDotNet := IsDotNetDesktop10Installed();
#ifdef ForcePrerequisites
  hasVcRedist := False;
  hasDotNet := False;
#endif
  Log('Noumenon: prerequisites - .NET 10 Desktop Runtime: ' + YesNo(hasDotNet) +
      ', VC++ x64 runtime: ' + YesNo(hasVcRedist) + ' (needed: ' + YesNo(NeedsVcRedist()) + ')');

  if NeedsVcRedist() and not hasVcRedist then
  begin
#ifdef IncludeVst2
    Result := RunRedist('{#VcRedistExe}', 'the Microsoft Visual C++ runtime');
    if Result <> '' then
      exit;
#endif
  end;

  if not hasDotNet then
  begin
    Result := RunRedist('{#DotNetExe}', 'the Microsoft .NET 10 Desktop Runtime');
    if Result <> '' then
      exit;
  end;

  SetPreparingText('');
end;

{ ---- content an earlier release left behind -------------------------------------------- }

procedure DeleteDroppedFile(const path: String);
begin
  if not FileExists(path) then
    exit;

  if DeleteFile(path) then
    Log('Noumenon: deleted ' + path + ', which this release no longer ships')
  else
    Log('Noumenon: could not delete ' + path + ', which this release no longer ships');
end;

procedure DeletePortableContent(const folder: String);
begin
  if not DirExists(folder) then
    exit;

  if DelTree(folder, True, True, True) then
    Log('Noumenon: deleted ' + folder + ', a portable copy''s content')
  else
    Log('Noumenon: could not delete ' + folder + ', a portable copy''s content');
end;

{ Deletes what an earlier release installed and this one no longer ships, once this release's
  files are in place. Not [InstallDelete]: that runs before the files are copied, and a
  cancelled or failed install never restores what it deleted. Only the files listed here go, so
  nothing a player put in the folder is touched: add what a release drops, and take a path off
  again if a later release ships it (build-release.ps1 -TestInstall fails when this deletes a
  file the release ships). A content folder in the plugin folders goes whole: the installer
  never puts one there, but a portable copy unpacked into the same place does, and the plugins
  would read its stale bank before the installed one. }
procedure DeleteDroppedContent();
var
  presets: String;
begin
  presets := ExpandConstant('{app}\Content\presets\');

  { 0.5.1: Living Faders and the FX family. }
  DeleteDroppedFile(presets + 'Drone\Living Faders.noumenon');
  DeleteDroppedFile(presets + 'FX\Input Breath.noumenon');
  DeleteDroppedFile(presets + 'FX\Input Cathedral.noumenon');
  DeleteDroppedFile(presets + 'FX\Input Drift.noumenon');
  DeleteDroppedFile(presets + 'FX\Input Halo.noumenon');
  DeleteDroppedFile(presets + 'FX\Input Resonator.noumenon');
  DeleteDroppedFile(presets + 'FX\Input Rust.noumenon');
  RemoveDir(presets + 'FX');

  if WizardIsComponentSelected('vst3') then
    DeletePortableContent(ExpandConstant('{#CommonRoot}\VST3\Noumenon.vst3\Contents\x86_64-win\content'));
#ifdef IncludeVst2
  if WizardIsComponentSelected('vst2\instrument') then
    DeletePortableContent(ExpandConstant('{#CommonRoot}\VST2\Noumenon\content'));
  if WizardIsComponentSelected('vst2\effect') then
    DeletePortableContent(ExpandConstant('{#CommonRoot}\VST2\Noumenon FX\content'));
#endif
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    DeleteDroppedContent();
end;
