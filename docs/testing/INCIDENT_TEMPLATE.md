# Plantilla de incidencia de regresión

Guardar una copia por incidencia. No incluir texto de planos reales, DWG,
respuestas de OpenAI, claves, rutas de usuario, handles ni otros metadatos
sensibles.

## Identificación

- ID:
- Título:
- Caso de regresión relacionado:
- Fecha y zona horaria:
- Reportado por:
- Severidad:
- Estado:

Severidades:

- **Crítica:** escritura sin confirmación, pérdida/corrupción, modificación tras
  cancelar, alteración de tokens protegidos o fallo de atomicidad.
- **Alta:** flujo principal inutilizable, `UNDO` incorrecto, conflicto no
  detectado o exposición de datos/secretos.
- **Media:** función recuperable con workaround, error de proveedor mal
  presentado o persistencia incorrecta sin pérdida del dibujo.
- **Baja:** texto, distribución visual o comportamiento menor sin impacto en la
  integridad ni en la decisión del usuario.

## Entorno

- Civil 3D/AutoCAD:
- Windows y .NET Framework:
- Configuración `Debug|x64` o `Release|x64`:
- Versión del ensamblado:
- SHA-256 de `CivilSpellAI.dll`:
- Commit base y existencia de cambios locales:
- Proveedor: local, simulado u OpenAI:
- Modelo, si aplica, sin incluir credencial:

## Reproducción anonimizada

Precondiciones:

1.

Pasos:

1.

Resultado esperado:

Resultado observado:

Frecuencia: siempre, intermitente o una vez.

## Impacto de seguridad

- ¿Cambió el dibujo?:
- ¿Se pulsó **Aplicar**?:
- ¿Cancelación/cierre produjo escritura?:
- ¿Cambió algún número, unidad, estación, código, formato o término protegido?:
- ¿`UNDO` restauró exactamente el estado anterior?:
- ¿Apareció texto o un secreto en mensajes, archivos o capturas?:

## Evidencia permitida

- Informe generado por `scripts\Validate-Mvp.ps1`.
- Captura recortada con fixtures anonimizados.
- Mensajes de Civil 3D sin contenido sensible.
- Diff mínimo usando exclusivamente los fixtures del runbook.

## Resolución

- Causa:
- Cambio aplicado:
- Pruebas añadidas:
- Casos manuales repetidos:
- Fecha de verificación:
- Resultado final:

