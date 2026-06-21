# Guía de instalación y mantenimiento — Parqueo Mahischa

Sistema local de parqueo (Windows + SQL Server). La aplicación se publica **self-contained**
(no requiere instalar .NET aparte) y se distribuye con un **instalador**.

---

## A. Para INSTALAR en la PC del cliente

### 1. Requisitos
- Windows 10/11 (64 bits).
- **SQL Server Express** (gratuito de Microsoft). Instalar con el nombre de instancia
  **`SQLEXPRESS`** (el valor por defecto). El instalador del programa detecta si falta y avisa.
- No hace falta instalar .NET: la app ya lo incluye.

### 2. Ejecutar el instalador
1. Ejecutar `ParqueoMahischa-Setup-x.y.z.exe` **como administrador**.
2. Seguir el asistente (instala el programa, crea accesos directos y concede permisos para
   las actualizaciones automáticas).
3. Si no hay SQL Server Express, el asistente lo avisa: instálelo (instancia `SQLEXPRESS`).

### 3. Configurar `SistemaParkingMaisha.dll.config` (junto al .exe, en la carpeta de instalación)

| Clave | Para qué | Valor por defecto |
|-------|----------|-------------------|
| `connectionString` → `Data Source` | Instancia de SQL Server | `.\SQLEXPRESS` |
| `BusinessName` | Nombre del negocio en pantallas y tiquete | `Parqueo Mahischa` |
| `ContactPhone` | Teléfono que sale en el tiquete | `+506 8687 5906 / +506 8366 9729` |
| `MinimumCashAmount` | Mínimo de caja | `50000` |
| `UpdateRepository` | Repo de GitHub para actualizaciones (`usuario/repo`) | *(vacío)* |
| `DefaultAdminUser` / `DefaultAdminCedula` / `DefaultAdminPassword` | Admin inicial | `admin` / `000000000` / `admin123` |

- Si la instancia del cliente NO se llama `SQLEXPRESS`, cambiar `Data Source=.\SQLEXPRESS`
  por `Data Source=.\NOMBRE_REAL`.

### 4. Primer arranque
1. Abrir el programa **como el usuario administrador de Windows** (que sea administrador de
   SQL Server). Se **crea sola** la base `ParqueoMaishaDB` con tablas, roles, tarifas de ejemplo
   y el administrador inicial.
2. Iniciar sesión con la **cédula** `000000000` y contraseña `admin123`.
3. El sistema **obliga a cambiar la contraseña** del administrador.
4. Crear los empleados desde **Usuarios** y asignar permisos.
5. *(Si otros usuarios de Windows usarán el programa, agréguelos como inicios de sesión en
   SQL Server con acceso a `ParqueoMaishaDB`, ya que la conexión usa seguridad integrada.)*

### 5. Respaldos
- **Automático:** una vez al día, al abrir el programa, se crea/sobrescribe un único archivo
  de respaldo (`ParqueoMahischa_Respaldo.bak`).
- **Configurar la carpeta:** menú lateral **Respaldos** (solo administrador). Para **cambiar la
  ruta** se pide la **contraseña** del administrador. También hay **"Respaldar ahora"**.
- Por defecto se guarda en la carpeta de respaldos del servidor SQL. Si elige una carpeta
  propia, el **servicio de SQL Server** debe tener permiso de escritura allí (el sistema lo
  prueba al configurarla). Conviene copiar el `.bak` a un disco externo o nube periódicamente.

### 6. Seguridad
- Contraseñas con hash PBKDF2-SHA256 (120 000 iteraciones + salt).
- Bloqueo de cuenta tras 5 intentos fallidos (1 minuto).
- Auditoría de ingresos, entradas/salidas, cobros, cierres, reimpresiones, cambios de
  usuarios/tarifas, respaldos y actualizaciones en `dbo.AuditLogs`.
- El instalador protege la carpeta; aun así conviene limitar por NTFS quién lee la carpeta
  (contiene la cadena de conexión).

---

## B. Para ACTUALIZAR el sistema (automático, desde GitHub)

Cuando hay una versión nueva, aparece un **botón llamativo "Actualizar a vX.Y.Z"** arriba a la
derecha. Al pulsarlo (o la próxima vez que abra el programa), descarga la nueva versión, se
cierra, reemplaza los archivos y se vuelve a abrir.

**Para publicar una actualización (vos, como desarrollador):**
1. Subir el código a un repositorio de GitHub y poner `usuario/repo` en la clave
   `UpdateRepository` del `.config` que instalás en el cliente.
2. Subir el número de versión en `SistemaParkingMaisha.csproj` (`<Version>`, `<FileVersion>`).
3. Ejecutar `publish.ps1` y comprimir el **contenido** de la carpeta `publish` en un `.zip`.
4. Crear un **Release** en GitHub con un **tag** mayor (ej. `v1.0.1`) y adjuntar ese `.zip`.

El programa compara el tag del último release con su versión y solo ofrece actualizar si es mayor.

---

## C. Para GENERAR el instalador (vos, como desarrollador)

1. Publicar la app: `powershell -ExecutionPolicy Bypass -File .\publish.ps1`
   (genera la carpeta `publish` self-contained).
2. Instalar **Inno Setup 6** (https://jrsoftware.org/isdl.php).
3. Abrir `installer\ParqueoMahischa.iss` y presionar **Compile**
   (o `ISCC.exe installer\ParqueoMahischa.iss`). El instalador queda en `installer\Output\`.
4. *(Opcional)* Para instalar SQL Server Express automáticamente: descargar `SQLEXPR_x64_ENU.exe`,
   colocarlo en `installer\redist\` y descomentar las líneas indicadas en el `.iss`.

---

## Notas
- La base de datos se llama `ParqueoMaishaDB` (nombre interno; no afecta la marca "Mahischa").
- En una instalación nueva la base arranca **limpia**.
- La ventana principal se puede maximizar y redimensionar.
- Para que la actualización automática reemplace archivos sin pedir UAC, el instalador concede
  permiso de modificación sobre la carpeta de instalación.
