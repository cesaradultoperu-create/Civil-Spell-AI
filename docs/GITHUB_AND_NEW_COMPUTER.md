# Continuar el desarrollo desde otra computadora

El repositorio incluye CivilSpellAI, sus recursos y licencias, las herramientas
de `tools/`, los programas Lisp y las fuentes propias de los prototipos de
`Test/`. No hace falta copiar `bin`, `obj` ni los paquetes descargados.

## Requisitos

- Windows x64 y .NET SDK 10 (SDK utilizado para comprobar esta copia; admite la
  solución `.slnx` y la compilación de los proyectos para .NET 8).
- .NET Framework 4.8 Developer Pack / targeting pack para los proyectos 2024 y
  las pruebas del núcleo. Se puede instalar con Visual Studio Build Tools.
- Civil 3D 2024 para las referencias de compilación locales de la variante 2024.
  Las variantes 2025/2026 restauran sus referencias AutoCAD mediante NuGet.
- Acceso a NuGet durante la primera restauración. Para ejecutar el complemento
  se necesita la versión correspondiente de Civil 3D, aunque compile sin ella.

## Clonar, restaurar y comprobar

```powershell
git clone https://github.com/cesaradultoperu-create/Civil-Spell-AI.git
cd Civil-Spell-AI
dotnet restore CivilSpellAI.slnx --packages .packages
dotnet build CivilSpellAI.slnx -c Debug -p:Platform=x64 --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-SpellCoreTests.ps1 -Configuration Debug
dotnet build CivilSpellAI.slnx -c Release -p:Platform=x64 --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-SpellCoreTests.ps1 -Configuration Release
```

La restauración es necesaria antes de usar los comandos existentes con
`--no-restore`. La solución compila CivilSpellAI 2024, 2025 y 2026; los proyectos
independientes `CivilSpellAI.BordeTin3D.csproj` y
`CivilSpellAI.Overlap2023.csproj` se compilan por separado si se necesitan.

Por defecto las referencias locales se buscan en
`C:\Program Files\Autodesk\AutoCAD 2024`. Si la instalación está en otra unidad:

```powershell
dotnet build CivilSpellAI.slnx -c Debug -p:Platform=x64 --no-restore '-p:AutoCAD2024Path=D:\Autodesk\AutoCAD 2024'
```

`Civil3D2024Path` permite cambiar por separado la carpeta de las referencias
Civil 3D; por defecto es `$(AutoCAD2024Path)\C3D`. No se distribuyen las DLL de
Autodesk dentro de este repositorio.

La DLL 2024 queda en `bin/x64/Debug/`; las de 2025/2026 quedan en las subcarpetas
`A25/` y `A26/`. Para crear el paquete instalable después de compilar Release:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-PilotBundle.ps1
```

La carga con `NETLOAD` y las comprobaciones interactivas se explican en
[DEVELOPMENT_AND_LOADING.md](DEVELOPMENT_AND_LOADING.md). No reemplazar una DLL
que esté cargada en una sesión abierta de AutoCAD/Civil 3D.

## Herramientas y prototipos

- `tools/RelaveAuto`: aplicación RELAVEAUTO y motor de visión. Su script de
  compilación es `scripts/Build-RelaveAuto.ps1`; restaura OpenCvSharp desde NuGet.
- `tools/SotramiExcel`, `tools/AttributeLocator`, `tools/DetailMarker` y
  `tools/DrainageBorders`: fuentes, instaladores y pruebas propias. Consultar
  el README de cada herramienta; no forman parte de la solución principal.
- `Test/`: fuentes históricas, utilidades y prototipos, incluidos FotosEnCuadro,
  diagnósticos y scripts de regresión. Conservan sus rutas originales. Algunas
  son capturas históricas de entregas; para una herramienta consolidada usar
  primero su proyecto de `tools/`.

Los ensayos con planos, fotos, libros Excel y conjuntos DST requieren los datos
locales correspondientes. Las rutas `Q:`, `G:` y las rutas personales de scripts
de entrega deben adaptarse antes de ejecutarlos. Esas entradas y sus resultados
no son dependencias de la compilación principal ni se transfieren con Git.
Los scripts de instalación/publicación son acciones explícitas: compilar no
implica instalarlos ni ejecutarlos.

La dependencia Python del prototipo DeteccionFuga se restaura, cuando se necesite,
con `python -m pip install -r Test/DeteccionFuga-20261029/requirements.txt --target Test/DeteccionFuga-20261029/python-libs`.

## Qué se conserva solamente en esta computadora

Los DWG, sus respaldos, imágenes, datos de terreno, presentaciones y entregas
existentes permanecen en el disco. Git ignora también `.work/`, `outputs/`,
perfiles de AutoCAD, cachés, `node_modules`, dependencias Python y resultados
generados. Ignorar un archivo no lo elimina y tampoco lo respalda en GitHub.

`Test/.gitignore` contiene excepciones para las fuentes revisadas. Al agregar
otro prototipo, añadir allí excepciones específicas para sus carpetas, fuentes
y recursos necesarios; comprobar `git status` antes de hacer commit. No subir
de nuevo perfiles o paquetes completos para incorporar un solo programa.

Las claves API y la configuración personal se preparan de nuevo en cada equipo.
No se incluyen variables de entorno ni datos de `%LOCALAPPDATA%\CivilSpellAI`.
