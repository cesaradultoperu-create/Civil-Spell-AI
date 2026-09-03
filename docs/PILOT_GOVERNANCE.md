# Gobierno del piloto

Fecha de decisión: 2026-08-27.

## Responsabilidad y alcance

El responsable operativo es el propietario del proyecto CivilSpellAI. Solo esa
persona autoriza equipos adicionales, cambios de credencial, exportaciones de
memoria y ampliaciones de alcance. El piloto 1.1.5.0 se limita a Civil 3D 2024
x64 en equipos controlados y a `DBText`/`MText` directos.

El paquete interno no está firmado. Cada instalación debe comprobar el SHA-256
publicado en `docs/codex/NEXT_TASK.md`. Equipos cuya política exija firma quedan
fuera hasta disponer de certificado de editor confiable.

## Datos y privacidad

Las pruebas usan únicamente:

- `Tests/TestCases/civil3d-annotations.json`;
- los fixtures sintéticos de los runbooks;
- dibujos desechables sin información de proyecto;
- una clave OpenAI de prueba con límite de gasto, cuando corresponda.

No se adjuntan DWG, textos reales, respuestas remotas, claves, rutas, handles ni
exportaciones de memoria a incidencias. La prueba de conexión usa una frase fija
y requiere confirmación explícita.

## Incidencias y decisión de avance

Todo fallo se registra con `docs/testing/INCIDENT_TEMPLATE.md`. Una escritura
sin confirmación, cambio aceptado de token técnico, escritura posterior a
cancelar o lote parcial detiene el piloto y se clasifica como crítico.

Los resultados del corte 1.1 se registran en
`docs/testing/PILOT_1_1_REGRESSION_RUNBOOK.md`. Solo después de completar las
filas aplicables sin defectos críticos puede ampliarse el piloto.
