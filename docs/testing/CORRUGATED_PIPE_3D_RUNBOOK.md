# Tubería corrugada TOP/FRONT — 30/10/2026

Referencia: `crear 3D DE TUBERIA 300mm.dwg` en `Pictures/TRABAJO DE HOY 29`.
El propietario confirmó que la figura superior es TOP, la inferior es FRONT
y que **FRONT gobierna las medidas** cuando hay discrepancias.

El lector anterior seleccionaba los bloques correctamente, pero interpretaba
los círculos del corrugado y sus hatches de máscara como perfiles de material.
Estos símbolos no definen una sección cerrada que pueda extruirse para obtener
una tubería con pendiente y T.

El nuevo lector usa las líneas transversales repetidas de `DF_CORRUGADO` para
obtener los ejes. Recorre bloques anidados y aplica sus transformaciones,
incluido el reflejo X de la vista FRONT. El diámetro procede de la geometría
del símbolo corrugado, no de un número fijo ni del nombre del archivo.
Las vistas convencionales de materiales siguen con los analizadores anteriores.

## Uso

1. Ejecutar `CREARFIGURA3D`.
2. Seleccionar la T superior **y sus tres tapas**, Enter.
3. Aceptar `Referencias`.
4. Seleccionar el FRONT inferior completo **y su tapa**, Enter.
5. Revisar la vista previa, diámetro, paso del corrugado, espesor en el valle y radio del codo.
6. `Crear 3D` e indicar un punto de inserción.
7. `EDITARFIGURA3D` permite editar el bloque generado. Si se conserva TOP,
   también incorpora el corrugado a los modelos lisos de la entrega inicial.

El eje principal original TOP mide aproximadamente 6,030 m; el recorrido FRONT
define aproximadamente 5,617 m de proyección horizontal. El modelo ajusta la
coordenada longitudinal TOP por 0,931497 y conserva el ancho de los ramales.
No se modifica la referencia 2D original. Las elevaciones del modelo son
relativas al punto de inserción, no cotas de terreno deducidas del dibujo.

## Alcance geométrico

- Tubos sólidos huecos con T y codo redondeado; los interiores se sustraen
  mutuamente en la T para que sus paredes no obstruyan el paso.
- Diámetro detectado en crestas: 0,300 m. Se extrae el perfil redondeado del
  símbolo 2D y se repite con paso ~0,038103 m mediante revolución ACIS.
  Crestas y valles son superficies del sólido. Relieve radial ~0,021187 m.
- Espesor inicial **supuesto y editable** medido en el valle: ~0,003813 m.
  Se conserva el diámetro interior inicial de 0,250 m; confirmar el espesor
  real antes de fabricar. Codo y conexiones permanecen lisos según el detalle.
- Las tapas se generan por revolución de la envolvente superior del símbolo:
  bandas, cambios de diámetro y remate. No se deducen espesores internos de
  las tapas cuando el dibujo no los define.
- Reconocimiento limitado a esta convención de líneas corrugadas, dos ejes
  TOP formando una T y dos tramos FRONT monotónicos en X. Se rechazan otras
  topologías con un mensaje explicativo.

## Prueba nativa reproducible

Compilar `CivilSpellAI.csproj` en Debug (AutoCAD 2024 / .NET Framework 4.8),
y ejecutar `scripts/Test-CorrugatedPipe3D.ps1` con la referencia, DLL y un
directorio nuevo. El fixture usa los handles 7092–7095 y 7351–7352 de este DWG.
Se comprueban cinco sólidos no vacíos, pendiente FRONT, diámetro, longitud
gobernada por FRONT, dos salidas, hueco interior, unión T sin obstrucción,
edición, metadatos y persistencia al reabrir. Una esfera de prueba intersecta
la cresta y no el valle, verificando relieve físico. También se prueba la
actualización de modelos lisos conservando su diámetro interior mediante
`AISPELLTESTPIPE300UPGRADE`. Se verifica SHA-256 del original.

Regresión: `scripts/Test-ReferenceAssembly3D.ps1 -IncludeCD` verifica los
detalles previos, arcos, huecos, vista previa, resaltado, deshacer y reapertura.
