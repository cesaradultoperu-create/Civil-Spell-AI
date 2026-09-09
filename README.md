# CivilSpellAI

Complemento para Civil 3D 2024 que revisa texto técnico en español e inglés con
reglas locales conservadoras, el diccionario abierto `es_PE` de Hunspell y,
opcionalmente, OpenAI. Toda la revisión local funciona sin conexión. `AISPELL` analiza un
`DBText`, `MText`, `MLeader` textual o atributo seleccionable, muestra
el original, la propuesta, el diff y la validación técnica en una ventana modal.
También permite editar manualmente el resultado, pero solo escribe después de
que el validador técnico lo aprueba y el usuario pulsa **Aplicar**.

`AISPELLSETTINGS` administra glosario personal, proveedor simulado,
consentimiento para OpenAI y memoria local explícita. La primera ejecución de
`AISPELLALL` permite abrir esta configuración o continuar localmente. Al
habilitar OpenAI solo se envía el contenido de los textos; la clave propia puede
guardarse en la variable de entorno de usuario `OPENAI_API_KEY` desde un campo
enmascarado y nunca se copia al proyecto ni al instalador.

`AISPELLALL` revisa `DBText`, `MText`, `MLeader` textual, celdas simples
editables de `Table` y textos/atributos de bloques locales en modelo y
presentaciones. Muestra progreso y permite cancelar, buscar, filtrar y elegir
la alternativa de cada fila antes de aplicar todo como un solo lote atómico.

- Compilación: `dotnet build CivilSpellAI.slnx --configuration Debug --no-restore`
- Pruebas: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-SpellCoreTests.ps1`
- Validación del corte: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Mvp.ps1`
- Paquete piloto: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-PilotBundle.ps1`
- Verificación de entrega: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReleaseArtifact.ps1 -ArchivePath artifacts/distribution/CivilSpellAI-1.2.4.0.zip`
- Prueba del verificador: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReleaseArtifactVerifier.ps1`
- Fixture piloto: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/New-PilotFixture.ps1 -Force`
- Regresión real de UNDO: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-BatchUndoIntegration.ps1`
- Regresión real de textos extendidos: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ExtendedTextIntegration.ps1`
- Estado del MVP y matriz de regresión: [docs/product/10_MVP_STATUS_AND_TEST_PLAN.md](docs/product/10_MVP_STATUS_AND_TEST_PLAN.md)
- Segundo alcance: [docs/product/11_SCOPE_2_ROADMAP.md](docs/product/11_SCOPE_2_ROADMAP.md)
- Desarrollo y carga: [docs/DEVELOPMENT_AND_LOADING.md](docs/DEVELOPMENT_AND_LOADING.md)
- Memoria y glosarios: [docs/LEARNING_AND_GLOSSARIES.md](docs/LEARNING_AND_GLOSSARIES.md)
- Diseño vigente: [docs/product/README.md](docs/product/README.md)

Última verificación automatizada 2026-09-09: la fuente posterior al candidato
1.2.4.0 pasa 126/126 pruebas en Debug y Release. El ZIP reproducible 1.2.4.0
instalado antes del endurecimiento posterior de LanguageTool tiene SHA-256
`AA2B4B7A399C537867F5D8FDB59D165295521EC5703FC6B55F179CFB2A96C46F`.
El diccionario `es_PE` restaura tildes inequívocas y corrige
erratas de una letra con una única sugerencia; términos técnicos, códigos y
casos ambiguos permanecen protegidos. Una regresión dentro de AutoCAD Core Console corrigió en el
mismo lote un `MLeader`, dos celdas de una `Table`, un `DBText` de definición de
bloque y un `AttributeReference`; una sola orden `U` restauró los cinco textos.
El resultado de `AISPELLALL` advierte que `U` debe usarse inmediatamente, antes
de zoom, desplazamiento u otro comando, para que el historial de AutoCAD llegue
directamente al lote.
La versión publicada 1.2.0.0 permanece disponible mientras se valida el nuevo
asistente de primera configuración de 1.2.1.0.

Los archivos de Hunspell proceden de LibreOffice/RLA-ES bajo MPL 1.1. La
procedencia, huellas y licencias se conservan en
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). RAE/ASALE se utiliza solo
como referencia normativa y sus diccionarios no se copian ni redistribuyen.
