# Contexto para Codex

CivilSpellAI es un complemento .NET Framework 4.8/x64 para Civil 3D 2024. El
MVP revisa `DBText` y `MText` con aprobación humana mediante tres comandos:
`AISPELL`, `AISPELLALL` y `AISPELLSETTINGS`.

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
