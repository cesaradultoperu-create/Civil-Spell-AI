# REG-2026-002 — Regla local bloqueada al corregir hacia el glosario

## Identificación

- ID: REG-2026-002
- Fecha y zona horaria: 2026-09-02, America/Lima
- Severidad: Media
- Estado: Cerrada
- Versión observada: 1.1.2.0
- Versión corregida: 1.1.3.0

## Reproducción

1. Ejecutar `AISPELLALL` sobre el fixture 1.1.2.
2. Seleccionar `THE EXISTENT SURFCE AT STATION 1+250.00`.
3. Revisar la propuesta local `SURFCE` → `SURFACE`.

Resultado observado: la fila queda inhabilitada con «La propuesta altera
términos del glosario protegidos» porque `SURFACE` aparece por primera vez.

Resultado esperado: una regla local confiable puede introducir la forma
correcta del glosario si conserva exactamente los términos protegidos que ya
existían en el original.

## Resolución

El validador distingue el origen `LocalRules`: conserva los términos originales
como subsecuencia exacta y permite nuevos términos. IA, memoria y edición manual
mantienen la comparación estricta, por lo que no pueden introducir ni alterar
términos protegidos.

La prueba `Las reglas locales pueden corregir hacia el glosario` pasa y también
confirma que la misma propuesta con origen IA permanece bloqueada.

## Revalidación

El 2026-09-02, Civil 3D 2024 cargó 1.1.3.0 y `AISPELLALL` mostró la propuesta
`THE EXISTING SURFACE AT STATION 1+250.00` habilitada, seleccionada y con
validación técnica aprobada. Resultado: PASS; no hubo escritura no solicitada.
