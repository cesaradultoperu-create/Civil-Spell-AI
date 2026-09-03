# CivilSpellAI

Complemento para Civil 3D 2024 que revisa texto técnico en español e inglés con
reglas locales conservadoras y, opcionalmente, OpenAI. `AISPELL` analiza un `DBText` o `MText`, muestra
el original, la propuesta, el diff y la validación técnica en una ventana modal.
También permite editar manualmente el resultado, pero solo escribe después de
que el validador técnico lo aprueba y el usuario pulsa **Aplicar**.

`AISPELLSETTINGS` administra glosario personal, proveedor simulado,
consentimiento para OpenAI y memoria local explícita. Al habilitar OpenAI solo
se envía el contenido de los textos; la clave se lee de `OPENAI_API_KEY` y no
se guarda en el proyecto.

`AISPELLALL` revisa todos los `DBText` y `MText` de los espacios de modelo y
presentación, muestra progreso y permite cancelar, buscar, filtrar y elegir la
alternativa de cada fila antes de aplicar todo como un solo lote atómico.

- Compilación: `dotnet build CivilSpellAI.slnx --configuration Debug --no-restore`
- Pruebas: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-SpellCoreTests.ps1`
- Validación del corte: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Mvp.ps1`
- Paquete piloto: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-PilotBundle.ps1`
- Verificación de entrega: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReleaseArtifact.ps1 -ArchivePath artifacts/distribution/CivilSpellAI-1.1.5.0.zip`
- Prueba del verificador: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReleaseArtifactVerifier.ps1`
- Fixture piloto: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/New-PilotFixture.ps1 -Force`
- Regresión real de UNDO: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-BatchUndoIntegration.ps1`
- Estado del MVP y matriz de regresión: [docs/product/10_MVP_STATUS_AND_TEST_PLAN.md](docs/product/10_MVP_STATUS_AND_TEST_PLAN.md)
- Segundo alcance: [docs/product/11_SCOPE_2_ROADMAP.md](docs/product/11_SCOPE_2_ROADMAP.md)
- Desarrollo y carga: [docs/DEVELOPMENT_AND_LOADING.md](docs/DEVELOPMENT_AND_LOADING.md)
- Memoria y glosarios: [docs/LEARNING_AND_GLOSSARIES.md](docs/LEARNING_AND_GLOSSARIES.md)
- Diseño vigente: [docs/product/README.md](docs/product/README.md)

Última verificación automatizada 2026-09-03: el candidato 1.1.5.0 conserva
105/105 pruebas y añade una regresión real en AutoCAD Core Console: una sola
orden `U` restaura las dos entidades de un lote. La 1.1.4.0 ya había completado
instalación, ciclo de vida y recorrido de teclado al 125 % en el DWG desechable.
La actualización administrada a 1.1.5.0 coincide por hash con el candidato.
La regresión interactiva confirmó además tres correcciones y su reversión
completa mediante una sola orden `U`.
