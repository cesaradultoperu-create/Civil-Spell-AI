# Fase 3 — Configuración y proveedor simulado: estado

Fecha: 2026-08-25

**Estado:** completada.

## Implementado

- Configuración JSON con esquema versión 1 bajo `%LOCALAPPDATA%\CivilSpellAI`.
- Glosario personal normalizado, sin duplicados y separado del glosario
  integrado.
- Ventana `AISPELLSETTINGS` para habilitar la simulación, elegir escenario y
  editar términos personales.
- `FakeAiCorrectionProvider` sin red ni credenciales, con escenarios
  `Successful`, `Unavailable`, `Timeout`, `InvalidResponse` y
  `UnsafeTechnicalChange`.
- El coordinador traduce fallos del proveedor a estado recuperable y conserva
  las propuestas locales.
- La ventana muestra carga y errores, permite reintentar y cancela solicitudes
  pendientes al cerrarse.
- Todas las propuestas pasan por el mismo `TechnicalTokenValidator` y reciben
  diff recalculado localmente.

## Verificación automatizada

- Compilación aislada `Debug|x64`: cero advertencias y cero errores.
- Treinta pruebas cubren persistencia, normalización, alternativas simuladas,
  fallos recuperables, bloqueo técnico y conservación de la propuesta local.

## Validación manual

La validación se completó cerrando Civil 3D, recompilando y cargando la DLL del
corte. En esta fase todavía no existía conexión con un proveedor real; esa
integración se añadió y registró posteriormente en la fase 4.

## Resultados manuales

- 2026-08-25: se cargó la DLL de fase 3 en Civil 3D 2024, se abrió
  `AISPELLSETTINGS`, se habilitó el escenario `Successful` y `AISPELL` mostró y
  aplicó correctamente las alternativas simuladas junto con la propuesta local.
- 2026-08-25: con `UnsafeTechnicalChange` y `CARRETERAA 25 m`, la alternativa
  que alteraba `25` a `999` quedó visible pero bloqueada, con explicación y
  **Aplicar** deshabilitado; la propuesta local segura permaneció disponible.
- 2026-08-25: con `Timeout`, la GUI informó el tiempo de espera, conservó la
  propuesta local y habilitó **Reintentar** sin modificar el dibujo.
- 2026-08-25: con `InvalidResponse`, la GUI informó la respuesta no válida,
  conservó la propuesta local y habilitó **Reintentar** sin modificar el dibujo.
- 2026-08-25: el término personal `PRINCIPALL` persistió al reabrir
  `AISPELLSETTINGS`; las reglas lo conservaron y la alternativa simulada que
  intentó modificarlo quedó bloqueada.
- 2026-08-25: al cancelar/cerrar durante la carga, la ventana terminó la
  solicitud pendiente, no modificó el objeto posteriormente y Civil 3D continuó
  operativo.

## Resultado

La GUI maneja propuestas locales y simuladas, fallos recuperables y bloqueos
técnicos sin escritura automática. La fase 3 queda cerrada. La fase 4 permanece
condicionada a las decisiones de proveedor, privacidad, presupuesto y custodia
de credenciales definidas en `02_SCOPE_REQUIREMENTS.md`.
