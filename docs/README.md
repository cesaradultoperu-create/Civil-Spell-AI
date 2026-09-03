# CivilSpellAI

Documentación oficial del proyecto.

## Estado actual

CivilSpellAI alcanzó un MVP con reglas locales, revisión visual WPF, revisión
global y OpenAI opcional. El build pasa sin advertencias y las 105 pruebas
automatizadas están correctas. La matriz manual del Hito 1 está completa y los
flujos de OpenAI fueron confirmados en `AISPELL` y `AISPELLALL`. El Hito 2 está
cerrado, el Hito 3 de diagnóstico seguro está cerrado y el autoloader del Hito 4
ya pasó desde `Program Files`. La versión 1.1.5.0 conserva la sincronización de foco
y selección en listas, descripciones accesibles de validación y comprobaciones
XAML automáticas, y protege con una regresión real que una sola orden `U`
restaure todo el lote. Instalación, autoload, ciclo de vida y recorrido de teclado al
125 % quedaron aprobados; los Hitos 4–6 están cerrados.

La fotografía operativa y la matriz de regresión están en
[product/10_MVP_STATUS_AND_TEST_PLAN.md](product/10_MVP_STATUS_AND_TEST_PLAN.md).
El trabajo posterior se prioriza en
[product/11_SCOPE_2_ROADMAP.md](product/11_SCOPE_2_ROADMAP.md).

## Documentación vigente

- [DEVELOPMENT_AND_LOADING.md](DEVELOPMENT_AND_LOADING.md): compilación, pruebas,
  carga con `NETLOAD` y diagnóstico.
- [DIAGNOSTICS.md](DIAGNOSTICS.md): códigos, privacidad, triage, conservación,
  exportación y borrado del diagnóstico local seguro.
- [LEARNING_AND_GLOSSARIES.md](LEARNING_AND_GLOSSARIES.md): memoria explícita,
  revocación, privacidad y glosario organizacional de solo lectura.
- [PILOT_GOVERNANCE.md](PILOT_GOVERNANCE.md): responsabilidad, alcance,
  fixtures, firma e incidencias del piloto interno.
- [TECHNICAL_AUDIT.md](TECHNICAL_AUDIT.md): decisiones de consolidación, mejoras
  aplicadas y riesgos pendientes.
- [testing/MVP_REGRESSION_RUNBOOK.md](testing/MVP_REGRESSION_RUNBOOK.md): sesión
  manual reproducible, evidencia completa y fixtures anonimizados del Hito 1.
- [testing/PILOT_1_1_REGRESSION_RUNBOOK.md](testing/PILOT_1_1_REGRESSION_RUNBOOK.md):
  cierre interactivo de instalación, experiencia y memoria del corte 1.1.
- [testing/INCIDENT_TEMPLATE.md](testing/INCIDENT_TEMPLATE.md): formato único de
  defectos sin contenido sensible.
- `codex/`: contexto operativo resumido para futuras sesiones de desarrollo.
- `product/`: estado vigente, blueprint, requisitos, arquitectura, registros de
  fases y segundo alcance.
- [CHANGELOG.md](CHANGELOG.md): cambios entregados por versión.

Los documentos de diseño describen intención y los registros de fase conservan
evidencia histórica. Para conocer el estado actual prevalece el documento `10`.
