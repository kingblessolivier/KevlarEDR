#ifndef SourceDir
  #error SourceDir must point to the published application directory
#endif
#ifndef OutputDir
  #error OutputDir must point to the artifacts directory
#endif

[Setup]
AppId={{E7872FF6-80D4-4B66-BF63-540538DA870A}
AppName=KevlarEDR
AppVersion=0.1.0
AppPublisher=KevlarEDR
AppPublisherURL=https://github.com/kingblessolivier/KevlarEDR
DefaultDirName={localappdata}\Programs\KevlarEDR
DefaultGroupName=KevlarEDR
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#OutputDir}
OutputBaseFilename=KevlarEDR-Setup-0.1.0-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#SourceDir}\KevlarEDR.ico
UninstallDisplayIcon={app}\KevlarEDR.exe
CloseApplications=yes
RestartApplications=no

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\KevlarEDR"; Filename: "{app}\KevlarEDR.exe"

[Run]
Filename: "{app}\KevlarEDR.exe"; Description: "Open KevlarEDR"; Flags: nowait postinstall skipifsilent

; No uninstall-delete rule touches the separate user recovery vault.
