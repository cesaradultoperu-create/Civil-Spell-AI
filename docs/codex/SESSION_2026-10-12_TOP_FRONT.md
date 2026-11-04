# Reconocimiento TOP/FRONT — 12/10/2026

El propietario informó que la pareja izquierda se reconstruye y la central y
superior fallan con «No se pudo relacionar la tubería en TOP con su eje y bordes
en FRONT». Reiteró que CREARFIGURA3D debe usar ambas referencias del dibujo
seleccionado para crear su geometría, no reproducir una caja predeterminada.

## Diagnóstico reproducido

Se capturó por WBLOCK el ModelSpace actual del DWG abierto, sin guardar ni
modificar su geometría. El original está en modo solo lectura y tiene cambios
sin guardar; el modo solo lectura no causa el error de reconocimiento.
La captura y sus datos locales están en
`artifacts/validation/figure3d/pipe-reference-20261012/`.
No se enviaron geometría ni DWG a servicios externos.

El candidato anterior reprodujo ambos errores sobre esa copia:

- Central: TOP 51 / FRONT 48 entidades. El eje de FRONT empieza
  aproximadamente 0.00084665 unidades después de sus bordes. La condición de
  cobertura completa rechazaba una geometría compatible.
- Superior: TOP 59 / FRONT 34 entidades. El tubo cruza el muro posterior en
  TOP y se ve de frente como círculo en FRONT. El algoritmo únicamente buscaba
  tubos laterales en X, y además suponía un solo canal opuesto al tubo.

## Corrección 1.3.7.0

- Relación del eje con los bordes mediante su tramo común y colinealidad;
  conserva el diámetro comprobado con TOP y la pendiente de FRONT.
- Relación de los bordes verticales de TOP, incluso fragmentados, con un
  círculo exacto de FRONT. Compara diámetro y centro X; un círculo de diámetro
  incompatible provoca rechazo. No confunde un rectángulo con un círculo.
- Los canales GRAVEL se emparejan por cada lado independientemente del tubo.
  Cuando existen dos, fondo/laterales llevan nombres izquierdo y derecho.
- La caja no exige una tubería lateral: cuando no se reconoce un tubo, una
  planta con indicios de recorrido pendiente de emparejar produce un error
  específico en vez de completar ese recorrido con dimensiones fijas.
- El editor muestra X/Y y cotas inicial/final del tubo, conservando resaltado.

El círculo de un corte FRONT determina diámetro y cota de ese corte, pero
no determina por sí solo una pendiente longitudinal. Para el tubo perpendicular
se muestra una representación a cota constante y una nota explícita que exige
revisar cotas/pendiente si FRONT es solo una sección. No se inventa espesor:
0 continúa generando superficie; un espesor conocido genera sólido hueco.

El modo general por perfiles emparejados sigue disponible para otros dibujos;
dos vistas no siempre definen un único sólido. Se conserva su geometría y
vacíos cerrados, sin sustituir perfiles desconocidos por esta familia de caja.
Esta entrega corrige los casos reportados, no certifica reconocimiento universal.

## Validación

AutoCAD Core Console 2024 sobre copias locales:

- Tres parejas reales, sólidos nativos, desplazamiento independiente de vistas
  y escala 1.5, rechazo de diámetro incompatible, volumen anular del tubo hueco.
- Sección del muro posterior: área de concreto menos el círculo de la salida,
  comprobando que se realizó el vaciado real, no solo dibujar un tubo encima.
- WPF: resaltado de ambos lados, edición del recorrido Y, restauración de valores.
- Persistencia de modelos completos y sólidos al guardar/reabrir.
- Regresión A+B/C+D anterior: perfiles generales con arcos/huecos, cama de arena,
  ediciones, una U y reapertura; no se repite como pendiente.

Registros:
`artifacts/validation/figure3d/reported-hole-20261012/` y
`artifacts/validation/figure3d/reference-regression-20261012/`.
Pruebas reproducibles: `scripts/Test-ReportedReference3D.ps1` con el snapshot
actual y un candidato Debug; `scripts/Test-ReferenceAssembly3D.ps1 -IncludeCD`
con el snapshot antiguo. Los comandos nativos de diagnóstico son solo Debug.

Release 2024/2025/2026 sin advertencias, 151 pruebas correctas. ZIP aceptado por
el verificador y variantes corruptas rechazadas. Evidencia:
`artifacts/validation/pilot-release-1.3.7.0.md`,
`artifacts/validation/figure3d/reference-release-1.3.7-20261012.log`,
`artifacts/validation/figure3d/reference-verifier-1.3.7-20261012.log`.
2025/2026 compilados; la prueba nativa se ejecutó en 2024.

## Entrega y estado

`Test/Drawing3D/Reconocimiento-TOP-FRONT-20261012/`: guía, DWG con las tres
reconstrucciones y referencias originales, vistas previas, modelos JSON,
ZIP verificado y bundle completo con actualizador. Paquete 1.3.7.0 preparado,
**todavía no instalado**. La instalación activa permanece en 1.3.6.0.
No cerrar ni guardar los dibujos del propietario. El actualizador exige
cerrar Civil 3D y comprueba las DLL por hash antes de actualizar todo el bundle.
No instalar una DLL individual mientras la aplicación está abierta.

SHA-256 Release:

- 2024: `57C196F43914C6C44E7B52A98FB6D2F4CC36FB27E54377AFFA28B4C7DE051E6D`.
- 2025: `BDB338987C25E9EF96BF2E5096F83275211BC5081113A609EF97B763297D13CB`.
- 2026: `FD17F321D10E5CE2500DAF56DC874D48F3476FC9C3DE157826D5DB1CC81D0E74`.
- ZIP: `35CC6B1A6D4A0CACAE2EA8F992E5A98CCC06587CC6D776BD0B7B486DD55BCF4C`.

Pendientes: instalación cuando Civil 3D esté cerrado, revisión interactiva de
las dos parejas antes rechazadas y confirmación de pendiente del tubo
perpendicular si el círculo es solo una sección. Continúa pendiente el material
del elemento interior de C+D; no inventar barras ni perforaciones.

## Instalación completada tras «cerrado»

El propietario confirmó el cierre después de recibir las instrucciones.
La comprobación fuera del sandbox no encontró procesos acad/accoreconsole.
Se ejecutó el actualizador preparado del bundle completo y se verificó
independientemente la instalación 1.3.7.0: 49/49 archivos coinciden por SHA-256,
sin bundle duplicado por usuario. No se abrieron ni modificaron dibujos.

Respaldo 1.3.6.0 verificado contra el comprobante anterior, 49/49 archivos:
`C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261012-104155-909`.
Comprobante nuevo:
`artifacts/validation/figure3d/reference-installation-1.3.7-20261012.json`,
copiado a la entrega como `INSTALACION_VERIFICADA.json`.

Siguiente paso: revisión interactiva de las parejas central y superior con
CREARFIGURA3D. El propietario quiere probar sobre el mismo plano en solo
lectura: puede crear el modelo para revisar; debe usar GUARDARCOMO para guardar
el resultado con otro nombre. No volver a instalar antes de esta prueba.
