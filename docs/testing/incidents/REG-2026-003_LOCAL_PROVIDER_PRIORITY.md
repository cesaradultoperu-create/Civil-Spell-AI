# REG-2026-003 — Recuerdo duplicado desplaza la regla local

## Identificación

- ID: REG-2026-003
- Fecha y zona horaria: 2026-09-02, America/Lima
- Severidad: Baja
- Estado: Cerrada
- Versión observada: 1.1.2.0
- Versión corregida: 1.1.3.0

## Reproducción

1. Recordar una corrección local.
2. Ejecutar nuevamente `AISPELL` o `AISPELLALL` sobre el texto original.
3. Abrir la lista de alternativas.

Resultado observado: la preferencia aprendida aparece antes y puede ocultar por
deduplicación la alternativa equivalente de reglas locales; la revisión puede
mostrar además idioma «No identificado».

Resultado esperado: las reglas locales deterministas tienen prioridad y fijan
el idioma; la memoria solo agrega alternativas realmente distintas y nunca se
autoaplica.

## Resolución

Una fábrica común construye los proveedores locales en el orden reglas y luego
memoria para `AISPELL` y `AISPELLALL`. La prueba `Las reglas locales preceden
recuerdos duplicados` confirma prioridad, idioma español, eliminación del
duplicado y conservación de un recuerdo distinto.

## Revalidación

El 2026-09-02 se recordaron y aplicaron tres decisiones, se restauró el lote con
un único `U` y se ejecutó nuevamente `AISPELLALL`. Los tres desplegables
mostraron una sola opción, `Alternativa 1 · Reglas locales`; ningún recuerdo
idéntico desplazó ni duplicó la regla. Resultado: PASS.
