﻿; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName          "WebRadio"
#define MyAppVersion       GetFileVersion('..\..\WebRadio\bin\Release\Publish\WebRadio.exe')
#define MyAppPlatform      "64bit"
#define MyAppPublisher     "Exploitox"
#define MyAppURL           "https://github.com/valnoxy/WebRadio"
#define MyAppExeName       "WebRadio.exe"
#define MyAppStartingYear  "2018"
#define MyAppEndingYear    GetDateTimeString('yyyy','','')
              
[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{61F70E73-5493-43E1-B777-D8587E3B5EF9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion} ({#MyAppPlatform})
AppMutex=valnoxyWebRadio

VersionInfoDescription={#MyAppName} Installer
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
AppCopyright=Copyright © {#MyAppStartingYear} - {#MyAppEndingYear} {#MyAppPublisher}. All rights reserved.       

AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

UninstallDisplayIcon={app}\WebRadio.exe
UninstallDisplayName={#MyAppName}
AppPublisher={#MyAppPublisher}

WizardStyle=modern
ShowLanguageDialog=yes
UsePreviousLanguage=no

ArchitecturesInstallIn64BitMode=x64

DefaultDirName={commonpf64}\{#MyAppPublisher}\{#MyAppName}
UsePreviousAppDir=yes
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE.md
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
OutputDir=..\Output
OutputBaseFilename=WebRadio_{#MyAppVersion}_x64
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\..\WebRadio\bin\Release\Publish\*"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure DeleteAutoStartEntry;
var
  RegKey: string;
begin
  RegKey := 'Software\Microsoft\Windows\CurrentVersion\Run';
  RegDeleteValue(HKEY_CURRENT_USER, RegKey, 'WebRadio');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ConfigPath: string;
begin
  if CurUninstallStep = usUninstall then
  begin
    ConfigPath := ExpandConstant('{userappdata}\valnoxy\WebRadio\config.json');
    if FileExists(ConfigPath) then
      DeleteFile(ConfigPath);

    DeleteAutoStartEntry;
  end;
end;