# Fase 2 — GUI y aplicación segura: estado de implementación

Fecha: 2026-08-25

**Estado actual:** implementada; flujo principal sobre `DBText` validado y
matriz manual complementaria pendiente.

## Implementado

- `ReviewCoordinator` reúne proveedores, elimina propuestas duplicadas, respeta
  el máximo solicitado y valida cada alternativa antes de exponerla.
- El diff y las advertencias mostrados se reconstruyen desde el validador local;
  no se confía en el diff declarado por un proveedor.
- `SpellReviewWindow` y `SpellReviewViewModel` muestran original, idioma,
  propuesta, explicación, cambios y estado técnico en una ventana WPF modal.
- Las decisiones disponibles son **Mantener original**, **Aplicar** y
  **Cancelar**. La edición manual se pospuso para evitar ampliar el riesgo del
  primer corte.
- `SelectionHelper` crea un `TextSnapshot` sin mantener abierta la transacción de
  lectura.
- `TextWriter` abre una transacción corta solo después de **Aplicar** y vuelve a
  comprobar huella del documento, handle, tipo de entidad y texto original.
- En este corte, `AISPELL` conservaba la ruta local y todavía no introducía red,
  credenciales, memoria ni dependencias de un proveedor de IA. Las fases
  posteriores agregaron proveedores sin cambiar las garantías de escritura.

## Verificación automatizada

- `Debug|AnyCPU` y `Debug|x64` compilan con cero advertencias y cero errores.
- El XAML se compila como recurso BAML del ensamblado.
- La salida x64 contiene el complemento, símbolos y glosario; no copia DLL de
  Autodesk.
- Pasan 24 pruebas: 19 del motor y dominio de fase 1, tres del coordinador y dos
  de decisiones del ViewModel.

## Validación manual restante

Falta completar dentro de Civil 3D 2024 la matriz de
`10_MVP_STATUS_AND_TEST_PLAN.md`: `MText`, Mantener, Cancelar, cierre de ventana,
conflicto y `UNDO`. `accoreconsole` no puede validar una ventana WPF interactiva,
por lo que una compilación correcta no sustituye esa comprobación. Las fases 3
y 4 se implementaron después de validar el flujo principal; estos casos siguen
siendo una puerta de estabilización antes del piloto.

## Incidencias de validación

- 2026-08-25: la primera carga interactiva llegó correctamente hasta la
  selección, pero AutoCAD terminó al representar la ventana. El informe CER
  identificó un enlace WPF `TwoWay` implícito hacia la propiedad de solo lectura
  `ProposedText`. Se corrigieron los enlaces de los dos `TextBox` a `OneWay` y se
  añadió una barrera de excepción al comando para evitar que un fallo de UI no
  controlado derribe el proceso.
- 2026-08-25: la repetición con la DLL corregida abrió la ventana, mostró el
  original `DISEÑO DE LA CARRETERAA PRINCIPALL`, propuso y aplicó correctamente
  `CARRETERAA` → `CARRETERA`, con diff local y validación técnica aprobada. Quedan
  pendientes las comprobaciones separadas de `MText`, Mantener, Cancelar,
  conflicto y `UNDO`.
