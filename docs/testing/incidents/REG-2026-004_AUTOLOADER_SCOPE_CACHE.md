# REG-2026-004 — Caché de autoloader después de migrar el alcance

## Identificación

- ID: REG-2026-004
- Fecha y zona horaria: 2026-09-02, America/Lima
- Severidad: Media
- Estado: Cerrada
- Versión observada y verificada: 1.1.3.0

## Reproducción

1. Instalar el mismo `ProductCode` primero en `CurrentUser` y después bajo
   `%PROGRAMFILES%\Autodesk\ApplicationPlugins`.
2. Verificar la copia administrada y retirar la copia del perfil con Civil 3D
   cerrado.
3. Abrir Civil 3D e invocar `AISPELLSETTINGS`.

Resultado observado: el paquete administrado aparece en el registro interno de
bundles, pero la primera invocación puede responder `Unknown command` porque la
sesión conserva la ruta procesada durante la migración.

Resultado esperado: `AISPELLSETTINGS` activa la DLL administrada por invocación
sin `NETLOAD` ni advertencias de confianza.

## Impacto de seguridad

- El dibujo no cambió.
- No hubo transacciones ni solicitudes remotas.
- No se expusieron textos, rutas de DWG, handles ni credenciales.

## Resolución

Se confirmó `APPAUTOLOAD=14`, la versión 1.1.3.0 y la igualdad de hashes del
manifiesto, DLL, glosario y ayuda. Al ejecutar `APPAUTOLOADER` y elegir **Reload**
una sola vez, `AISPELLSETTINGS` abrió desde `Program Files` sin `NETLOAD` ni
aviso de archivo no firmado. El instalador y la guía muestran ahora este paso de
recuperación después de una migración o actualización.

Fecha de verificación: 2026-09-02. Resultado final: PASS.
