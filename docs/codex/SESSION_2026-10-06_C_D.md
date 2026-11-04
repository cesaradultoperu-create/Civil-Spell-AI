# Ampliación para otros ejemplos TOP + FRONT, 06/10/2026

**Estado posterior:** 1.3.5.0 ya instalado y verificado 49/49 después de que el
propietario confirmó Civil 3D cerrado. Los apartados que describen el paquete
como «no instalado» conservan el estado al prepararlo; ver el cierre al final.

## Objetivo y respuesta al propietario

El propietario recordó que el propósito de CREARFIGURA3D es generar modelos
nuevos desde las referencias que seleccione, no limitarse a editar el ejemplo
A+B. Señaló C como TOP y D como FRONT en la segunda pareja del mismo DWG.
Después preguntó qué comando usar para crear desde cero. Se explicó el flujo:
CREARFIGURA3D → TOP completo → Referencias → FRONT completo → vista previa →
Crear 3D → punto de inserción. EDITARFIGURA3D es para conjuntos existentes.

Su captura confirmó que el editor de 1.3.4.0 abre el modelo A+B. No confirmó
todavía giro/zoom, cancelación, aplicación ni selección interactiva de C+D.

## Problema demostrado y corrección

La prueba de C+D con el reconocimiento anterior falló: solo buscaba mampostería
a la izquierda y un tubo inclinado a la derecha. Evidencia de esa limitación:
`artifacts/validation/figure3d/reference-CD-baseline-20261006/prepare.log`.

1.3.5.0 reconoce bordes/eje de tubería a izquierda o derecha, incluyendo
tubería horizontal. Relaciona diámetro, eje, lado y tramo común de ambas vistas.
Los bordes fragmentados se unen solo si son colineales y se solapan o tocan.
El canal se busca en el lado opuesto al tubo; cama de arena y entrada de caja
se orientan al lado correspondiente. Las cotas permanecen las de FRONT.

C+D también contiene un elemento interior. TOP muestra dos bordes paralelos
que atraviesan el vacío de la caja; FRONT aporta un soporte cerrado AR-CONC y
los tres bordes superiores que comparten el borde superior del soporte. Se
reconstruyen ambas piezas cuando los límites coinciden; no se agrega un cierre
que no exista en el dibujo. La pieza superior se distingue con color y nombre
«Elemento interior: material por confirmar».

Se preguntó al propietario si representa un tabique o rejilla/pantalla. Al
preparar el paquete no se había recibido respuesta. Se conserva su envolvente
geométrica; no se inventan barras, perforaciones ni clasificación de material.

## Datos del modelo C+D

- Caja 1.00 × 1.30, altura de concreto 0.95; base 0.15; solado separado 0.05.
- Fondo de mampostería entre 0.45 y 0.55; laterales entre 0.55 y 0.85.
- Soporte interior de concreto entre 0.15 y 0.35, espesor geométrico X=0.10.
- Pieza interior superior entre 0.35 y 0.85, misma franja de TOP.
- Tubo horizontal en Z=0.60, diámetro geométrico 0.10, X entre -1 y 0.189944.
- Cama de arena exterior a la izquierda, aproximadamente entre Z=0.50/0.55.
- Espesor de tubo sigue sin definir: superficie abierta, no cilindro macizo.

Son datos leídos de esta pareja de referencias, no valores fijos impuestos a
otros ejemplos. La prueba reconoce C+D a escala 1.5 y con desplazamientos
independientes de TOP/FRONT y verifica que esas medidas se transformen.

## Validación y artefactos

- Debug 2024 compila sin advertencias.
- `artifacts/validation/figure3d/reference-AB-CD-1.3.5-20261006/`: prueba nativa
  A+B, C+D, perfiles arbitrarios con arcos/vacíos, C+D desplazado/escalado,
  persistencia, una U y reapertura. Se usan copias del WBLOCK del 05/10 y el
  script verifica que su hash permanezca intacto.
- Release 2024/2025/2026 compila sin advertencias y pasa 151 pruebas.
- El verificador independiente acepta el ZIP y rechaza seis variantes inválidas.
- Registros: `reference-CD-bundle-1.3.5-20261006.log` y
  `reference-CD-verifier-1.3.5-20261006.log` en artifacts/validation/figure3d.
- Carpeta de revisión: `Test/Drawing3D/Reconstruccion-A-B-C-D-20261006/`.
  DWG con A+B y C+D etiquetados, vistas previas de ambos, JSON de parámetros,
  bundle completo, ZIP, checksum, guía y actualizador verificado.

Release 2024 SHA-256:
`BE4EC20ECB4DDD0EE90733E5601E7F869C5080990612240594AAB1BD9DA6AE59`.
Release 2025:
`00ED7984EFC336066CA2258E33273C0089284CC0C9C320018461B04EF26B72D0`.
Release 2026:
`2E691FA1CA59B307A07950D12E8BF4653FF77780A72AE8B61D8B3A75C9245B3B`.
ZIP 1.3.5.0:
`8C676E2C58FD3B3EB975E2B64B0EB1CB6C9B7726BA46D854AD118B7057DFE8DC`.

## Instalación y siguiente paso

1.3.5.0 está preparado y **no instalado**. Civil 3D está abierto con 1.3.4.0,
instalado anteriormente en esta jornada y verificado 49/49. No se sustituye
una DLL cargada ni se guardan/cerran dibujos del propietario automáticamente.
El nuevo actualizador requiere Civil 3D cerrado, verifica los hashes Release,
actualiza el bundle completo y conserva respaldo mediante el gestor existente.

Al cerrar Civil 3D se puede instalar el nuevo paquete y después probar
CREARFIGURA3D seleccionando C y D en la copia. Falta confirmar el material del
elemento interior, correspondencia visual con el plano y flujo interactivo.
Otros ensamblajes desconocidos siguen requiriendo correspondencias por pieza;
el modo Componente admite perfiles cerrados con arcos y vacíos. Esta ampliación
no promete una reconstrucción única de cualquier estructura desde dos vistas.

## Instalación completada

El propietario intentó CREARFIGURA3D con C y D en 1.3.4.0 y mostró el mensaje
«Seleccione el canal de mampostería completo en TOP y FRONT, incluidos sus
hatches GRAVEL». Se explicó que el reconocimiento anterior buscaba el canal
del lado contrario. El propietario respondió «cerrado civil3d».

Se comprobó ausencia de procesos acad/accoreconsole, checksums de la carpeta,
ZIP Release y coincidencia del gestor empaquetado con el gestor revisado. Se
ejecutó el actualizador preparado y se verificó de forma independiente:

- Bundle instalado 1.3.5.0 para 2024/2025/2026: 49/49 archivos idénticos al origen.
- Manifiesto instalado AppVersion=1.3.5.0; sin copia duplicada por usuario.
- Respaldo completo de 1.3.4.0:
  `C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261006-103601-819`.
- DLL 2024 respaldada SHA-256
  `AE0F5B2EF3821CFC6C72B6A3D7DCEEE34AF3DA28CCA8D92A8D893B74C376A7C7`.
- Comprobante: `artifacts/validation/figure3d/reference-CD-installation-1.3.5-20261006.json`,
  también en la carpeta de revisión como `INSTALACION_VERIFICADA.json`.

No se abrió ni se guardó ningún DWG del propietario para instalar. Siguiente
paso: abrir Civil 3D y repetir CREARFIGURA3D con C como TOP y D como FRONT.
La prueba interactiva de esta corrección y el material del elemento interior
siguen pendientes de confirmación del propietario.
