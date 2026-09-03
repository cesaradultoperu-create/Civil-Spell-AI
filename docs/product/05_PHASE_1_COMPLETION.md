# Fase 1 — Núcleo de revisión sin UI: resultado

Fecha: 2026-08-25

## Entregado

- Modelos inmutables para snapshot, solicitud, propuesta, diff, validación y
  decisión del usuario en `Domain/`, sin referencias a Autodesk, UI, red o disco.
- Contratos `ITextCorrectionProvider`, `IProposalValidator`,
  `ITechnicalGlossary`, `ILearningStore` e `ITextDiffer`.
- `RuleBasedCorrectionProvider`, que adapta `SpellEngine` sin cambiar su API
  pública y devuelve propuestas con origen, idioma, explicación y diff local.
- `TextDiffer` por tokens, que calcula localmente los segmentos modificados.
- `TechnicalTokenValidator`, que bloquea alteraciones, eliminaciones o cambios de
  orden en números, unidades, estaciones, códigos y términos del glosario.
- Diecinueve pruebas autocontenidas: nueve del motor existente y diez del nuevo
  dominio, proveedor, diff y validador.

## Garantías obtenidas

- Una propuesta externa no puede declarar su propio diff como fuente de verdad.
- Una propuesta idéntica al original no es aplicable.
- Una corrección lingüística sí es aplicable cuando conserva todos los tokens
  técnicos detectados de forma exacta y en el mismo orden.
- `SpellEngine` continúa independiente de Autodesk y conserva sus métodos de
  compatibilidad.

## Próximo paso

Fase 2: crear el coordinador y la ventana WPF modal, conectar `AISPELL` al nuevo
flujo y probar aplicar, conservar, cancelar, conflicto y `Undo` en Civil 3D 2024.
