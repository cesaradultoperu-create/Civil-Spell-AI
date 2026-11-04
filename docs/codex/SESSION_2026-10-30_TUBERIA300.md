# CREARFIGURA3D — tubería 300 mm

El propietario pidió adaptar el comando al DWG
`C:/Users/Cesar Ugaz/Pictures/TRABAJO DE HOY 29/crear 3D DE TUBERIA 300mm.dwg`.
Confirmó que son TOP y FRONT, que sus medidas deberían coincidir y que **FRONT
manda**. Después confirmó el cierre de Civil 3D y autorizó la instalación.

Diagnóstico: seis INSERT en Model. TOP: 7092–7095 (T más tres tapas).
FRONT: 7351–7352 (codo reflejado X y tapa). La selección genérica sí leía los
bloques, pero el lector de material interpretaba los círculos del corrugado
y máscaras como perfiles. No había reconocimiento del eje de esta tubería.

Cambios: `CorrugatedPipe3DAnalyzer` extrae ejes de trazos transversales
repetidos, respeta bloques anidados/reflejados y relaciona T TOP con pendiente
y codo FRONT. Diámetro geométrico 0,300 m. Eje TOP original ~6,030 m;
proyección FRONT ~5,617 m; ajuste longitudinal TOP ~0,931497. Las referencias
originales permanecen intactas. Modelo con cinco sólidos editables: tubo con
codo, ramal T, tres tapas. Se recortan los interiores cruzados en la T.

Límites expresos en la vista previa y guía: envolvente lisa del corrugado,
tapas cilíndricas simplificadas, espesor inicial supuesto D/12 = 25 mm,
editable; cotas relativas a inserción. No es un modelo de fabricación.

Verificación nativa AutoCAD 2024: creación de cinco sólidos, diámetro,
pendiente, longitud FRONT, ramales, tubos huecos, paso interior libre,
edición, guardado y reapertura. También pasó la regresión TOP/FRONT anterior
incluyendo C+D, arcos/huecos, resaltado y deshacer.
Prueba reproducible: `scripts/Test-CorrugatedPipe3D.ps1`.
Runbook: `docs/testing/CORRUGATED_PIPE_3D_RUNBOOK.md`.

Entrega junto al original:
`Pictures/TRABAJO DE HOY 29/CREARFIGURA3D - Tuberia300 - 20261030`.
Incluye DWG 3D, PNG obtenido de las mallas nativas, guía y complemento 2024.

Instalación 2024 completada con administrador tras el cierre confirmado;
23 archivos verificados. DLL Release SHA-256:
`B2004004E8328525BB6F0B05047757F9E3BEF3811F6DCEAADB24BF9864208644`.
Respaldo:
`Test/Tuberia300-20261030/Respaldo-instalacion-20261030-154014`.
Comprobante: `Test/Tuberia300-20261030/Instalacion-verificada.json`.
Original SHA-256 conservado:
`EC07B4CC779EF55CBB6D956D11C913768A7FADF0FECB4A6E3573FD4A6A683B22`.

La revisión de la interfaz por Computer Use no se pudo realizar porque la
aprobación de acceso a la app expiró. Se revisó la imagen de las mallas reales
y se probaron el generador y la persistencia con el motor nativo.

## Corrección tras observación del propietario: corrugado real

El propietario señaló que el sólido liso no se parecía al tubo corrugado del
detalle. Se reemplazó esa simplificación: `CorrugatedPipeSolidBuilder` revuelve
el perfil redondeado original (bulges incluidos), con paso ~38,103 mm y
relieve radial ~21,187 mm. Crestas/valles son caras ACIS, unidas al cuerpo
hueco. Se conservan las zonas lisas de conexiones/codo. Las tapas pasan a
revolución del perfil superior del símbolo, con sus bandas y remate.

El parámetro de espesor se refiere ahora al valle (~3,813 mm supuesto),
manteniendo el diámetro interior inicial de 250 mm. `EDITARFIGURA3D` migra
los modelos lisos con referencias TOP disponibles antes de mostrar la vista
previa; el chequeo de concurrencia conserva la representación original.

Pruebas nativas: `PIPE300_REAL_CORRUGATION_PASS` mediante interferencia de
esferas en cresta/valle, creación/edición/huecos/T/reapertura, migración
idempotente con conservación del diámetro interior, y regresiones anteriores.
Se verificó también el relieve con la DLL Release instalada.

Tras un segundo cierre confirmado por el propietario, se instaló la corrección
2024, con 23 archivos verificados. DLL SHA-256 actual:
`A0F6CD7625F2C94FDBB71CFF9F478DBF7BD2B957451D8FBDE62D3A02EF5A5401`.
Respaldo: `Test/Tuberia300-20261030/Respaldo-instalacion-20261030-160716`.
Comprobante: `Test/Tuberia300-20261030/Instalacion-corrugado-verificada.json`.

Entrega vigente: `Pictures/TRABAJO DE HOY 29/CREARFIGURA3D - CORRUGADO REAL - 20261030`.
DWG: `crear 3D DE TUBERIA 300mm - CORRUGADO 3D.dwg`, SHA-256:
`3156CBF4F13BC5759F411BB88F600B8E8CAC30A9A592F521B51A402334E4E51E`.
Se guardó con VSCURRENT Conceptual. PNG revisado de las mallas nativas. El
original 2D conserva su hash inicial.

## Revisión interactiva: trazos azules de las referencias

El usuario mostró salientes en TOP/FRONT dentro del estilo Conceptual. Se
revisó la sesión real con Computer Use: desaparecen en 2D Wireframe y el
sólido corrugado se ve correcto en Shaded. Las seis referencias 2D generaban
trazos extra en la visualización 3D. Se probó sobre copias normalizar posición,
representación de líneas y opciones visuales; esas pruebas no se aplicaron al
DWG entregado. Fast shaded mode quedó restaurado en Off y LINESMOOTHING en 1.

Se conservó la geometría/posición del DWG y se asignaron las seis referencias
(7092–7095, 7351–7352) a `REFERENCIAS_TOP_FRONT_2D`. La capa queda congelada
para la revisión del sólido. Off no basta para ocultar hijos de bloques en
otras capas; se comprobó visualmente que Freeze sí elimina la superposición.
Se guardaron vistas `REFERENCIAS_TOP_FRONT` (Wireframe2D) y
`MODELO_TUBERIA_3D` (Shaded), con zoom al sólido. Las vistas no guardaron estado
de capas; para alternar, descongelar/encender o congelar la capa explícitamente.

Se guardó con Ctrl+S en el mismo DWG final y quedó abierto en Civil 3D con solo
el tubo, codo, T y tapas visibles, sin líneas azules. Se cerraron las dos copias
de diagnóstico, conservándolas dentro del repositorio. No se actualizó la DLL:
el cambio fue de organización/visualización del archivo.

Comprobación del archivo guardado mediante motor nativo:
`PIPE_CLEAN_SAVED_PASS refs=6 solids=5 position=(579168,8297887,0)`.
SHA-256 actual del DWG:
`55AA5A48A1CF8656B2797BC57032A6729087523F99850D468FDEF697A3B3B15D`.
Copia verificada: `Test/Tuberia300-20261030/Lineas-estilo/Guardado-vista-limpia.dwg`.
El .bak del guardado quedó fuera de la entrega en
`Test/Tuberia300-20261030/Lineas-estilo/Respaldo-antes-guardar-vista-limpia.bak`.
La carpeta visible conserva un único DWG y el original 2D mantiene su hash.
