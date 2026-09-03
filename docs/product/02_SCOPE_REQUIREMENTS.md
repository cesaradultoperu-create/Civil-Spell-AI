# Alcance y requisitos de la primera versión con IA

> Registro del alcance original. Durante la validación se añadió revisión por
> lote y se difirieron edición manual, memoria y distribución. El cumplimiento
> actual se registra en `10_MVP_STATUS_AND_TEST_PLAN.md`.

## 1. Alcance comprometido

La primera versión con IA cubre la revisión manual de un texto seleccionado en
Civil 3D 2024. Su propósito es redactar mejor anotaciones técnicas en español,
inglés o mixtas, no diseñar ni recalcular elementos de ingeniería.

| Área | Decisión de alcance |
| --- | --- |
| Entidades | Un `DBText` o `MText` por revisión. |
| Interfaz | Ventana WPF modal integrada con el comando `AISPELL`. |
| Alternativas | Una de reglas y hasta tres de IA; siempre incluye conservar el original. |
| Aplicación | Solo después de una elección explícita y una validación local. |
| Idiomas | Español, inglés y texto mixto; detección mejorable, seleccionable por usuario. |
| Conocimiento técnico | Glosario integrado + archivo administrado + preferencias locales. |
| Aprendizaje | Memoria local, editable, por usuario y con confirmación. |
| IA | Proveedor remoto opcional, desacoplado y con respuesta estructurada. |
| Compatibilidad | .NET Framework 4.8, x64 y Civil 3D/AutoCAD 2024. |

## 2. Requisitos funcionales

### Revisión

- **RF-01.** `AISPELL` permitirá seleccionar exclusivamente un `DBText` o
  `MText`; cancelar la selección no altera el dibujo.
- **RF-02.** El sistema mostrará una ventana de revisión con el texto original,
  idioma detectado y estado del proveedor de IA.
- **RF-03.** El motor de reglas seguirá funcionando sin red y generará una
  propuesta cuando existan correcciones seguras.
- **RF-04.** La IA, cuando esté habilitada, devolverá entre una y tres propuestas
  estructuradas con texto y explicación; una falla de IA no bloquea la revisión.
- **RF-05.** El usuario podrá seleccionar una alternativa, mantener el original,
  editar el resultado o cancelar.
- **RF-06.** La interfaz mostrará un diff calculado localmente y advertencias de
  protección técnica antes de habilitar la aplicación.
- **RF-07.** El sistema actualizará solo el objeto inicialmente seleccionado y
  solo después de pulsar **Aplicar**.
- **RF-08.** Antes de escribir, el sistema verificará que el texto no haya
  cambiado desde el snapshot. Ante conflicto, no escribirá.
- **RF-09.** El usuario podrá marcar una decisión como recordable. El sistema la
  podrá sugerir en el futuro, pero no aplicarla automáticamente.
- **RF-10.** El usuario podrá consultar, editar, desactivar y borrar términos y
  recuerdos locales.

### Protección técnica y privacidad

- **RF-11.** Se protegerán números, unidades, estaciones, coordenadas, códigos,
  identificadores y términos del glosario. Una modificación no validable se
  bloquea.
- **RF-12.** Antes del primer uso remoto se mostrará el proveedor y el alcance de
  los datos enviados; la aceptación será explícita y revocable.
- **RF-13.** Las claves se guardarán protegidas para el usuario actual de Windows.
- **RF-14.** Los errores de red, autenticación, timeout y formato se comunicarán
  en la GUI sin revelar secretos ni alterar el dibujo.

## 3. Requisitos no funcionales

- Mantener la independencia de `SpellEngine` y de todo el dominio respecto a
  AutoCAD.
- No mantener una transacción o bloqueo de documento durante una llamada de red.
- Soportar cancelación de la petición y cerrar la ventana sin dejar operaciones
  pendientes que escriban en segundo plano.
- Responder la propuesta local de forma inmediata; la llamada remota debe tener
  timeout configurable y una indicación de progreso.
- Cubrir con pruebas unitarias reglas, glosario, diff, validador y memoria;
  cubrir con pruebas de integración el flujo de aplicar/cancelar/conflicto.
- Mantener registros técnicos sin texto de usuario por defecto.
- Distribuir una configuración reproducible de build, carga y actualización.

## 4. Fuera de alcance explícito

No se implementará en esta entrega:

- atributos de bloque, tablas, etiquetas de Civil 3D, referencias externas u
  otros tipos de entidad distintos de `DBText` y `MText`;
- modificación automática, incluso si la confianza parece alta;
- traducción completa, generación de memorias, cálculos de ingeniería o cambio
  de datos de diseño;
- fine-tuning, entrenamiento automático del LLM, memoria compartida en nube o
  sincronización entre usuarios;
- soporte de versiones de Civil 3D distintas de 2024;
- una paleta no modal, Ribbon, análisis de telemetría o portal web.

Estas capacidades se podrán planificar después de validar el flujo de revisión
individual. No deben añadirse a la primera versión por conveniencia.

### Ampliación aprobada durante la validación

El propietario amplió expresamente el alcance el 2026-08-25 para incluir la
revisión de todos los `DBText` y `MText` directos del dibujo abierto. La lista se
revisa antes de aplicar y el lote conserva las mismas garantías de validación,
conflicto y aprobación humana. Atributos, tablas y etiquetas nativas de Civil 3D
continúan fuera de alcance.

## 5. Decisiones de la conexión remota

Las decisiones técnicas del MVP quedaron resueltas de esta forma:

1. OpenAI es el proveedor remoto implementado mediante Responses API;
2. solo se envía `CorrectionRequest.Text`, con consentimiento versionado y
   `store: false`;
3. la clave se lee de `OPENAI_API_KEY` y no se persiste en la aplicación;
4. términos, unidades, estaciones, códigos y formatos MText se validan
   localmente;
5. el proveedor queda desactivado por defecto y las reglas locales permanecen
   disponibles.

Presupuesto, custodia organizacional de la clave, política corporativa de datos
y administración de un glosario compartido deben cerrarse antes de distribuir
el producto a un equipo. Un almacén seguro administrado se evaluará en el
segundo alcance; la variable de entorno es el mecanismo vigente del MVP.

## 6. Criterios de aceptación de la entrega

Esta lista conserva el objetivo original. La edición manual, la memoria de
preferencias y la distribución limpia quedaron diferidas. El estado real y los
criterios actuales para piloto están en `10_MVP_STATUS_AND_TEST_PLAN.md`.

La entrega se acepta cuando se demuestre que:

1. un `DBText` y un `MText` pueden revisarse, cancelarse y aplicarse desde
   `AISPELL`;
2. existe al menos una propuesta local y, con el proveedor simulado, varias
   propuestas que se pueden seleccionar;
3. el texto original permanece intacto hasta la confirmación y se puede deshacer
   con el mecanismo nativo de AutoCAD;
4. un cambio de número, unidad o término protegido queda bloqueado y explicado;
5. una caída de IA, una respuesta inválida y una cancelación no escriben en el
   dibujo;
6. una preferencia guardada vuelve a presentarse en una sesión posterior, sin
   autoaplicarse;
7. las pruebas automatizadas y la guía de carga para Civil 3D 2024 pasan en una
   instalación limpia.
