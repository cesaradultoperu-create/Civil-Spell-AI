# Instalación y operación del piloto 1.1.4.0

## Fixture desechable de regresión

El paquete incluye `New-PilotFixture.ps1`. Con Civil 3D abierto o cerrado,
puede generar un DWG aislado mediante AutoCAD Core Console sin leer ni
modificar dibujos del usuario:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\New-PilotFixture.ps1
```

El script crea `artifacts\testing\CivilSpellAI-Pilot-Fixture-1.1.4.dwg` bajo
el directorio desde el que se distribuyó el proyecto, valida cuatro entidades
`TEXT`/`MTEXT` y muestra la huella SHA-256 de esa ejecución. Use únicamente ese
dibujo reproducible para la regresión; nunca un DWG de trabajo.

Este paquete está dirigido exclusivamente a Autodesk Civil 3D 2024 de 64 bits
(`R24.3`) sobre .NET Framework 4.8. No incluye DLL de Autodesk, claves, dibujos,
configuración del usuario ni diagnósticos.

## Comprobación previa

Cerrar Civil 3D y, desde la carpeta extraída, ejecutar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test-CivilSpellAIEnvironment.ps1
```

El resultado comprueba arquitectura, Civil 3D 2024, .NET 4.8, permiso de
escritura, esquema de configuración, presencia opcional de `OPENAI_API_KEY` y
firma del ensamblado candidato. Nunca muestra el valor de la credencial. Un
`WARN` de confianza no impide instalar, pero exige decidir conscientemente cómo
autorizar el complemento antes de abrir un DWG de trabajo. También advierte si
existe otra copia del bundle en un alcance distinto.

La entrega incluye `CivilSpellAI-1.1.4.0.zip.sha256` junto al ZIP. Antes de
extraerlo, ambos archivos deben permanecer en la misma carpeta y puede
comprobarse la huella con:

```powershell
$expected = (Get-Content .\CivilSpellAI-1.1.4.0.zip.sha256).Split()[0]
$actual = (Get-FileHash .\CivilSpellAI-1.1.4.0.zip -Algorithm SHA256).Hash
$expected -eq $actual
```

El resultado debe ser `True`.

## Instalar y actualizar

Para este piloto sin firma se recomienda abrir PowerShell como administrador e
instalar en la ubicación implícitamente confiable de Autodesk:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Manage-CivilSpellAI.ps1 -Action Install -Scope AllUsers
```

Para sustituir una versión existente:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Manage-CivilSpellAI.ps1 -Action Update -Scope AllUsers
```

La versión anterior se mueve primero a
`%LOCALAPPDATA%\CivilSpellAI\deployment-backups\AllUsers`. Los ajustes
`settings.v3.json`, el glosario, la memoria `learning.v1.json` y los
diagnósticos no se sobrescriben.

Al migrar desde `CurrentUser`, instalar y verificar primero `AllUsers`; después,
con Civil 3D cerrado, ejecutar `Uninstall -Scope CurrentUser`. El instalador
impide crear posteriormente una copia por usuario que pueda ocultar la versión
administrada.

Al abrir Civil 3D, ejecutar `AISPELL`, `AISPELLALL` o `AISPELLSETTINGS`. El
autoloader registra los comandos y carga la DLL al invocar uno de ellos. Tras
migrar el mismo paquete desde `CurrentUser` a `AllUsers`, Civil 3D puede conservar
en caché la ruta anterior y responder `Unknown command` en el primer inicio. En
ese caso, ejecutar `APPAUTOLOADER`, elegir **Reload** una sola vez y volver a
probar `AISPELLSETTINGS`; no es necesario usar `NETLOAD`. La regresión 1.1.3.0
confirmó este flujo desde `Program Files` sin advertencia de firma.

Si el problema persiste, comprobar que `APPAUTOLOAD` tenga el valor normal `14`.
Un paquete sin firma bajo el perfil del usuario también puede quedar registrado
pero ser rechazado por `SECURELOAD`; verificar primero su SHA-256 y preferir la
instalación administrada bajo `Program Files` o una entrega firmada.

`NETLOAD` se conserva como diagnóstico: cargar
`CivilSpellAI.bundle\Contents\Windows\CivilSpellAI.dll`.

## Rollback y desinstalación

Para restaurar la copia anterior más reciente:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Manage-CivilSpellAI.ps1 -Action Rollback -Scope AllUsers
```

Para quitar el bundle conservando una copia recuperable y la configuración:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Manage-CivilSpellAI.ps1 -Action Uninstall -Scope AllUsers
```

Todas estas operaciones requieren Civil 3D cerrado. `-Scope AllUsers` usa
`%PROGRAMFILES%\Autodesk\ApplicationPlugins` y requiere elevación. El alcance
`CurrentUser` permanece disponible para un paquete firmado o una carpeta
autorizada por la política CAD de la organización.

## Verificación posterior

1. Confirmar que los tres comandos aparecen y que `AISPELLSETTINGS` abre.
2. Ejecutar `AISPELL` sobre un dibujo desechable y cancelar; el texto debe
   permanecer idéntico.
3. Comprobar en Propiedades de `CivilSpellAI.dll` que la versión sea 1.1.4.0.
4. Conservar juntos el ZIP y su archivo `.zip.sha256` para poder identificar o
   restaurar el corte.

Política de este piloto interno: paquete sin firma, instalación administrada
solo en equipos controlados y verificación obligatoria del SHA-256. No debe
ampliarse a equipos que exijan firma. Una distribución organizacional posterior
requiere certificado de editor confiable y una nueva aprobación de seguridad.

La regresión completa del corte está en
`REGRESION_PILOTO_1_1.md`, incluido junto a este LEAME.
