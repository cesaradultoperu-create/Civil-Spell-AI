# ACTUALIZACIÓN DEL LISTADO DE PLANOS SOTRAMI

Este paquete contiene una copia de revisión de la lámina de títulos y del conjunto de planos. Lee el Excel original de Q: y actualiza ambos con el comando **SOTRAMIEXCEL**. La actualización se ejecuta al usar el comando; no se produce en segundo plano al editar Excel.

Fuente: `Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\1107.001.00.16-17-LE-001-RB - Lista de entregables.xlsx`, hoja **LE**.

1. Abra `1107.001.00.14-200-00-P-001 - REVISION EXCEL.dwg`, presentación **A**.
2. En cada nueva sesión de Civil 3D, use **APPLOAD** y seleccione `CARGAR-SOTRAMI.lsp`. El cargador selecciona el DLL para 2023, 2024 o 2025 según la versión abierta. En la sesión 2024 usada para preparar esta entrega ya está cargado.
3. Guarde los cambios que realice en el Excel y ejecute **SOTRAMIEXCEL** en la copia del DWG.
4. Guarde el DWG con **Ctrl+S**. El conjunto copiado se guarda al actualizarse. Abra `SOTRAMI - REVISION EXCEL.dst` en el Administrador de conjuntos para consultar sus nombres actualizados.

Mantenga el DLL, el DWG, el DST y `layout-catalog.json` de este paquete en la misma carpeta. La herramienta está limitada a esta copia de revisión; no modifica los dibujos originales de Q: ni el Excel.

Compatibilidad del vínculo:

| Civil 3D | DLL que carga | Comprobación |
| --- | --- | --- |
| 2023 | `VinculoPlanosSOTRAMI.v5.2023-2024.dll` | Compilado con el SDK 2023 y .NET Framework 4.8; prueba dentro de Civil 3D 2023 pendiente |
| 2024 | `VinculoPlanosSOTRAMI.v5.2023-2024.dll` | Probado dentro de Civil 3D 2024 con los 50 planos |
| 2025 | `VinculoPlanosSOTRAMI.v5.2025.dll` | Compilado con el SDK 2025 y .NET 8; prueba dentro de Civil 3D 2025 pendiente |

También puede usar **NETLOAD** directamente con el DLL de su versión. Los componentes COM del conjunto de planos están integrados en el vínculo; no se distribuyen DLLs de la instalación de Autodesk. Los dos DLLs del paquete son alternativas: cargue solo el correspondiente a la versión abierta.

En otra computadora se requiere Windows, Civil 3D instalado y acceso al mismo Excel y a los DWG mediante la ruta **Q:**. Descomprima toda la carpeta antes de usarla. Si Civil 3D solicita confiar en el código, revise la carpeta de este paquete y agréguela a sus ubicaciones de confianza conforme a la configuración de su equipo; no es necesario desactivar SECURELOAD.

La compatibilidad del vínculo no convierte los objetos de Civil 3D de un DWG a versiones anteriores. La tabla es un objeto de AutoCAD; los objetos civiles de una versión posterior pueden aparecer como proxies en una anterior. Trabaje sobre esta copia de revisión y conserve los dibujos originales en la versión del proyecto.

Referencias de Autodesk: [compatibilidad de .NET y SDK](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Customization/files/GUID-A6C680F2-DE2E-418A-A182-E4884073338A.htm), [interoperabilidad de dibujos](https://help.autodesk.com/cloudhelp/2024/ESP/Civil3D-UserGuide/files/GUID-589C3DC0-EC98-40BC-B8CD-4623AA79D763.htm) y [compatibilidad de objetos civiles](https://help.autodesk.com/cloudhelp/2024/ENU/Civil3D-UserGuide/files/GUID-FA18FB87-C55B-4E1B-BA56-C4CD879C958B.htm).

Reglas solicitadas:

- Títulos y encabezados en **MAYÚSCULAS**, también en los nombres del conjunto.
- Numeración que reinicia en **1** al comenzar cada especialidad, según el orden del Excel.
- Revisión **A** cuando el Excel esté vacío, incluidos los ocho planos de Geología. Una revisión real informada en el Excel tiene prioridad. Los valores 1–8 de J53:J60 se interpretan como la numeración antigua de Geología, de acuerdo con la aclaración del usuario.
- Fechas de emisión vacías cuando no estén informadas. Para revisiones A, B y 0 se consultan las columnas K, L y M respectivamente.
- Filas ajustadas para que los títulos largos se lean completos.
- Los códigos y títulos proceden del Excel; no se renombran archivos DWG.

Alcance: **50 planos de la etapa inicial**, códigos `.14-200-`: 3 generales, 8 geológicos, 2 geotécnicos, 13 hidráulicos y 24 civiles. Los cuatro planos `.16-300-` pertenecen a la etapa final y no forman parte de esta lámina.

La referencia `1107.001.00.14-200-13-P-002.dwg` se encontró en **05_Drawing\Old**, con presentación A. El conjunto copiado apunta a ese archivo existente. Verifique su vigencia antes de emitir los planos; no se movió ni se modificó.

Si se agregan códigos al Excel, la herramienta reutiliza las presentaciones conocidas del catálogo. Para un DWG nuevo, importe su presentación en el conjunto copiado o abra ese dibujo en Civil 3D antes de ejecutar SOTRAMIEXCEL; el comando podrá identificar su presentación si es única o coincide con DRAWING_NO. Si falta el archivo o no puede identificar la presentación, informa que el conjunto está pendiente de completar. Al retirar códigos de la etapa inicial del Excel, retira sus entradas del conjunto copiado y de la tabla; no elimina los archivos DWG.

Las 50 referencias actuales ya se comprobaron y se corrigieron las que apuntaban a presentaciones inexistentes. La actualización habitual conserva esas referencias sin cargar los DWG externos ni sus superficies. Para el plano civil P-017 se comprobó que Layout1 tiene el código P-017 y el rótulo SISTEMA DE REVESTIMIENTO / DETALLE; la presentación duplicada no tiene ese rótulo.

La versión v4 evita la lectura adicional de dibujos que provocaba el aviso «External Surface File Not Found» por `1107.001.00.14-200-15-P-022_de15.mms`. Ese archivo no se encontró en el proyecto. La actualización del listado no reconstruye ni modifica la superficie TERRENO EXISTENTE. El aviso podría aparecer al abrir directamente el dibujo que necesita esa geometría.

Verificación realizada en Civil 3D 2024: 50 entradas en la tabla, 50 en el conjunto, títulos coincidentes con Excel después de convertir a mayúsculas, revisión A en las 50 entradas y numeración consecutiva en cada grupo. Se comprobó también la repetición del comando sin duplicados.
