# Archivo organizado de programas — 14/10/2026

El usuario pidió guardar individualmente los trabajos ya correctos en la carpeta que creó: `G:\01. PERSONALES\Cesar\Programas con el Visual Studio para el CIVIL3D`. Confirmó incluir también otros trabajos terminados, además de los seis de CivilSpellAI inicialmente identificados.

Se guardaron siete carpetas:

1. `01_Corrector_de_textos`: paquete completo CivilSpellAI 1.3.7.0.
2. `02_Guias_angulares_MLeader`: mismo paquete 1.3.7.0; guía con casos interactivos confirmados y matriz restante.
3. `03_Limpieza_de_polilineas`: TramosDWG 1.3.3.2 independiente; fuentes, guía y ejemplos CORTE corregido/comparación.
4. `04_Secciones_y_medidas_editables`: paquete 1.3.7.0; documentación y DWG/imagen de medidas.
5. `05_Figuras_3D_desde_TOP_FRONT`: paquete 1.3.7.0; tres parejas, imágenes y parámetros de referencia. La guía distingue validación automatizada y comparación visual del último reconocimiento pendiente por el usuario.
6. `06_Bordes_TIN_3D`: entrega exacta 0.1.3 con las seis DLL, fuente independiente, licencias, guía y ejemplos. Exterior y dos interiores confirmados por el usuario.
7. `07_Marcador_de_cortes_CORTE2P`: DLL SectionMarkerToolFINAL 10.0.0.0, fuente de su proyecto y DWG USUARIO FINAL, instrucciones y vista previa. La guía final existente registra auditoría sin errores e inserción probada.

Cada carpeta es una copia individual con LEEME.txt y programa. Cuatro herramientas comparten la DLL CivilSpellAI: se preserva el mismo paquete completo en cada carpeta y se aclara que solo se instala una vez. Se preservaron binarios originales, sin compilar, instalar ni modificar dibujos. El corrector no lleva configuración privada, claves ni el servidor de LanguageTool.

Las cuatro herramientas del complemento compartido llevan Codigo_fuente.zip, una captura de las fuentes de trabajo al 14/10, explícitamente posterior al binario 1.3.7.0 y sin afirmar reproducción binaria exacta. Contiene solución, proyectos, fuentes, recursos, documentación y scripts; excluye .git, bin/obj, paquetes, ajustes locales y archivos de usuario. El proyecto antiguo CivilSpellAI.csproj conserva HintPath históricos: LEEME_CODIGO indica revisarlos al cambiar de ubicación. Las tres herramientas independientes llevan Código_fuente legible y COMPILAR.txt.

No se incluyeron entregas antiguas descartadas, proyectos de práctica de repos hermanos ni prototipos LISP sin confirmación de cierre. No presentar esos prototipos como trabajos validados.

Preparación reproducible: `scripts/Prepare-OrganizedWorkArchive.py`. Staging final: `artifacts/programas-ordenados-20261014-521acb97`, 331 archivos, 59 769 095 bytes. El paquete CivilSpellAI 1.3.7.0 pasó el verificador de Release original; los ZIP TramosDWG/CivilSpellAI y las seis DLL TIN se contrastaron con sus hashes existentes.

Copia: `scripts/Copy-OrganizedWorkArchive.ps1`, con elevación aprobada automáticamente para acceder a G:. Comprobación previa de todos los archivos, rechazo de sobreescritura de contenido diferente, copia y comprobación SHA-256 de 331/331 archivos incluida la manifestación. VERIFICAR_ARCHIVO.ps1 del destino comprobó las 330 entradas de SHA256.json (el manifiesto se verifica aparte durante la copia). Finalizó con código 0.

Entrada accesible: `INDICE.html`; además LEEME.txt, CATALOGO.json con versiones/procedencia y SHA256.json con las huellas. No se cerró ni manipuló Civil 3D durante esta tarea.
