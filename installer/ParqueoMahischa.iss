; Instalador de Parqueo Mahischa (Inno Setup 6).
; Instala el programa (self-contained, no requiere .NET) y, si se incluye el redistribuible,
; instala SQL Server Express automáticamente (instancia SQLEXPRESS) con el método de
; dos pasos: extraer el paquete y luego ejecutar su SETUP.exe en modo desatendido.
; La base de datos se crea automáticamente la primera vez que se ejecuta el programa.
;
; Para INCLUIR la instalación automática de SQL Express:
;   1) Descargar SQL Server Express ("Express Core"). Queda un .exe autoextraíble.
;   2) Copiarlo como:  installer\redist\SQLEXPR_x64_ENU.exe
;   3) Recompilar (build-installer.bat). Si el archivo no está, el instalador se compila
;      igual y solo avisa al cliente que instale SQL Express a mano.

#define AppName "Parqueo Mahischa"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
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
; El paquete de SQL se coloca en {tmp} solo si falta la instancia SQLEXPRESS.
Source: "{#SqlRedist}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsSql
#endif

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
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

#ifdef IncludeSql
procedure InstallSqlExpress();
var
  Code: Integer;
  Pkg, ExtractDir, SetupExe: String;
begin
  Pkg := ExpandConstant('{tmp}\SQLEXPR_x64_ENU.exe');
  if not FileExists(Pkg) then
    Exit;

  ExtractDir := ExpandConstant('{tmp}\sqlexpr');

  { Paso 1: extraer el paquete autoextraíble. }
  WizardForm.StatusLabel.Caption := 'Extrayendo SQL Server Express...';
  if not Exec(Pkg, '/q /x:"' + ExtractDir + '"', '', SW_SHOW, ewWaitUntilTerminated, Code) then
  begin
    MsgBox('No se pudo extraer SQL Server Express (código ' + IntToStr(Code) + ').', mbError, MB_OK);
    Exit;
  end;

  SetupExe := ExtractDir + '\SETUP.exe';
  if not FileExists(SetupExe) then
  begin
    MsgBox('No se encontró SETUP.exe luego de extraer SQL Server Express.', mbError, MB_OK);
    Exit;
  end;

  { Paso 2: instalar el motor de base de datos en modo desatendido (instancia SQLEXPRESS). }
  WizardForm.StatusLabel.Caption := 'Instalando SQL Server Express (puede tardar varios minutos)...';
  Exec(SetupExe,
    '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install /FEATURES=SQLENGINE /INSTANCENAME=SQLEXPRESS' +
    ' /SQLSVCSTARTUPTYPE=Automatic /BROWSERSVCSTARTUPTYPE=Automatic /UPDATEENABLED=False' +
    ' /TCPENABLED=1 /NPENABLED=1 /SQLSYSADMINACCOUNTS="BUILTIN\Administrators"',
    '', SW_SHOW, ewWaitUntilTerminated, Code);

  if not SqlExpressInstalled() then
    MsgBox('La instalación automática de SQL Server Express no se completó (código ' + IntToStr(Code) + ').' + #13#10 + #13#10 +
           'Puede instalarlo manualmente (instancia SQLEXPRESS). Al abrir el programa, la base se creará sola.',
           mbInformation, MB_OK);
end;
#endif

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
#ifdef IncludeSql
    if not SqlExpressInstalled() then
      InstallSqlExpress();
#else
    if not SqlExpressInstalled() then
      MsgBox('No se detectó SQL Server Express (instancia SQLEXPRESS).' + #13#10 + #13#10 +
             'Parqueo Mahischa lo necesita. Instálelo (instancia SQLEXPRESS) y luego abra el programa.',
             mbInformation, MB_OK);
#endif
  end;
end;
