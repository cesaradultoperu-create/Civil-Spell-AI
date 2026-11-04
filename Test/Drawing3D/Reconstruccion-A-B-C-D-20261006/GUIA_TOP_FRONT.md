# Reconstrucción de geometría seleccionada en TOP y FRONT

Candidato 1.3.5.0, revisión 2026-10-06 para A+B y C+D. Release 2024/2025/2026
sin advertencias y 151 pruebas correctas. Instalado después del aviso de cierre
del propietario: 49/49 archivos verificados y respaldo completo de 1.3.4.0.

Carpeta preparada: `Test/Drawing3D/Reconstruccion-A-B-C-D-20261006/`.
Contiene DWG/PNG, comparación PNG/PDF, ZIP verificado, bundle descomprimido y
`ACTUALIZAR_DRAWING3D.cmd`. El actualizador verifica las DLL Release y los
comandos 3D, actualiza el bundle completo y conserva el respaldo administrado.
Estado y límites: `docs/codex/SESSION_2026-10-06_C_D.md`.

## Uso

1. Esta computadora ya tiene la ampliación instalada. Para volver a actualizar,
   guardar los dibujos y cerrar Civil 3D antes de usar
   `Test/Drawing3D/Reconstruccion-A-B-C-D-20261006/ACTUALIZAR_DRAWING3D.cmd`.
2. Abrir `Ejemplos-A-B-y-C-D.dwg` de esa carpeta. Incluye ambos modelos,
   etiquetados A+B y C+D, y las referencias originales conservadas.
3. Ejecutar `CREARFIGURA3D`. Seleccionar TOP completo, Enter, aceptar
   **Referencias** y seleccionar su FRONT completo, Enter. Incluir hatches de
   material y contornos; seleccionar una sola pareja. Los sólidos existentes
   no se usan como contornos de referencia.
4. Revisar las pestañas TOP/FRONT y la geometría 3D giratoria. Confirmar
   componentes, elevaciones, diámetro y longitud antes de **Crear 3D**.
5. `EDITARFIGURA3D` permite revisar componentes y el recorrido de tubería.
   La posición y el giro del bloque se conservan. Cancelar no modifica el DWG.
   Usar `U` inmediatamente para deshacer una creación o edición.

Las letras del ejemplo indican las parejas: A o C es TOP; B o D es su FRONT.
Las letras no son entradas del comando ni condiciones del reconocimiento.
Para crear un modelo nuevo, comenzar con `CREARFIGURA3D` y seleccionar las
referencias 2D. `EDITARFIGURA3D` abre un modelo que ya existe.

## Segundo ejemplo C+D

1.3.5.0 reconoce la tubería horizontal a la izquierda y el canal a la derecha.
Lee altura de caja 0.95, base 0.15, solado 0.05, fondo del canal entre 0.45/0.55
y laterales entre 0.55/0.85 desde las referencias C y D conservadas. No copia
las cotas de A+B. En C+D los bordes del tubo en FRONT están repartidos entre
segmentos solapados: se unen solo cuando son colineales y tienen tramo común.

El componente interior relaciona la franja de TOP con el soporte AR-CONC de
FRONT y el perfil cerrado de la pieza superior. La pieza superior comparte
el borde superior dibujado del soporte; no se cierra un contorno abierto mediante
un segmento supuesto. Su material queda por confirmar. Si es rejilla o pantalla,
se muestra la envolvente de su perfil; barras, perforaciones y espesor constructivo
requieren el detalle correspondiente.

**Caja** conserva el editor anterior de cajas paramétricas. No es el modo
predeterminado de reconstrucción. **Componente**, o `CREARCOMPONENTE3D`, acepta
perfiles cerrados arbitrarios de una pieza en ambas vistas: la planta define
X/Y y el perfil de elevación limita X/Z. Admite plantas cóncavas, círculos,
arcos de polilínea y huecos interiores. No fuerza un contorno rectangular.
Las vistas deben estar a la misma escala y tener el mismo ancho X.
Sin marcas de alineación, se relacionan sus extremos izquierdos.

## Detalle C y sección 7 del plano de pruebas

El layout A se leyó para comprobar la organización y las anotaciones del plano.
La copia WBLOCK captura la geometría 2D/3D actual sin guardar ni alterar el DWG
abierto. La reconstrucción usa exclusivamente las referencias 2D seleccionadas.

- TOP aporta marco exterior 1.00 × 1.30, interior 0.70 × 1.00 y perfiles GRAVEL
  del canal y sus salientes. Los límites de hatch se leen tanto de polilíneas
  como de colecciones de segmentos y arcos, aunque no sean asociativos.
- FRONT aporta altura de concreto 0.80, piso de 0.15, solado separado de 0.05,
  fondo de mampostería entre 0.40/0.50 y laterales entre 0.50/0.80.
- La coincidencia entre dos bordes de tubo en TOP y tres líneas paralelas en
  FRONT determina diámetro geométrico 0.10, posición en profundidad y pendiente.
  En la geometría modificada por el propietario, la pendiente es aproximadamente
  40.4026 %, descendente. El 22 % rotulado es un mínimo, no se impone como
  pendiente fija. El Ø4″ nominal rotulado equivale a 0.1016 m: la discrepancia
  con la geometría se muestra para revisión, sin cambiarla silenciosamente.
- Las longitudes mostradas por líneas de corte no son longitudes constructivas
  definitivas. Se conserva el tramo común visible y se permite revisar los
  extremos y elevaciones del tubo.
- No hay espesor de tubería determinado por estas dos vistas. Con espesor 0
  se genera una superficie cilíndrica abierta, junto con sólidos estructurales.
  Un espesor positivo conocido genera un `3DSOLID` hueco. No se supone que un
  cilindro macizo representa la tubería.
- La cama de arena se obtiene de su perfil AR-SAND en FRONT y su anchura en
  TOP, limitada al tramo exterior común visible. El solado inferior identificado
  también se conserva. El terreno circundante no se convierte en un sólido.
- Para el canal se conservan las cotas geométricas de FRONT; revisar la pendiente
  mínima rotulada y las cotas de entrada, porque el perfil seleccionado es horizontal.

Los componentes se guardan dentro de un bloque con perfiles, parámetros y
referencias persistentes. La vista previa se obtiene de los mismos sólidos y
superficies nativos utilizados para escribir el dibujo.

## Alcance de dos referencias

Un corte de una estructura completa puede mostrar vacíos interiores y ocultar
paredes situadas delante o detrás del plano de corte. Por eso, fuera de la
familia de detalles de drenaje reconocida, **Componente** requiere perfiles
de material de una misma pieza. Para ensamblajes desconocidos, seleccionar las
piezas por separado; no cruzar indiscriminadamente todo el suelo, textos y
contornos de varias piezas. No se garantiza la reconstrucción única de cualquier
ensamblaje a partir de dos selecciones sin correspondencias explícitas.

Actualmente se admiten polilíneas cerradas y círculos, incluyendo arcos/bulges,
y hatches con bordes de líneas/arcos. Contornos abiertos, elipses y splines
requieren preparar un perfil cerrado compatible. Las islas anidadas dentro
de vacíos se seleccionan como componentes separados. No se cierran perfiles
abiertos mediante una línea supuesta.

## Evidencia

`scripts/Test-ReferenceAssembly3D.ps1` trabaja sobre copias y perfiles aislados
en AutoCAD Core Console 2024. Comprueba caja/base/saliente, geometría del
tubo, contornos con arcos y vacíos, planta L, FRONT inclinado, tubo hueco,
vista previa nativa, edición de parámetros, una U y persistencia al reabrir.
`scripts/Test-Figure3D.ps1` conserva la regresión de cajas anteriores.
La validación Release 2024/2025/2026 pasa 151 pruebas del núcleo.

Para validar ambas parejas con el candidato Debug, ejecutar el script con
`-IncludeCD` y un directorio nuevo. La regresión comprueba que C+D tenga sus
propias cotas, orientación y componente interior, y reconoce también sus
referencias desplazadas de forma independiente y escaladas por 1.5. Los comandos
de prueba permanecen excluidos de Release.

La revisión nativa del 06/10 también comprueba las cotas 0.40/0.50/0.80 del
canal, el tipo superficie del tubo sin espesor y el volumen anular exacto de
un tubo con espesor sintético, usado solo en la regresión. Exporta las mallas
nativas y las referencias seleccionadas en `reference-comparison.json`.
`scripts/Plot-ReferenceAssemblyComparison.py` genera la planta y el corte
central Y=0.65 del conjunto y de la extrusión aproximada. Esta última se
desplaza Z=-0.05 para comparar coronaciones; no se toma como diseño definitivo.

Pendiente de confirmación interactiva del propietario: giro/zoom, cancelación
y comparación visual del nuevo conjunto contra el layout A. Ningún resultado
de Core Console sustituye esa revisión de correspondencias de la sección.
