# Segundo alcance: estabilización, distribución y mejoras

Fecha de definición: 2026-08-25

Prioridad aprobada: estabilizar y distribuir antes de ampliar entidades.

## 1. Objetivo

Convertir el MVP validado en una herramienta repetible y soportable para un
equipo piloto. El segundo alcance conserva la aprobación humana, la validación
local y el aislamiento de Autodesk. Las nuevas funciones no pueden debilitar la
protección de texto técnico ni introducir escrituras automáticas.

La ejecución se realiza en orden. Cada hito debe cerrar sus criterios de salida
antes de iniciar el siguiente, salvo trabajo documental o prototipos que no
afecten al producto distribuido.

## 2. Hitos comprometidos

### Hito 1 — Cierre de regresión del MVP

**Prioridad:** P0.

**Estado:** completado el 2026-08-26. Build Debug/Release sin errores ni
advertencias, 36/36 pruebas en ese corte, matriz manual completa y
REG-2026-001 cerrada. La suite creció a 44/44 durante el Hito 2.

- Ejecutar y registrar toda la matriz pendiente de
  `10_MVP_STATUS_AND_TEST_PLAN.md`.
- Cubrir `DBText`, `MText` con formato, mantener, cancelar, cierre, conflicto y
  `UNDO` individual y por lote.
- Probar OpenAI con clave inválida, desconexión, timeout, cancelación y `UNDO`.
- Crear un formato único de incidencia con versión, entorno, pasos, resultado,
  severidad y evidencia anonimizada.

**Criterio de salida:** cero defectos críticos abiertos y evidencia completa de
que cancelación, fallos y conflictos no modifican el dibujo.

**Resultado:** cumplido. Se validaron `DBText`, `MText`, salidas sin escritura,
conflictos, lotes, `UNDO`, simulación, autenticación inválida, desconexión y
OpenAI real. La cancelación durante `SlowSuccessful` no produjo reapertura ni
escritura tardía y Civil 3D permaneció operativo.

### Hito 2 — Pruebas de integración y robustez

**Prioridad:** P0.

**Estado:** completado el 2026-08-27. Suite total 52/52, Debug/Release x64
validados y regresión interactiva mínima completa en Civil 3D.

**Avance:** la prevalidación y el commit se aislaron mediante una frontera
sustituible. También se aislaron selección y contexto de documento; ya están
cubiertos objeto inexistente, tipo/texto cambiado, documento distinto o
cerrado, lote vacío, conflicto parcial, fallo antes del commit y commit válido.
La carga, cancelación, respuesta tardía, solicitud sustituida y reintento se
prueban sin red ni secretos. El dataset ampliado se ejecuta como parte de la
suite y cubre todos los grupos comprometidos.

Las revalidaciones interactivas `SAFE-02` y `BATCH-02` con la nueva DLL
confirmaron conflicto individual y por lote, siempre con cero escrituras. Un
commit aprobado sobre MText conservó `\pxqj;` y un único `UNDO` restauró texto y
formato. La frontera de escritura de este primer corte queda revalidada.

- Separar las fronteras de selección, documento y escritura detrás de puntos
  sustituibles para probar comandos sin una sesión interactiva completa.
- Añadir pruebas de objeto inexistente, tipo cambiado, documento distinto,
  texto modificado, lote vacío, conflicto parcial y fallo antes del commit.
- Probar ciclo de vida de peticiones: carga, reintento, cancelación, cierre de
  ventana y cierre/cambio de documento.
- Añadir datasets anonimizados con español, inglés, texto mixto, MText largo,
  códigos de formato, estaciones, unidades y repeticiones.
- Mantener pruebas remotas deterministas mediante transporte falso; las pruebas
  normales nunca consumirán API ni requerirán secretos.

**Criterio de salida:** los caminos de escritura y degradación segura tienen
cobertura automatizada en sus fronteras, y la matriz manual se reserva para lo
que realmente requiere Civil 3D.

**Resultado:** cumplido. Selección/cancelación, lote selectivo con `UNDO`, cierre
durante una solicitud lenta, conflictos individual/por lote y cambio de
documento se revalidaron con la DLL Debug `E264C705…04B0AC`; todos quedaron en
PASS y no se observaron escrituras tardías ni parciales.

### Hito 3 — Diagnóstico seguro y soporte

**Prioridad:** P1.

**Estado:** completado el 2026-08-27. Códigos estables, registro optativo,
privacidad por construcción, rotación, exportación, borrado y siete pruebas
específicas; suite total 59/59 y regresión interactiva completa.

- Definir códigos estables para selección, validación, conflicto, configuración,
  red, autenticación, timeout, respuesta inválida y escritura.
- Incorporar un registro local configurable con fecha, versión, comando, código
  y duración; excluir texto, prompts, respuestas, claves, rutas de DWG, handles
  y metadatos por defecto.
- Permitir exportar un paquete de diagnóstico revisable por el usuario sin
  contenido del plano ni secretos.
- Documentar severidades, pasos de triage, conservación y borrado de registros.
- Medir duración y cantidad de textos de forma agregada para detectar problemas
  de rendimiento sin crear telemetría remota por defecto.

**Criterio de salida:** un fallo del piloto puede reproducirse o clasificarse
sin solicitar el DWG ni revelar su contenido.

**Resultado:** cumplido. `H3-DIAG-01` confirmó exportación y borrado con solo
los siete campos permitidos. `H3-DIAG-02` reprodujo un timeout como `TMO-001`
para un elemento, mantuvo la cancelación segura y no registró texto, DWG,
rutas, handles, prompts, respuestas ni credenciales.

### Hito 4 — Distribución y operación

**Prioridad:** P1.

**Estado:** completado el 2026-09-03. Candidato piloto vigente 1.1.4.0
generado con Application Bundle restringido a Civil 3D 2024 x64, carga por
invocación, preflight y ciclo administrado de instalación, actualización,
rollback y desinstalación. La instalación real bajo `Program Files` y el
autoloader, rollback y desinstalación están en PASS. La política del piloto
quedó fijada: sin firma, instalación administrada,
solo en equipos controlados y con verificación obligatoria del hash.

**Avance:** Release x64 conserva la versión visible 1.1.4.0 y no incluye DLL de
Autodesk. El ciclo de despliegue pasó de extremo a extremo en directorios
temporales aislados; la instalación 1.1.0.0 y actualización real a 1.1.1.0
también pasaron. Las versiones sustituidas quedan recuperables y la
configuración del usuario no se elimina. `AISPELLSETTINGS` confirmó la carga real
por invocación de 1.1.3.0 desde `Program Files`, sin `NETLOAD` ni aviso de firma.
La migración desde `CurrentUser` exigió un único `APPAUTOLOADER` → `Reload`;
la actualización real a 1.1.4.0, rollback, desinstalación y reinstalación
pasaron conservando los datos locales. El autoload de la instalación final se
confirmó sin `NETLOAD`, recarga ni aviso de firma.

- Producir una compilación reproducible `Release|x64`, con versión visible y sin
  copiar ensamblados de Autodesk.
- Empaquetar el complemento como Autodesk Application Bundle con autocarga y
  comandos registrados; conservar `NETLOAD` como vía de diagnóstico.
- Definir instalación, actualización, compatibilidad de configuración,
  desinstalación y rollback a la versión anterior.
- Evaluar firma de ensamblado/paquete y documentar requisitos de confianza de
  AutoCAD para el entorno piloto.
- Añadir una comprobación previa de Civil 3D 2024, .NET Framework, permisos,
  configuración y presencia de credencial sin mostrarla.

**Criterio de salida:** un usuario piloto puede instalar, actualizar, revertir y
desinstalar sin compilar el proyecto ni copiar archivos manualmente.

### Hito 5 — Experiencia de revisión

**Prioridad:** P2, después de estabilización y paquete piloto.

**Estado:** completado el 2026-09-03. La matriz principal se revalidó en 1.1.3.0 y la
cancelación durante la preparación global quedó en PASS sin modificaciones. El
candidato 1.1.4.0 sincroniza foco y selección en listas, expone ayuda accesible
de validación y pasa las garantías XAML automáticas. El recorrido con teclado al
125 % quedó en PASS en Configuración, revisión individual y lote.

- Añadir edición manual del resultado y pasarla por el mismo diff y validador
  antes de habilitar **Aplicar**.
- Mostrar progreso, conteos, cancelación y resumen de fallos durante
  `AISPELLALL`.
- Añadir búsqueda y filtros por layout, entidad, estado, origen y validación;
  permitir seleccionar una alternativa distinta por fila.
- Mejorar navegación por teclado, foco, contraste, escalado y lectura de estados
  para accesibilidad.
- Reemplazar el modelo libre por opciones admitidas o validación explícita;
  agregar prueba de conexión que no persista texto y presente coste/alcance de
  la operación por lote antes de enviarla.

**Criterio de salida:** las nuevas decisiones siguen siendo explícitas, toda
edición se valida y un lote grande puede entenderse y cancelarse de forma segura.

**Avance:** edición manual, diff y bloqueo técnico; progreso/cancelación;
búsqueda y filtros por layout/entidad/estado/origen/validación; selección de
alternativa por fila; navegación inicial por teclado; modelos admitidos, aviso
previo de alcance/coste del lote y prueba de conexión con texto fijo.

### Hito 6 — Memoria local y glosarios administrados

**Prioridad:** P2.

**Estado:** completado el 2026-09-03 y revalidado en 1.1.3.0/1.1.4.0. Memoria, activación,
exportación, borrado, ausencia de autoaplicación y prioridad de reglas locales
están en PASS. El glosario organizacional no aplica al equipo piloto actual.

- Implementar el contrato `ILearningStore` con esquema versionado por usuario.
- Registrar solo decisiones marcadas expresamente para recordar; nunca aplicar
  una preferencia sin mostrarla en la revisión.
- Permitir listar, buscar, desactivar, borrar y exportar recuerdos.
- Incorporar un glosario organizacional de solo lectura o administrado, con
  precedencia documentada frente al integrado y personal.
- Probar deduplicación, migraciones, corrupción recuperable, privacidad y
  ausencia de autoaplicación.

**Criterio de salida:** una preferencia aprobada reaparece como alternativa
explicable y puede revocarse completamente desde la configuración.

**Avance:** almacenamiento esquema 1 limitado a 500 recuerdos, registro opt-in
después de una escritura correcta, alternativa explicable sin autoaplicación,
búsqueda/activación/exportación/borrado y glosario organizacional de solo
lectura bajo `%PROGRAMDATA%`.

## 3. Evaluación posterior al piloto

Estas iniciativas no forman parte del compromiso inicial del segundo alcance.
Se priorizarán con evidencia de uso, volumen y riesgo del piloto:

1. atributos de bloque, incluidos atributos anidados y multirreferencia;
2. celdas de tablas y texto multilínea relacionado;
3. etiquetas nativas de Civil 3D, con una política específica para contenido
   derivado de estilos o propiedades dinámicas;
4. perfiles y glosarios compartidos por proyecto u organización;
5. soporte para otras versiones de Civil 3D mediante builds separados y matriz
   de compatibilidad;
6. Ribbon o paleta no modal, solo después de resolver ciclo de vida de documentos
   y operaciones asíncronas;
7. traducción u otras funciones generativas como productos separados de la
   corrección conservadora.

Cada iniciativa requiere diseño de lectura/escritura, protección técnica,
reversión, privacidad, pruebas y aceptación humana antes de incorporarse.

## 4. Indicadores y gobierno del piloto

- Cero modificaciones sin confirmación y cero escrituras después de cancelar.
- Cero alteraciones aceptadas de tokens protegidos en el dataset de regresión.
- Tasa de sesiones completadas, fallos por código y tiempo de revisión medidos
  sin almacenar texto del usuario.
- Defectos clasificados por severidad y resueltos antes de ampliar entidades.
- Revisión de coste de OpenAI, política de datos y custodia de credenciales por
  el responsable de la organización.
- Decisión explícita al final de cada hito: continuar, corregir o posponer; nunca
  ampliar alcance para ocultar defectos pendientes.

## 5. Compatibilidad e invariantes

- Civil 3D/AutoCAD 2024, .NET Framework 4.8 y x64 siguen siendo la plataforma
  comprometida hasta que exista una matriz nueva aprobada.
- `SpellEngine` y `Domain` permanecen independientes de Autodesk, WPF y red.
- No se mantiene una transacción abierta durante UI o solicitudes remotas.
- Toda propuesta, incluida edición manual o memoria, pasa por diff y validación
  local.
- El texto escrito es exactamente el mostrado y confirmado por el usuario.
- Una falla local o remota conserva el dibujo y, cuando sea posible, mantiene
  disponibles las reglas locales.
- Ningún secreto, texto de plano o dato técnico se incluye en logs o artefactos
  de soporte por defecto.
