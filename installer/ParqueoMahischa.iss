; Instalador de Parqueo Mahischa (Inno Setup 6).
; Instala el programa (self-contained, no requiere .NET) y, si se incluye el redistribuible,
; instala SQL Server Express automáticamente (instancia SQLEXPRESS).
; La base de datos se crea automáticamente la primera vez que se ejecuta el programa.
;
; Para INCLUIR la instalación automática de SQL Express:
;   1) Descargar SQL Server Express ("Express Core") de Microsoft. Queda un .exe autoextraíble.
;   2) Copiarlo como:  installer\redist\SQLEXPR_x64_ENU.exe
;   3) Recompilar este script. (Si el archivo no está, el instalador se compila igual,
;      solo que avisará al cliente que debe instalar SQL Express a mano.)
;
; Para compilar: publish.ps1 (genera ..\publish) y luego abrir este .iss en Inno Setup -> Compile.

#define AppName "Parqueo Mahischa"
#define AppVersion "1.0.0"
#define AppExe "SistemaParkingMaisha.exe"
#define Publisher "Parqueo Mahischa"
#define SqlRedist "redist\SQLEXPR_x64_ENU.exe"

#ifexist SqlRedist
  #define IncludeSql
#endif

[Setup]
AppId={{8F3A1C2E-9B4D-4E7A-AB12-7C5D9E0F1234}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=ParqueoMahischa-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

; Concede a los usuarios permiso de modificación sobre la carpeta de instalación,
; necesario para que la actualización automática (desde GitHub) reemplace los archivos sin pedir UAC.
[Dirs]
Name: "{app}"; Permissions: users-modify

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
#ifdef IncludeSql
Source: "{#SqlRedist}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsSql
#endif

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
#ifdef IncludeSql
; Instalación silenciosa de SQL Server Express (solo si no existe la instancia SQLEXPRESS).
Filename: "{tmp}\SQLEXPR_x64_ENU.exe"; \
  Parameters: "/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install /FEATURES=SQLENGINE /INSTANCENAME=SQLEXPRESS /TCPENABLED=1 /SQLSVCSTARTUPTYPE=Automatic /SQLSYSADMINACCOUNTS=""BUILTIN\Administrators"""; \
  StatusMsg: "Instalando SQL Server Express (puede tardar varios minutos)..."; \
  Check: NeedsSql
#endif
Filename: "{app}\{#AppExe}"; Description: "Iniciar {#AppName} ahora"; Flags: nowait postinstall skipifsilent

[Code]
function SqlExpressInstalled(): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetValueNames(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
      if CompareText(Names[I], 'SQLEXPRESS') = 0 then
        Result := True;
  end;
end;

function NeedsSql(): Boolean;
begin
  Result := not SqlExpressInstalled();
end;

#ifndef IncludeSql
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and (not SqlExpressInstalled()) then
  begin
    MsgBox('No se detectó SQL Server Express (instancia SQLEXPRESS) en esta computadora.' + #13#10 + #13#10 +
           'Parqueo Mahischa necesita SQL Server Express para funcionar. Descárguelo gratis del sitio ' +
           'de Microsoft e instálelo usando el nombre de instancia "SQLEXPRESS".' + #13#10 + #13#10 +
           'La primera vez que abra el programa (como administrador), la base de datos se creará sola.',
           mbInformation, MB_OK);
  end;
end;
#endif
