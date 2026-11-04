# Contexto para Codex

CivilSpellAI es un complemento x64 para Civil 3D 2024, 2025 y 2026. Usa .NET
Framework 4.8 en 2024 y .NET 8 en 2025/2026, con una DLL por versión. El
candidato revisa `DBText`, `MText`, `MLeader` textual, celdas simples de
`Table` y textos o atributos de bloques locales con aprobación humana mediante
tres comandos: `AISPELL`, `AISPELLALL` y `AISPELLSETTINGS`.

Arquitectura actual:

- `Commands`: entrada y orquestación interactiva.
- `Autodesk`: selección, snapshots, escaneo y escritura transaccional.
- `Application`: coordinación individual y por lote.
- `Domain`: contratos, propuestas, decisiones, diff y validación.
- `Infrastructure`: configuración, glosario, simulación y OpenAI.
- `Spell`: reglas deterministas independientes.
- `UI`: ventanas WPF y ViewModels.

Antes de cambiar código, leer `docs/product/10_MVP_STATUS_AND_TEST_PLAN.md`,
`docs/product/11_SCOPE_2_ROADMAP.md` y `docs/codex/DO_NOT_BREAK.md`. Los documentos
de fases conservan historia; no deben usarse aisladamente para inferir el estado
actual.
