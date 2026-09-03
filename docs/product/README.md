# Producto, estado y evolución

La carpeta separa el estado operativo actual, el diseño objetivo, los registros
históricos de implementación y el segundo alcance. Para decidir qué está
realmente implementado o pendiente prevalece el documento `10`.

| Documento | Contenido |
| --- | --- |
| [00_AI_PRODUCT_BLUEPRINT.md](00_AI_PRODUCT_BLUEPRINT.md) | Visión, experiencia de usuario y decisiones de producto. |
| [01_ARCHITECTURE_AND_DATA.md](01_ARCHITECTURE_AND_DATA.md) | Arquitectura, contratos, datos, seguridad e integración con Civil 3D. |
| [02_SCOPE_REQUIREMENTS.md](02_SCOPE_REQUIREMENTS.md) | Alcance de la primera versión con IA, requisitos y criterios de aceptación. |
| [03_IMPLEMENTATION_ROADMAP.md](03_IMPLEMENTATION_ROADMAP.md) | Plan incremental de implementación, pruebas y salida piloto. |
| [04_PHASE_0_COMPLETION.md](04_PHASE_0_COMPLETION.md) | Resultado verificable de la base reproducible. |
| [05_PHASE_1_COMPLETION.md](05_PHASE_1_COMPLETION.md) | Resultado verificable del dominio, diff y validación técnica. |
| [06_PHASE_2_IMPLEMENTATION.md](06_PHASE_2_IMPLEMENTATION.md) | Implementación de GUI y escritura segura; flujo principal validado y matriz complementaria pendiente. |
| [07_PHASE_3_IMPLEMENTATION.md](07_PHASE_3_IMPLEMENTATION.md) | Configuración, glosario personal y proveedor simulado. |
| [08_BATCH_REVIEW_IMPLEMENTATION.md](08_BATCH_REVIEW_IMPLEMENTATION.md) | Ampliación aprobada para revisión global del dibujo. |
| [09_OPENAI_TEXT_ONLY_IMPLEMENTATION.md](09_OPENAI_TEXT_ONLY_IMPLEMENTATION.md) | Integración de OpenAI con consentimiento, privacidad y validación local. |
| [10_MVP_STATUS_AND_TEST_PLAN.md](10_MVP_STATUS_AND_TEST_PLAN.md) | Fuente de verdad del MVP, evidencia, riesgos y matriz de regresión. |
| [11_SCOPE_2_ROADMAP.md](11_SCOPE_2_ROADMAP.md) | Segundo alcance priorizado por estabilización y distribución. |

## Estado resumido

- Fases 0, 1 y 3: completadas.
- Fase 2: implementada y validada en su flujo principal; conserva pruebas
  manuales específicas de `MText`, acciones sin escritura, conflicto y `UNDO`.
- Revisión global: implementada y validada en su flujo local principal.
- Fase 4: implementada; los flujos felices de OpenAI real están confirmados en
  `AISPELL` y `AISPELLALL`. Siguen pendientes fallos reales y `UNDO` remoto.
- Fases 5 y 6: reorganizadas dentro del segundo alcance.

Los archivos `00` a `03` describen intención y requisitos originales. Los
archivos `04` a `09` conservan evidencia por fase y no sustituyen la fotografía
vigente del MVP.
