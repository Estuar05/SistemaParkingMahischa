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
            2. Elija el TIPO DE TARIFA (por hora, por día, etc.).
            3. Presione 'Registrar entrada'.
            4. Se abre el tiquete con un código QR. Presione 'Imprimir' y entréguelo al cliente.
               (Guarde el tiquete: ese QR es lo que se escanea a la salida.)

            ── REGISTRAR UNA SALIDA (cuando el auto se va) ──
            FORMA RÁPIDA, con el escáner:
            1. Haga clic dentro del campo 'Código QR / ticket'.
            2. Escanee el QR del tiquete del cliente con el lector.
            3. Se abre automáticamente una ventana con los datos del vehículo y el MONTO A COBRAR.
            4. Cobre, presione 'Registrar salida' y confirme.

            FORMA MANUAL, buscando la placa:
            1. Escriba la placa en 'Buscar por placa' y presione 'Buscar placa'.
            2. Haga clic en el vehículo en la lista.
            3. Presione 'Registrar salida' y confirme.

            ── AL COBRAR (ventana de cobro) ──
            Al registrar la salida se abre la ventana de COBRO:
            • MONTO EXTRA: si el cliente se pasó unos minutos, puede sumar un monto adicional.
            • FORMA DE PAGO: elija 'Efectivo' o 'SINPE'.
            • En EFECTIVO puede escribir con cuánto paga el cliente ('Paga con') y el sistema
              calcula automáticamente el VUELTO.
            • En SINPE puede anotar la referencia o comprobante (opcional).

            ── TARIFA PERSONALIZADA ──
            Para cobrar diferente a un cliente puntual: seleccione el vehículo y presione
            'Tarifa personalizada'. Defina la unidad (hora/día/semana/mes/fija) y el monto.
            Se aplica SOLO a esa estadía y queda registrada.

            ── OTRAS OPCIONES ──
            • 'Reimprimir': vuelve a imprimir el tiquete del vehículo seleccionado.
            • 'Ocultar vehículos con salida': muestra solo los autos que siguen adentro.

            La tarifa por hora se cobra por hora, pero al pasar del tope de ₡3000 por cada 12 horas
            se cobra automáticamente como tarifa diaria.
            """,

        ["Ingresos"] =
            """
            El módulo INGRESOS muestra todo el dinero cobrado (cada salida es un ingreso).

            ── BUSCAR INGRESOS ──
            1. Elija el rango DESDE / HASTA.
            2. (Opcional) Filtre por FORMA DE PAGO (Efectivo / SINPE) o por EMPLEADO.
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
            5. MINUTOS DE GRACIA: minutos al inicio que NO se cobran
               (ejemplo: 10 = los primeros 10 minutos son gratis).
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
            Aquí se hacen los CIERRES de caja y de empleados, y se consulta el historial.

            ═══ PESTAÑA 'REGISTRAR CIERRES' ═══

            ── CIERRE DE EMPLEADO (lado izquierdo) ──
            Sirve para saber cuánto EFECTIVO debe entregar un empleado.
            1. Elija el EMPLEADO.
            2. Elija el rango DESDE / HASTA (las fechas y horas del turno).
            3. Presione 'Calcular esperado'. El sistema muestra el EFECTIVO esperado y, aparte,
               cuánto cobró por SINPE (el SINPE no se entrega en físico).
            4. Cuente los billetes y monedas ENTREGADOS y escriba la cantidad de cada uno.
            5. Presione 'Cerrar empleado'. El sistema muestra si hay diferencia.

            ── CIERRE DE CAJA (lado derecho) ──
            Sirve para cuadrar la caja al final del día.
            En la caja SIEMPRE deben quedar ₡20 000 como FONDO BASE.

            El sistema le muestra:
            • Fondo de caja (base): los ₡20 000 que siempre deben estar.
            • Efectivo cobrado hoy: el efectivo que se cobró hoy (el SINPE se muestra aparte).
            • Esperado en caja: el fondo base + el efectivo cobrado (lo que DEBERÍA haber en físico).
            El SINPE NO se cuenta en la caja física, por eso se muestra por separado.

            Pasos:
            1. Cuente los billetes y monedas y escriba la cantidad de cada uno.
            2. El sistema suma el CONTADO y le muestra la DIFERENCIA:
               • Cuadra: el dinero coincide con lo esperado. Todo bien.
               • Sobra: hay MÁS dinero del esperado.
               • Falta: hay MENOS dinero del esperado.
            3. Presione 'Cerrar caja' para guardar el cierre.

            ═══ PESTAÑA 'HISTORIAL' ═══
            Busque cierres anteriores por fecha y por tipo (empleado o caja),
            y descargue el PDF de cualquiera con el botón 'Descargar PDF'.
            """
    };
}
