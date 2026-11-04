# Resaltado del componente seleccionado, 06/10/2026

**Estado actual:** 1.3.6.0 instalado después del aviso «civil3d CERRADO».
49/49 archivos instalados coinciden por SHA-256; respaldo completo de 1.3.5.0
verificado. Los apartados siguientes conservan el estado al preparar el paquete.

## Solicitud y estado

El propietario mostró EDITARFIGURA3D del modelo C+D y señaló la cota inferior
0.55 de «Mampostería: laterales». Pidió iluminar lo seleccionado para identificar
en la vista 3D la pieza que va a modificar. Sus últimas capturas muestran la
reconstrucción C+D y su apertura posterior en el editor instalado 1.3.5.0.

1.3.6.0 está preparado y probado, todavía sin instalar. Al preparar el paquete
Civil 3D permanecía abierto (acad 9548); el manifiesto instalado sigue 1.3.5.0.
No se cerraron ni guardaron sus dibujos. Tras el aviso de cierre, actualizar
el bundle completo y verificar los archivos, conservando el respaldo administrado.

## Comportamiento

- Clic o foco de teclado en un campo resalta en verde solo su componente.
- Los encabezados son botones accesibles: identifican también piezas sin campos.
- La franja sobre la vista indica componente y campo seleccionado.
- Se conserva la selección al regenerar la vista después de cambiar un valor.
- «Quitar resaltado» recupera los materiales originales de la vista previa.
- El foco solo cambia materiales de WPF: no reconstruye mallas ni mueve la cámara.
- Abrir el editor conserva la precisión de los valores originales; los seis
  decimales de presentación ya no redondean valores que el usuario no editó.
- La selección no se serializa ni cambia los colores de entidades del DWG.

Se identifica el componente directo; el vaciado del paso de tubería en la caja
continúa actualizándose con su lógica anterior. No se modificó el reconocimiento
TOP/FRONT ni se asignó material a la pieza interior pendiente de aclaración.

## Validación y evidencia

Debug compila sin advertencias. AutoCAD Core Console sobre copia del snapshot:
`artifacts/validation/figure3d/reference-highlight-20261006/`.
Las pruebas ejercitan los eventos WPF de foco/clic y comprueban que laterales y
fondo, aunque comparten material, se seleccionan por separado. Selección sin
cambios de parámetros, mallas ni cámara; cambio de cota modifica los laterales
y conserva el verde; transición a tubo y pieza sin campos; recuperación de
materiales. También pasan geometría A+B/C+D, escala/desplazamiento de C+D,
perfiles generales, volumen de tubo hueco, edición/una U y reapertura.
La prueba verifica que el DWG fuente no cambió. La interacción humana con la
nueva versión permanece pendiente de instalación; los eventos se ejercitan
en una ventana WPF renderizada fuera de pantalla por Core Console.

Release 2024/2025/2026: cero advertencias, 151 pruebas correctas.
Verificador de artefacto acepta el ZIP y rechaza sus variantes corruptas.
Registros `reference-highlight-release-1.3.6-20261006.log` y
`reference-highlight-verifier-1.3.6-20261006.log` en artifacts/validation/figure3d.
Los comandos de diagnóstico permanecen dentro de DEBUG.

## Entrega preparada

`Test/Drawing3D/Resaltado-Componentes-20261006/` contiene DWG etiquetado A+B/C+D
copiado del paquete anterior, vistas previas con los laterales resaltados,
guía, ZIP y checksum, paquete descomprimido y actualizador completo con DLL
y versión fijadas por hash. No instalar DLL individuales.

SHA-256 Release 1.3.6.0:

- 2024: `755BD17D34417D2C1601DB8C132837ACA324D8DAB798C1830C2DFEB6EF9FDE26`.
- 2025: `998101371676330A56CE4E2A0D8DAB395A9FEDD1E1CCAC74F3952891EC59ECB3`.
- 2026: `0D0BB73D09C1720ABF3FD43FAEDFE20E59C570F065F15D9F47F55E06A25C3B82`.
- ZIP: `9CDAAB62DCBE3F98FBD7C511800A03597D5725507CB099527FCD6F2A6425A267`.

Cambios principales: UI/ReferenceAssembly3DWindow.cs,
Commands/ReferenceAssembly3DTestCommands.cs, scripts/Test-ReferenceAssembly3D.ps1,
versiones de ensamblado/manifiesto/paquete y scripts/Update-ReferenceAssemblyBundle.ps1.

## Instalación completada tras el aviso de cierre

El propietario confirmó «civil3d CERRADO». La comprobación fuera del sandbox
confirmó que acad.exe ya no estaba ejecutándose; la vista de procesos dentro
del sandbox aún mostraba un proceso antiguo. Se instaló el paquete completo
con el actualizador preparado, sin cerrar ni guardar dibujos del propietario.

Verificación independiente: manifiesto 1.3.6.0, 49/49 archivos coincidentes con
el paquete validado, ninguna copia adicional por usuario. Respaldo de 1.3.5.0
con sus 49 archivos verificados en
`C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261006-105627-712`.
Comprobante: `artifacts/validation/figure3d/reference-highlight-installation-1.3.6-20261006.json`,
copiado también al directorio de entrega como `INSTALACION_VERIFICADA.json`.

Siguiente paso interactivo: abrir Civil 3D, ejecutar EDITARFIGURA3D y seleccionar
«Mampostería: laterales / Cota inferior». Deben iluminarse los laterales en verde;
seleccionar otro campo o nombre cambia la pieza. Probar «Quitar resaltado».

## Confirmación interactiva del propietario

El propietario confirmó «sí lo hace perfectamente» y adjuntó una captura de
EDITARFIGURA3D con «Mampostería: fondo / Cota inferior» seleccionado y el fondo
del canal resaltado en verde. Queda confirmada la identificación interactiva
de componentes en la versión instalada. Después preguntó si TOP y FRONT
correctos permiten reconstruir cualquier dibujo: explicar los requisitos y
límites del modo general por componentes, sin prometer reconocimiento universal
ni confundirlo con una superficie de terreno.
