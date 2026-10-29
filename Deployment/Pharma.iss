#define AppName "Pharma"
#define AppPublisher "Pharma Project"
#define AppExeName "Pharma.exe"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PUBLISH_DIR
  #define PUBLISH_DIR "..\Livrable\Deployment\publish"
#endif
#ifndef OUTPUT_DIR
  #define OUTPUT_DIR "..\Livrable\Deployment\installer"
#endif

[Setup]
AppId={{B52BE7E6-7E84-4DF3-8F9A-202610280001}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Pharma
DefaultGroupName={#AppName}
OutputDir={#OUTPUT_DIR}
OutputBaseFilename=Setup-Pharma
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayName=Pharma - Gestion officinale
WizardStyle=modern

[Files]
Source: "{#PUBLISH_DIR}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "pharma.db,*.log"
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{commonappdata}\Pharma"
Name: "{commonappdata}\Pharma\Backups"

[Icons]
Name: "{autodesktop}\Pharma"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\Pharma"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Lancer Pharma"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
