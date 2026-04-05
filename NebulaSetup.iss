; Script de Inno Setup para Nebula Launcher
; Generado para Leandro

[Setup]
; Información básica de la aplicación
AppId={{8B2C3D4E-5F6A-7B8C-9D0E-1F2A3B4C5D6E}
AppName=Nebula Launcher
AppVersion=1.0.0
AppPublisher=Leandro
AppPublisherURL=https://github.com/leaboga/nebula-modpack
AppSupportURL=https://github.com/leaboga/nebula-modpack
AppUpdatesURL=https://github.com/leaboga/nebula-modpack
DefaultDirName={autopf}\Nebula Launcher
DefaultGroupName=Nebula Launcher
AllowNoIcons=yes
; Icono que aparecerá en el panel de control y en el instalador
SetupIconFile=C:\Users\Leandro\source\repos\NebulaLauncher\nebula.ico
; Nombre del instalador final
OutputBaseFilename=NebulaSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; NOTA: Asegúrate de que el archivo NebulaLauncher.exe esté en esta ruta antes de compilar
Source: "C:\Users\Leandro\source\repos\NebulaLauncher\bin\Release\net8.0-windows\win-x64\publish\NebulaLauncher.exe"; DestDir: "{app}"; Flags: ignoreversion
; Si el launcher necesita otros archivos (como DLLs no embebidas), agrégalos aquí:
; Source: "C:\Users\Leandro\source\repos\NebulaLauncher\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Nebula Launcher"; Filename: "{app}\NebulaLauncher.exe"
Name: "{autodesktop}\Nebula Launcher"; Filename: "{app}\NebulaLauncher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\NebulaLauncher.exe"; Description: "{cm:LaunchProgram,Nebula Launcher}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\NebulaLauncher"
