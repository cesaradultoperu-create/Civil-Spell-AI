# Contexto para Codex

Arquitectura actual

Presentation
- AiSpellCommand

Adapters
- SelectionHelper
- TextEditor

Core
- SpellEngine

Reglas
- SpellEngine no depende de AutoCAD.
- Toda interacción con Civil 3D pasa por la capa Autodesk.
- Mantener responsabilidades separadas.
