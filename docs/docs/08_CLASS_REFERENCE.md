# Class Reference

## AiSpellCommand
Responsabilidad:
- Punto de entrada del comando AISPELL.
- Coordina selección, corrección y actualización.

Dependencias:
- SelectionHelper
- SpellEngine
- TextEditor

## SpellEngine
Responsabilidad:
- Analizar texto.
- Detectar errores.
- Generar texto corregido.
- Sin dependencias de AutoCAD.

## SelectionHelper
Responsabilidad:
- Seleccionar entidades DBText/MText.
- Extraer ObjectId y contenido.

## TextEditor
Responsabilidad:
- Escribir el texto corregido en la entidad.
- Gestionar modificaciones mediante transacciones.

## Flujo

AiSpellCommand
 -> SelectionHelper
 -> SpellEngine
 -> TextEditor
