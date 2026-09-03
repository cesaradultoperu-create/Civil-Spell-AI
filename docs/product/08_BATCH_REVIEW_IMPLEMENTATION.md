# Revisión global del dibujo: implementación

Fecha: 2026-08-25

## Objetivo aprobado

Revisar todos los textos compatibles del dibujo, mostrar una lista de errores y
aplicar las correcciones seleccionadas solo después de confirmación explícita.

## Implementado

- `AISPELLALL` escanea `DBText` y `MText` directos en Model Space y layouts sin
  mantener abierta la transacción durante el análisis o la ventana.
- El análisis reutiliza proveedores locales/simulados, glosario, diff y
  `TechnicalTokenValidator`; admite hasta cuatro revisiones concurrentes.
- La lista muestra handle, tipo, origen, original, propuesta, diff y validación.
- Se preselecciona la alternativa aplicable con mayor cobertura; el usuario
  puede excluir cualquier fila.
- `TextWriter.ApplyBatch` revalida documento, handle, tipo y texto original de
  todos los objetos antes de modificar el primero.
- Todo el lote se confirma en una transacción. Un conflicto aborta la operación
  completa y un `UNDO` revierte todos los cambios.

## Límites actuales

- No incluye atributos de bloque, tablas, etiquetas de Civil 3D ni textos dentro
  de referencias externas.
- Las reglas locales tienen una amplitud lingüística limitada. La fase 4 añadió
  OpenAI real para español, inglés y texto mixto; todas sus propuestas siguen
  pasando por el mismo validador local.

## Verificación

- En el corte de esta ampliación pasaban treinta y dos pruebas automatizadas; el
  corte actual del MVP pasa treinta y seis.
- `Debug|x64` compila de forma aislada con cero advertencias y cero errores.
- Validación manual en Civil 3D 2024: `AISPELLALL` revisó 10 textos, mostró tres
  correcciones locales aprobadas y permitió aplicarlas como lote. Con el
  escenario `InvalidResponse`, registró los fallos simulados sin impedir las
  correcciones locales.
- Pendientes específicos: conflicto provocado durante la ventana y `UNDO`
  documentado. La ampliación a tipos distintos de `DBText`/`MText` se evaluará
  después del piloto.
