#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppName "FitCore"
#define AppPublisher "FitCore"
#define AppExeName "FitCore.Desktop.exe"
#define SourcePath "..\publish\app"

[Setup]
AppId={{E71EB554-9F1B-4C87-B5B7-000000000001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=Output
OutputBaseFilename=FitCore-Setup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
MinVersion=10.0.17763
DisableProgramGroupPage=yes
SetupIconFile=..\src\FitCore.Desktop\Resources\fitcore.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\defaults\fitcore.defaults.json"; DestDir: "{app}\defaults"; Flags: ignoreversion
Source: "..\src\FitCore.Desktop\Resources\vendor-public-key.txt"; DestDir: "{app}\Resources"; Flags: ignoreversion
Source: "..\src\FitCore.Desktop\Resources\fitcore.ico"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{commonappdata}\FitCore"; Permissions: users-modify
Name: "{commonappdata}\FitCore\logs"; Permissions: users-modify
Name: "{commonappdata}\FitCore\backups"; Permissions: users-modify

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\fitcore.ico"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\fitcore.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\FitCore\logs"

