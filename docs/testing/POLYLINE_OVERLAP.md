# Diagnostico de superposiciones V3

## Caso del quiebre reportado el 2026-10-05 (1.3.3.0)

Se inspecciono una copia local del DWG indicado por el usuario. La entidad
210CF de la captura es una LWPOLYLINE cerrada de 70 vertices, sin arcos,
longitud 6.61440483937822. No presentaba solapes internos a 1e-6. El quiebre
real (vertice original 68, indices desde cero) no debe eliminarse; el vertice
69 es redundante en el segmento de cierre. Un grip rectangular de segmento
no constituye evidencia de duplicacion.

Limpiar antes conservaba los vertices colineales monotones y no comprobaba
el segmento de cierre durante la simplificacion. Ahora simplifica esos
recorridos, comprobando cada punto original contra el segmento final para
evitar acumular errores. Conserva arcos, anchos y esquinas fuera de tolerancia.
Puede cambiar el primer vertice de una cerrada si ese punto es redundante;
mantiene el cierre y un minimo de tres vertices.

Prueba sin guardar el DWG: 439 polilineas conservadas, 427 ajustadas y 15143
vertices retirados. Cero solapes, tramos casi nulos y geometria invalida antes
y despues. La entidad reportada pasa de 70 a 61 vertices; desviacion maxima
de sus vertices originales respecto al resultado 1.78e-9 unidades (menor que
1e-6), longitud conservada a precision numerica. La esquina 68 se conserva.
RESTAURARPOLILINEAS despues de ZOOM recupera exactamente vertices, bulges,
anchos, cierre, normal y elevacion de las 439 polilineas.

Evidencia local: artifacts/validation/overlap-case-20261005/cleanup.log y
geometry-check.txt. Prueba sintetica permanente en AISPELLTESTSAFECLEANUP:
avance colineal, cierre, primer vertice redundante, idempotencia, esquina real,
arcos, anchos y ejemplo local del caso reportado. Las capturas no prueban
solapes por si solas; no se certifica eliminacion de todos los quiebres visibles.

## Filtro de entidades y verificacion del par

Analisis y limpieza comparten una comprobacion de tipo exacto `Polyline`,
DXF `LWPOLYLINE` y propietario Model. No extraen curvas de superficies TIN,
geometria de bloques ni entidades LINE. Una curva extraida previamente que ya
sea LWPOLYLINE independiente si pertenece al alcance.
En Revisar, `Identificar` muestra handles, tipo, capa, color, elevacion, cierre
y vertices, y selecciona las entidades exactas del caso. El circulo solo indica
una zona: no implica que todos los objetos visibles dentro sean superpuestos.
Las coincidencias son 3D; verse una encima de otra en planta no basta si tienen
distinta elevacion. El diagnostico no borra entidades. La opcion Corregir
presenta un plan y requiere confirmacion antes de sustituir polilineas abiertas.

Comandos:
- REVISARPOLILINEAS: menu integrado recomendado (ver abajo).
- AISPELLDETECTARSUPERPUESTAS: diagnostico de Model o seleccion.
- AISPELLQUITARMARCAS: retira solo las marcas etiquetadas de diagnostico.
- AISPELLVERMARCAS: Todas / Superpuestas / Nulos / Invalidas / Ocultar.
- RESTAURARPOLILINEAS: recupera la ultima limpieza controlada mientras el dibujo siga abierto.
- RESTAURARSOLAPES: recupera los originales sustituidos por Corregir mientras el dibujo siga abierto.

## Flujo

Elegir alcance, tolerancia (0.000001 por defecto) y radio minimo (1 unidad).
Se comparan segmentos rectos y arcos circulares de LWPOLYLINE, internos y entre
entidades seleccionadas, en coordenadas 3D. No se ejecuta OVERKILL.
Las polilineas originales se abren solo para lectura.

Colores y etiquetas:
- Rojo S: superposiciones de tramos.
- Amarillo N: segmentos de longitud <= tolerancia, contados aparte.
- Magenta I: arcos degenerados o curvatura no finita, sin certificacion geometrica.
- xN: cantidad de hallazgos agrupados en una zona.

Se agrupan hallazgos del mismo tipo cuyos centros estan a distancia <= radio
del ancla del grupo, en 3D. El circulo se amplifica para encerrar los extremos
y extremos geometricos de todos los tramos del grupo. Los conteos del informe
son hallazgos, no circulos. Los arcos se resaltan temporalmente mediante una
aproximacion visual; su deteccion y longitud utilizan el arco circular.

Acercar / Siguiente / Anterior recorren los hallazgos individuales e indican
el identificador de la zona. Salir o Escape dejan las marcas; el zoom permanece.
AISPELLVERMARCAS cambia la visibilidad de las capas de diagnostico.
Repetir el diagnostico reemplaza TODAS las marcas previas y vuelve a mostrarlas.
Las marcas y el historial se guardan con el DWG.

Antes de OVERKILL, retirar marcas con AISPELLQUITARMARCAS y seleccionar solo
polilineas. Repetir el diagnostico despues. La retirada de marcas conserva el
historial anterior para la comparacion.

## Comparacion antes / despues

Un Xrecord en el diccionario del dibujo almacena el ultimo resumen. Se compara
solo cuando coinciden alcance y tolerancia, y ninguna busqueda fue truncada.
Modelo abarca todas las LWPOLYLINE de Model; Seleccion usa una huella SHA256
de los handles seleccionados. Un cambio de seleccion reinicia la referencia.

Muestra conteos anteriores y actuales y, si no hay geometria invalida, su
reduccion neta. No identifica correcciones individuales ni atribuye los cambios
a OVERKILL. Con geometria invalida advierte que la revision es incompleta.
La deteccion de arcos no finitos es importante: una disminucion de vertices
no garantiza que el resultado sea geometria valida.

## Limites

Arcos: se compara centro, radio y plano dentro de tolerancia, con intervalos
angulares que respetan sentidos contrarios y paso por cero. Un par de arcos
mayores puede producir dos tramos comunes separados. Contactos puntuales y
cruces de recta/arco no son superposiciones de longitud positiva. No se cuentan
rectas casi tangentes como si fueran arcos duplicados.

No analiza POLYLINE antiguas, entidades Polyline3d, anchos, bloques ni superficies
Civil. No excluye pares por capa. Las coincidencias son candidatas a revisar,
no autorizaciones para eliminar objetos con significado distinto.

Limites: 10000 coincidencias, 20 millones de candidatos y hasta 10000 marcas
individuales de cada tipo degenerado. Se avisa al truncar la busqueda.
XData CIVILSPELLAI_OVERLAP_V2 identifica marcas. Solo se borran objetos etiquetados,
nunca todos los objetos de una capa. Las tres capas son no imprimibles.

## Verificacion 2024

- Compilacion sin errores ni advertencias.
- AISPELLTESTOVERLAP: rectas, cruces, elevacion, contactos, longitud comun,
  barrido global y pares entre entidades.
- Arcos parciales, inversos, diferentes planos/elevaciones, contacto puntual,
  intervalos partidos y conversion de bulge positivo/negativo.
- Ciclo crear/reemplazar/borrar marcas conserva polilineas y circulos ajenos.
- Agrupacion mantiene separados los tipos.
- Test/test-overlap-v3.scr: OVERLAP_TEST_OK, FILTER_OK, SOURCE_UNCHANGED_OK.
- Copia real original: 437 polilineas, 10 coincidencias en 4 entidades,
  422 casi nulos. Marcas agrupadas: 2 zonas rojas y 412 amarillas.
- Tras OVERKILL en la copia de prueba: 10 coincidencias, 18 casi nulos y
  401 segmentos degenerados (bulge NaN, extremos coincidentes).
  Comparacion antes/despues reconoce y avisa de estos segmentos invalidos.

Los scripts de Test contienen rutas de esta estacion. Las pruebas en consola
no sustituyen la comprobacion del aspecto visual en Civil 3D interactivo.

## Menu integrado REVISARPOLILINEAS

- Analizar: solicita alcance, tolerancia y radio la primera vez; conserva ese
  conjunto durante la sesion. Diagnostica y vuelve al menu sin entrar al recorrido.
- Revisar: recorre los hallazgos con Acercar / Siguiente / Anterior / Salir.
- Ver: filtra capas de marcas.
- Elegir: cambia el conjunto, tolerancia o radio y lo analiza.
- Limpiar: actualiza el diagnostico previo, muestra alcance/configuracion y pide
  Si/No (No por defecto). Solo Si ejecuta la limpieza controlada sobre los IDs de
  la sesion, sin OVERKILL nativo. Comprueba automaticamente con la misma tolerancia.
- Comprobar: repite diagnostico y comparacion del conjunto de la sesion.
- Corregir: recalcula el diagnostico, presenta los originales que propone
  sustituir, su capa/color, longitud coincidente, referencias conservadas y
  numero de fragmentos. Solo Si aplica el plan; No es el valor por defecto.
- Restaurarsolapes: restaura los originales de Corregir y retira sus fragmentos.
  Rechaza toda la operacion si faltan fragmentos, fueron editados o protegidos,
  o hay capas bloqueadas. Despues puede usarse Restaurar para deshacer Limpiar.
- Quitarmarcas: retira las marcas sin borrar polilineas ni historial.
- Restaurar: recupera los vertices previos de la ultima limpieza controlada, incluso
  despues de PAN/ZOOM. Rechaza la operacion completa si hubo ediciones geometricas
  posteriores, entidades borradas o capas bloqueadas. Respaldo solo en memoria del
  documento abierto; no sustituye una copia DWG. Fuera del menu: RESTAURARPOLILINEAS.
- Finalizar: apaga las tres capas de circulos y textos y sale. No borra polilineas
  ni otros objetos del dibujo. AISPELLVERMARCAS / Todas permite mostrar las marcas.
- Salir: termina. U inmediatamente despues deshace toda esta sesion del comando,
  incluidas limpieza, marcas e historial. No se guarda el dibujo automaticamente.

La limpieza nativa anterior produjo 401 bulges NaN sobre extremos coincidentes.
Por eso Limpiar ahora usa una transaccion propia: retira vertices consecutivos
redundantes y retrocesos de tramos rectos contenidos en el segmento de sus extremos.
Desde 1.3.3.0 simplifica tambien vertices colineales monotones. Conserva IDs, elevacion, normal, cierre
y extremos de polilineas abiertas. Omite capas bloqueadas, reactores persistentes
y ancho constante distinto de cero. Solo repara bulges no finitos si sus extremos
coinciden exactamente; no intenta reconstruir arcos invalidos de longitud positiva.
No elimina coincidencias entre entidades distintas ni une contornos.
La limpieza no garantiza cero hallazgos; reporta geometria invalida o analisis
parcial, sin afirmar que el dibujo esta completamente corregido.

Pruebas de la limpieza controlada (2026-10-01, consola AutoCAD 2024, sin guardar):
- Original local: 437 entidades conservadas; 422 vertices retirados; casi nulos
  422 -> 0; invalidos 0 -> 0; coincidencias entre entidades 10 -> 10.
- Copia previamente procesada por OVERKILL: casi nulos 18 -> 0; invalidos 401 -> 0;
  419 vertices retirados; coincidencias 10 -> 10.
- Test/test-safe-cleanup-finalize.scr: FINALIZE_HIDDEN_OK y SAFE_RESTORE_EXACT_OK
  en ambas copias. Restauracion exacta de vertices, bulges (incluido NaN), anchos,
  cierre, normal y elevacion despues de ZOOM. No se modifica el DWG del usuario.
- AISPELLTESTSAFECLEANUP: retroceso recto, excursion que debe conservarse,
  duplicado con bulge no finito y preservacion del arco siguiente.

Pruebas integradas sobre copia local de 437 polilineas, sin guardar:
- test-review-polylines.scr: confirmacion No, filtros, Si, comprobacion automatica;
  U restaura exactamente los entget anteriores (WORKFLOW_UNDO_OK).
- test-review-selection.scr: No conserva la entidad; Si limpia solo una polilinea
  y las otras 436 quedan iguales (DECLINED_CLEANUP_OK, OUTSIDE_SELECTION_UNCHANGED_OK).

## Correccion entre entidades y cierre de pruebas (2026-10-02)

Corregir admite fuentes abiertas de tramos rectos, sin anchos, espesor, XData,
diccionario de extension ni reactores persistentes, en capas desbloqueadas.
Conserva los contornos cerrados como referencias. Entre abiertas compatibles,
el orden por handle determina la referencia conservada. Una fuente sustituida
no se utiliza como referencia para borrar otra. Arcos y contornos cerrados no
se recortan. Un analisis incompleto o con geometria invalida bloquea Corregir.

Se sustraen exclusivamente los intervalos coincidentes verificados en 3D.
Los intervalos no cubiertos quedan como nuevas polilineas con las propiedades
de la original; sus identificadores cambian. Una duplicada totalmente cubierta
puede desaparecer sin crear fragmentos. Las referencias quedan intactas.
No se guarda el DWG automaticamente. Solo se permite una correccion pendiente
de restauracion por documento abierto. Tras U, RESTAURARSOLAPES libera el
respaldo ya deshecho para permitir otra correccion.

Ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-OverlapCorrection.ps1`.
El script compila Debug y usa AutoCAD Core Console 2024, un perfil aislado y
una copia de acad.dwt dentro de artifacts/validation. No abre dibujos reales.
Comprueba marcadores completos del registro; no acepta como evidencia el eco
de una expresion Lisp. Requiere permiso para crear el perfil aislado de AutoCAD.

Cobertura: geometria de solapes y limpieza, conservacion de ambos extremos
en un solape parcial, intervalos duplicados, distinto Z, cruces, huecos,
duplicada completa invertida, proteccion de arcos/anchos/capas/datos,
cancelacion No, contorno y entidad fuera de seleccion intactos, rechazo de
restauracion tras editar color o bloquear capa, restauracion exacta despues
de ZOOM, una U inmediata, nueva correccion despues de U y restauracion de
duplicadas completas sin fragmentos.

El script antiguo `Test/create-corrected-overlap-copy.scr` ahora usa Corregir
en lugar de borrar dos entidades por handle. No debe recuperarse el borrado
manual anterior: eliminaba tambien tramos no superpuestos. Ese script guarda
el dibujo abierto y solo debe ejecutarse sobre una copia desechable verificada.
La regresion automatizada no sustituye la revision visual del DWG real.

Resultado del 2026-10-02: 15/15 verificaciones en AutoCAD Core Console 2024.
Registro: `artifacts/validation/overlap-correction-2d34cdd1c9e647c7b85805ed862a7935/console.log`.
Debug 2024 y Release 2024/2025/2026 compilan sin errores ni advertencias.
Las variantes 2025/2026 no se han ejecutado en esos productos durante esta prueba.

Instalacion 2026-10-02: con AutoCAD cerrado, se actualizo la DLL de Civil 3D
2024 en Program Files, conservando respaldo en artifacts. La DLL instalada
coincide por SHA-256 con la compilacion Debug validada:
`FEF5FC9A027963E9BC3AA1C9619731C0BC75BD71B8075B19210FF98F4B82A375`.
Queda pendiente la revision visual del DWG en Civil 3D interactivo.

La captura posterior del usuario confirma 437 polilineas revisadas y 10 -> 0
solapes, con dos fragmentos conservados. Persisten 422 tramos casi nulos;
no es evidencia de haber ejecutado Limpiar. La entrega interna 1.3.2.0 incluye
GUIA_CORREGIR_SOLAPES.md. La DLL Release 2024 del ZIP coincide con la probada
en `overlap-correction-c6a0c1beba644950915aaee5573892e4/console.log`:
12 verificaciones del flujo aprobadas, ademas de 151 pruebas generales.
