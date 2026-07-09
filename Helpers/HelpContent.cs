namespace SistemaParkingMahischa.Helpers;

/// <summary>
/// Instrucciones detalladas de cada módulo, pensadas para personas con poca experiencia
/// en tecnología. Se muestran desde el botón "Ayuda" de cada pantalla.
/// </summary>
public static class HelpContent
{
    public static string For(string moduleTitle) =>
        Items.TryGetValue(moduleTitle, out var text) ? text : Default;

    private const string Default =
        "Use el menú de la izquierda para moverse entre los módulos. En cada pantalla puede " +
        "presionar el botón 'Ayuda' para ver instrucciones detalladas.";

    private static readonly Dictionary<string, string> Items = new()
    {
        ["Panel"] =
            """
            El PANEL es la pantalla de inicio. Solo muestra información; no hay que hacer nada aquí.

            TARJETAS (arriba):
            • Vehículos activos: cuántos autos están DENTRO del parqueo en este momento.
            • Salidas hoy: cuántos vehículos salieron y pagaron hoy.
            • Efectivo hoy: total cobrado hoy en EFECTIVO.
            • SINPE hoy: total cobrado hoy por SINPE.

            TABLA (abajo):
            La lista de vehículos que están dentro del parqueo ahora mismo, con su placa,
            la hora de entrada, la tarifa y el tiempo que llevan adentro.

            Para registrar entradas o salidas, vaya al módulo 'Entrada / salida' en el menú izquierdo.
            """,

        ["Entrada / salida"] =
            """
            Aquí se registra cuando un auto ENTRA y cuando SALE del parqueo.

            ── REGISTRAR UNA ENTRADA (cuando el auto llega) ──
            1. Escriba la PLACA del vehículo.
            2. El TIPO DE TARIFA ya viene seleccionado ('Por hora' es la única tarifa activa;
               el cobro por día se elige al momento de COBRAR la salida).
            3. Presione 'Registrar entrada'.
            4. Se abre el tiquete con un código QR. Presione 'Imprimir' y entréguelo al cliente.
               (Guarde el tiquete: ese QR es lo que se escanea a la salida.)

            ── REGISTRAR UNA SALIDA (cuando el auto se va) ──
            FORMA RÁPIDA, con el escáner:
            1. Haga clic dentro del campo 'Código QR / ticket'.
            2. Escanee el QR del tiquete del cliente con el lector.
            3. Se abre DIRECTAMENTE la ventana de COBRO con la placa y el monto.
            4. Cobre y presione 'Cobrar y registrar salida'.
            (Si el vehículo YA PAGÓ, al escanear su QR se ofrece reimprimir el comprobante.)

            FORMA MANUAL, buscando la placa:
            1. Escriba la placa en 'Buscar por placa' y presione 'Buscar placa'.
            2. Haga clic en el vehículo en la lista.
            3. Presione 'Registrar salida', cobre y confirme.

            ── AL COBRAR (ventana de cobro) ──
            La ventana muestra el TIEMPO que estuvo el vehículo (en estadías largas también
            el total de horas): úselo para verificar que el monto calculado sea correcto.
            • COBRAR POR DÍA: si la estadía se cobra por días, marque la casilla
              'Cobrar por día' y escriba la CANTIDAD DE DÍAS; el total es días x ₡3 000
              (reemplaza el cálculo por hora).
            • MONTO EXTRA: si el cliente se pasó unos minutos, puede sumar un monto adicional.
            • FORMA DE PAGO: elija 'Efectivo', 'SINPE' o 'Mixto' (una parte en efectivo y
              otra por SINPE).
            • En EFECTIVO puede escribir con cuánto paga el cliente ('Paga con') y el sistema
              calcula automáticamente el VUELTO.
            • En SINPE puede anotar la referencia o comprobante (opcional).
            • En MIXTO escriba cuánto paga en EFECTIVO: el resto se completa solo como SINPE
              (las dos partes deben sumar el total).
            • IMPRIMIR COMPROBANTE: el comprobante es OPCIONAL; marque la casilla solo si el
              cliente lo pide. Siempre se puede reimprimir después (botón 'Reimprimir' o
              escaneando el QR del tiquete).

            ── TARIFA PERSONALIZADA ──
            Para cobrar diferente a un cliente puntual: seleccione el vehículo y presione
            'Tarifa personalizada'. Defina la unidad (hora/día/semana/mes/fija) y el monto.
            Se aplica SOLO a esa estadía y queda registrada.

            ── OTRAS OPCIONES ──
            • 'Reimprimir': si el vehículo sigue adentro, reimprime su tiquete de ENTRADA;
              si ya pagó su salida, reimprime el COMPROBANTE DE PAGO.
            • 'Ocultar vehículos con salida': muestra solo los autos que siguen adentro.

            ── CÓMO COBRA LA TARIFA POR HORA ──
            • La PRIMERA HORA se cobra completa (₡700) desde que el vehículo ingresa,
              aunque lleve pocos minutos.
            • Después de cada hora completa, el excedente se cobra por fracción:
              hasta 10 min extra: ₡200 · 20 min: ₡300 · 30 min: ₡400 · 40 min: ₡500 · 50 min: ₡600.
              Ejemplo: 1 hora y 20 minutos = ₡700 + ₡300 = ₡1000.
            • Horas completas: ₡700 cada una (2 horas ₡1400, 3 horas ₡2100, etc.).
            • El tiempo de gracia evita cobrar de más por pasarse unos minutos: con gracia de 10,
              una estadía de 1 hora y 2 minutos cobra solo la hora (₡700).
            • Al pasar del tope de ₡3000 por cada 12 horas, se cobra automáticamente como diaria.
            """,

        ["Ingresos"] =
            """
            El módulo INGRESOS muestra todo el dinero cobrado (cada salida es un ingreso).

            ── BUSCAR INGRESOS ──
            1. Elija el rango DESDE / HASTA.
            2. (Opcional) Filtre por FORMA DE PAGO (Efectivo / SINPE / Mixto) o por EMPLEADO.
               En los pagos MIXTOS, la parte en efectivo y la parte SINPE se suman a los
               totales correspondientes.
            3. Presione 'Buscar'.

            Arriba se ven los TOTALES: efectivo, SINPE, total y cantidad de cobros.
            La tabla muestra cada cobro con su placa, tarifa, forma de pago, monto y empleado.

            ── DESCARGAR PDF ──
            Presione 'Descargar PDF del rango' para guardar un reporte de los ingresos del rango.
            """,

        ["Tarifas"] =
            """
            Las TARIFAS definen cuánto se cobra. Normalmente solo el administrador las cambia.

            ── CREAR UNA TARIFA NUEVA ──
            1. Presione 'Nueva' para limpiar el formulario.
            2. Escriba un NOMBRE (por ejemplo: 'Por hora').
            3. Elija el TIPO:
               • Hora: cobra por cada hora.
               • Dia / Semana / Mes: cobra por cada día, semana o mes.
               • Fija: cobra un monto único, sin importar el tiempo.
            4. Escriba el MONTO en colones.
            5. MINUTOS DE GRACIA: margen para no cobrar de más por pasarse unos minutos del
               último cobro (ejemplo: con gracia de 10, una estadía de 1h02m cobra solo la hora).
               El cobro siempre empieza desde que el vehículo ingresa.
            6. TOPE POR 12H (solo tarifas por hora): si lo marca, la tarifa cobra por hora pero
               nunca más del monto indicado por cada 12 horas. Al pasar de ese tope, la estadía
               se cobra como tarifa diaria. Ejemplo: ₡700/hora con tope de ₡3000 por 12h.
            7. Marque 'Activa' para que la tarifa aparezca al registrar entradas.
            8. Presione 'Guardar tarifa'.

            ── EDITAR UNA TARIFA ──
            Haga clic en una tarifa de la lista, cambie lo que necesite y presione 'Guardar tarifa'.

            Si desmarca 'Activa', la tarifa deja de ofrecerse pero NO se borra (puede reactivarla luego).
            """,

        ["Usuarios"] =
            """
            Aquí se crean y administran los EMPLEADOS que usan el sistema. Solo el administrador.

            ── CREAR UN EMPLEADO ──
            1. Presione 'Nuevo'.
            2. Escriba la CÉDULA (será su usuario para iniciar sesión).
            3. Escriba el NOMBRE COMPLETO.
            4. Escriba una CONTRASEÑA.
            5. Elija el PUESTO:
               • Empleado: acceso limitado según los permisos que marque.
               • Administrador: acceso total a todo el sistema.
            6. Marque los PERMISOS (qué módulos podrá usar ese empleado).
            7. Deje marcado 'Activo'.
            8. Presione 'Guardar usuario'.

            ── EDITAR UN EMPLEADO ──
            Haga clic en un usuario de la lista y cambie sus datos.
            • Para cambiarle la contraseña, escriba una nueva.
            • Si deja la contraseña EN BLANCO, se mantiene la que ya tenía.

            Para que un empleado ya no pueda entrar (porque dejó de trabajar), desmarque 'Activo'.
            No se borra: solo queda inactivo.
            """,

        ["Cierres"] =
            """
            Aquí se hace el CIERRE DEL DÍA y se consulta el historial.

            ═══ PESTAÑA 'REGISTRAR CIERRES' ═══
            Presione el botón verde grande 'HACER EL CIERRE DEL DÍA' y siga los pasos:

            PASO 1 — ENTREGAR EL DINERO COBRADO (cierre de empleado):
            Cuente el dinero cobrado en el turno (lo que va a entregar) y escriba la
            cantidad de cada billete y moneda. El sistema muestra el EFECTIVO esperado y,
            aparte, cuánto se cobró por SINPE (el SINPE no se entrega en físico).
            Presione 'Continuar': el cierre se guarda y su tiquete se imprime solo.

            PASO 2 — CONTAR EL FONDO DE LA CAJA (cierre de caja):
            Cuente el dinero que QUEDA en la caja (el fondo para vueltos) y escriba las
            cantidades. El sistema compara al instante:
            • Cuadra: queda exactamente el fondo. Todo bien.
            • Sobra: hay MÁS dinero que el fondo.
            • Falta: hay MENOS dinero que el fondo.
            Presione 'Finalizar cierre': se guarda y su tiquete se imprime solo. ¡Listo!

            Si el conteo no cuadra, el sistema avisa y pregunta si desea continuar.
            Entregue cada tiquete impreso junto con el dinero.

            El administrador puede corregir el FONDO DE CAJA con el botón 'Cambiar fondo';
            el valor queda guardado en la base de datos para todas las computadoras.

            En el HISTORIAL puede reimprimir el tiquete de cualquier cierre con el botón
            'Imprimir tiquete'.

            ═══ PESTAÑA 'HISTORIAL' ═══
            Busque cierres anteriores por fecha y por tipo (empleado o caja),
            y descargue el PDF de cualquiera con el botón 'Descargar PDF'.
            """
    };
}
