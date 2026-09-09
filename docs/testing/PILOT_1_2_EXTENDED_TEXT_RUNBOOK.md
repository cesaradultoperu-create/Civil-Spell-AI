# Regresión del piloto CivilSpellAI 1.2.5.0

Fecha del candidato: 2026-09-09.

## Objetivo

Confirmar en Civil 3D 2024 x64 que `AISPELLALL` conserva el comportamiento de
1.1.5.0 y amplía la revisión a `MLeader`, celdas simples de `Table` y textos o
atributos de bloques locales sin perder aprobación humana, atomicidad ni `UNDO`.

## Evidencia automatizada

Ejecutar con Civil 3D cerrado, desde el repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Validate-Mvp.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-BatchUndoIntegration.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-ExtendedTextIntegration.ps1
```

Resultados requeridos:

- 127 pruebas correctas y cero fallos;
- `DICT-01` carga `es_PE` dentro de AutoCAD y restaura una tilde sin conexión;
- `INTEGRACION UNDO AISPELLALL CORRECTA`;
- `INTEGRACION DE TEXTOS EXTENDIDOS CORRECTA`.

## Primera configuración por usuario

Esta prueba usa otra computadora o un perfil de Windows desechable sin
configuración previa de CivilSpellAI. Nunca se borra ni se mueve la
configuración real para simular el primer inicio.

1. Ejecutar `AISPELLALL` y confirmar que aparece una sola vez la explicación de
   primera configuración antes de escanear el dibujo.
2. Elegir **No** y comprobar que continúa con el diccionario local, sin pedir
   clave ni conectarse a OpenAI.
3. Ejecutar `AISPELLALL` otra vez y confirmar que la explicación no se repite.
4. Abrir `AISPELLSETTINGS`: el estado debe indicar si `OPENAI_API_KEY` está
   disponible, pero el campo enmascarado debe aparecer siempre vacío.
5. Con una clave desechable y limitada, pegarla en el campo, pulsar **Guardar
   clave** y comprobar que el estado cambia a «detectada» sin mostrar el valor.
6. Autorizar el texto fijo y pulsar **Probar conexión**. Confirmar que el fallo
   de red o autenticación es seguro, o que la conexión correcta no guarda la
   respuesta.
7. Pulsar **Quitar clave**, confirmar que el estado cambia a «no detectada» y
   verificar que la revisión local continúa disponible.

No pegar una clave personal en capturas, registros, incidencias ni dibujos.

Desde la carpeta extraída del ZIP, generar el fixture empaquetado con:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\New-PilotFixture.ps1 -Force
```

`EXTENDED-01` crea destinos sintéticos en una copia temporal: un `MLeader`, dos
celdas ordinarias de la misma tabla, un `DBText` dentro de una definición de
bloque y un `AttributeReference`. Aplica los cinco cambios en un lote, ejecuta
una sola `U` y exige la restauración exacta. El fixture base no se modifica.

## Prueba visual en una copia del DWG

1. Cerrar Civil 3D, instalar o actualizar el bundle 1.2.5.0 y volver a abrirlo.
2. Abrir exclusivamente una copia de prueba del dibujo y ejecutar
   `AISPELLSETTINGS` para comprobar la versión cargada.
3. Ejecutar `AISPELLALL` y esperar a que termine el análisis.
4. Confirmar que las filas identifican los tipos `MLeader`, `TableCell`,
   `DBText`, `MText` o `AttributeReference` esperados.
5. Revisar cada propuesta. Desmarcar cualquier cambio dudoso y pulsar
   **Aplicar seleccionados**.
6. Sin hacer zoom, desplazarse, seleccionar ni ejecutar otro comando, usar una
   sola `U` inmediatamente y comprobar que todo el lote vuelve al estado
   anterior. Si la línea de comandos muestra `INTELLIPAN`, todavía se está
   deshaciendo un movimiento de vista y la prueba debe repetirse desde cero.
7. Repetir y aplicar solo las correcciones aprobadas; guardar únicamente la
   copia de prueba.
8. Reinstalar 1.2.5.0 para continuar la matriz con el candidato autorizado.

## Resultado del candidato 1.2.5.0

PASS el 2026-09-09 en Civil 3D 2024 sobre un dibujo desechable. El complemento
cargó sin `NETLOAD`; `AISPELLALL` revisó un MText y mostró una propuesta de
`Reglas locales`, dos cambios de concordancia, cero fallos y validación técnica
aprobada. Después de aplicar la fila seleccionada, una sola `U` inmediata
restauró exactamente el texto original. Las regresiones automatizadas
`DICT-01`, `UNDO-04` y `EXTENDED-01` también permanecen en PASS.

## Límites intencionales

- Los textos fijos de un bloque se corrigen una vez en su definición y el
  resultado aparece en todas las inserciones del bloque.
- Los atributos editables se revisan por cada `BlockReference`.
- Se omiten Xrefs y overlays, etiquetas nativas de Civil 3D, celdas combinadas
  que no sean su celda superior izquierda y celdas con fórmulas, campos,
  vínculos de datos, valores no textuales o contenido de bloque.
- El modo local solo corrige errores cubiertos por reglas seguras. OpenAI puede
  proponer más correcciones cuando está habilitado con consentimiento, pero
  ninguna corrección se aplica automáticamente ni se garantiza detectar todos
  los errores del idioma.

Un cambio no confirmado, una alteración de números/unidades/códigos, una
escritura parcial o un `UNDO` incompleto detiene la prueba y debe registrarse
como incidencia antes de usar el candidato en un DWG de trabajo.
