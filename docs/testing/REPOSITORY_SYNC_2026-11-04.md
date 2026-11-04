# Verificación de las fuentes para GitHub — 4 de noviembre de 2026

Se comprobó una copia extraída exclusivamente de los archivos guardados en Git,
sin reutilizar `bin`, `obj` ni `.packages` del directorio de trabajo. La
restauración NuGet utilizó una carpeta de paquetes inicialmente vacía.

## Resultados

- CivilSpellAI 2024/2025/2026: compilación Debug y Release, sin advertencias ni
  errores; 152 pruebas del núcleo correctas en cada configuración.
- Proyectos 2025/2026: 110 fuentes y cinco archivos XAML, sin incorporar código
  de herramientas independientes ni de `Test/`.
- Compilación Release de 16 variantes adicionales: BordeTIN3D, Overlap2023,
  DrenBordes, AttributeLocator, Field, DetailMarker, MLeaderDiagnostics,
  SotramiExcel y sus pruebas, RelaveAuto 2023/2024/2025/2026, RelaveVision,
  AdaptiveProbe y FotosEnCuadro Paso4.
- AdaptiveProbe: prueba sintética correcta; error de área de −1,35 %, rechazo
  de sombra aislada y exclusión del reflejo remoto.
- SotramiExcel: prueba correcta con el libro local original; 52 planos,
  incluidos los tres de la etapa final. El libro permanece fuera de Git.
- Paquete RELAVEAUTO 0.3.5: reconstruido desde las fuentes.
- Paquete CivilSpellAI 1.3.7.0: reconstruido y aceptado por el verificador.
  SHA-256 del ZIP de esta comprobación:
  `553DE64381823E501283E546A18DB3599E4A87734989AC69F1A0B5CCD8E1C237`.
- Pruebas del verificador: acepta el paquete válido y rechaza cinco casos de
  dependencias/licencias de geometría ausentes, un comando ausente, rutas y
  versiones incorrectas del runbook, nombre/checksum incoherente, checksum
  inválido y archivos adicionales no previstos.
- Los cuatro diccionarios conservan las huellas originales de `SOURCE.md`.
  `.gitattributes` impide que la conversión de saltos de línea de Windows
  altere esos archivos al recuperarlos desde Git.
- La compilación y el empaquetado no modificaron los archivos fuente de la
  copia limpia.

La revisión de los objetos nuevos del historial pendiente, incluidos los ocho
commits anteriores a esta sincronización, no detectó los patrones de claves
privadas y tokens comprobados ni archivos de 50 MiB o más. Es una comprobación
por patrones, no una garantía de ausencia de cualquier dato sensible.

## Límites y conservación local

No se ejecutaron pruebas interactivas dentro de Civil 3D ni se modificaron sus
instalaciones. Los planos, fotos, presentaciones, entregas, configuraciones y
perfiles de prueba permanecen en el disco y no se respaldan con este push.
Los prototipos históricos pueden necesitar esos datos y adaptar rutas locales.

Se identificaron dos `node_modules` como enlaces a una dependencia compartida
fuera del repositorio. Se excluyen de Git y se conservan: su tamaño aparente
no corresponde a dos copias locales que se puedan borrar para liberar espacio.

La limpieza posterior a confirmar GitHub se limita a intermediarios `obj`, la
dependencia Python restaurable del prototipo y archivos temporales de
verificación/descargas. El inventario y el resultado concreto de la limpieza
se guardan localmente en `artifacts/workspace-maintenance/`.

Para recuperar el proyecto: [guía de clonación y compilación](../GITHUB_AND_NEW_COMPUTER.md).
