# REG-2026-001 — Alternativa adicional inaccesible con mouse

## Identificación

- ID: REG-2026-001
- Caso relacionado: SAFE-01 / `UnsafeTechnicalChange`
- Fecha y zona horaria: 2026-08-26, America/Lima
- Severidad: Media
- Estado: Cerrada
- DLL de revalidación: `2B4F7168C5D9472D198BDEB893463FA0B0C30B83EF7032270ED551AC508F5E20`

## Entorno

- Civil 3D 2024.
- CivilSpellAI `Debug|x64`, versión 1.0.0.0.
- SHA-256: `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7`.
- Proveedor simulado: `UnsafeTechnicalChange`.
- Fixture anonimizado: `CARRETERAA 25 m`.

## Reproducción

1. Habilitar el proveedor simulado con `UnsafeTechnicalChange`.
2. Ejecutar `AISPELL` sobre el fixture.
3. Esperar el mensaje `Proveedor de IA: 1 alternativa(s) añadida(s)`.
4. Intentar seleccionar con mouse la alternativa adicional.

Resultado esperado: ambas alternativas son visibles y seleccionables con mouse
y teclado.

Resultado observado: la lista muestra únicamente la alternativa local y una
barra de desplazamiento mínima. No se puede alcanzar la alternativa adicional
con mouse; al enfocar la primera fila y pulsar `↓`, la segunda alternativa se
selecciona y el resultado cambia a `999 m`.

## Impacto

- No se modificó el dibujo.
- La validación de seguridad funciona y bloquea **Aplicar** para `999 m`.
- Workaround: seleccionar la primera alternativa y pulsar `↓`.
- Impacto principal: descubrimiento, uso con mouse y accesibilidad de las
  alternativas adicionales.

## Resolución y revalidación

- Se aumentó el alto de la ventana y del área de alternativas, con un mínimo
  suficiente y scrollbar explícito para hasta tres propuestas.
- El 2026-08-26 se cargó la DLL de revalidación `Debug|x64` con SHA-256
  `2B4F7168C5D9472D198BDEB893463FA0B0C30B83EF7032270ED551AC508F5E20`.
- La ventana mostró simultáneamente dos alternativas completas; la segunda se
  seleccionó con el mouse, actualizó el resultado propuesto y permaneció bajo
  el control normal de **Aplicar**/**Cancelar**.
- Resultado: PASS. La incidencia se cierra sin escrituras no solicitadas.
- La verificación con lector de pantalla queda como mejora de accesibilidad del
  segundo alcance y no como defecto de seguridad del MVP.
