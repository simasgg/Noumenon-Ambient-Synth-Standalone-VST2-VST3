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
;   * Visual C++ 2015-2022 Redistributable (x64) - only for the VST2 plugins: VST.NET's native
;     bridge imports VCRUNTIME140.dll. The standalone and NPlug's statically linked VST3 proxy
;     need nothing.
;
; Build it with build-release.ps1 in the repository root, which passes the defines below. Two
; more defines exist for testing: Sandbox=<folder> installs everything under that folder without
; elevation (the registry value goes to HKCU), and ForcePrerequisites runs the two redistributable
; installers even when the machine has them (point RedistDir at stand-ins).

#define AppName       "Noumenon"
#define AppPublisher  "Noumenon"
#define AppUrl        "https://github.com/simasgg/Noumenon"

#ifndef AppVersion
  #define AppVersion  "1.0.0"
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

#ifdef Sandbox
  #define ProgramRoot Sandbox + "\Program Files"
  #define CommonRoot  Sandbox + "\Common Files"
#else
  #define ProgramRoot "{commonpf64}"
  #define CommonRoot  "{commoncf64}"
#endif

[Setup]
AppId={{353DF8FB-06C3-44D1-924E-78F739FF755A}
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
UsePreviousAppDir=yes
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
; portable zips; the installed plugins read this one instead (see [Registry]).
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

{ True when a Microsoft.WindowsDesktop.App 10.x shared framework is under the given root. }
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
        if (findRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
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

{ True when the VC++ 2015-2022 x64 runtime is present: the key the redistributable writes, in
  both views, or the DLL in System32. }
function IsVcRedistInstalled(): Boolean;
var
  installed: Cardinal;
begin
  Result := False;

  if RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', installed) then
    if installed = 1 then
    begin
      Result := True;
      exit;
    end;

  if RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', installed) then
    if installed = 1 then
    begin
      Result := True;
      exit;
    end;

  Result := FileExists(ExpandConstant('{sys}\vcruntime140.dll'));
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
