# Corregir polilíneas superpuestas en DWG — CivilSpellAI 1.3.3.0

Guía de entrega interna, 5 de octubre de 2026. Puede copiarse en un bloc de
notas o importarse como fuente de documentación en un notebook.

## Estado y alcance

La función está preparada para un piloto interno en Civil 3D 2024 de 64 bits.
Se verificaron corrección, cancelación, conservación de fragmentos, restauración
y deshacer en AutoCAD Core Console 2024. En el DWG revisado por el usuario,
437 polilíneas dieron 10 coincidencias antes y 0 después. Permanecían 422 tramos
casi nulos: son una categoría distinta y requieren la opción Limpiar.

Puede aplicarse a otros dibujos con entidades compatibles, empezando por una
copia. No es una reparación universal de DWG ni certifica todo el dibujo.
El paquete incluye variantes 2024, 2025 y 2026, pero 2025/2026 solo están
compiladas: falta validar este flujo dentro de esos productos.
Es el complemento CivilSpellAI completo, con sus otras herramientas incluidas.
La revisión geométrica es local: no requiere API, créditos ni conexión a Internet.

La entrega anterior 1.3.2.0 pasó 12 verificaciones del flujo en
Core Console 2024 y las 151 pruebas generales. La compilación Debug anterior
pasó además los comandos de prueba de geometría (15 verificaciones en total).
El contenido e integridad del ZIP se verifican durante su construcción.

## Instalación en el equipo del jefe

1. Cerrar todas las ventanas de AutoCAD/Civil 3D.
2. Copiar juntos CivilSpellAI-1.3.3.0.zip y CivilSpellAI-1.3.3.0.zip.sha256.
3. Extraer el ZIP completo. Entrar en la carpeta CivilSpellAI-1.3.3.0,
   donde están Manage-CivilSpellAI.ps1 y CivilSpellAI.bundle.
4. Abrir PowerShell como administrador y situarse en esa carpeta.
5. Ejecutar la comprobación previa:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test-CivilSpellAIEnvironment.ps1
```

6. Para una primera instalación:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Manage-CivilSpellAI.ps1 -Action Install -Scope AllUsers
```

Si ya existe CivilSpellAI en AllUsers, usar Update en vez de Install:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Manage-CivilSpellAI.ps1 -Action Update -Scope AllUsers
```

7. Abrir Civil 3D 2024 y una copia del DWG. Escribir REVISARPOLILINEAS.
   El complemento se carga al iniciar Civil 3D. No es necesario NETLOAD.
   Si no reconoce el comando, revisar LEAME.md y la instalación antes de
   copiar DLL sueltas o cambiar la configuración de seguridad.

La instalación queda en
C:\Program Files\Autodesk\ApplicationPlugins\CivilSpellAI.bundle.
Update conserva un respaldo de la instalación anterior. Si el preflight
detecta una instalación por usuario, seguir la migración descrita en LEAME.md.

## Procedimiento recomendado sobre una copia del DWG

1. Escribir REVISARPOLILINEAS y elegir Analizar.
2. Elegir Modelo para todas las LWPOLYLINE de Model, o Seleccion para un conjunto.
3. Empezar con tolerancia 0.000001, expresada en unidades del dibujo.
   La tolerancia determina qué geometría se considera coincidente.
4. Aceptar radio 1 para las marcas. Este radio solo controla la presentación;
   no modifica la tolerancia de detección.
5. Revisar los conteos. Rojo S: solapes. Amarillo N: tramos casi nulos.
   Magenta I: geometría inválida. Revisar permite recorrer e identificar objetos.
6. Si hay tramos casi nulos, elegir Limpiar, leer el resumen y confirmar Si.
   La limpieza retira vértices redundantes y retrocesos rectos verificables.
   Desde 1.3.3.0 también retira puntos intermedios colineales aunque avancen
   en un solo sentido, incluido el tramo de cierre de polilíneas cerradas.
   Conserva esquinas reales y arcos: un quiebre o un grip de segmento no
   demuestra por sí solo que existan líneas superpuestas.
   Comprueba automáticamente el resultado. Algunos casos protegidos se omiten.
7. Elegir Corregir, escribir la palabra completa y revisar la propuesta.
   Muestra identificadores, capa, color, longitud que retira, referencias que
   conserva y cantidad de fragmentos. Confirmar Si solo si la propuesta es
   adecuada al significado de esas entidades en el plano; No es el valor inicial.
8. Elegir Comprobar, también con la palabra completa para evitar confundirla
   con Corregir. Buscar «0 pares de segmentos superpuestos» y comprobar que
   no haya aviso de análisis incompleto o geometría inválida.
9. Revisar visualmente el resultado. Elegir Finalizar para ocultar las marcas.
10. Guardar la copia corregida cuando se haya aceptado el resultado.
    El comando no guarda automáticamente el DWG.

## Qué corrige y qué conserva

Analiza segmentos rectos y arcos de LWPOLYLINE independientes de Model en 3D,
tanto dentro de una polilínea como entre entidades del conjunto seleccionado.
Dos líneas que se cruzan en un punto no son tramos superpuestos. Tampoco basta
que se vean juntas en planta si tienen distinta elevación.

Corregir actúa sobre polilíneas abiertas de tramos rectos compatibles. Sustrae
los intervalos cubiertos por referencias conservadas y mantiene los tramos
restantes como nuevas polilíneas. Una duplicada completa puede retirarse entera.
Los fragmentos heredan las propiedades de la original y cambian de identificador.
Los contornos cerrados se conservan como referencias: no se recortan.
Entre abiertas compatibles, el orden por handle determina cuál se conserva.
No hay todavía un selector manual de prioridad entre dos entidades.

No corrige automáticamente arcos, anchos, espesores, objetos con XData,
diccionarios de extensión o reactores persistentes, ni capas bloqueadas.
No analiza entidades LINE, POLYLINE antiguas, Polyline3d, geometría interna de
bloques, referencias externas ni curvas visibles de superficies Civil/TIN.
Una curva extraída como LWPOLYLINE independiente sí puede analizarse.
Los solapes internos no compatibles con Limpiar pueden quedar pendientes.
Los avisos de búsqueda truncada o geometría inválida bloquean Corregir.

## Interpretación del resultado

«0 pares de segmentos superpuestos» significa que no se detectaron solapes
en el conjunto de LWPOLYLINE y con la tolerancia usados, siempre que el análisis
fuera completo. No certifica entidades excluidas ni todos los posibles defectos.

Los tramos casi nulos se informan aparte. Tener 0 solapes y 422 casi nulos no
significa que la corrección falló: queda una limpieza diferente por realizar.
Puede haber vértices colineales redundantes incluso con cero solapes y cero
tramos casi nulos. Para retirarlos, ejecutar Limpiar; Analizar no los elimina.
Las coincidencias se cuentan por pares de segmentos, no por líneas eliminadas.

## Círculos, cancelar y restaurar

- AISPELLQUITARMARCAS elimina solo las marcas etiquetadas del diagnóstico.
  No elimina polilíneas ni deshace la corrección.
- Finalizar oculta las marcas; AISPELLVERMARCAS permite volver a mostrarlas.
- Salir o Escape al final dejan las marcas y conservan las operaciones ya aplicadas.
- Cancelar la confirmación de Corregir no aplica ese plan.
- Volver a analizar crea marcas nuevas, aunque se hayan retirado previamente.
- RESTAURARSOLAPES recupera originales sustituidos y retira sus fragmentos
  mientras el documento siga abierto. Rechaza toda la restauración si faltan
  fragmentos, se editaron o protegieron, o hay capas bloqueadas.
- RESTAURARPOLILINEAS revierte la última limpieza controlada si las entidades
  siguen en el estado esperado. Si se limpió y luego corrigió, restaurar primero
  solapes y después limpieza.
- Una sola U inmediatamente después de salir de REVISARPOLILINEAS deshace
  esa sesión completa. No hacer zoom, pan ni ejecutar otro comando antes de U.
- Los respaldos de restauración están en memoria; no sobreviven al cierre del DWG.
  Corregir permite una corrección pendiente por documento. No restaurar solo
  para repetir una comprobación: para verificar, usar Analizar o Comprobar.

## Criterio de aceptación por cada nuevo trabajo

Trabajar en una copia, revisar el alcance y la tolerancia, inspeccionar la
propuesta antes de aplicar, comprobar de nuevo y revisar visualmente los
fragmentos. Si quedan casos incompatibles, deben revisarse individualmente;
no aumentar la tolerancia solo para forzar su eliminación.
