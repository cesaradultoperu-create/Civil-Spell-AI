# Sesión guardada: reconstrucción TOP + FRONT

El propietario terminó su jornada el 2026-10-05 y pidió guardar la información
para continuar mañana. Detener mejoras e instalación hasta retomar la sesión.

## Objetivo confirmado

`CREARFIGURA3D` debe comprender la geometría que se seleccione en TOP y FRONT
y crear el sólido a partir de ambas referencias. No limitar cualquier figura
a una caja rectangular ni considerar cerrado el trabajo por cambiar el sombreado.
El propietario creó una extrusión aproximada para comparar y señaló el layout
**A** de `1107-P-011-PRUEBAS.dwg` para verificar el detalle C y la sección 7.

## Datos y referencias conservados

- DWG abierto del propietario:
  `Test/Drawing3D/Pruebas-20261002/1107-P-011-PRUEBAS.dwg`.
  Tenía cambios sin guardar. Se leyó mediante COM sin escribir geometría ni
  cambiar la vista. No guardarlo ni cerrarlo automáticamente al retomar.
- Snapshot WBLOCK del estado completo de ModelSpace, sin modificar el original:
  `artifacts/validation/figure3d/reference-snapshot-84b0a83871964209bee0865ab7385b01.dwg`.
  SHA-256: `0CD34A0FF360299FD614BAA4F04B687CF549B945986E58B2B49054AA86B9E5D0`.
- Exportaciones de geometría, hatches y organización del layout A:
  `artifacts/validation/figure3d/reference-current-20261005.json`,
  `reference-hatches-20261005.json`, `layout-A-20261005.json`.
- La captura del plano muestra caja de concreto de 0.80, base 0.15 y solado
  separado de 0.05; los 0.85 del ejemplo incluyen ambos cuerpos. TOP define
  exterior 1.00 × 1.30 e interior 0.70 × 1.00. Saliente izquierdo: mampostería
  GRAVEL, no asumir que todo el saliente es concreto.
- La tubería se rotula Ø4″, pero sus contornos geométricos miden 0.10. La
  geometría modificada tiene pendiente descendente aproximada 40.4026 %;
  el texto 22 % es un mínimo, no la pendiente geométrica que debe imponerse.
  Los cortes de dibujo no determinan la longitud construida definitiva.
- FRONT es una sección; no tratarla como silueta exterior de toda la estructura.
  El espesor de tubería no queda definido por las dos referencias seleccionadas.

## Cambios en código, sin instalar

- `Geometry/ReferenceAssembly3D.cs`: componentes con contornos/arcos/vacíos,
  perfiles FRONT, cotas, tubo y datos persistentes.
- `Autodesk/ReferenceProfileReader.cs`: lectura nativa de polilíneas/círculos y
  límites de hatch, incluso límites no asociativos definidos por líneas/arcos.
- `Autodesk/ReferenceAssemblyDrawingService.cs`: sólidos por extrusión de
  perfiles, límites FRONT, huecos circulares inclinados y bloques persistentes.
  Tubo con espesor 0: superficie abierta; espesor positivo: sólido hueco.
- `Autodesk/DrainageReference3DAnalyzer.cs`: reconocimiento del detalle de
  drenaje relacionando TOP y FRONT; caja, solado, mampostería, tubería y, en el
  último cambio, cama de arena desde perfiles AR-SAND emparejados.
- `Autodesk/PairedProfile3DAnalyzer.cs`: perfiles cerrados arbitrarios de una
  pieza, con planta cóncava, arcos y huecos. No resuelve por sí solo cualquier
  ensamblaje desconocido sin correspondencias entre piezas.
- `CREARFIGURA3D` propone Referencias; Componente/`CREARCOMPONENTE3D` relaciona
  perfiles de la misma pieza; Caja mantiene el editor anterior. La selección
  excluye sólidos existentes. `EDITARFIGURA3D` conserva el nuevo conjunto.
- `UI/ReferenceAssembly3DWindow.cs`: vista previa basada en los mismos sólidos
  nativos que se escriben y pestañas con las selecciones TOP/FRONT.
- Versión candidata de fuentes 1.3.4.0; instalación 2024 anterior 1.3.3.0
  conservada durante la sesión abierta. No instalar una DLL Debug de pruebas.

## Validación final y diferencia entre candidatos

La **última fuente**, incluyendo cama de arena, compila Debug 2024 y pasa la
prueba nativa sobre copia, renderizado, perfiles L/circulares con vacíos y
FRONT inclinado, tubo hueco de prueba, edición, una U y reapertura:

`artifacts/validation/figure3d/reference-complete-20261005/`.

DLL Debug correspondiente:
`artifacts/validation/figure3d/reference-candidate/CivilSpellAI.dll`.

El Release 2024/2025/2026 y el bundle **anteriores al último añadido de cama de
arena** pasaron 151 pruebas, sin advertencias. Bundle verificado:
`artifacts/distribution/CivilSpellAI-1.3.4.0.zip`.
Su DLL 2024 tiene SHA-256
`53DDF94A9A026EBC4B5B69454B1F585106DBE5C13059B210B7F854686E01B677`.
**Ese ZIP aún debe regenerarse con el último cambio antes de entregarlo o
instalarlo.** No declarar que contiene la cama de arena.

El modo Caja anterior pasó también su regresión nativa completa:
`artifacts/validation/figure3d/reference-legacy-regression-20261005/`.

Se conservaron el DWG e imagen finales en
`Test/Drawing3D/Reconstruccion-TOP-FRONT-20261005/` para revisión. Esa carpeta
todavía no tiene DLL Release ni instalador final; prepararlos después de validar
el último cambio. Guía: `docs/testing/REFERENCE_ASSEMBLY_3D_RUNBOOK.md`.

## Retomar mañana

1. Comparar el nuevo conjunto con el layout A y la extrusión aproximada del
   propietario; confirmar la interpretación del canal, especialmente sus
   salientes y paredes/fondo. El perfil del canal seleccionado es horizontal:
   revisar la pendiente mínima rotulada y las cotas de entrada.
2. Confirmar diámetro, longitud y espesor de tubo que se quieran modelar. No
   introducir un espesor inventado ni convertir la tubería en cilindro macizo.
3. Probar interactivamente selección TOP/FRONT, giro/zoom, cancelación y edición.
   Las pruebas nativas no sustituyen la revisión visual de correspondencias.
4. Ejecutar Release multiversión y regenerar el bundle para el último cambio de
   cama de arena; completar el paquete de actualización en la carpeta de revisión.
   Instalar únicamente con Civil 3D cerrado y según el alcance retomado.
5. Continuar ampliando el reconocimiento general por componentes con ejemplos
   de otras geometrías; no prometer inferencia única de todo dibujo con dos vistas.

El repositorio ya tenía muchos cambios y archivos sin seguimiento antes de
esta sesión. No hacer reset, limpiar ni sobrescribir esos trabajos.
