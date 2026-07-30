# Architecture

Layers

Presentation
- AiSpellCommand

Adapters
- SelectionHelper
- TextEditor

Core
- SpellEngine

Future
- AI Services
- Dictionary Services
- UI (WPF)

Dependency Rule

Presentation -> Adapters -> Core

Core must not depend on AutoCAD APIs.
