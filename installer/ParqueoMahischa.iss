; Instalador de Parqueo Mahischa (Inno Setup 6).
; Instala el programa (self-contained, no requiere .NET) y detecta SQL Server Express.
; La base de datos se crea automáticamente la primera vez que se ejecuta el programa.
;
; Requisitos para compilar este instalador:
;   1) Ejecutar primero  publish.ps1  (genera la carpeta ..\publish).
;   2) Tener instalado Inno Setup 6 (https://jrsoftware.org/isdl.php).
;   3) Abrir este archivo con Inno Setup y presionar "Compile" (o ISCC.exe ParqueoMahischa.iss).

#define AppName "Parqueo Mahischa"
#define AppVersion "1.0.0"
#define AppExe "SistemaParkingMaisha.exe"
#define Publisher "Parqueo Mahischa"

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
; --- SQL Server Express opcional ---
; Para instalar SQL Express automáticamente, descargue SQLEXPR_x64_ENU.exe, colóquelo en
; installer\redist\ y descomente la siguiente línea y la correspondiente en [Run].
; Source: "redist\SQLEXPR_x64_ENU.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not SqlExpressInstalled

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
; --- Instalación silenciosa de SQL Server Express (opcional, ver [Files]) ---
; Filename: "{tmp}\SQLEXPR_x64_ENU.exe"; \
;   Parameters: "/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install /FEATURES=SQLENGINE /INSTANCENAME=SQLEXPRESS /TCPENABLED=1 /SQLSYSADMINACCOUNTS=BUILTIN\Administrators"; \
;   StatusMsg: "Instalando SQL Server Express (puede tardar varios minutos)..."; \
;   Check: not SqlExpressInstalled
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
