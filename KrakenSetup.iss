; Script de Inno Setup para KRAKEN Launcher
; Generado para Leandro

[Setup]
AppId={{8B2C3D4E-5F6A-7B8C-9D0E-1F2A3B4C5D6E}
AppName=KRAKEN Launcher
AppVersion=3.1.8
AppPublisher=Leandro
AppPublisherURL=https://github.com/leaboga/nebula-modpack
AppSupportURL=https://github.com/leaboga/nebula-modpack
AppUpdatesURL=https://github.com/leaboga/nebula-modpack
DefaultDirName={autopf}\KRAKEN Launcher
DefaultGroupName=KRAKEN Launcher
AllowNoIcons=yes
SetupIconFile=C:\Users\Leandro\source\repos\NebulaLauncher\kraken.ico
OutputBaseFilename=KrakenSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\Leandro\source\repos\NebulaLauncher\bin\Release\net8.0-windows\win-x64\publish\KrakenLauncher.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\KRAKEN Launcher"; Filename: "{app}\KrakenLauncher.exe"
Name: "{autodesktop}\KRAKEN Launcher"; Filename: "{app}\KrakenLauncher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\KrakenLauncher.exe"; Description: "{cm:LaunchProgram,KRAKEN Launcher}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\KrakenLauncher"
