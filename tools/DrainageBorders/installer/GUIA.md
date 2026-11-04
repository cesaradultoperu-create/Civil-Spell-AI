# DrenBordes 0.1.1 — instalación y uso

Para **Autodesk Civil 3D 2024, Windows de 64 bits**. El programa ya está compilado: para instalarlo y usarlo no necesitas Visual Studio, Python ni conexión a Internet.

## Instalar una vez

1. Cierra Civil 3D, especialmente si cargaste una versión anterior con NETLOAD.
2. En la carpeta de esta entrega, haz doble clic en **INSTALAR.cmd**. Si recibiste el ZIP, extráelo primero completo.
3. Espera el mensaje **INSTALACION CORRECTA**. Se copiará `DrenBordes.bundle` a la carpeta local de tu usuario:

   `%APPDATA%\Autodesk\ApplicationPlugins\DrenBordes.bundle`

4. Abre Civil 3D 2024. Para autorizar esta carpeta local, ejecuta **OPTIONS** → **Files / Archivos** → **Trusted Locations / Ubicaciones de confianza** → **Add / Añadir**. Agrega la carpeta de los DLL:

   `%APPDATA%\Autodesk\ApplicationPlugins\DrenBordes.bundle\Contents\Windows\2024`

   Para obtener la ruta completa con tu nombre de usuario, pega esa ruta en la barra del Explorador de Windows y copia la dirección resuelta. Si Civil 3D solicita autorizar la carga de `DrenBordes.2024.dll`, permite cargar este archivo.
5. Reinicia Civil 3D 2024 y escribe **DRENBORDES**. Debe aparecer la lista de capas de tu dibujo. Después de instalarlo queda disponible sin repetir NETLOAD.

La carpeta de G: es tu archivo del programa. El instalador crea la copia local que usa Civil 3D; no hace falta trabajar con los DLL directamente desde la unidad de red. El instalador no requiere permisos de administrador para su ubicación predeterminada. Si ya existe una versión distinta, guarda un respaldo antes de sustituirla.

## Aplicarlo a un plano similar

1. Abre el DWG o DXF y guarda una copia de trabajo.
2. Ejecuta **DRENBORDES**.
3. Escribe el número de la capa que contiene los ejes. Puede tener otro nombre: no necesita llamarse Drainage.
4. Indica la distancia a **cada lado**. Enter acepta **0.50**: la separación total entre bordes será **1.00**.
5. Acepta **Ejes** para procesar las curvas abiertas en **Z=0**. La opción **Todas** incluye curvas cerradas y otras elevaciones horizontales; cada elevación se procesa por separado.
6. Revisa las uniones y guarda el archivo con otro nombre.

| Propiedad | Resultado |
| --- | --- |
| Capa de bordes | `0_DCL_Bordes` |
| Color de capa | Blue, índice 5 |
| Color de objetos | ByLayer |
| Ancho inicial, final y global | **0.000**, incluidos los ejes de la capa elegida |
| Coordenadas de los ejes | Se conservan |
| Uniones | Encuentros en T, cruces y esquinas ajustados |
| Extremos libres | Abiertos |

**El offset 0.50 es una distancia geométrica. El ancho de polilínea 0.000 es su representación gráfica:** no debes poner 0.50 o 2.00 como ancho global.

Puedes repetir el comando: sustituye sus bordes anteriores sin duplicarlos. Una **U** deshace el lote, incluidos los cambios de ancho. **REDO** lo rehace si se ejecuta inmediatamente después de U. El comando no guarda el dibujo automáticamente.

Si `0_DCL_Bordes` contiene bordes manuales, el comando se detiene para conservarlos. Usa una copia de trabajo que no tenga esos bordes si quieres regenerarlos. La finalización de Stage 1 incluida en esta entrega ya se hizo en una copia separada.

## Comprobar que funciona

Abre una copia de uno de los planos de **PlanosTerminados**, ejecuta DRENBORDES y acepta sus valores. Los bordes deben quedar en la capa azul, con propiedades de ancho en **0.000**. Al repetir el comando, la cantidad de bordes debe mantenerse.

Resultados comprobados con el motor AutoCAD 2024:

| Plano | Ejes | Encuentros | Polilíneas de borde |
| --- | ---: | ---: | ---: |
| Stage 1 | 53 | 51 | 54 |
| Stage 2 | 72 | 71 | 74 |
| Stage 3 | 85 | 82 | 85 |

Las pruebas verifican la generación, los anchos cero, la capa azul, regeneración sin duplicados, U/REDO, conservación de coordenadas y reapertura DXF/DWG. Los registros están en **Validacion**. El instalador se comprueba en una carpeta temporal; esta entrega no instala el complemento en tu perfil hasta que ejecutes INSTALAR.cmd.

## Si no aparece el comando

- Confirma que abriste **Civil 3D 2024**. Esta DLL no corresponde a Civil 3D 2025/2026.
- Comprueba que existe `%APPDATA%\Autodesk\ApplicationPlugins\DrenBordes.bundle\PackageContents.xml` y que permanecen juntos sus seis DLL.
- Revisa la ubicación de confianza indicada arriba y reinicia Civil 3D. Mantén las configuraciones de seguridad de tu aplicación.
- Si **APPAUTOLOAD** está en 0, la carga automática está desactivada. Su valor predeterminado es 14; puedes restaurarlo si quieres habilitar ese comportamiento de carga de complementos.
- Para cargar esta sesión de forma manual, ejecuta **NETLOAD** y selecciona `DrenBordes.2024.dll` dentro de la carpeta local `Contents\Windows\2024`. Después ejecuta **DRENBORDES**.
- Si Windows bloqueó un ZIP descargado, abre sus Propiedades, marca **Desbloquear** si aparece y vuelve a extraerlo completo antes de instalar.

## Alcance y desinstalación

Admite líneas, arcos y polilíneas simples horizontales. No aplana ejes con cotas variables ni prolonga ejes desconectados. Los arcos se aproximan con error de cuerda máximo 0.001 unidades; los tres planos de ejemplo contienen tramos rectos. La precisión topológica es 0.000001 unidades. Las esquinas extremadamente agudas se limitan a diez veces el offset. La distancia siempre utiliza las unidades del dibujo.

Para desinstalar, cierra Civil 3D y mueve `DrenBordes.bundle` fuera de `%APPDATA%\Autodesk\ApplicationPlugins`. Los bordes que ya creaste permanecen como polilíneas editables en sus dibujos.

## Código fuente para Visual Studio

**CodigoFuente** contiene el proyecto independiente y las dependencias NuGet incluidas en **Paquetes**. Requiere Visual Studio con desarrollo de escritorio .NET, el paquete de destino .NET Framework 4.8 y las bibliotecas de AutoCAD 2024 instaladas. Abre `DrenBordes.csproj`, selecciona **Release** y compila.

También puedes usar `dotnet build DrenBordes.csproj -c Release` desde CodigoFuente. Si AutoCAD está en una ubicación distinta, indica `/p:Autodesk2024Dir="C:\ruta\AutoCAD 2024"`. El proyecto configura sus paquetes locales mediante NuGet.config. Para usar el programa basta con el instalador; no hace falta compilar.

## Referencias de Autodesk

- [Instalación y carga de complementos .bundle](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-Customization/files/GUID-5E50A846-C80B-4FFD-8DD3-C20B22098008.htm).
- [Plataformas y versiones del manifiesto](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-Customization/files/GUID-1591CA01-EF87-48CD-952B-772FE26037F1.htm).
- [APPAUTOLOAD](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-Core/files/GUID-33E75B9A-93AC-4FBF-B941-0F99A6AC3617.htm).
