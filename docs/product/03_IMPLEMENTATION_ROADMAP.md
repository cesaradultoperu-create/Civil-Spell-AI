# Plan de implementación incremental

## Estado del roadmap

Este documento conserva la secuencia que condujo al MVP. El estado verificable
actual está en `10_MVP_STATUS_AND_TEST_PLAN.md` y la ejecución posterior se rige
por `11_SCOPE_2_ROADMAP.md`. La revisión global se incorporó durante el MVP y la
fase 4 ya está implementada.

## Regla de ejecución

Cada fase debe compilar, conservar `AISPELL` operativo y añadir pruebas antes de
pasar a la siguiente. No se conectará un proveedor real de IA hasta que la GUI,
los validadores y el proveedor simulado estén terminados.

## Fase 0 — Base reproducible y contrato de cambio

**Estado:** completada. Ver `04_PHASE_0_COMPLETION.md`.

**Objetivo:** eliminar ambigüedades de entrega antes de modificar el producto.

- Añadir una guía de instalación, carga con `NETLOAD` y diagnóstico de DLL
  bloqueada.
- Crear `.gitignore` y retirar de control de versiones `bin/` y `obj/` en una
  modificación de mantenimiento separada y revisada.
- Crear un proyecto de pruebas compatible con .NET Framework 4.8 y configurar
  ejecución local.
- Definir ejemplos reales anonimizados de anotaciones de Civil 3D y los términos
  que nunca se pueden cambiar.

**Salida:** compilación limpia en un directorio de salida aislado, pruebas
ejecutables y dataset de casos de aceptación. Esta fase no altera el flujo del
usuario.

## Fase 1 — Núcleo de revisión sin UI

**Estado:** completada. Ver `05_PHASE_1_COMPLETION.md`.

**Objetivo:** representar propuestas y decisiones sin depender de AutoCAD ni IA.

- Crear los modelos `TextSnapshot`, `CorrectionRequest`, `CorrectionProposal`,
  `ReviewDecision` y `ProposalSource`.
- Definir `ITextCorrectionProvider`, `IProposalValidator`, `ITechnicalGlossary`
  e `ILearningStore`.
- Envolver el actual `SpellEngine` en `RuleBasedCorrectionProvider`, preservando
  sus métodos públicos de compatibilidad.
- Implementar diff local y `TechnicalTokenValidator` con pruebas de números,
  unidades, estaciones, códigos y términos del glosario.

**Salida:** pruebas unitarias prueban que una propuesta insegura no es aplicable
y que las reglas actuales siguen generando los mismos resultados.

## Fase 2 — GUI de revisión y aplicación segura

**Estado:** implementación terminada; flujo principal validado y matriz manual
parcial pendiente. Ver
`06_PHASE_2_IMPLEMENTATION.md`.

**Objetivo:** reemplazar la confirmación de consola por una revisión visual
completa, todavía con reglas locales.

- Agregar referencias WPF necesarias para .NET Framework 4.8 y crear
  `SpellReviewWindow`, ViewModel y recursos mínimos.
- Cambiar `AISPELL` para capturar `TextSnapshot`, invocar el coordinador y abrir
  la ventana modal.
- Implementar acciones Mantener original, Aplicar y Cancelar; agregar edición
  manual validada si no incrementa el riesgo del primer corte.
- Crear `TextWriter`/adaptador que revalida el snapshot al aplicar y hace una
  transacción de escritura corta.
- Verificar en Civil 3D que Undo revierte una aplicación y que cancelación no
  deja cambios.

**Salida:** un usuario elige una propuesta de reglas en una GUI y el flujo
selección -> corrección -> edición se conserva con aprobación obligatoria.

## Fase 3 — Configuración, glosario y proveedor simulado

**Estado:** completada. Ver `07_PHASE_3_IMPLEMENTATION.md`.

**Objetivo:** preparar la IA sin depender todavía de red ni credenciales.

- Implementar configuración versionada por usuario, glosario personal y pantalla
  de administración mínima.
- Crear `FakeAiCorrectionProvider` con respuestas deterministas de prueba,
  incluida respuesta malformada, timeout y alternativas inseguras.
- Incorporar estados de carga, error, reintento y cancelación en el ViewModel.
- Asegurar que las propuestas de reglas, simuladas y futuras de IA pasan por el
  mismo validador y el mismo diff.

**Salida:** la GUI maneja varias alternativas y todos los errores de proveedor
sin cambiar el dibujo.

## Fase 4 — Proveedor LLM real, con seguridad

**Estado:** implementada. Flujo feliz de OpenAI validado en `AISPELL` y
`AISPELLALL`; errores reales y `UNDO` remoto pendientes. Ver
`09_OPENAI_TEXT_ONLY_IMPLEMENTATION.md`.

**Precondición:** las cinco decisiones externas de `02_SCOPE_REQUIREMENTS.md`
están aprobadas y existe una prueba de privacidad autorizada.

- Implementar el adaptador del proveedor elegido detrás de
  `ITextCorrectionProvider`; no filtrar su SDK fuera de infraestructura.
- Definir solicitud y respuesta JSON estructurada, límite de longitud, timeout,
  cancelación, errores traducidos y reintentos acotados.
- Implementar la pantalla de consentimiento, selección de proveedor y guardado
  protegido de credenciales.
- Inyectar solo texto seleccionado, restricciones y términos relevantes; excluir
  el DWG completo y secretos.
- Ejecutar pruebas de contrato con respuestas grabadas anonimizadas y pruebas de
  seguridad contra cambios de tokens protegidos.

**Salida:** la IA aporta alternativas explicadas y seguras; al desconectarla,
CivilSpellAI sigue funcionando con reglas locales.

## Fase 5 — Aprendizaje local controlado

**Estado:** no implementada; trasladada al hito 6 del segundo alcance.

**Objetivo:** adaptar las sugerencias a decisiones del profesional sin perder
control ni privacidad.

- Implementar el almacén local de decisiones y su esquema versionado.
- Guardar únicamente elecciones que el usuario marcó como recordables o aprobó
  explícitamente para aprendizaje.
- Usar coincidencias exactas/normalizadas para elevar preferencias y ampliar el
  contexto del proveedor; nunca aplicar una memoria sin GUI.
- Crear administración de memoria: listar, borrar, exportar e inhabilitar.
- Añadir pruebas de persistencia, migración de esquema y no autoaplicación.

**Salida:** una preferencia aprobada reaparece como sugerencia explicable en otra
sesión del mismo usuario y se puede revocar.

## Fase 6 — Calidad, distribución y piloto

**Estado:** iniciada con build y pruebas del núcleo; estabilización, integración,
diagnóstico y distribución continúan en los hitos 1 a 4 del segundo alcance.

**Objetivo:** convertir el MVP en una entrega utilizable por un equipo piloto.

- Añadir pruebas de integración del comando con mocks de AutoCAD donde sea
  posible y una matriz manual para Civil 3D 2024.
- Probar textos largos, MText con formato, español/inglés/mixto, offline,
  conflicto de edición, cancelación y cierre de documento.
- Preparar empaquetado, versión de ensamblado, guía de actualización y mecanismo
  de rollback.
- Realizar un piloto con textos anonimizados, registrar errores técnicos sin
  contenido y revisar el glosario con especialistas de Civil 3D.

**Salida:** paquete instalable, manual de operación y lista priorizada de mejoras
basada en uso real.

## Priorización posterior al piloto

La corrección masiva fue aprobada e implementada durante el MVP. Etiquetas,
atributos, perfiles compartidos, Ribbon, paleta no modal, traducción y otras
versiones continúan sujetos a evaluación posterior al piloto. Cada iniciativa
deberá mantener las mismas garantías de validación y aprobación humana.

## Matriz mínima de pruebas

Esta tabla conserva los casos originales. La matriz ejecutable y su estado están
en `10_MVP_STATUS_AND_TEST_PLAN.md`.

| Caso | Resultado esperado |
| --- | --- |
| Error simple de español | Propuesta local visible; aplicar solo tras confirmación. |
| Texto técnico protegido | El término permanece idéntico. |
| Número/unidad alterados por IA | Propuesta bloqueada, con motivo. |
| Usuario cancela | El objeto no cambia. |
| IA sin red o timeout | Se informa el error y quedan reglas locales. |
| Objeto cambia durante la GUI | Se detecta conflicto y no se sobrescribe. |
| Preferencia recordada | Se sugiere en una sesión futura, sin autoaplicar. |
