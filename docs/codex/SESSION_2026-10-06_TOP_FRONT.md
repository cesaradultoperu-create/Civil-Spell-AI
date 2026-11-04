# Reconstrucción TOP + FRONT: sesión retomada el 06/10/2026

**Estado actualizado:** después de preparar la revisión, el propietario indicó
«civil3D cerrado». Se instaló el bundle 1.3.4.0 y se verificaron 49/49 archivos.
Los apartados que indican «no instalado» describen el momento de preparación;
el cierre de instalación consta al final de este registro.

El propietario pidió continuar las mejoras a partir del registro del 05/10.
Se conservaron todos los trabajos previos y se trabajó sobre copias.

## Resultado verificable

- Debug 2024 compila sin advertencias. Prueba nativa en
  `artifacts/validation/figure3d/reference-review-native-20261006/`: TOP/FRONT,
  cotas de mampostería, superficie de tubo sin espesor, tubo hueco con volumen
  anular comprobado, perfiles L/circulares con vacíos, FRONT inclinado, vista
  previa, edición, una U y persistencia al reabrir. El espesor sintético 0.004
  se usa solo en regresión; el DWG entregado conserva espesor cero.
- Release 2024/2025/2026 compila sin advertencias y pasa 151 pruebas. Incluye
  el último añadido de cama de arena que faltaba en el paquete del 05/10.
- Se añadieron `CREARFIGURA3D`, `CREARCOMPONENTE3D` y `EDITARFIGURA3D` al
  manifiesto para las tres versiones. Los verificadores comprueban los comandos;
  la regresión negativa rechaza un manifiesto sin CREARCOMPONENTE3D.
- Se generó una comparación geométrica PNG/PDF desde las mallas nativas,
  contornos TOP/FRONT y sólidos aproximados conservados en el WBLOCK.
  No es una captura del layout A ni sustituye su revisión visual.

## Comparación con la extrusión aproximada

La extrusión se desplaza Z=-0.05 únicamente en el gráfico para alinear su
coronación original 0.85 con la coronación de concreto 0.80. Los DWG no se
modifican para esa alineación. TOP y FRONT se expresan en coordenadas locales
de la caja. FRONT del gráfico es un corte real Y=0.65 de las mallas, no la
silueta proyectada de todas las paredes.

| Elemento | Reconstrucción a partir de referencias |
| --- | --- |
| Caja exterior | 1.00 × 1.30, altura 0.80 |
| Vacío interior | 0.70 × 1.00, sobre base de concreto de 0.15 |
| Solado | Componente separado entre -0.05 y 0.00 |
| Fondo de mampostería | Entre 0.40 y 0.50 |
| Laterales de mampostería | Entre 0.50 y 0.80 |
| Cama de arena | Planta TOP intersectada con perfil inclinado FRONT |
| Tubo | Diámetro geométrico 0.10, pendiente -40.402623 %, espesor sin definir |

En el corte central, el fondo de la extrusión aproximada queda entre -0.05
y 0.40 después de esa alineación, frente a la banda de mampostería 0.40–0.50.
La extrusión aproximada no tiene una base continua de concreto como la que
aparece en FRONT. Los salientes altos no separan los componentes de material.
Son diferencias de la referencia manual aproximada, no nuevos parámetros
constructivos que deban copiarse automáticamente.

El canal sigue horizontal, tal como su perfil geométrico. Falta revisar la
pendiente mínima rotulada y las cotas de entrada contra detalle C y sección 7
del layout A. Ø4″ nominal y diámetro geométrico 0.10 no se igualan silenciosamente;
longitudes entre cortes y espesor no constituyen dimensiones definitivas.
Se consultó al propietario si deseaba conservar la tubería dibujada o aportar
medidas definitivas; al preparar esta revisión no se había recibido respuesta.

## Artefactos preparados

`Test/Drawing3D/Reconstruccion-TOP-FRONT-20261006/` contiene:

- `Referencia-TOP-FRONT-3D.dwg`, con referencias y modelo en X=40.
- `VISTA_PREVIA.png`, `COMPARACION_TOP_FRONT.png` y PDF equivalente.
- `reference-model.json` y `reference-comparison.json`.
- ZIP Release con checksum y bundle completo descomprimido.
- `ACTUALIZAR_DRAWING3D.cmd`, `Update-ReferenceAssemblyBundle.ps1`, guía y checksums.

El actualizador exige cerrar AutoCAD/Civil 3D, verifica los hashes de las tres
DLL Release y los comandos del manifiesto, solicita elevación para la
instalación administrada y llama al gestor existente del bundle. Conserva el
respaldo completo y verifica la copia. Se revisó su sintaxis; no se ejecutó
la instalación. La instalación 2024 conserva la DLL 1.3.3.0 con hash
`871DA3C22B00EC26C66C27BEAAC3A028435DA5D457540734013DCC828DC51719`.

Release 2024: `AE0F5B2EF3821CFC6C72B6A3D7DCEEE34AF3DA28CCA8D92A8D893B74C376A7C7`.
Release 2025: `B141F6453DA3C69DAF812D63B5F249F12B31B501739469DB6882B147AFFBCAA6`.
Release 2026: `ED39829F3800463B7ED34D1A7A5B93D0E7CFDC12626546FF548144F1BA6A7DB9`.
ZIP final: `B4839380EB0E5795499528B02063DB67D04E0332EFE566D63331F9A21CA7E257`.

Evidencias adicionales: `reference-bundle-final-20261006.log`,
`reference-verifier-20261006.log` y `pilot-release-1.3.4.0.md`.
El verificador acepta el paquete y rechaza seis variantes inválidas.

## Estado del dibujo y límites de la revisión

La primera consulta COM fuera del sandbox encontró otro plano abierto, con
cambios sin guardar, y ningún `1107-P-011-PRUEBAS.dwg`. No se cambió ningún
documento. Posteriormente se pudo copiar el DWG de pruebas desde disco, pero
Get-FileHash encontró un bloqueo de lectura; no se afirma haber verificado
el hash de ese original. Core Console leyó solo la copia de disco en
`artifacts/validation/figure3d/layout-A-review-20261006/` sin guardarla; las dos
imágenes de A son logotipos, no la geometría del detalle. La comparación
geométrica se basa en el WBLOCK de la sesión del 05/10, cuyo hash comprobó
el script de prueba nativa. No se guardó ni cerró ningún DWG del propietario.

La primera prueba nativa dentro del sandbox falló antes de cargar el dibujo
porque AutoCAD no podía crear su perfil de registro aislado. La misma prueba
fuera del sandbox pasó en un directorio nuevo. El diagnóstico queda conservado
en `reference-review-20261006/`; no representa un fallo de geometría.

## Siguiente revisión

1. Revisar visualmente el canal con el propietario contra layout A, detalle C y
   sección 7. Si cambió las referencias desde ayer, capturar una nueva copia.
2. Resolver diámetro nominal, longitud construida y espesor cuando se definan;
   hasta entonces conservar geometría seleccionada y superficie de tubo.
3. Actualizar con Civil 3D cerrado y comprobar interactivamente selección,
   giro/zoom, cancelación, creación y edición. Los tests nativos no sustituyen
   esta comprobación ni demuestran funcionamiento interactivo en 2025/2026.
4. Ampliar reconocimiento por componentes con ejemplos adicionales, sin
   prometer una solución única para cualquier ensamblaje desde dos vistas.

## Instalación completada tras el aviso del propietario

Se comprobó la ausencia de procesos acad/accoreconsole, los checksums de la
carpeta de revisión, el ZIP Release y el gestor empaquetado. Se ejecutó el
actualizador preparado con elevación para la instalación administrada existente.
No se reconstruyó ni se sustituyó por una DLL de pruebas.

- Destino: `C:/Program Files/Autodesk/ApplicationPlugins/CivilSpellAI.bundle`.
- Versión de las DLL 2024/2025/2026: 1.3.4.0, con los hashes Release registrados.
- Verificación independiente posterior: 49/49 archivos idénticos al bundle fuente.
- Sin bundle duplicado en ApplicationPlugins del usuario.
- Respaldo completo:
  `C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261006-091753-423`.
- DLL 2024 respaldada: SHA-256
  `871DA3C22B00EC26C66C27BEAAC3A028435DA5D457540734013DCC828DC51719`,
  correspondiente a la instalación anterior 1.3.3.0.
- Comprobante: `artifacts/validation/figure3d/reference-installation-20261006.json`,
  copiado también a la carpeta de revisión como `INSTALACION_VERIFICADA.json`.

No se abrió Civil 3D ni se modificaron dibujos para instalar. La revisión
interactiva posterior a esta actualización sigue pendiente. El siguiente paso
es abrir la copia `Referencia-TOP-FRONT-3D.dwg`, ejecutar `EDITARFIGURA3D` sobre
el conjunto en X=40 y revisar después la creación seleccionando TOP y FRONT.
