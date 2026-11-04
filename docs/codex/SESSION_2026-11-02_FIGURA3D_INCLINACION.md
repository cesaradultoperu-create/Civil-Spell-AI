# CREARFIGURA3D: inclinación directa de la tubería

El usuario mostró una tubería azul de un detalle de drenaje y pidió ingresar
su inclinación dentro de `CREARFIGURA3D`, para evitar modificarla manualmente.
La segunda captura confirma un descenso de 19° hacia la caja.

`ReferenceAssembly3DWindow` incorpora **Inclinación (°)** bajo **Tubería**, tanto
en creación como en `EDITARFIGURA3D`, con selector para conservar la cota
inicial o final. El signo sigue el sentido inicial→final: positivo sube,
negativo baja. Se conservan X/Y; la otra cota es la cota fija ± longitud
horizontal por tangente del ángulo. Cambian la longitud 3D y la abertura del
muro, reconstruida por el servicio nativo existente. Los componentes restantes
mantienen sus parámetros. Los ángulos/cotas se sincronizan al salir del campo
y al aplicar, sin redondear los parámetros originales que no se editaron.

`ReferencePipe3D` contiene el cálculo y la validación. Se rechazan valores no
finitos, ángulos fuera de (-90°, 90°) y recorridos sin proyección horizontal.
No cambia el formato de persistencia: se guardan los extremos existentes.
El alcance es el tubo recto de los detalles de drenaje. Los modelos corrugados
de codos/T conservan los controles existentes de cotas por punto.

Compilación Debug/Release 2024 x64 sin advertencias con MSBuild 18 y
`/p:ResolveNuGetPackages=false` (paquetes locales disponibles).
La compilación sin esa propiedad encuentra un conflicto anterior de NuGet con
`RuntimeIdentifier=win-x64`; no se modificó la configuración del repositorio.

Verificación nativa con `scripts/Test-ReferenceAssembly3D.ps1 -IncludeCD`:
`artifacts/validation/figure-inclination-20261102/native3`.
Pasaron TOP/FRONT, C+D, resaltado, ángulos 22°/-19,5°/0°, ambos extremos
fijos, coma decimal, sincronización, entradas inválidas, tubo hueco, guardado
y reapertura del modelo inclinado, y una U para deshacer el cambio de ángulo.
Las pruebas operan sobre copias y conservan los originales por hash.
También pasó `scripts/Test-ReportedReference3D.ps1` con las tres parejas,
incluida la tubería perpendicular a FRONT, y su reapertura:
`artifacts/validation/figure-inclination-20261102/three-pairs-verified`.
El guion limpia ejemplos 3D previos solo en la copia desechable, evitando
coincidencias duplicadas en la verificación; no deja una línea vacía tras
NETLOAD, porque AutoCAD la interpreta como repetir el comando.
La primera ejecución restringida no pudo crear el perfil de registro aislado;
la ejecución con aprobación automática completó las pruebas nativas.

Entrega preparada en `Test/Figura3D-Inclinacion-20261102`: DLL Release 2024,
`ACTUALIZAR.cmd`, actualizador PowerShell, guía, manifiesto SHA-256 y captura
de la vista previa nativa con el campo de ángulo visible. El actualizador
verifica el paquete, requiere Civil 3D cerrado, conserva/verifica un respaldo
y comprueba la DLL instalada. `-VerifyOnly` pasó sin modificar la instalación.
SHA-256: `8C7D4BAFC46A65253EE712EDED9D65383611A89396204C4584135FCED3B74BA5`.

La instalación todavía no se aplicó. Una consulta restringida dejó de mostrar
el proceso, pero el instalador con acceso completo detectó Civil 3D abierto y
se detuvo antes de modificar archivos. La comprobación posterior confirmó
`acad.exe` PID 40776 con el dibujo `1107.001.00.14-200-14-P-012.dwg` abierto.
Se pidió al usuario guardar y cerrar la aplicación. No se cerró su sesión ni
se alteraron sus dibujos o la DLL instalada.

## Instalación completada

El usuario confirmó que guardó y cerró Civil 3D. Se ejecutó `Actualizar.ps1`
con elevación y terminó correctamente. Se verificaron de nuevo por SHA-256
la DLL instalada y el respaldo de la anterior: ambos PASS.
Destino: `C:/Program Files/Autodesk/ApplicationPlugins/CivilSpellAI.bundle/Contents/Windows/2024/CivilSpellAI.dll`.
SHA-256 instalado: `8C7D4BAFC46A65253EE712EDED9D65383611A89396204C4584135FCED3B74BA5`.
Respaldo y comprobante:
`Test/Figura3D-Inclinacion-20261102/Respaldo-20261102-104944-6f6fd147/`.
La actualización estará disponible al volver a abrir Civil 3D.

## Corrección posterior: bloqueo antes de abrir la ventana

El usuario reportó «no deja» con una captura del error «TOP contiene un
recorrido de tubería sin una correspondencia única en FRONT». El fallo ocurre
antes de abrir la ventana: la primera actualización solo añadía el control de
ángulo y no cubría el reconocimiento del bloque modificado del detalle actual.

Se usó la habilidad Computer Use para exportar por WBLOCK una copia local del
DWG activo, sin guardar cambios en el original. Se excluyó la información de
Map de esa copia geométrica. Se dejó al usuario en el mismo layout A.
Captura: `Test/Figura3D-Reconocimiento-20261102/Referencia-actual.dwg`.
SHA-256: `662E7403A2C33962ADD45C079A0FB3C74B0C460F1B8FB1D6E57CFBC1681919C6`.
La prueba reprodujo exactamente los conteos de la captura: TOP 54 / FRONT 42.
El bloque `tuberia` conserva Rotation=0, pero su definición contiene bordes
inclinados; sus extremos X difieren y el eje de delineación está desplazado
respecto al centro de los bordes. Su diámetro es 0,100 y su inclinación leída
es -18,5°, editable en la ventana. El rótulo del plano no se usa como entrada.

`DrainageReference3DAnalyzer.ReadPipe` calcula el diámetro por la separación
perpendicular de los bordes y obtiene el eje promedio. Reconoce extremos
normales al tubo, incluidos los de contornos redondeados/girados, y conserva
la intersección longitudinal disponible en TOP/FRONT. Puede prescindir de la
línea central cuando un extremo inclinado normal identifica el tubo. Para
contornos sin ese extremo conserva la correspondencia de extremos y eje:
evita que otras franjas del canal creen candidatos falsos. Se mantiene la
exigencia de diámetro compatible con TOP y de una única correspondencia.

Pruebas nativas finales, todas PASS:

- `scripts/Test-InclinedDrainageDetail.ps1`: copia actual, apertura de vista
  previa/edición del ángulo, bloque con un giro adicional, eje retirado,
  desplazamiento/escala, sólido hueco, persistencia y reapertura.
  Evidencia: `artifacts/validation/figure-recognition-20261102/current-complete`.
- `scripts/Test-ReportedReference3D.ps1`: tres parejas previas, sección circular,
  diámetro incompatible rechazado, controles de inclinación y reapertura.
  Evidencia: `artifacts/validation/figure-recognition-20261102/three-pairs-complete`.
- `scripts/Test-ReferenceAssembly3D.ps1 -IncludeCD`: regresión A+B/C+D,
  geometría/perfiles/vacíos, controles de inclinación, U y reapertura.
  Evidencia: `artifacts/validation/figure-recognition-20261102/reference-complete`.

Las primeras pruebas de la nueva lectura detectaron candidatos de las franjas
del canal en los ejemplos anteriores; se ajustaron los criterios anteriores
antes de preparar la entrega. Debug y Release 2024 compilan sin advertencias.
Entrega final: `Test/Figura3D-Reconocimiento-20261102/`, con DLL, actualizador,
guía, manifiesto y vista previa obtenida del modelo nativo del detalle actual.
`Actualizar.ps1 -VerifyOnly` pasó. SHA-256 de Release:
`C444835F43DE98CD9BA6D64E1038C3342E0272863ABC5627EBBE4BE84ACFFD7E`.
Se solicitó guardar/cerrar Civil 3D para aplicar esta segunda actualización;
la DLL anterior sigue instalada hasta completar ese paso.

El usuario confirmó nuevamente que guardó/cerró Civil 3D. Se instaló la
segunda corrección y se verificaron por SHA-256 tanto el destino como el
respaldo: PASS. DLL instalada:
`C444835F43DE98CD9BA6D64E1038C3342E0272863ABC5627EBBE4BE84ACFFD7E`.
Respaldo y comprobante:
`Test/Figura3D-Reconocimiento-20261102/Respaldo-20261102-114433-729182fd/`.
El respaldo corresponde a la primera actualización
`8C7D4BAFC46A65253EE712EDED9D65383611A89396204C4584135FCED3B74BA5`.
La corrección del reconocimiento estará disponible al volver a abrir Civil 3D.

## Selección actual 47/42 y verificación en la sesión real

El usuario volvió a reportar el mismo rechazo con TOP 47 / FRONT 42. Se
comprobó dentro de la sesión que la DLL cargada era la instalada C444835F…,
desde el bundle 2024, MVID `e4c82332-b40e-47fa-b74d-e4b5c507b07b`.
La herramienta local de diagnóstico registró las referencias actuales y
reprodujo el rechazo con esos conteos. Evidencia:
`Test/Figura3D-Seleccion-20261102/Diagnostico-8174fb310adc4a40a41bfb3a3e4ab24f.json`.
Se exportó una copia del estado actual sin guardar/modificar el original:
`Test/Figura3D-Seleccion-20261102/Referencia-actual.dwg`, SHA-256
`34E2B90994833D72F4A9435F39614B314B8E658626841120B63BCAEE3500F483`.

Esta planta contiene líneas interiores colineales con los bordes del tubo.
Al unir todos los trazos, el lector extendía los bordes hasta la otra cara de
la caja y después los descartaba por no ser un tramo de entrada lateral.
`ReadPipe` conserva ahora los segmentos originales junto con los unidos.
Agrupa representaciones repetidas/fragmentadas sobre el mismo eje de TOP;
dos ejes distintos continúan siendo una correspondencia ambigua y se rechazan.
Se conservan las comprobaciones de diámetro, extremos inclinados y FRONT.

Pasaron las cuatro pruebas nativas finales sobre copias, conservando los
hashes de las referencias:

- `artifacts/validation/figure-selection-20261102/current-final`: TOP 47/FRONT 42,
  giro del bloque, ausencia del eje, escala/traslación, dos ejes ambiguos
  rechazados, control de ángulo, sólido hueco, guardado y reapertura.
- `artifacts/validation/figure-selection-20261102/previous-54`: selección
  anterior TOP 54/FRONT 42 con las mismas comprobaciones.
- `artifacts/validation/figure-selection-20261102/three-pairs`: tres parejas
  anteriores y su reapertura, incluido tubo perpendicular.
- `artifacts/validation/figure-selection-20261102/reference`: regresión
  A+B/C+D, perfiles, vacíos, ángulos, persistencia y una U.

El usuario interrumpió Computer Use con Esc; se detuvo la automatización y se
retomó solo después de que confirmó «ya terminé; puedes revisar».
Se cargó la candidata Release en memoria mediante un comando de prueba local
que invoca su propio `Figure3DCommands.CreateFigure3D`, sin reemplazar ni
registrar los comandos de la instalación activa. Se seleccionaron ambas
vistas en pantalla, modo Referencias, y se abrió la ventana real del modelo.
Se mostró **Tubería → Inclinación (°)** con valor inicial -18,5 y resaltado
del tubo. La entrada de -19 por teclado no se completó mediante Computer Use;
la edición numérica está verificada por las pruebas nativas WPF anteriores.
Se canceló la ventana con su botón, sin insertar geometría ni guardar el DWG.

La DLL probada tiene MVID `55c91d5b-9e88-4861-9b52-d3aae96b598c` y SHA-256
`E18ACB0AC4A088473AD1E3782EFCB50A46B6652771DFCFE783724DE4180AADBD`.
Entrega: `Test/Figura3D-Seleccion-20261102/Actualizacion/`.
El usuario confirmó que guardó y cerró Civil 3D. Se instaló exactamente esa
DLL y se verificaron por SHA-256 el destino y el respaldo: PASS.
Respaldo/comprobante:
`Test/Figura3D-Seleccion-20261102/Actualizacion/Respaldo-20261102-130346-fd54a5e7/`.
El respaldo es la versión C444835F… anterior. El comando habitual usará la
corrección al volver a abrir Civil 3D; las herramientas temporales de
diagnóstico/prueba no se instalaron en el bundle.
