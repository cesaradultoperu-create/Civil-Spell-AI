# Caja 3D editable desde TOP y FRONT

El flujo actual 1.3.4.0 se describe en [REFERENCE_ASSEMBLY_3D_RUNBOOK.md](REFERENCE_ASSEMBLY_3D_RUNBOOK.md).
`CREARFIGURA3D` propone **Referencias**; las instrucciones históricas siguientes
corresponden al modo explícito **Caja**. Usar el paquete
`Test/Drawing3D/Reconstruccion-TOP-FRONT-20261005` para la corrección geométrica.

## Corrección visual del 2026-10-05

La captura interactiva mostraba falsas ranuras verticales por el sombreado
de caras compartidas en la vista previa. La malla corregida expone solo
las caras exteriores, con normales independientes y contornos en aristas
reales. Los pasos mantienen muro encima y no cortan la coronación.

Paquete final: `Test/Drawing3D/Correccion-3D-Contornos-20261005`.
Guardar y cerrar Civil 3D antes de ejecutar su `ACTUALIZAR_DRAWING3D.cmd`.
La imagen `VISTA_PREVIA_IZQUIERDA.png` muestra la pareja izquierda de la
captura: altura 0.80, base 0.15 y paso izquierdo a cota 0.40.
La altura 0.95 corresponde a la pareja central del fixture.

Debug/Release 2024/2025/2026 pasa 151/151, sin advertencias. El ensayo
nativo 2024 compara volumen de malla y sólido, área de coronación completa,
cotas, edición, copia independiente, U y reapertura para ambas referencias.
Los conductos externos siguen pendientes de definición con el propietario.

## Uso en Civil 3D 2024

Actualización de vista preparada el 2026-10-05:
`Test/Drawing3D/Correccion-Vista-3D-20261005/ACTUALIZAR_DRAWING3D.cmd`.
Aplicarla cuando lo indique el propietario, guardando y cerrando Civil 3D.
Corrige el sombreado de muros/huecos de la vista previa. No añade componentes
al alcance geométrico descrito abajo.

1. Guardar los dibujos y cerrar Civil 3D. Ejecutar
   `Test/Drawing3D/Pruebas-3D-Editable-20261002/ACTUALIZAR_DRAWING3D.cmd`.
2. Abrir `1107-P-011-3D.dwg` de esa carpeta. La vista inicial encuadra dos
   modelos de prueba en perspectiva isométrica; se conservan las vistas 2D
   del plano fuente.
3. Ejecutar `EDITARSECCION` o `EDITARFIGURA3D` sobre uno de los bloques 3D.
   Revisar ancho, profundidad, altura, cuatro espesores de muro, espesor
   de base y los pasos laterales. La vista previa gira con arrastre y permite
   zoom con la rueda. Aceptar regenera sólido y cotas; una sola `U` deshace.
4. Para crear otro modelo: ejecutar `CREARFIGURA3D`, seleccionar **solo TOP**
   con una ventana y Enter, seleccionar **solo FRONT** y Enter. Revisar las
   medidas reconocidas y pulsar **Crear 3D**. Indicar la esquina inferior
   frontal donde se coloca el nuevo modelo.
5. También se puede partir de `EDITARSECCION` sobre TOP, cambiar medidas
   interiores y pulsar **Crear 3D con FRONT…**. El modelo incorpora el borrador
   editado de TOP; las referencias 2D se conservan.

## Medidas y alcance

La pareja izquierda señalada por el propietario en la captura tiene altura
**0.80**, paso izquierdo a **0.40** y base **0.15**. La pareja central de
los ejemplos tiene altura **0.95**. Las dos parejas están verificadas;
no deben intercambiarse sus medidas al comparar contra FRONT.

El marco exterior/interior de TOP define ancho, profundidad y cuatro
espesores laterales. Un perfil en U de FRONT, con coronación común, define
altura y base. El ancho exterior original de ambas selecciones debe coincidir.
Las cotas y textos no intervienen en la detección geométrica.

En el fixture del P-011 se reconocen ancho **1.00**, profundidad **1.30**,
altura **0.95**, base **0.15** y muros **0.15**. Son medidas geométricas, en
unidades del dibujo; se revisan y pueden cambiarse antes de crear el sólido.
El paso izquierdo tiene ancho 0.10 en TOP, alto 0.10 en FRONT y cota inferior
0.55 desde la cara inferior de la base.

FRONT define para el paso derecho alto 0.10 y cota inferior 0.45. TOP dibuja
esa cara de forma continua y no proporciona dos bordes para su ancho. El
propietario confirmó **0.10 como medida inicial editable**: el paso comienza
activo con ese ancho, y su centro inicial es la mitad de la profundidad.
Este ancho es un valor inicial acordado; no se infiere del relleno. En el
editor, ancho, alto, centro y cota inferior pueden cambiarse por separado,
y al aceptar se regeneran el hueco del sólido y sus cotas. Si FRONT tampoco
define una abertura única, el paso requiere completar las medidas.

El modelo es una **caja rectangular abierta con base y pasos rectangulares**,
con un sólido nativo `3DSOLID` y cotas `Dimension` en planos XY y XZ. No
reconstruye suelo, hatch de relleno, tapas, accesorios ni tuberías circulares
a partir de símbolos. Los pasos son huecos reales en los muros. La geometría
queda dentro de un bloque con parámetros persistentes. Una copia de AutoCAD
puede editarse sin modificar otra referencia; se mantienen posición y giro.
Restablecer la escala X/Y/Z del bloque a 1 antes de editar sus medidas.

El primer ejemplo, a X=50/Y=6, conserva las medidas reconocidas. El segundo,
a X=54/Y=6 y girado 90°, demuestra una altura 1.15 y un paso derecho de ancho
0.20, alto 0.12, centro 0.70 y cota inferior 0.50. Son modificaciones del
ejemplo para comprobar que el 0.10 inicial no queda fijo.

## Validación del 2026-10-02

- Release 2024/2025/2026 y Debug 2024 compilan sin advertencias. La suite
  general conserva 151/151 resultados correctos.
- `scripts/Test-Figure3D.ps1` usa copias y perfiles aislados en Core Console
  2024. Comprueba medidas de TOP/FRONT, rechazo de referencias invertidas,
  base inválida y paso fuera del muro, sólido nativo, volumen analítico,
  cotas reales (11 más tres por paso), edición del paso de 0.10 a 0.20 × 0.12,
  persistencia de ancho/alto/posición, independencia de copias, giro,
  rollback, una sola U y guardado/reapertura.
- La ventana real se renderizó con la vista 3D. La regresión 2D de medidas,
  hatch, cotas y deshacer vuelve a pasar con la nueva DLL.
- Evidencia: `artifacts/validation/figure3d/editable-010-20261002/prepare.log`,
  `verify.log` y `fixture.figure3d.png`; informe general
  `artifacts/validation/figure3d/editable-010-release-20261002.md`.
- La DLL 2024 con pasos iniciales editables 0.10 quedó instalada después de
  confirmar el cierre de Civil 3D; se verificaron instalación y respaldo
  por SHA-256. Comprobante:
  `artifacts/validation/figure3d/editable-010-installation.json`.
- Pendiente: recorrido interactivo de selección TOP/FRONT,
  giro/zoom y confirmación en Civil 3D. 2025/2026 están compilados; los ensayos
  nativos se ejecutaron en 2024.
